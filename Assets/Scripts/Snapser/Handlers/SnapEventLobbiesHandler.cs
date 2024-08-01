
using System;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Hermes;
using Lobbies;
using UnityEngine;

namespace Snapser.Handlers
{
    public class SnapEventLobbiesHandler : BaseHandler
    {
        public static SnapEventLobbiesHandler Instance => _instance ??= new SnapEventLobbiesHandler();
        private static SnapEventLobbiesHandler _instance;

        private const string ServiceName = "lobbies";

        public event EventHandler<OnLobbiesMemberJoinedEventArgs> OnLobbiesMemberJoined;
        public event EventHandler<OnLobbiesMemberLeftEventArgs> OnLobbiesMemberLeft;
        public event EventHandler<OnLobbiesLobbyDisbandedEventArgs> OnLobbiesLobbyDisbanded;
        public event EventHandler<OnLobbiesLobbyOwnerChangedEventArgs> OnLobbiesLobbyOwnerChanged;
        public event EventHandler<OnLobbiesMemberReadyEventArgs> OnLobbiesMemberReady;
        public event EventHandler<OnLobbiesMatchStartedEventArgs> OnLobbiesMatchStarted;
        public event EventHandler<OnLobbiesMemberInvitedEventArgs> OnLobbiesMemberInvited;
        public event EventHandler<OnLobbiesMemberMetadataUpdatedEventArgs> OnLobbiesMemberMetadataUpdated;
        
        public void HandleServerMessage(ServerMessage serverMessage)
        {
            if (serverMessage.MessageType != MessageType.SnapEvent)
            {
                Debug.LogError("invalid server message type for snap event Lobbies handler");
            }
            
            if (serverMessage.SnapEvent.ServiceName != ServiceName)
            {
                Debug.LogError("invalid service name for snap event Lobbies handler");
            }
            
            var payload = serverMessage.SnapEvent.Payload.ToByteArray();
            var evType = (LobbiesEventType)serverMessage.SnapEvent.EventId;
            
            switch (evType)
            {
                case LobbiesEventType.LobbiesMemberJoined:
                    var joinedMsg = ParsePayload<EventLobbiesMemberJoined>(payload);
                    OnLobbiesMemberJoined?.Invoke(this, new OnLobbiesMemberJoinedEventArgs
                    {
                        MessageId = serverMessage.Mid,
                        PublishedAt = DateTimeOffset.FromUnixTimeSeconds(joinedMsg.PublishedAt).DateTime,
                        JoinedUserId = joinedMsg.JoinedUserId,
                        JoinedUserMetadata = joinedMsg.JoinedUserMetadata,
                        JoinedUserPlacement = joinedMsg.Placement ?? 0
                    });
                    break;
                case LobbiesEventType.LobbiesMemberLeft:
                    var leftMsg = ParsePayload<EventLobbiesMemberLeft>(payload);
                    OnLobbiesMemberLeft?.Invoke(this, new OnLobbiesMemberLeftEventArgs
                    {
                        MessageId = serverMessage.Mid,
                        PublishedAt = DateTimeOffset.FromUnixTimeSeconds(leftMsg.PublishedAt).DateTime,
                        LeftUserId = leftMsg.LeftUserId,
                        LeftUserMetadata = leftMsg.LeftUserMetadata,
                        Reason = leftMsg.Reason
                    });
                    break;
                case LobbiesEventType.LobbiesLobbyDisbanded:
                    var deleteMsg = ParsePayload<EventLobbiesLobbyDisbanded>(payload);
                    OnLobbiesLobbyDisbanded?.Invoke(this, new OnLobbiesLobbyDisbandedEventArgs
                    {
                        MessageId = serverMessage.Mid,
                        PublishedAt = DateTimeOffset.FromUnixTimeSeconds(deleteMsg.PublishedAt).DateTime,
                        LobbiesId = deleteMsg.LobbyId,
                        OwnerUserId = deleteMsg.OwnerUserId
                    });
                    break;
                case LobbiesEventType.LobbiesOwnerChanged:
                    var ownerChangedMsg = ParsePayload<EventLobbiesOwnerChanged>(payload);
                    OnLobbiesLobbyOwnerChanged?.Invoke(this, new OnLobbiesLobbyOwnerChangedEventArgs
                    {
                        MessageId = serverMessage.Mid,
                        PublishedAt = DateTimeOffset.FromUnixTimeSeconds(ownerChangedMsg.PublishedAt).DateTime,
                        LobbiesId = ownerChangedMsg.LobbyId,
                        OldOwnerId = ownerChangedMsg.OldOwnerUserId,
                        NewOwnerId = ownerChangedMsg.NewOwnerUserId
                    });
                    break;
                case LobbiesEventType.LobbiesMatchStarted:
                    var matchStartedMsg = ParsePayload<EventLobbiesMatchStarted>(payload);
                    OnLobbiesMatchStarted?.Invoke(this, new OnLobbiesMatchStartedEventArgs
                    {
                        MessageId = serverMessage.Mid,
                        PublishedAt = DateTimeOffset.FromUnixTimeSeconds(matchStartedMsg.PublishedAt).DateTime,
                        LobbiesId = matchStartedMsg.LobbyId,
                        ConnectionString = matchStartedMsg.ConnectionString,
                        JoinCode = matchStartedMsg.JoinCode
                    });
                    break;
                case LobbiesEventType.LobbiesMemberMetadataUpdated:
                    var metadataUpdatedMsg = ParsePayload<EventLobbiesMemberMetadataUpdated>(payload);
                    OnLobbiesMemberMetadataUpdated?.Invoke(this, new OnLobbiesMemberMetadataUpdatedEventArgs
                    {
                        MessageId = serverMessage.Mid,
                        PublishedAt = DateTimeOffset.FromUnixTimeSeconds(metadataUpdatedMsg.PublishedAt).DateTime,
                        LobbyId = metadataUpdatedMsg.LobbyId,
                        UserId = metadataUpdatedMsg.UserId,
                        UserMetadata = metadataUpdatedMsg.UserMetadata
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

    public class OnLobbiesMemberJoinedEventArgs : EventArgs
    {
        public string MessageId { get; set; }
        public DateTime PublishedAt { get; set; }
        public string JoinedUserId { get; set; }
        public Struct JoinedUserMetadata { get; set; }
        public int JoinedUserPlacement { get; set; }
    }

    public class OnLobbiesMemberLeftEventArgs : EventArgs
    {
        public string MessageId { get; set; }
        public DateTime PublishedAt { get; set; }
        public string LeftUserId { get; set; }
        public Struct LeftUserMetadata { get; set; }
        public string Reason { get; set; }
    }
    
    public class OnLobbiesLobbyDisbandedEventArgs : EventArgs
    {
        public string MessageId { get; set; }
        public DateTime PublishedAt { get; set; }
        public string LobbiesId { get; set; }
        public string OwnerUserId { get; set; }
    }
    
    public class OnLobbiesLobbyOwnerChangedEventArgs : EventArgs
    {
        public string MessageId { get; set; }
        public DateTime PublishedAt { get; set; }
        public string LobbiesId { get; set; }
        public string OldOwnerId { get; set; }
        public string NewOwnerId { get; set; }
    }
    
    public class OnLobbiesMemberReadyEventArgs : EventArgs
    {
        public string MessageId { get; set; }
        public DateTime PublishedAt { get; set; }
        public string UserId { get; set; }
        public bool IsReady { get; set; }
    }
    
    public class OnLobbiesMatchStartedEventArgs : EventArgs
    {
        public string MessageId { get; set; }
        public DateTime PublishedAt { get; set; }
        public string LobbiesId { get; set; }
        public string ConnectionString { get; set; }
        public string JoinCode { get; set; }
    }
    
    public class OnLobbiesMemberInvitedEventArgs : EventArgs
    {
        public string MessageId { get; set; }
        public DateTime PublishedAt { get; set; }
        public string InviteId { get; set; }
        public string InviterUserId { get; set; }
        public string InvitedUserId { get; set; }
    }
    
    public class OnLobbiesMemberMetadataUpdatedEventArgs : EventArgs
    {
        public string MessageId { get; set; }
        public DateTime PublishedAt { get; set; }
        public string LobbyId { get; set; }
        public string UserId { get; set; }
        public Struct UserMetadata { get; set; }
    }
}
