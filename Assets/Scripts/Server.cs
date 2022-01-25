using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PlayFab;
using PlayFab.MultiplayerAgent.Model;
using Unity.Collections;
using Unity.Networking.Transport;

public class Server : MonoBehaviour
{
    private List<ConnectedPlayer> players;
    public bool RunLocal;

    public NetworkDriver networkDriver;
    private NativeList<NetworkConnection> connections;
    const int numEnemies = 12; // Total number of enemies
    private byte[] enemyStatus;
    private int numPlayers = 0;

    IEnumerator ReadyForPlayers()
    {
        yield return new WaitForSeconds(.5f);
        PlayFabMultiplayerAgentAPI.ReadyForPlayers();
    }

    private void OnServerActive()
    {
        StartServer();
    }

    private void OnAgentError(string error)
    {
        Debug.Log(error);
    }

    private void OnShutdown()
    {
        Debug.Log("Server is shutting down");
        networkDriver.Dispose();
        connections.Dispose();
        StartCoroutine(Shutdown());
    }

    IEnumerator Shutdown()
    {
        yield return new WaitForSeconds(5f);
        Application.Quit();
    }

    private void OnMaintenance(DateTime? NextScheduledMaintenanceUtc)
    {
        Debug.LogFormat("Maintenance scheduled for: {0}", NextScheduledMaintenanceUtc.Value.ToLongDateString());
    }

    void StartPlayFabAPI()
    {
        players = new List<ConnectedPlayer>();
        PlayFabMultiplayerAgentAPI.Start();
        PlayFabMultiplayerAgentAPI.OnMaintenanceCallback += OnMaintenance;
        PlayFabMultiplayerAgentAPI.OnShutDownCallback += OnShutdown;
        PlayFabMultiplayerAgentAPI.OnServerActiveCallback += OnServerActive;
        PlayFabMultiplayerAgentAPI.OnAgentErrorCallback += OnAgentError;

        StartCoroutine( ReadyForPlayers() );
    }

    void StartServer()
	{
        Debug.Log( "Starting Server" );

        // Start transport server
        networkDriver = NetworkDriver.Create();
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = 7777;
        var connectionInfo = PlayFabMultiplayerAgentAPI.GetGameServerConnectionInfo();
        if( connectionInfo != null )
        {
            // Set the server to the first available port
            foreach( var port in connectionInfo.GamePortsConfiguration )
            {
                endpoint.Port = (ushort)port.ServerListeningPort;
                break;
            }
        }
        if( networkDriver.Bind( endpoint ) != 0 )
        {
            Debug.Log( "Failed to bind to port " + endpoint.Port );
        }
        else
        {
            networkDriver.Listen();
        }

        connections = new NativeList<NetworkConnection>( 16, Allocator.Persistent );

        enemyStatus = new byte[ numEnemies ];
        for( int i = 0; i < numEnemies; i++ )
        {
            enemyStatus[ i ] = 1;
        }
        Debug.Log( "Server Started From Agent Activation" );
    }

    void Start()
    {
        if( RunLocal )
		{
            StartServer(); // Run the server locally
		}
        else
		{
            StartPlayFabAPI();
        }
    }

	void OnDestroy()
	{
        networkDriver.Dispose();
        connections.Dispose();
	}

    // Update is called once per frame
    void Update()
    {
        networkDriver.ScheduleUpdate().Complete();

        // Clean up connections
        for( int i = 0; i < connections.Length; i++ )
        {
            if( !connections[ i ].IsCreated )
            {
                connections.RemoveAtSwapBack( i );
                --i;
            }
        }

        // Accept new connections
        NetworkConnection c;
        while( ( c = networkDriver.Accept() ) != default( NetworkConnection ) )
        {
            connections.Add( c );
            Debug.Log( "Accepted a connection" );
            numPlayers++;
        }

        DataStreamReader stream;
        for( int i = 0; i < connections.Length; i++ )
        {
            if( !connections[ i ].IsCreated )
			{
                continue;
			}
            NetworkEvent.Type cmd;
            while( ( cmd = networkDriver.PopEventForConnection( connections[ i ], out stream ) ) != NetworkEvent.Type.Empty )
            {
                if( cmd == NetworkEvent.Type.Data )
                {
                    uint number = stream.ReadUInt();
                    if( number == numEnemies ) // Check that the number of enemies match
					{
                        for( int b = 0; b < numEnemies; b++ )
						{
                            byte isAlive = stream.ReadByte();
                            if( isAlive == 0 && enemyStatus[ b ] > 0 )
							{
                                Debug.Log( "Enemy " + b + " destroyed by Player " + i );
                                enemyStatus[ b ] = 0;
							}
						}
                    }
                }
                else if( cmd == NetworkEvent.Type.Disconnect )
                {
                    Debug.Log( "Client disconnected from server" );
                    connections[ i ] = default( NetworkConnection );
                    numPlayers--;
                    if( numPlayers == 0 )
                    {
                        // All players are gone, shutdown
                        OnShutdown();
                    }
                }
            }

            // Broadcast Game State
            networkDriver.BeginSend( NetworkPipeline.Null, connections[ i ], out var writer );
            writer.WriteUInt( numEnemies );
            for( int b = 0; b < numEnemies; b++ )
            {
                writer.WriteByte( enemyStatus[ b ] );
            }
            networkDriver.EndSend( writer );
        }
	}
}
