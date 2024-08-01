using System;
using Google.Protobuf;
using Hermes;
using UnityEngine;

namespace Snapser.Handlers
{
    public class SnapApiProxyHandler : BaseHandler
    {
        public static SnapApiProxyHandler Instance => _instance ??= new SnapApiProxyHandler();
        private static SnapApiProxyHandler _instance;

        public event EventHandler<OnSnapProxyResponseArgs> OnSnapProxyResponse;

        public void HandleServerMessage(ServerMessage serverMessage)
        {
            if (serverMessage.MessageType != MessageType.SnapApiProxy)
            {
                Debug.LogError("invalid server message type for snap proxy handler");
                return;
            }

            var apiResponse = new OnSnapProxyResponseArgs
            {
                MessageId = serverMessage.Mid,
                IsError = serverMessage.ApiResponse.IsError,
                Payload = serverMessage.ApiResponse.Payload,
                Error = serverMessage.ApiResponse.Error
            };

            OnSnapProxyResponse?.Invoke(this, apiResponse);
        }

        public async void SendSnapApiProxyMessage(string method, ByteString payload, string messageId)
        {
            var clientMsg = new ClientMessage
            {
                Mid = messageId,
                MessageType = MessageType.SnapApiProxy,
                Timestamp = DateTimeOffset.Now.ToUnixTimeSeconds(),
                SnapApiRequest = new Message_SnapApiRequest
                {
                    Method = method,
                    Payload = payload
                }
            };

            await sendClientMessage.Invoke(clientMsg.ToByteArray());
        }
    }

    public class OnSnapProxyResponseArgs : EventArgs
    {
        public string MessageId { get; set; }
        public bool IsError { get; set; }
        public ByteString Payload { get; set; }
        public SnapApiError Error { get; set; }
    }
}