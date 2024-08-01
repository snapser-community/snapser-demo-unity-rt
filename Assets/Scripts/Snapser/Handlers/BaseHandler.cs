using System.Threading.Tasks;

namespace Snapser.Handlers
{

    public class BaseHandler
    {
        public delegate Task SendClientMessage(byte[] clientMessage);
        protected SendClientMessage sendClientMessage;
        
        public void SetSendClientMessage(SendClientMessage sendClientMessageDelegate)
        {
            sendClientMessage += sendClientMessageDelegate;
        }
    }
    
}