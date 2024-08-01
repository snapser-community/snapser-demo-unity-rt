using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Google.Protobuf;
using Titan;

namespace Snapser.Handlers
{
    public class TitanMessageHandler : BaseHandler
    {
        public static TitanMessageHandler Instance => _instance ??= new TitanMessageHandler();
        private static TitanMessageHandler _instance;

        // Event Handlers
        public event EventHandler<OnMatchReadyArgs> OnMatchReady;
        public event EventHandler<OnMatchJoinedArgs> OnMatchJoined;
        public event EventHandler<OnMatchLeftArgs> OnMatchLeft;
        public event EventHandler<OnMatchOverArgs> OnMatchOver;
        public event EventHandler<OnRelayDataArgs> OnRelayData;

        public void HandleTitanMessage(TitanMessage titanMessage)
        {
            switch (titanMessage.MessageType)
            {
                case MessageType.MatchReady:
                    // yank out the proto repeatedfield
                    var readyPlayers = new MatchPlayer[titanMessage.MatchReady.MatchPlayers.Count];
                    titanMessage.MatchReady.MatchPlayers.CopyTo(readyPlayers, 0);
                    
                    var matchReady = new OnMatchReadyArgs
                    {
                        MatchId = titanMessage.MatchReady.MatchId,
                        MatchPlayers = readyPlayers
                    };
                    OnMatchReady?.Invoke(this, matchReady);
                    break;
                case MessageType.MatchJoined:
                    var joinedPlayers = new MatchPlayer[titanMessage.MatchJoined.MatchPlayers.Count];
                    titanMessage.MatchJoined.MatchPlayers.CopyTo(joinedPlayers, 0);
                    
                    var matchJoined = new OnMatchJoinedArgs
                    {
                        MatchId = titanMessage.MatchJoined.MatchId,
                        PlayerJoinedId = titanMessage.MatchJoined.PlayerJoinedId,
                        MatchPlayers = joinedPlayers
                    };
                    OnMatchJoined?.Invoke(this, matchJoined);
                    break;
                case MessageType.MatchLeft:
                    var leftPlayers = new MatchPlayer[titanMessage.MatchLeft.MatchPlayers.Count];
                    titanMessage.MatchLeft.MatchPlayers.CopyTo(leftPlayers, 0);
                    
                    var matchLeft = new OnMatchLeftArgs
                    {
                        MatchId = titanMessage.MatchLeft.MatchId,
                        PlayerLeftId = titanMessage.MatchLeft.PlayerLeftId,
                        MatchPlayers = leftPlayers
                    };
                    OnMatchLeft?.Invoke(this, matchLeft);
                    break;
                case MessageType.MatchOver:
                    var matchOver = new OnMatchOverArgs
                    {
                        MatchId = titanMessage.MatchOver.MatchId,
                        ReasonForOver = titanMessage.MatchOver.ReasonForOver
                    };
                    
                    OnMatchOver?.Invoke(this, matchOver);
                    break;
                case MessageType.RelayData:
                    var deserializedData = DeserializeRelayData(titanMessage.RelayData.Data.ToByteArray());
                    var relayData = new OnRelayDataArgs
                    {
                        MatchId = titanMessage.RelayData.MatchId,
                        Sender = titanMessage.Sender,
                        Data = deserializedData,
                        Channel = titanMessage.RelayData.Channel
                    };
                    OnRelayData?.Invoke(this, relayData);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public async void SendMatchHostReady(string matchId, string reasonForUnready)
        {
            var messageId = Guid.NewGuid().ToString();
           
            var titanMessage = new TitanMessage
            {
                MessageId = messageId,
                SendTime = DateTimeOffset.Now.ToUnixTimeSeconds(),
                MessageType = MessageType.MatchHostReady,
                MatchHostReady = new MatchHostReady
                {
                    MatchId = matchId,
                }
            };
            
            if (!string.IsNullOrEmpty(reasonForUnready))
            {
                titanMessage.MatchHostReady.ReasonForUnready = reasonForUnready;
            }

            await sendClientMessage(titanMessage.ToByteArray());
        }

        public async void SendRelayData(string matchId, string[] recipients, int channel, object data)
        {
            var messageId = Guid.NewGuid().ToString();

            var serializedData = SerializeRelayData(data);
            var titanMessage = new TitanMessage
            {
                MessageId = messageId,
                SendTime = DateTimeOffset.Now.ToUnixTimeSeconds(),
                MessageType = MessageType.RelayData,
                RelayData = new RelayData
                {
                    MatchId = matchId,
                    Channel = channel,
                    Data = ByteString.CopyFrom(serializedData)
                }
            };
            
            if (recipients != null)
            {
                titanMessage.Recipients.AddRange(recipients);
            }

            await sendClientMessage(titanMessage.ToByteArray());
        }

        private object DeserializeRelayData(byte[] data)
        {
            var binF = new BinaryFormatter();
            var memS = new MemoryStream();

            memS.Write(data, 0, data.Length);
            memS.Seek(0, SeekOrigin.Begin);

            var obj = binF.Deserialize(memS);

            return obj;
        }

        private byte[] SerializeRelayData(object data)
        {
            var binF = new BinaryFormatter();
            var memS = new MemoryStream();

            binF.Serialize(memS, data);

            return memS.ToArray();
        }
    }

    public class OnMatchJoinedArgs : EventArgs
    {
        public string MatchId { get; set; }
        public string PlayerJoinedId { get; set; }
        public MatchPlayer[] MatchPlayers { get; set; }
    }
    
    public class OnMatchLeftArgs : EventArgs
    {
        public string MatchId { get; set; }
        public string PlayerLeftId { get; set; }
        public MatchPlayer[] MatchPlayers { get; set; }
    } 
    
    public class OnMatchReadyArgs : EventArgs
    {
        public string MatchId { get; set; }
        public MatchPlayer[] MatchPlayers { get; set; }
    }

    public class OnMatchOverArgs : EventArgs
    {
        public string MatchId { get; set; }
        public string ReasonForOver { get; set; }
    }

    public class OnRelayDataArgs : EventArgs
    {
        public string MatchId { get; set; }
        public string Sender { get; set; }
        public Object Data { get; set; }
        public int Channel { get; set; }
    }
}