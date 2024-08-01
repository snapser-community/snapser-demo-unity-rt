using System;
using System.Collections.Generic;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Hermes;
using Lobbies;
using Snapser.Handlers;
using Snapser.UI;
using UI;
using UnityEngine;

namespace Snapser
{
    public class LobbiesHandler
    {
        private readonly Dictionary<string, List<string>> _snapApiMessageIds = new Dictionary<string, List<string>>();

        private readonly string _userId;
        private readonly string _username;

        public Lobby CurrentLobby;

        public LobbiesHandler(string userId, string username)
        {
            _userId = userId;
            _username = username;
        }

        public void CreateLobby(string name)
        {
            var createLobbyReq = new CreateLobbyRequest
            {
                Name = name,
                Description = "This is my lobby... ",
                MaxMembers = 4,
                Private = false,
                PlacementSettings = new PlacementSettings
                {
                    NumPlacements = 2,
                    PlacementStrategy = PlacementStrategy.PlacementBalanced,
                    AllowMemberPlacementUpdates = false
                },
                SearchMetadata = new Struct
                {
                    Fields =
                    {
                        ["gamemode"] = Value.ForString("interstellar")
                    }
                },
                OwnerMetadata = new Struct
                {
                    Fields =
                    {
                        ["username"] = Value.ForString(_username)
                    }
                }
            };

            var messageId = Guid.NewGuid().ToString();
            var method = "/lobbies.LobbiesService/CreateLobby";

            SnapApiProxyHandler.Instance.OnSnapProxyResponse += (_, e) =>
            {
                if (e.MessageId != messageId) return;

                if (e.IsError)
                {
                    HandleSnapApiError(e);
                    return;
                }

                Debug.Log("Got create lobbies response");

                var createLobbyResponse = CreateLobbyResponse.Parser.ParseFrom(e.Payload);
                Debug.Log($"Lobby created: {createLobbyResponse.Lobby.Id}");

                CurrentLobby = createLobbyResponse.Lobby;
            };

            SnapApiProxyHandler.Instance.SendSnapApiProxyMessage(method, createLobbyReq.ToByteString(), messageId);
        }

        public void ListLobbies(string gamemode)
        {
            var searchLobbiesReq = new ListLobbiesRequest
            {
                MetadataFilters =
                {
                    new[]
                    {
                        new MetadataFilter
                        {
                            Key = "gamemode",
                            Op = "=",
                            Value = Value.ForString(gamemode)
                        }
                    }
                }
            };
            
            var messageId = Guid.NewGuid().ToString();
            var method = "/lobbies.LobbiesService/ListLobbies";
            
            SnapApiProxyHandler.Instance.OnSnapProxyResponse += (_, e) =>
            {
                if (e.MessageId != messageId) return;

                if (e.IsError)
                {
                    HandleSnapApiError(e);
                    return;
                }

                Debug.Log("Got list lobbies response");

                var listLobbiesResponse = ListLobbiesResponse.Parser.ParseFrom(e.Payload);
                Debug.Log($"Lobbies found: {listLobbiesResponse.Lobbies.Count}");
                
                var listLobbiesUI = GameObject.Find("LobbiesList").GetComponent<ListLobbiesUI>();
                listLobbiesUI.ClearLobbyEntries();
                
                foreach (var lobby in listLobbiesResponse.Lobbies)
                {
                    Debug.Log($"Lobby: {lobby.Id}");
                    listLobbiesUI.AddLobbyEntry(lobby, l =>
                    {
                        JoinLobby(l.Id);
                    });
                }
            };
            
            SnapApiProxyHandler.Instance.SendSnapApiProxyMessage(method, searchLobbiesReq.ToByteString(), messageId);
        }

        public void JoinLobby(string lobbyId)
        {
            var joinLobbyReq = new JoinLobbyRequest
            {
                LobbyId = lobbyId,
                UserId = _userId,
                Metadata = new Struct
                {
                    Fields =
                    {
                        ["username"] = Value.ForString(_username)
                    }
                } 
            };
            
            var messageId = Guid.NewGuid().ToString();
            var method = "/lobbies.LobbiesService/JoinLobby";
            
            SnapApiProxyHandler.Instance.OnSnapProxyResponse += (_, e) =>
            {
                if (e.MessageId != messageId) return;

                if (e.IsError)
                {
                    HandleSnapApiError(e);
                    return;
                }

                Debug.Log("Got join lobby response");

                var joinLobbyResponse = JoinLobbyResponse.Parser.ParseFrom(e.Payload);
                Debug.Log($"Joined lobby: {joinLobbyResponse.Lobby}");

                CurrentLobby = joinLobbyResponse.Lobby;
            };
            
            SnapApiProxyHandler.Instance.SendSnapApiProxyMessage(method, joinLobbyReq.ToByteString(), messageId);
        }
        
        public void LeaveLobby()
        {
            if (CurrentLobby == null)
            {
                Debug.LogWarning("No lobby to leave. Try Joining a Lobby.");
                return;
            }
            
            var leaveLobbyReq = new LeaveLobbyRequest
            {
                LobbyId = CurrentLobby.Id,
                UserId = _userId
            };
            
            var messageId = Guid.NewGuid().ToString();
            var method = "/lobbies.LobbiesService/LeaveLobby";
            
            SnapApiProxyHandler.Instance.OnSnapProxyResponse += (_, e) =>
            {
                if (e.MessageId != messageId) return;

                if (e.IsError)
                {
                    HandleSnapApiError(e);
                    return;
                }

                Debug.Log($"Left lobby");
                CurrentLobby = null;
            };
            
            SnapApiProxyHandler.Instance.SendSnapApiProxyMessage(method, leaveLobbyReq.ToByteString(), messageId);
        }

        public void DeleteLobby()
        {
            if (CurrentLobby == null)
            {
                Debug.LogWarning("No lobby to delete. Try Joining a Lobby.");
                return;
            }
            
            var deleteLobbyReq = new DeleteLobbyRequest
            {
                Id = CurrentLobby.Id,
                Owner = _userId
            };
            
            var messageId = Guid.NewGuid().ToString();
            var method = "/lobbies.LobbiesService/DeleteLobby";
            
            SnapApiProxyHandler.Instance.OnSnapProxyResponse += (_, e) =>
            {
                if (e.MessageId != messageId) return;

                if (e.IsError)
                {
                    HandleSnapApiError(e);
                    return;
                }

                Debug.Log($"Deleted lobby");
                CurrentLobby = null;
            };
            
            SnapApiProxyHandler.Instance.SendSnapApiProxyMessage(method, deleteLobbyReq.ToByteString(), messageId);
        }

        public void ReadyCheck()
        {
            if (CurrentLobby == null)
            {
                Debug.LogWarning("No lobby to ready check. Try Joining a Lobby.");
                return;
            }

            var readyCheckReq = new ReadyMemberRequest
            {
                LobbyId = CurrentLobby.Id,
                UserId = _userId,
                Ready = true
            };
            
            var messageId = Guid.NewGuid().ToString();
            var method = "/lobbies.LobbiesService/ReadyMember";
            
            SnapApiProxyHandler.Instance.OnSnapProxyResponse += (_, e) =>
            {
                if (e.MessageId != messageId) return;

                if (e.IsError)
                {
                    HandleSnapApiError(e);
                    return;
                }

                Debug.Log($"Ready check");
            };
            
            SnapApiProxyHandler.Instance.SendSnapApiProxyMessage(method, readyCheckReq.ToByteString(), messageId);
        }

        public void UpdateMetadata()
        {
            if (CurrentLobby == null)
            {
                Debug.LogWarning("No lobby to update metadata. Try Joining a Lobby.");
                return;
            }
            
            var updateMetadataReq = new UpdateLobbyMemberMetadataRequest
            {
                LobbyId = CurrentLobby.Id,
                UserId = _userId,
                Metadata = new Struct
                {
                    Fields =
                    {
                        ["username"] = Value.ForString(_username),
                        ["character-selection-class"] = Value.ForString("warrior")
                    }
                }
            };
            
            var messageId = Guid.NewGuid().ToString();
            var method = "/lobbies.LobbiesService/UpdateLobbyMemberMetadata";
            
            SnapApiProxyHandler.Instance.OnSnapProxyResponse += (_, e) =>
            {
                if (e.MessageId != messageId) return;

                if (e.IsError)
                {
                    HandleSnapApiError(e);
                    return;
                }

                Debug.Log($"Updated metadata");
            };
            
            SnapApiProxyHandler.Instance.SendSnapApiProxyMessage(method, updateMetadataReq.ToByteString(), messageId);
        }

        public void StartMatch()
        {
            if (CurrentLobby == null)
            {
                Debug.LogWarning("No lobby to start match. Try Joining a Lobby.");
                return;
            }
            
            var startMatchReq = new StartMatchRequest
            {
               LobbyId = CurrentLobby.Id,
               Owner = _userId,
               Region = "oregon",
               // FleetName = "lobbiestestfleet"
               
            };
            
            var messageId = Guid.NewGuid().ToString();
            var method = "/lobbies.LobbiesService/StartMatch";
            
            SnapApiProxyHandler.Instance.OnSnapProxyResponse += (_, e) =>
            {
                if (e.MessageId != messageId) return;

                if (e.IsError)
                {
                    HandleSnapApiError(e);
                    return;
                }

                var response = StartMatchResponse.Parser.ParseFrom(e.Payload);
                CurrentLobby = response.Lobby;
                
                Debug.Log($"Started match: {CurrentLobby}");
            };
            
            SnapApiProxyHandler.Instance.SendSnapApiProxyMessage(method, startMatchReq.ToByteString(), messageId);
        }
        
        // Event Handlers
        public void OnMemberJoined(object s, OnLobbiesMemberJoinedEventArgs ev)
        {
            Debug.Log($"SnapEventLobbiesHandler OnMemberJoined: {ev.JoinedUserId}; metadata: {ev.JoinedUserMetadata}; placement: {ev.JoinedUserPlacement}");
            CurrentLobby.Members.Add(ev.JoinedUserId, new Member
            {
                Id = ev.JoinedUserId,
                Metadata = ev.JoinedUserMetadata,
                ReadyCheck = false
            });
        }
        
        public void OnMemberLeft(object s, OnLobbiesMemberLeftEventArgs ev)
        {
            Debug.Log($"SnapEventLobbiesHandler OnMemberLeft: {ev.LeftUserId} - {ev.Reason}");
            foreach (var member in CurrentLobby.Members)
            {
                if (member.Key == ev.LeftUserId)
                {
                    CurrentLobby.Members.Remove(member.Key);
                    break;
                }
            }
        }
        
        public void OnLobbyDisbanded(object s, OnLobbiesLobbyDisbandedEventArgs ev)
        {
            Debug.Log($"SnapEventLobbiesHandler OnLobbyDisbanded: {ev.LobbiesId}");
            CurrentLobby = null;
        }

        public void OnMemberMetadataUpdated(object s, OnLobbiesMemberMetadataUpdatedEventArgs ev)
        {
            Debug.Log($"SnapEventLobbiesHandler OnMemberMetadataUpdated: {ev.UserId} - {ev.UserMetadata}");
        }

        public void OnLobbyMatchStarted(object s, OnLobbiesMatchStartedEventArgs ev)
        {
            var isHost = CurrentLobby.Owner == _userId;
            
            Debug.Log($"Match Created.  Starting relay: {ev.ConnectionString} {ev.JoinCode} {isHost}");
            if (isHost)
            {
                SnapserNetworkManager.singleton.StartRelayHost(CurrentLobby.Id, _userId, _username, ev.ConnectionString, ev.JoinCode);
            }
            else
            {
                SnapserNetworkManager.singleton.StartRelayClient(CurrentLobby.Id, _userId, _username, ev.ConnectionString, ev.JoinCode);
            }
            
            var lobbiesUI = GameObject.Find("LobbiesUI").GetComponent<LobbiesUI>();
            lobbiesUI.UpdateForMatch();
        }

        private void HandleSnapApiError(OnSnapProxyResponseArgs ev)
        {
            Debug.LogError($"SnapApi Error: {ev.Error.Message}, Code: {ev.Error.Code}, Details: {ev.Error.Details}");
        }
    }
}