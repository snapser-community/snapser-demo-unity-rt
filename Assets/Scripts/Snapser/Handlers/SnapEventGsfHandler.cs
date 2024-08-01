using System;
using GameServerFleets;
using Google.Protobuf;
using Hermes;

namespace Snapser.Handlers
{
    public class SnapEventGsfHandler : BaseHandler
    {
        public static SnapEventGsfHandler Instance => _instance ??= new SnapEventGsfHandler();
        private static SnapEventGsfHandler _instance;
        
        private const string ServiceName = "gsf";
        
        public event EventHandler<OnGsfGameServerStateUpdatedArgs> OnGameServerStateUpdated;

        public void HandleServerMessage(ServerMessage serverMessage)
        {
            if (serverMessage.MessageType != MessageType.SnapEvent)
            {
                Console.WriteLine("invalid server message type for snap event Gsf handler");
            }
            
            if (serverMessage.SnapEvent.ServiceName != ServiceName)
            {
                Console.WriteLine("invalid service name for snap event Gsf handler");
            }
            
            var payload = serverMessage.SnapEvent.Payload.ToByteArray();
            var evType = (GameServerFleetsEventType)serverMessage.SnapEvent.EventId;
            
            switch (evType)
            {
                case GameServerFleetsEventType.GsfGameServerStateUpdated:
                    var stateUpdatedMsg = ParsePayload<EventGameServerStateUpdated>(payload);
                    OnGameServerStateUpdated?.Invoke(this, new OnGsfGameServerStateUpdatedArgs
                    {
                        MessageId = serverMessage.Mid,
                        PublishedAt = DateTimeOffset.FromUnixTimeSeconds(stateUpdatedMsg.PublishedAt).DateTime,
                        GameServerName = stateUpdatedMsg.GameServerName,
                        PreviousState = stateUpdatedMsg.PreviousState.ToString(),
                        NewState = stateUpdatedMsg.NewState.ToString()
                    });
                    break;
                default:
                    Console.WriteLine("unknown Gsf event type: " + serverMessage.SnapEvent.EventId);
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

    public class OnGsfGameServerStateUpdatedArgs : EventArgs
    {
        public string MessageId { get; set; }
        public DateTime PublishedAt { get; set; }
        public string GameServerName { get; set; }
        public string PreviousState { get; set; }
        public string NewState { get; set; }
        
    }
}

/*
message EventGameServerStateUpdated {
     GameServerFleetsEventType event_type = 1;
     string game_server_name = 2;
     GameServerState previous_state = 3;
     GameServerState new_state = 4;
   }
*/