using System;
using Hermes;
using UnityEngine;

namespace Snapser.Handlers
{
    public class ErrorHandler : BaseHandler
    {
        public static ErrorHandler Instance => _instance ??= new ErrorHandler();
        private static ErrorHandler _instance;
        
        // Event Handlers
        public event EventHandler<OnErrorEventArgs> OnError;
        
        public void HandleServerMessage(ServerMessage serverMessage)
        {
            switch (serverMessage.MessageCase)
            {
                case ServerMessage.MessageOneofCase.Error:
                    var errorArgs = new OnErrorEventArgs
                    {
                        Mid = serverMessage.Mid,
                        MessageTime = DateTimeOffset.FromUnixTimeSeconds(serverMessage.Timestamp).DateTime,
                        Error = serverMessage.Error
                    };
                    
                    Debug.LogError($"Received server error: {errorArgs.Error.Message} {errorArgs.Error.Code}");
                    
                    OnError?.Invoke(this, errorArgs);
                    break;
            }
        }
    }
    
    public class OnErrorEventArgs : EventArgs
    {
        public string Mid { get; set; }
        public DateTime MessageTime { get; set; }
        public Message_Error Error { get; set; }
    }
}