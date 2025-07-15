using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Hermes;
using Matchmaking;
using Mirror;
using Parties;
using Snapser.Handlers;
using Snapser.Model;
using UnityEngine;

namespace Snapser
{
    [RequireComponent(typeof(HermesWebsocketConnectionManager))]
    public class SnapserNetworkManager : NetworkManager
    {
        public new static SnapserNetworkManager singleton => NetworkManager.singleton as SnapserNetworkManager;

        [Header("Snapser Settings")] public string snapendUrl = "localhost";
        public string userId;
        public string username = "deviceId";
        public string sessionToken;

        public bool IsAuthenticated => sessionToken != null;
        public bool HermesConnected => hermesWebsocketConnection.IsConnected();

        public TitanTransport titanTransport;
        public HermesWebsocketConnectionManager hermesWebsocketConnection;

        private SnapserManager _snapserManager;


        // Game stuff
        [SerializeField, Required] Transform playerSpawnPoint, lightControllerSpawnPoint;
        [SerializeField, Required] private List<Color> spaceshipColors;
        private GameObject lightController;
        private List<GameObject> players = new List<GameObject>();

        public PartiesHandler PartiesHandler;
        public LobbiesHandler LobbiesHandler;
        public GameServerFleetsHandler GsfHandler;

        public override void Awake()
        {
            base.Awake();
            titanTransport = gameObject.GetComponent<TitanTransport>();
            hermesWebsocketConnection = gameObject.GetComponent<HermesWebsocketConnectionManager>();
            _snapserManager = new SnapserManager(snapendUrl);
        }

        public void Login(string username)
        {
            this.username = username;
            SnapserManager.Instance.AuthenticationAnonLogin(response =>
            {
                Debug.Log($"Login response: {response}");
                if (response.Success)
                {
                    var data = (AuthAnonLoginResponse)response.Data;
                    userId = data.User.Id;
                    sessionToken = data.User.SessionToken;
                    Debug.Log($"Login successful. UserId: {userId}, SessionToken: {sessionToken}");

                    ConnectToHermes();
                }
                else
                {
                    Debug.LogError($"Login failed. Error: {response.ResponseMessage}");
                }
            }, this.username);
        }

        public void ConnectToHermes()
        {
            if (sessionToken == null)
            {
                Debug.LogError("SessionToken is null. Please login first.");
                return;
            }

            UriBuilder uri = new UriBuilder(snapendUrl);
            if (uri.Scheme == "https")
            {
                uri.Scheme = "wss";
            }
            else
            {
                uri.Scheme = "ws";
            }

            uri.Path += "/v1/relay/ws";
            uri.Query += $"token={sessionToken}&username={username}";

            RegisterHandlers();
            hermesWebsocketConnection.Connect(uri.Uri.ToString());
        }

        private void RegisterHandlers()
        {
            PartiesHandler = new PartiesHandler(userId, username);
            LobbiesHandler = new LobbiesHandler(userId, username);
            GsfHandler = new GameServerFleetsHandler();

            hermesWebsocketConnection.OnConnectionClosed += OnConnectionClosed;
            hermesWebsocketConnection.OnConnectionError += () => { Debug.LogError("Connection Err"); };

            SnapEventMatchmakingHandler.Instance.OnMatchmakingQueued += OnMatchmakingQueued;
            SnapEventMatchmakingHandler.Instance.OnMatchmakingDeQueued += OnMatchmakingDeQueued;
            SnapEventMatchmakingHandler.Instance.OnMatchmakingNoRulesApply += OnMatchmakingNoRulesApply;
            SnapEventMatchmakingHandler.Instance.OnMatchmakingMatchFound += OnMatchmakingMatchFound;
            SnapEventMatchmakingHandler.Instance.OnMatchmakingMatchCreated += OnMatchmakingMatchCreated;

            SnapEventPartyHandler.Instance.OnPartyJoined += PartiesHandler.OnPartyJoined;
            SnapEventPartyHandler.Instance.OnPartyLeft += PartiesHandler.OnPartyLeft;
            SnapEventPartyHandler.Instance.OnPartyDeleted += PartiesHandler.OnPartyDeleted;
            SnapEventPartyHandler.Instance.OnPlayerMetadataUpdated += PartiesHandler.OnPartyPlayerMetadataUpdated;

            SnapEventLobbiesHandler.Instance.OnLobbiesMemberJoined += LobbiesHandler.OnMemberJoined;
            SnapEventLobbiesHandler.Instance.OnLobbiesMemberLeft += LobbiesHandler.OnMemberLeft;
            SnapEventLobbiesHandler.Instance.OnLobbiesLobbyDisbanded += LobbiesHandler.OnLobbyDisbanded;
            SnapEventLobbiesHandler.Instance.OnLobbiesMemberMetadataUpdated += LobbiesHandler.OnMemberMetadataUpdated;
            SnapEventLobbiesHandler.Instance.OnLobbiesMatchStarted += LobbiesHandler.OnLobbyMatchStarted;

            SnapEventGsfHandler.Instance.OnGameServerStateUpdated += GsfHandler.OnGameServerStateUpdated;
        }

        public void StartRelayHost(string matchId, string userId, string username, string address, string joinCode)
        {
            titanTransport.ConfigureRelay(matchId, userId, username, address, joinCode, true);
            StartHost();
        }

        public void StartRelayClient(string matchId, string userId, string username, string address, string joinCode)
        {
            titanTransport.ConfigureRelay(matchId, userId, username, address, joinCode, false);
            StartClient();
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            var matchPlayerCount = titanTransport.GetMatchPlayerCount();

            var playerToBeCreatedCount = numPlayers + 1;
            var position = playerSpawnPoint.position;
            var startPosition = new Vector3(position.x, position.y - (playerToBeCreatedCount - 1) * 8, position.z);

            var player = Instantiate(playerPrefab, startPosition, playerSpawnPoint.rotation);

            players.Add(player);

            NetworkServer.AddPlayerForConnection(conn, player);

            Debug.Log("Player " + numPlayers + " of " + matchPlayerCount + " created at location " +
                      player.transform.position);

            //When all players have been instantiated, instantiated the game light controller for all clients
            if (numPlayers != matchPlayerCount) return;

            Debug.Log("Creating the game light after " + numPlayers + " of " + matchPlayerCount +
                      " joined and were created.");

            lightController = Instantiate(spawnPrefabs.Find(prefab => prefab.name == "GameLightController"));

            var lightControllerPosition = lightController.transform.position;
            lightController.transform.position = new Vector3(lightControllerSpawnPoint.position.x,
                lightControllerPosition.y, lightControllerPosition.z);

            NetworkServer.Spawn(lightController);
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            //Destroy the game light controller when server disconnects
            if (lightController != null)
                NetworkServer.Destroy(lightController);

            //Call base functionality (actually destroys the player)
            base.OnServerDisconnect(conn);
        }

        public void OnConnectionClosed()
        {
            Debug.Log("Hermes closed");
        }



        public void QueueMatchmaking()
        {
            Debug.Log("Queueing into Matchmaking");
            var createTicketReq = new CreateTicketRequest
            {
                UserId = userId,
                Metadata = { { "game-mode", "interstellar" } },
                SearchFields = new SearchFields { Tags = { "interstellar", "snap_titan_geo_oregon" } }
            };

            Debug.Log($"Sending matchmaking request: {createTicketReq}");

            SnapEventMatchmakingHandler.Instance.OnMatchmakingMatchCreateError += OnMatchmakingCreateMatchError;

            var messageId = Guid.NewGuid().ToString();
            SnapApiProxyHandler.Instance.SendSnapApiProxyMessage("/matchmaking.MatchmakingService/CreateTicket",
                createTicketReq.ToByteString(), messageId);
        }

        private void OnMatchmakingCreateMatchError(object s, OnMatchmakingMatchCreateErrorArgs ev)
        {
            SnapEventMatchmakingHandler.Instance.OnMatchmakingMatchCreateError -= OnMatchmakingCreateMatchError;
            Debug.LogError($"Matchmaking Match Create Error: {ev.Error}");
        }

        public void OnMatchmakingMatchFound(object s, OnMatchmakingMatchFoundEventArgs ev)
        {
            Debug.Log($"Match found. Accepting: {ev.MatchId} - {ev.TicketId} in 2 seconds ...");

            System.Threading.Thread.Sleep(2000);

            var matchAcceptReq = new Matchmaking.AcceptMatchRequest
            {
                UserId = userId,
                Accept = true,
                MatchId = ev.MatchId
            };

            SnapApiProxyHandler.Instance.SendSnapApiProxyMessage(
                "/matchmaking.MatchmakingService/AcceptMatch",
                matchAcceptReq.ToByteString(),
                Guid.NewGuid().ToString());
        }

        public void OnMatchmakingMatchCreated(object s, OnMatchmakingMatchCreatedEventArgs ev)
        {
            Debug.Log($"Match Created.  Starting relay: {ev.ConnectionString} {ev.JoinCode} {ev.IsHost}");
            // ev.JoinCode = "trash";
            if (ev.IsHost)
            {
                StartRelayHost(ev.MatchId, userId, username, ev.ConnectionString, ev.JoinCode);
            }
            else
            {
                StartRelayClient(ev.MatchId, userId, username, ev.ConnectionString, ev.JoinCode);
            }
        }

        public void OnMatchmakingQueued(object s, OnMatchmakingQueuedEventArgs ev)
        {
            Debug.Log($"Snapser: OnMatchmakingQueued at {ev.QueuedAt}");
        }

        public void OnMatchmakingDeQueued(object s, OnMatchmakingDeQueuedEventArgs ev)
        {
            Debug.Log($"Snapser: OnMatchmakingDeQueued {ev.Reason}");
        }

        public void OnMatchmakingNoRulesApply(object s, OnMatchmakingNoRulesApplyEventArgs ev)
        {
            Debug.Log($"Snapser: OnMatchmakingNoRulesApply {ev.Reason}");
        }

        public Color GetSpaceshipColor(GameObject player)
        {
            Debug.Log($"have colors: {spaceshipColors.Count}");
            if (players.Contains(player))
            {
                var playerIndex = players.IndexOf(player) % spaceshipColors.Count;
                return spaceshipColors[playerIndex];
            }

            Debug.LogWarning("Game object not found in the player game object lists ");
            return UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
        }
    }
}