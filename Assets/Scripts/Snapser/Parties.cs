using System;
using System.Collections.Generic;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Hermes;
using Matchmaking;
using Parties;
using Snapser.Handlers;
using UnityEngine;

namespace Snapser
{
    public class PartiesHandler
    {
        private const string SnapApiMessagePartyCreate = "party-create";
        private const string SnapApiMessagePartySearch = "party-search";
        private const string SnapApiMessagePartyJoin = "party-join";
        private const string SnapApiMessagePartyDelete = "party-delete";
        private const string SnapApiMessagePartyQueue = "party-queue";
        private const string SnapApiMessagePartyDequeue = "party-dequeue";
        
        private readonly Dictionary<string, List<string>> _snapApiMessageIds = new()
        {
            {SnapApiMessagePartyCreate, new List<string>()},
            {SnapApiMessagePartySearch, new List<string>()},
            {SnapApiMessagePartyJoin, new List<string>()},
            {SnapApiMessagePartyDelete, new List<string>()},
            {SnapApiMessagePartyQueue, new List<string>()},
            {SnapApiMessagePartyDequeue, new List<string>()}
        };
        
        public Party CurrentParty;
        private readonly string _userId;
        private readonly string _username;
       
        public PartiesHandler(string userId, string username)
        {
            _userId = userId;
            _username = username;
        }
        
        public void CreateParty()
        {
            var createPartyReq = new CreatePartyRequest
            {
                PartyName = "CJDsParty",
                OwnerId = _userId,
                OwnerMetadata = new Struct
                {
                    Fields =
                    {
                        ["username"] = Value.ForString(_username)
                    }
                },
                MaxPlayers = 2,
                Metadata = new Struct
                {
                    Fields =
                    {
                        ["some"] = Value.ForString("some metadata")
                    }
                },
                SearchProperties = new Struct
                {
                    Fields =
                    {
                        ["gamemode"] = Value.ForString("interstellar"),
                        ["arbitrary"] = Value.ForString("data")
                    }
                },
                Visibility = "PARTY_VISIBILITY_PUBLIC"
            };

          
            // subscribe to party events
            SnapEventPartyHandler.Instance.OnPartyJoined += OnPartyJoined;
            SnapEventPartyHandler.Instance.OnPartyLeft += OnPartyLeft;
            SnapEventPartyHandler.Instance.OnPartyDeleted += OnPartyDeleted;

            SnapApiProxyHandler.Instance.OnSnapProxyResponse += OnCreatePartyResponse;
            
            var messageId = Guid.NewGuid().ToString();
            var method = "/parties.PartiesService/CreateParty";
            _snapApiMessageIds[SnapApiMessagePartyCreate].Add(messageId);
            
            Debug.Log($"Creating party with message id: {messageId}");
            
            SnapApiProxyHandler.Instance.SendSnapApiProxyMessage(method, createPartyReq.ToByteString(), messageId);
        }

        private void OnCreatePartyResponse(object s, OnSnapProxyResponseArgs ev)
        {
            if (!_snapApiMessageIds["party-create"].Contains(ev.MessageId))
            {
                return;
            }

            Debug.Log($"Got create parties response for message id: {ev.MessageId}");

            // Clean up delegate and tracked message ids
            _snapApiMessageIds["party-create"].Remove(ev.MessageId);
            SnapApiProxyHandler.Instance.OnSnapProxyResponse -= OnCreatePartyResponse;

            if (ev.IsError)
            {
                var snapApiError = SnapApiError.Parser.ParseFrom(ev.Payload);
                Debug.LogError($"Error creating party: {snapApiError}");
                return;
            }

            var createPartyResponse = CreatePartyResponse.Parser.ParseFrom(ev.Payload);
            CurrentParty = createPartyResponse.Party;

            Debug.Log($"Party created: {createPartyResponse.Party.Id}");
        }

        public void SearchParties()
        {
            var searchParties = new SearchPartiesRequest
            {
                Name = "CJDsParty",
                MetadataFilters =
                {
                    new[]
                    {
                        new MetadataFilter
                        {
                            Key = "gamemode",
                            Op = "=",
                            Value = Value.ForString("interstellar")
                        }
                    }
                }
            };

            var messageId = Guid.NewGuid().ToString();
            var method = "/parties.PartiesService/SearchParties";

            _snapApiMessageIds["party-search"].Add(messageId);
            SnapApiProxyHandler.Instance.OnSnapProxyResponse += OnSearchPartiesResponse;

            Debug.Log($"Searching for parties with message id: {messageId}");
            SnapApiProxyHandler.Instance.SendSnapApiProxyMessage(method, searchParties.ToByteString(), messageId);
        }

        private void OnSearchPartiesResponse(object s, OnSnapProxyResponseArgs ev)
        {
            if (!_snapApiMessageIds["party-search"].Contains(ev.MessageId))
            {
                return;
            }

            Debug.Log($"Got search parties response for message id: {ev.MessageId}");

            // Clean up delegate and tracked message ids
            _snapApiMessageIds["party-search"].Remove(ev.MessageId);
            SnapApiProxyHandler.Instance.OnSnapProxyResponse -= OnSearchPartiesResponse;

            if (ev.IsError)
            {
                var snapApiError = SnapApiError.Parser.ParseFrom(ev.Payload);
                Debug.LogError($"Error searching for parties: {snapApiError.Message} - {snapApiError.Details}");
                return;
            }

            var searchPartiesResponse = SearchPartiesResponse.Parser.ParseFrom(ev.Payload);
            Debug.Log($"Found {searchPartiesResponse.Parties.Count} parties");

            if (searchPartiesResponse.Parties.Count > 0)
            {
                var party = searchPartiesResponse.Parties[^1];
                Debug.Log($"Joining party: {party.Id}");

                JoinParty(party.Id);
            }
        }

        public void JoinParty(string partyId)
        {
            var req = new JoinPartyRequest
            {
                PartyId = partyId,
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
            var method = "/parties.PartiesService/JoinParty";

            _snapApiMessageIds["party-join"].Add(messageId);

            // subscribe to party events
            SnapEventPartyHandler.Instance.OnPartyJoined += OnPartyJoined;
            SnapEventPartyHandler.Instance.OnPartyLeft += OnPartyLeft;
            SnapEventPartyHandler.Instance.OnPartyDeleted += OnPartyDeleted;
            
            SnapApiProxyHandler.Instance.OnSnapProxyResponse += OnJoinPartyResponse;
            
            Debug.Log($"Joining party with message id: {messageId}");
            SnapApiProxyHandler.Instance.SendSnapApiProxyMessage(method, req.ToByteString(), messageId);
        }

        private void OnJoinPartyResponse(object s, OnSnapProxyResponseArgs ev)
        {
            if (!_snapApiMessageIds["party-join"].Contains(ev.MessageId))
            {
                return;
            }

            Debug.Log($"Got join party response for message id: {ev.MessageId}");

            // Clean up delegate and tracked message ids
            _snapApiMessageIds["party-join"].Remove(ev.MessageId);
            SnapApiProxyHandler.Instance.OnSnapProxyResponse -= OnJoinPartyResponse;

            if (ev.IsError)
            {
                var snapApiError = SnapApiError.Parser.ParseFrom(ev.Payload);
                Debug.LogError($"Error joining party: {snapApiError.Message} - {snapApiError.Details.Count}");
                return;
            }

            var joinPartyResponse = JoinPartyResponse.Parser.ParseFrom(ev.Payload);
            Debug.Log($"Joined party: {joinPartyResponse.Success}");

            CurrentParty = joinPartyResponse.Party;
        }

        public void LeaveParty()
        {
            if (CurrentParty == null)
            {
                Debug.LogError("No party to leave");
                return;
            }
            
            var req = new LeavePartyRequest
            {
                PartyId = CurrentParty.Id,
                UserId = _userId
            };
            
            var messageId = Guid.NewGuid().ToString();
            var method = "/parties.PartiesService/LeaveParty";

            SnapApiProxyHandler.Instance.OnSnapProxyResponse += (_, ev) =>
            {
                if (ev.MessageId != messageId) return;

                Debug.Log($"Got leave party response for message id: {ev.MessageId}");
                
                CurrentParty = null;

                if (!ev.IsError) return;
            
                var snapApiError = SnapApiError.Parser.ParseFrom(ev.Payload);
                Debug.LogError($"Error leaving party: {snapApiError.Message} - {snapApiError.Details.Count}");
            };
            
            Debug.Log($"Leaving party with message id: {messageId}");
            SnapApiProxyHandler.Instance.SendSnapApiProxyMessage(method, req.ToByteString(), messageId);
        }

        public void DeleteParty()
        {
            if (CurrentParty == null)
            {
                Debug.LogError("No party to delete");
                return;
            }

            var req = new DeletePartyRequest
            {
                PartyId = CurrentParty.Id,
                OwnerId = _userId
            };

            var messageId = Guid.NewGuid().ToString();
            var method = "/parties.PartiesService/DeleteParty";

            _snapApiMessageIds["party-delete"].Add(messageId);

            SnapApiProxyHandler.Instance.OnSnapProxyResponse += OnDeletePartyResponse;

            Debug.Log($"Deleting party with message id: {messageId}");
            SnapApiProxyHandler.Instance.SendSnapApiProxyMessage(method, req.ToByteString(), messageId);
        }

        private void OnDeletePartyResponse(object s, OnSnapProxyResponseArgs ev)
        {
            if (!_snapApiMessageIds["party-delete"].Contains(ev.MessageId))
            {
                return;
            }

            Debug.Log($"Got delete party response for message id: {ev.MessageId}");

            // Clean up delegate and tracked message ids
            _snapApiMessageIds["party-delete"].Remove(ev.MessageId);
            SnapApiProxyHandler.Instance.OnSnapProxyResponse -= OnDeletePartyResponse;

            if (ev.IsError)
            {
                var snapApiError = SnapApiError.Parser.ParseFrom(ev.Payload);
                Debug.LogError($"Error deleting party: {snapApiError.Message} - {snapApiError.Details.Count}");
                return;
            }

            var deletePartyResponse = DeletePartyResponse.Parser.ParseFrom(ev.Payload);
            Debug.Log($"Deleted party: {deletePartyResponse.Success}");
        }

        public void QueueParty()
        {
            if (CurrentParty == null)
            {
                Debug.LogError("No party to queue into");
                return;
            }
            
            Debug.Log($"Queueing into Party Matchmaking: {CurrentParty.Id}");

            var req = new CreateTicketsForPartyRequest
            {
                PartyId = CurrentParty.Id,
                UserId = _userId,
                Metadata = { { "game-mode", "interstellar" } },
                SearchFields = new SearchFields { Tags = { "interstellar", "snap_titan_geo_oregon" } }
            };
            
            var messageId = Guid.NewGuid().ToString();
            var method = "/matchmaking.MatchmakingService/CreateTicketsForParty";
            
            _snapApiMessageIds["party-queue"].Add(messageId);
            SnapApiProxyHandler.Instance.OnSnapProxyResponse += OnQueuePartyMatchmakingResponse;
            
            Debug.Log($"Queueing party matchmaking with message id: {messageId}");
            SnapApiProxyHandler.Instance.SendSnapApiProxyMessage(method, req.ToByteString(), messageId);
        }
        
        private void OnQueuePartyMatchmakingResponse(object s, OnSnapProxyResponseArgs ev)
        {
            if (!_snapApiMessageIds["party-queue"].Contains(ev.MessageId))
            {
                return;
            }

            Debug.Log($"Got queue party matchmaking response for message id: {ev.MessageId}");

            // Clean up delegate and tracked message ids
            _snapApiMessageIds["party-queue"].Remove(ev.MessageId);
            SnapApiProxyHandler.Instance.OnSnapProxyResponse -= OnQueuePartyMatchmakingResponse;

            if (ev.IsError)
            {
                var snapApiError = SnapApiError.Parser.ParseFrom(ev.Payload);
                Debug.LogError($"Error queueing party matchmaking: {snapApiError.Message} - {snapApiError.Details.Count}");
                return;
            }

            var createTicketsForPartyResponse = ListTicketResponse.Parser.ParseFrom(ev.Payload);
            Debug.Log($"Queued party matchmaking: {createTicketsForPartyResponse.Tickets.Count}");
            
            foreach (var ticket in createTicketsForPartyResponse.Tickets)
            {
                Debug.Log($"Ticket: {ticket.Id}");
            }
        }

        public void DequeueParty()
        {   
            if (CurrentParty == null)
            {
                Debug.LogError("No party to dequeue from");
                return;
            }
            
            Debug.Log($"Dequeueing from Party Matchmaking: {CurrentParty.Id}");

            var req = new DeleteTicketsForPartyRequest
            {
                PartyId = CurrentParty.Id,
                UserId = _userId
            };
            
            var messageId = Guid.NewGuid().ToString();
            var method = "/matchmaking.MatchmakingService/DeleteTicketsForParty";
            
            _snapApiMessageIds["party-dequeue"].Add(messageId);
            SnapApiProxyHandler.Instance.OnSnapProxyResponse += OnDequeuePartyMatchmakingResponse;
            
            Debug.Log($"Dequeueing party matchmaking with message id: {messageId}");
            SnapApiProxyHandler.Instance.SendSnapApiProxyMessage(method, req.ToByteString(), messageId);
        }
        
        private void OnDequeuePartyMatchmakingResponse(object s, OnSnapProxyResponseArgs ev)
        {
            if (!_snapApiMessageIds["party-dequeue"].Contains(ev.MessageId))
            {
                return;
            }

            Debug.Log($"Got dequeue party matchmaking response for message id: {ev.MessageId}");

            // Clean up delegate and tracked message ids
            _snapApiMessageIds["party-dequeue"].Remove(ev.MessageId);
            SnapApiProxyHandler.Instance.OnSnapProxyResponse -= OnDequeuePartyMatchmakingResponse;

            if (!ev.IsError) return;
            
            var snapApiError = SnapApiError.Parser.ParseFrom(ev.Payload);
            Debug.LogError($"Error dequeueing party matchmaking: {snapApiError.Message} - {snapApiError.Details.Count}");
        }

        public void UpdatePlayerMetadata()
        {
            if (CurrentParty == null)
            {
                Debug.LogError("No party to update metadata for");
                return;
            }
            
            var req = new UpdatePartyPlayerMetadataRequest
            {
                PartyId = CurrentParty.Id,
                UserId = _userId,
                Metadata = new Struct
                {
                    Fields =
                    {
                        ["username"] = Value.ForString(_username),
                        ["readycheck"] = Value.ForBool(true)
                    }
                }
            };
            
            var messageId = Guid.NewGuid().ToString();
            var method = "/parties.PartiesService/UpdatePartyPlayerMetadata";

            SnapApiProxyHandler.Instance.OnSnapProxyResponse += (_, ev) =>
            {
                if (ev.MessageId != messageId) return;

                Debug.Log($"Got update party player metadata response for message id: {ev.MessageId}");

                if (!ev.IsError) return;
            
                var snapApiError = SnapApiError.Parser.ParseFrom(ev.Payload);
                Debug.LogError($"Error updating party player metadata: {snapApiError.Message} - {snapApiError.Details.Count}");
            };
            
            Debug.Log($"Updating party player metadata with message id: {messageId}");
            SnapApiProxyHandler.Instance.SendSnapApiProxyMessage(method, req.ToByteString(), messageId);
        }
        
        public void OnPartyJoined(object s, OnPartyJoinedEventArgs ev)
        {
            Debug.Log($"Snapser: OnPartyJoined {ev.JoinedUserId} {ev.JoinedUserMetadata}");
        }

        public void OnPartyLeft(object s, OnPartyLeftEventArgs ev)
        {
            Debug.Log($"Snapser: OnPartyLeft {ev.LeftUserId} {ev.LeftUserMetadata}");
        }

        public void OnPartyDeleted(object s, OnPartyDeleteEventArgs ev)
        {
            Debug.Log($"Snapser: OnPartyDeleted {ev.PartyId} {ev.Reason}");
        }
        
        public void OnPartyPlayerMetadataUpdated(object s, OnPartyPlayerMetadataUpdateEventArgs ev)
        {
            Debug.Log($"Snapser: OnPartyPlayerMetadataUpdated {ev.UserId} {ev.UserMetadata}");
        }
    }
}