
using System;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Hermes;
using Parties;
using UnityEngine;

namespace Snapser.Handlers
{
    public class SnapEventPartyHandler : BaseHandler
    {
        public static SnapEventPartyHandler Instance => _instance ??= new SnapEventPartyHandler();
        private static SnapEventPartyHandler _instance;

        private const string ServiceName = "parties";

        public event EventHandler<OnPartyJoinedEventArgs> OnPartyJoined;
        public event EventHandler<OnPartyLeftEventArgs> OnPartyLeft;
        public event EventHandler<OnPartyDeleteEventArgs> OnPartyDeleted;
        public event EventHandler<OnPartyPlayerMetadataUpdateEventArgs> OnPlayerMetadataUpdated;

        public void HandleServerMessage(ServerMessage serverMessage)
        {
            if (serverMessage.MessageType != MessageType.SnapEvent)
            {
                Debug.LogError("invalid server message type for snap event parties handler");
            }
            
            if (serverMessage.SnapEvent.ServiceName != ServiceName)
            {
                Debug.LogError("invalid service name for snap event parties handler");
            }
            
            var payload = serverMessage.SnapEvent.Payload.ToByteArray();
            var evType = (PartiesEventType)serverMessage.SnapEvent.EventId;

            switch (evType)
            {
                case PartiesEventType.PartyJoined:
                    var joinedMsg = ParsePayload<EventPartyJoined>(payload);
                    OnPartyJoined?.Invoke(this, new OnPartyJoinedEventArgs
                    {
                        MessageId = serverMessage.Mid,
                        PublishedAt = DateTimeOffset.FromUnixTimeSeconds(joinedMsg.PublishedAt).DateTime,
                        JoinedUserId = joinedMsg.JoinedUserId,
                        JoinedUserMetadata = joinedMsg.JoinedUserMetadata
                    });
                    break;
                case PartiesEventType.PartyLeft:
                    var leftMsg = ParsePayload<EventPartyLeft>(payload);
                    OnPartyLeft?.Invoke(this, new OnPartyLeftEventArgs
                    {
                        MessageId = serverMessage.Mid,
                        PublishedAt = DateTimeOffset.FromUnixTimeSeconds(leftMsg.PublishedAt).DateTime,
                        LeftUserId = leftMsg.LeftUserId,
                        LeftUserMetadata = leftMsg.LeftUserMetadata
                    });
                    break;
                case PartiesEventType.PartyDeleted:
                    var deleteMsg = ParsePayload<EventPartyDeleted>(payload);
                    OnPartyDeleted?.Invoke(this, new OnPartyDeleteEventArgs
                    {
                        MessageId = serverMessage.Mid,
                        PublishedAt = DateTimeOffset.FromUnixTimeSeconds(deleteMsg.PublishedAt).DateTime,
                        PartyId = deleteMsg.PartyId,
                        Reason = deleteMsg.Reason
                    });
                    break;
                case PartiesEventType.PartyPlayerMetadataUpdated:
                    var metadataMsg = ParsePayload<EventPartyPlayerMetadataUpdated>(payload);
                    OnPlayerMetadataUpdated?.Invoke(this, new OnPartyPlayerMetadataUpdateEventArgs
                    {
                        MessageId = serverMessage.Mid,
                        PublishedAt = DateTimeOffset.FromUnixTimeSeconds(metadataMsg.PublishedAt).DateTime,
                        UserId = metadataMsg.UserId,
                        UserMetadata = metadataMsg.Metadata
                    });
                    break;
            }
        }
        
        private T ParsePayload<T>(byte[] payload) where T : IMessage, new()
        {
            T msg = new T();
            msg.MergeFrom(payload);

            return msg;
        }

    }

    public class OnPartyJoinedEventArgs : EventArgs
    {
        public string MessageId { get; set; }
        public DateTime PublishedAt { get; set; }
        public string JoinedUserId { get; set; }
        public Struct JoinedUserMetadata { get; set; }
    }

    public class OnPartyLeftEventArgs : EventArgs
    {
        public string MessageId { get; set; }
        public DateTime PublishedAt { get; set; }
        public string LeftUserId { get; set; }
        public Struct LeftUserMetadata { get; set; }
    }
    
    public class OnPartyDeleteEventArgs : EventArgs
    {
        public string MessageId { get; set; }
        public DateTime PublishedAt { get; set; }
        public string PartyId { get; set; }
        public string Reason { get; set; }
    }
    
    public class OnPartyPlayerMetadataUpdateEventArgs : EventArgs
    {
        public string MessageId { get; set; }
        public DateTime PublishedAt { get; set; }
        public string UserId { get; set; }
        public Struct UserMetadata { get; set; }
    }
}