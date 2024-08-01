using System;

namespace Snapser.Handlers
{
    public class InvalidMessageTypeException : Exception
    {
        public string MessageType { get; }
        public InvalidMessageTypeException(string message, string messageType) : base(message)
        {
            MessageType = messageType;
        }
    }
}