using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Google.Protobuf;
using Hermes;
using Matchmaking;

namespace Snapser.Handlers
{
    public class HermesMessageHandler : BaseHandler
    {
        public static HermesMessageHandler Instance => _instance ??= new HermesMessageHandler();
        private static HermesMessageHandler _instance;

        public event EventHandler<OnHermesProxyResponseArgs> OnHermesProxyResponse;

        public void HandleServerMessage(ServerMessage serverMessage)
        {
            switch (serverMessage.MessageCase)
            {
                case ServerMessage.MessageOneofCase.ApiResponse:
                    var apiResponse = new OnHermesProxyResponseArgs
                    {
                        MessageId = serverMessage.Mid,
                        Errored = serverMessage.ApiResponse.IsError,
                        Payload = DeserializePayload(serverMessage.ApiResponse.Payload.ToByteArray())
                    };

                    OnHermesProxyResponse?.Invoke(this, apiResponse);
                    break;

                case ServerMessage.MessageOneofCase.SnapEvent:
                    var serviceName = serverMessage.SnapEvent.ServiceName;
                    var eventId = serverMessage.SnapEvent.EventId;
                    var payload = serverMessage.SnapEvent.Payload.ToByteArray();
                    IMessage snapMsg = null;

                    switch (serviceName)
                    {
                        case "matchmaking":
                            var evType = (MatchmakingEventType)eventId;
                            switch (evType)
                            {
                                case MatchmakingEventType.MatchmakingQueued:
                                    snapMsg = ParsePayload<EventMatchmakingQueued>(payload);
                                    break;
                                case MatchmakingEventType.MatchmakingDequeued:
                                    snapMsg = ParsePayload<EventMatchmakingDequeued>(payload);
                                    break;
                            }

                            break;
                    }

                    var snapEvent = new OnHermesSnapEventArgs()
                    {
                        EventId = eventId,
                        ServiceName = serviceName,
                        Payload = payload,
                        ProtoMsg = snapMsg
                    };
                    break;
            }
        }

        private T ParsePayload<T>(byte[] payload) where T : IMessage, new()
        {
            T msg = new T();
            msg.MergeFrom(payload);

            return msg;
        }

        private byte[] SerializePayload(object payload)
        {
            var binF = new BinaryFormatter();
            var memS = new MemoryStream();

            binF.Serialize(memS, payload);

            return memS.ToArray();
        }

        private object DeserializePayload(byte[] payload)
        {
            var binF = new BinaryFormatter();
            var memS = new MemoryStream();

            memS.Write(payload, 0, payload.Length);
            memS.Seek(0, SeekOrigin.Begin);

            var obj = binF.Deserialize(memS);

            return obj;
        }
    }

    public class OnHermesProxyResponseArgs : EventArgs
    {
        public string MessageId { get; set; }
        public object Payload { get; set; }
        public bool Errored { get; set; }
        public SnapApiError Error { get; set; }
    }

    public class OnHermesSnapEventArgs : EventArgs
    {
        public uint EventId { get; set; }
        public string ServiceName { get; set; }
        public object Payload { get; set; }
        public IMessage ProtoMsg { get; set; }
    }
}

/*
 message Message_SnapEvent {
   uint32 event_id = 1;
   string service_name = 2;
   bytes payload = 3;
   }
message Message_SnapApiRequest {
   string method = 1;
   bytes payload = 2;
   }

   message Message_SnapApiResponse {
   bytes payload = 1;
   bool errored = 2;
   optional SnapApiError error = 3;
   }
   */