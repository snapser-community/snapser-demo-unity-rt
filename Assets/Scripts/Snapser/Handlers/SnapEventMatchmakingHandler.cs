using System;
using Google.Protobuf;
using Hermes;
using Matchmaking;
using UnityEngine;

namespace Snapser.Handlers
{
    public class SnapEventMatchmakingHandler : BaseHandler
    {
        public static SnapEventMatchmakingHandler Instance => _instance ??= new SnapEventMatchmakingHandler();
        private static SnapEventMatchmakingHandler _instance;

        private const string ServiceName = "matchmaking";

        public event EventHandler<OnMatchmakingQueuedEventArgs> OnMatchmakingQueued;
        public event EventHandler<OnMatchmakingDeQueuedEventArgs> OnMatchmakingDeQueued;
        public event EventHandler<OnMatchmakingMatchFoundEventArgs> OnMatchmakingMatchFound;
        public event EventHandler<OnMatchmakingMatchCreatedEventArgs> OnMatchmakingMatchCreated;
        public event EventHandler<OnMatchmakingMatchCancelledEventArgs> OnMatchmakingMatchCancelled;
        public event EventHandler<OnMatchmakingNoRulesApplyEventArgs> OnMatchmakingNoRulesApply;
        public event EventHandler<OnMatchmakingMatchCreateErrorArgs> OnMatchmakingMatchCreateError;

        public void HandleServerMessage(ServerMessage serverMessage)
        {
            if (serverMessage.MessageType != MessageType.SnapEvent)
            {
                Debug.LogError("invalid server message type for snap event matchmaking handler");
                return;
            }


            if (serverMessage.SnapEvent.ServiceName != ServiceName)
            {
                Debug.LogError("invalid service name for snap event matchmaking handler");
                return;
            }

            var payload = serverMessage.SnapEvent.Payload.ToByteArray();
            var evType = (MatchmakingEventType)serverMessage.SnapEvent.EventId;

            switch (evType)
            {
                case MatchmakingEventType.MatchmakingQueued:
                    var msg = ParsePayload<EventMatchmakingQueued>(payload);
                    OnMatchmakingQueued?.Invoke(this, new OnMatchmakingQueuedEventArgs
                    {
                        MessageId = serverMessage.Mid,
                        PublishedAt = DateTimeOffset.FromUnixTimeSeconds(msg.PublishedAt).DateTime,
                        QueuedAt = DateTimeOffset.FromUnixTimeSeconds(msg.QueuedAt).DateTime,
                        TicketId = msg.TicketId
                    });
                    break;
                case MatchmakingEventType.MatchmakingDequeued:
                    var deQueueMsg = ParsePayload<EventMatchmakingDequeued>(payload);
                    OnMatchmakingDeQueued?.Invoke(this, new OnMatchmakingDeQueuedEventArgs
                    {
                        MessageId = serverMessage.Mid,
                        PublishedAt = DateTimeOffset.FromUnixTimeSeconds(deQueueMsg.PublishedAt).DateTime,
                        Name = deQueueMsg.Name,
                        Reason = deQueueMsg.Reason,
                        TicketId = deQueueMsg.TicketId
                    });
                    break;
                case MatchmakingEventType.MatchmakingMatchFound:
                    var matchFoundMsg = ParsePayload<EventMatchmakingMatchFound>(payload);
                    OnMatchmakingMatchFound?.Invoke(this, new OnMatchmakingMatchFoundEventArgs
                    {
                        MessageId = serverMessage.Mid,
                        PublishedAt = DateTimeOffset.FromUnixTimeSeconds(matchFoundMsg.PublishedAt).DateTime,
                        MatchId = matchFoundMsg.MatchId,
                        MatchedAt = DateTimeOffset.FromUnixTimeSeconds(matchFoundMsg.MatchedAt).DateTime,
                        TicketId = matchFoundMsg.TicketId
                    });
                    break;
                case MatchmakingEventType.MatchmakingMatchCreated:
                    var matchCreatedMsg = ParsePayload<EventMatchmakingMatchCreated>(payload);
                    OnMatchmakingMatchCreated?.Invoke(this, new OnMatchmakingMatchCreatedEventArgs
                    {
                        MessageId = serverMessage.Mid,
                        PublishedAt = DateTimeOffset.FromUnixTimeSeconds(matchCreatedMsg.PublishedAt).DateTime,
                        MatchId = matchCreatedMsg.MatchId,
                        CreatedAt = DateTimeOffset.FromUnixTimeSeconds(matchCreatedMsg.CreatedAt).DateTime,
                        ConnectionString = matchCreatedMsg.ConnectionString,
                        JoinCode = matchCreatedMsg.JoinCode,
                        IsHost = matchCreatedMsg.IsHost,
                        TicketId = matchCreatedMsg.TicketId
                    });
                    break;
                case MatchmakingEventType.MatchmakingMatchCancelled:
                    var matchCancelledMsg = ParsePayload<EventMatchmakingMatchCancelled>(payload);
                    OnMatchmakingMatchCancelled?.Invoke(this, new OnMatchmakingMatchCancelledEventArgs
                    {
                        MessageId = serverMessage.Mid,
                        PublishedAt = DateTimeOffset.FromUnixTimeSeconds(matchCancelledMsg.PublishedAt).DateTime,
                        Reason = matchCancelledMsg.Reason
                    });
                    break;
                case MatchmakingEventType.MatchmakingNoRulesApply:
                    var noRulesApplyMsg = ParsePayload<EventMatchmakingNoRulesApply>(payload);
                    OnMatchmakingNoRulesApply?.Invoke(this, new OnMatchmakingNoRulesApplyEventArgs
                    {
                        MessageId = serverMessage.Mid,
                        PublishedAt = DateTimeOffset.FromUnixTimeSeconds(noRulesApplyMsg.PublishedAt).DateTime,
                        Reason = noRulesApplyMsg.Reason,
                        TicketCreatedAt = DateTimeOffset.FromUnixTimeSeconds(noRulesApplyMsg.TicketCreatedAt).DateTime,
                        MaxRulesAppliesForSeconds = noRulesApplyMsg.MaxRuleAppliesForSeconds,
                        TicketId = noRulesApplyMsg.TicketId
                    });
                    break;
                case MatchmakingEventType.MatchmakingMatchCreateError:
                    var errMsg = ParsePayload<EventMatchmakingMatchCreateError>(payload);
                    OnMatchmakingMatchCreateError?.Invoke(this, new OnMatchmakingMatchCreateErrorArgs
                    {
                        MessageId = serverMessage.Mid,
                        PublishedAt = DateTimeOffset.FromUnixTimeSeconds(errMsg.PublishedAt).DateTime,
                        Error = errMsg.Error,
                        TicketId = errMsg.TicketId
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

    public class OnMatchmakingQueuedEventArgs : EventArgs
    {
        public string MessageId { get; set; }
        public DateTime PublishedAt { get; set; }
        public DateTime QueuedAt { get; set; }
        public string TicketId { get; set; }
    }

    public class OnMatchmakingDeQueuedEventArgs : EventArgs
    {
        public string MessageId { get; set; }
        public DateTime PublishedAt { get; set; }
        public string Name { get; set; }
        public string Reason { get; set; }
        public string TicketId { get; set; }
    }

    public class OnMatchmakingMatchFoundEventArgs : EventArgs
    {
        public string MessageId { get; set; }
        public DateTime PublishedAt { get; set; }
        public string MatchId { get; set; }
        public DateTime MatchedAt { get; set; }
        public string TicketId { get; set; }
    }

    public class OnMatchmakingMatchCreatedEventArgs : EventArgs
    {
        public string MessageId { get; set; }
        public DateTime PublishedAt { get; set; }
        public string MatchId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ConnectionString { get; set; }
        public string JoinCode { get; set; }
        public bool IsHost { get; set; }
        public string TicketId { get; set; }
    }

    public class OnMatchmakingMatchCancelledEventArgs : EventArgs
    {
        public string MessageId { get; set; }
        public DateTime PublishedAt { get; set; }
        public string Reason { get; set; }
    }

    public class OnMatchmakingNoRulesApplyEventArgs : EventArgs
    {
        public string MessageId { get; set; }
        public DateTime PublishedAt { get; set; }
        public string Reason { get; set; }
        public DateTime TicketCreatedAt { get; set; }
        public Int64 MaxRulesAppliesForSeconds { get; set; }
        public string TicketId { get; set; }
    }

    public class OnMatchmakingMatchCreateErrorArgs : EventArgs
    {
        public string MessageId { get; set; }
        public DateTime PublishedAt { get; set; }
        public string Error { get; set; }
        public string TicketId { get; set; }
    }
}