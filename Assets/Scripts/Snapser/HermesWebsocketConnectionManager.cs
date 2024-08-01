using NativeWebSocket;
using Hermes;
using UnityEditor;
using UnityEngine;
using MessageType = Hermes.MessageType;

namespace Snapser
{
    public class HermesWebsocketConnectionManager : BaseWebsocketConnectionManager
    {
        public override void RegisterDefaultHandlers()
        {
            Debug.Log("Registering hermes default handlers ...");
            base.RegisterDefaultHandlers();
            
            Handlers.SnapApiProxyHandler.Instance.SetSendClientMessage(async message =>
            {
                if (Conn.State == WebSocketState.Open)
                    await Conn.Send(message);
            });
            
            Handlers.ErrorHandler.Instance.OnError += (_, args) =>
            {
                Debug.LogError("Error received from server! " + args.Error.Message + " " + args.Error.Code);
            }; 
        }

        public override void RegisterMessageHandlers()
        {
            Debug.Log("Registering hermes message handlers ...");
            Conn.OnMessage += message =>
            {
                var serverMessage = ServerMessage.Parser.ParseFrom(message);
                switch (serverMessage.MessageType)
                {
                    case MessageType.Error:
                        Handlers.ErrorHandler.Instance.HandleServerMessage(serverMessage);
                        break;
                    case MessageType.SnapEvent:
                        switch (serverMessage.SnapEvent.ServiceName)
                        {
                            case "matchmaking":
                                Handlers.SnapEventMatchmakingHandler.Instance.HandleServerMessage(serverMessage);
                                break;
                            case "parties": 
                                Handlers.SnapEventPartyHandler.Instance.HandleServerMessage(serverMessage);
                                break;
                            case "lobbies":
                                Handlers.SnapEventLobbiesHandler.Instance.HandleServerMessage(serverMessage);
                                break;
                            case "game-server-fleets":
                                Handlers.SnapEventGsfHandler.Instance.HandleServerMessage(serverMessage);
                                break;
                            default:
                                Debug.LogWarning("unknown snap event type: " + serverMessage.SnapEvent.ServiceName);
                                Debug.LogWarning($"snap event: {serverMessage.SnapEvent}");
                                break;
                        }
                        break;
                    case MessageType.SnapApiProxy:
                        Handlers.SnapApiProxyHandler.Instance.HandleServerMessage(serverMessage);
                        break;
                    default:
                        Debug.LogWarning("Unknown message type! " + serverMessage.MessageType);
                        break;
                }
            }; 
        }
    }
    
}