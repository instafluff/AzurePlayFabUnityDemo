using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PlayFab;
using PlayFab.MultiplayerAgent.Model;

public class Server : MonoBehaviour
{
    private List<ConnectedPlayer> players;
    public bool Debugging = true;

    IEnumerator ReadyForPlayers()
    {
        yield return new WaitForSeconds(.5f);
        PlayFabMultiplayerAgentAPI.ReadyForPlayers();
    }

    private void OnServerActive()
    {
        Debug.Log("Server Started From Agent Activation");
    }

    private void OnPlayerRemoved(string playfabId)
    {
        ConnectedPlayer player = players.Find(x => x.PlayerId.Equals(playfabId, StringComparison.OrdinalIgnoreCase));
        players.Remove(player);
        PlayFabMultiplayerAgentAPI.UpdateConnectedPlayers(players);
    }

    private void OnPlayerAdded(string playfabId)
    {
        players.Add(new ConnectedPlayer(playfabId));
        PlayFabMultiplayerAgentAPI.UpdateConnectedPlayers(players);
    }

    private void OnAgentError(string error)
    {
        Debug.Log(error);
    }

    private void OnShutdown()
    {
        Debug.Log("Server is shutting down");
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

    // Start is called before the first frame update
    void Start()
    {
        players = new List<ConnectedPlayer>();
        PlayFabMultiplayerAgentAPI.Start();
        PlayFabMultiplayerAgentAPI.IsDebugging = Debugging;
        PlayFabMultiplayerAgentAPI.OnMaintenanceCallback += OnMaintenance;
        PlayFabMultiplayerAgentAPI.OnShutDownCallback += OnShutdown;
        PlayFabMultiplayerAgentAPI.OnServerActiveCallback += OnServerActive;
        PlayFabMultiplayerAgentAPI.OnAgentErrorCallback += OnAgentError;

        StartCoroutine(ReadyForPlayers());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
