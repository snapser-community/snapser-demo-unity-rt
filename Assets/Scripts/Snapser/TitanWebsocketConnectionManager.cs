using NativeWebSocket;
using Titan;
using UnityEngine;

namespace Snapser
{
    public class TitanWebsocketConnectionManager : BaseWebsocketConnectionManager
    {
        public override void RegisterDefaultHandlers()
        {
            base.RegisterDefaultHandlers();

            Handlers.TitanMessageHandler.Instance.SetSendClientMessage(async message =>
            {
                if (Conn.State == WebSocketState.Open)
                    await Conn.Send(message);
            });
        }

        public override void RegisterMessageHandlers()
        {
            Conn.OnMessage += message =>
            {
                var titanMessage = TitanMessage.Parser.ParseFrom(message);
                Handlers.TitanMessageHandler.Instance.HandleTitanMessage(titanMessage);
            };
        }
    }
}