using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.MultiplayerModels;

public class PlayFabCode : MonoBehaviour
{
    public InputField Username, Email, Password;
    public Text ErrorMessage;
    public string SceneName = "";

    public void RegisterClick()
    {
        var register = new RegisterPlayFabUserRequest { Username = Username.text, Email = Email.text, Password = Password.text };
        PlayFabClientAPI.RegisterPlayFabUser(register, OnRegisterSuccess, OnRegisterFailure);
    }

    private void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        ErrorMessage.text = "";
        SceneManager.LoadScene(SceneName); // Load Main Scene
    }

    private void OnRegisterFailure(PlayFabError error)
    {
        if (error.ErrorDetails != null && error.ErrorDetails.Count > 0)
        {
            using (var iter = error.ErrorDetails.Keys.GetEnumerator())
            {
                iter.MoveNext();
                string key = iter.Current;
                ErrorMessage.text = error.ErrorDetails[key][0];
            }
        }
        else
        {
            ErrorMessage.text = error.ErrorMessage;
        }
    }

    public void LoginClick()
    {
        var login = new LoginWithPlayFabRequest { Username = Username.text, Password = Password.text };
        PlayFabClientAPI.LoginWithPlayFab(login, OnLoginSuccess, OnLoginFailure);
    }

    private void OnLoginSuccess(LoginResult result)
    {
        ErrorMessage.text = "";
        //SceneManager.LoadScene(SceneName); // Load Main Scene
        RequestMatchmaking(); // MATCHMAKING-ONLY
        SetDisplayNameForUser( Username.text );
    }

    private void OnLoginFailure(PlayFabError error)
    {
        if (error.ErrorDetails != null && error.ErrorDetails.Count > 0)
        {
            using (var iter = error.ErrorDetails.Keys.GetEnumerator())
            {
                iter.MoveNext();
                string key = iter.Current;
                ErrorMessage.text = error.ErrorDetails[key][0];
            }
        }
        else
        {
            ErrorMessage.text = error.ErrorMessage;
        }
    }


    // FOR LEADERBOARD
    private void SetDisplayNameForUser( string name )
    {
        UpdateUserTitleDisplayNameRequest requestData = new UpdateUserTitleDisplayNameRequest() {
            DisplayName = name
        };
        PlayFabClientAPI.UpdateUserTitleDisplayName( requestData, OnSetDisplayNameForUserResult, OnSetDisplayNameForUserError );
    }

    private void OnSetDisplayNameForUserResult( UpdateUserTitleDisplayNameResult response )
	{

	}

    private void OnSetDisplayNameForUserError( PlayFabError error )
    {
        Debug.Log( error.ErrorMessage );
    }


    // FOR MATCHMAKING

    private string matchmakingTicket = "";
    private float ticketTimer = 0;
    private void RequestMatchmaking()
    {
        CreateMatchmakingTicketRequest requestData = new CreateMatchmakingTicketRequest();
        requestData.Creator = new MatchmakingPlayer {
            Entity = new PlayFab.MultiplayerModels.EntityKey {
                Id = PlayFabSettings.staticPlayer.EntityId,
                Type = PlayFabSettings.staticPlayer.EntityType,
            },
            Attributes = new MatchmakingPlayerAttributes {
                DataObject = new {
                    latencies = new object[] {
                        new {
                            region = "EastUs",
                            latency = 100,
                        },
                    },
                },
            },
        };
        requestData.QueueName = "TestQueue"; // Matchmaking Queue Name
        requestData.GiveUpAfterSeconds = 120; // 120 seconds
        PlayFabMultiplayerAPI.CreateMatchmakingTicket( requestData, OnCreateMatchmakingTicketResult, OnCreateMatchmakingTicketError );
    }

    private void OnCreateMatchmakingTicketResult( CreateMatchmakingTicketResult response )
    {
        CheckMatchmakingTicket( response.TicketId );
    }

    private void OnCreateMatchmakingTicketError( PlayFabError error )
    {
        Debug.Log( error.ErrorMessage );
    }

    private void CheckMatchmakingTicket( string ticketId )
    {
        Debug.Log( "Checking ticket " + ticketId );
        matchmakingTicket = ticketId;
        GetMatchmakingTicketRequest requestData = new GetMatchmakingTicketRequest();
        requestData.QueueName = "TestQueue"; // Matchmaking Queue Name
        requestData.TicketId = ticketId;
        PlayFabMultiplayerAPI.GetMatchmakingTicket( requestData, OnCheckMatchmakingTicketResult, OnCheckMatchmakingTicketError );
    }

    private void OnCheckMatchmakingTicketResult( GetMatchmakingTicketResult response )
    {
        bool queueTicketCheck = false;
        switch( response.Status )
        {
            case "Matched":
                ErrorMessage.text = "Found Match!";
                Debug.Log( "Found Match " + response.MatchId );
                matchmakingTicket = "";
                JoinMatch( response.MatchId );
                break;
            case "WaitingForMatch":
                ErrorMessage.text = "Waiting for match";
                Debug.Log( "Waiting for match..." );
                queueTicketCheck = true;
                break;
            case "WaitingForPlayers":
                ErrorMessage.text = "Waiting for players";
                Debug.Log( "Waiting for players..." );
                queueTicketCheck = true;
                break;
            case "WaitingForServer":
                ErrorMessage.text = "Waiting for server";
                Debug.Log( "Waiting for server..." );
                queueTicketCheck = true;
                break;
            case "Canceled":
                ErrorMessage.text = "Canceled";
                Debug.Log( "Canceled..." );
                matchmakingTicket = "";
                break;
            default:
                break;
        }

        if( queueTicketCheck )
        {
            ticketTimer = 6.0f;
        }
    }

    private void OnCheckMatchmakingTicketError( PlayFabError error )
    {
        Debug.Log( error.ErrorMessage );
    }

    private void JoinMatch( string matchId )
    {
        Debug.Log( "Joining Match..." );
        GetMatchRequest requestData = new GetMatchRequest();
        requestData.QueueName = "TestQueue"; // Matchmaking Queue Name
        requestData.MatchId = matchId;
        PlayFabMultiplayerAPI.GetMatch( requestData, OnGetMatchResult, OnGetMatchError );
    }

    private void OnGetMatchResult( GetMatchResult response )
    {
        Client.matchAddress = response.ServerDetails.IPV4Address;
        Client.matchPort = (ushort)response.ServerDetails.Ports[ 0 ].Num;
		SceneManager.LoadScene( SceneName ); // Load Main Scene

		//connectToServer( response.ServerDetails.IPV4Address, (ushort)response.ServerDetails.Ports[ 0 ].Num );
	}

    private void OnGetMatchError( PlayFabError error )
    {
        Debug.Log( error.ErrorMessage );
    }

    // Start is called before the first frame update
    void Start()
    {
        ErrorMessage.text = "";
    }

    // Update is called once per frame
    void Update()
    {
        // Update matchmaking ticket check
        if( ticketTimer > 0 && matchmakingTicket != "" )
        {
            ticketTimer -= Time.deltaTime;
            if( ticketTimer <= 0.0f )
            {
                CheckMatchmakingTicket( matchmakingTicket );
            }
        }
    }
}
