using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Mirror;
using Snapser.Handlers;
using UnityEngine;

namespace Snapser
{
    [RequireComponent(typeof(TitanWebsocketConnectionManager)), RequireComponent(typeof(NetworkManager))]
    public class TitanTransport : Transport
    {
        private TitanWebsocketConnectionManager _titanConnectionManager;
        private NetworkManager _networkManager;

        private bool _isConnected;
        private bool _isClient;
        private bool _isServer;

        // Inverse maps for quick lookups
        private Dictionary<int, string> _playerConnectionIds = new Dictionary<int, string>();
        private Dictionary<string, int> _playerUserIds = new Dictionary<string, int>();

        private string _userId;
        private string _username;
        private int _connectionId;

        private string _relayServerAddress;
        private string _relayServerJoinCode;
        private bool _isHost;
        private string _matchId;

        private void Awake()
        {
            _networkManager = gameObject.GetComponent<NetworkManager>();
            _titanConnectionManager = gameObject.GetComponent<TitanWebsocketConnectionManager>();
            RegisterTitanHandlers();
        }

        public void ConfigureRelay(string matchId, string userId, string username, string address, string joinCode, bool isHost)
        {
            _matchId = matchId;
            _userId = userId;
            _username = username;
            _connectionId = CreateConnectionId(userId);
            _relayServerJoinCode = joinCode;
            _isHost = isHost;

            var uriB = new UriBuilder(address)
            {
                Query = $"joincode={joinCode}&username={username}"
            };
            _relayServerAddress = uriB.Uri.ToString();
        }

        // Websockets avail on all platforms
        public override bool Available() => true;

        public override bool ClientConnected() => _titanConnectionManager.IsConnected();

        public override void ClientConnect(string address)
        {
            if (!Available())
            {
                Debug.LogError("TitanTransport not available");
                return;
            }

            if (_isClient || _isServer)
            {
                Debug.LogWarning("Already connected");
                return;
            }

            _titanConnectionManager.OnConnectionOpened += () =>
            {
                _isClient = true;
                _isConnected = true;
                
                
            };

            RegisterTitanHandlers();
            _titanConnectionManager.Connect(_relayServerAddress);
        }

        public override void ClientSend(ArraySegment<byte> segment, int channelId = Channels.Reliable)
        {
            TitanMessageHandler.Instance.SendRelayData(_matchId, null, channelId, segment);
            OnClientDataSent?.Invoke(segment, channelId);
        }

        public override void ClientDisconnect()
        {
            Debug.Log("ClientDisconnect");

            _titanConnectionManager.Disconnect();

            _isClient = false;
            _isConnected = false;

            OnClientDisconnected?.Invoke();
        }

        public override Uri ServerUri() => new(_relayServerAddress);

        public override bool ServerActive() => _isServer && _titanConnectionManager.IsConnected();

        public override void ServerStart()
        {
            if (!Available())
            {
                Debug.LogError("TitanTransport not available");
                return;
            }

            if (_isClient || _isServer)
            {
                Debug.LogWarning("Already connected");
                return;
            }

            _titanConnectionManager.OnConnectionOpened += () =>
            {
                _isServer = true;
                _isConnected = true;

                TitanMessageHandler.Instance.SendMatchHostReady(_matchId, null);
            };

            _titanConnectionManager.Connect(_relayServerAddress);
        }

        public override void ServerSend(int connectionId, ArraySegment<byte> segment, int channelId = Channels.Reliable)
        {
            var userId = _playerConnectionIds[connectionId];
            if (userId == null)
            {
                Debug.LogError($"No user found for connectionId: {connectionId}");
                return;
            }

            var recipients = new[] { userId };

            TitanMessageHandler.Instance.SendRelayData(_matchId, recipients, channelId, segment);
            OnServerDataSent?.Invoke(connectionId, segment, channelId);
        }

        public override void ServerDisconnect(int connectionId)
        {
            Debug.Log("ServerDisconnect");
            if (!_isConnected)
            {
                _isServer = false;
                return;
            }

            _titanConnectionManager.OnConnectionClosed += () =>
            {
                _isServer = false;
                _isConnected = false;

                OnServerDisconnected?.Invoke(connectionId);
            };

            _titanConnectionManager.Disconnect();
            UnregisterTitanHandlers();
        }

        public override string ServerGetClientAddress(int connectionId) => _relayServerAddress;

        public override void ServerStop()
        {
            ServerDisconnect(0);
        }

        public override int GetMaxPacketSize(int channelId = Channels.Reliable)
        {
            return 1024;
        }

        public override void Shutdown()
        {
            _titanConnectionManager.Disconnect();
            _isClient = false;
            _isServer = false;
            _isConnected = false;

            _playerConnectionIds.Clear();
            _playerUserIds.Clear();
        }

        #region Snapser

        public bool IsServer()
        {
            return _isServer;
        }

        public bool IsClient()
        {
            return _isClient;
        }

        public void RegisterTitanHandlers()
        {
            TitanMessageHandler.Instance.OnMatchReady += OnMatchReady;
            TitanMessageHandler.Instance.OnRelayData += OnRelayData;
            
            TitanMessageHandler.Instance.OnMatchJoined += OnMatchJoined;
            TitanMessageHandler.Instance.OnMatchLeft += OnMatchLeft;
            TitanMessageHandler.Instance.OnMatchOver += OnMatchOver;
        }

        public void UnregisterTitanHandlers()
        {
            TitanMessageHandler.Instance.OnRelayData -= OnRelayData;
        }

        public void OnMatchJoined(object sender, OnMatchJoinedArgs args)
        {
            Debug.Log($"OnMatchJoined: {args.PlayerJoinedId}");
            foreach (var player in args.MatchPlayers)
            {
                Debug.Log($"... match player: {player.UserId}");
            }
        }

        public void OnMatchLeft(object sender, OnMatchLeftArgs args)
        {
            Debug.Log($"OnMatchLeft: {args.PlayerLeftId}");
            foreach (var player in args.MatchPlayers)
            {
                Debug.Log($"... match player: {player.UserId}");
            } 
        }
        
        public void OnMatchOver(object sender, OnMatchOverArgs args)
        {
            Debug.Log($"OnMatchOver: {args.ReasonForOver}");
        }

        public void OnMatchReady(object sender, OnMatchReadyArgs args)
        {
            Debug.Log("OnMatchReady");

            foreach (var mp in args.MatchPlayers)
            {
                var connId = CreateConnectionId(mp.UserId);
                
                Debug.Log($"adding connId: {connId} for userId: {mp.UserId}");
                
                _playerConnectionIds.Add(connId, mp.UserId);
                _playerUserIds.Add(mp.UserId, connId);

                if (_isServer)
                {
                    OnServerConnected?.Invoke(connId);
                }
            }

            if (_isClient)
            {
                OnClientConnected?.Invoke();
            }
        }

        public void OnRelayData(object sender, OnRelayDataArgs ev)
        {
            if (_isServer)
            {
                var connId = _playerUserIds[ev.Sender];
                OnServerDataReceived?.Invoke(connId, (ArraySegment<byte>)ev.Data, ev.Channel);
            }
            else if (_isClient)
            {
                OnClientDataReceived?.Invoke((ArraySegment<byte>)ev.Data, ev.Channel);
            }
            else
            {
                Debug.LogError("Not connected or client/server");
            }
        }

        public int GetMatchPlayerCount()
        {
            return 2; //_playerUserIds.Count;
        }

        private static int CreateConnectionId(string userId)
        {
            var hasher = MD5.Create();
            var hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(userId));
            return BitConverter.ToInt32(hash, 0);
        }

        #endregion
    }
}