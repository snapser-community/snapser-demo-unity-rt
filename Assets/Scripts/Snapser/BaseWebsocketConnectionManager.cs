using NativeWebSocket;
using UnityEngine;
using UnityEngine.Events;

namespace Snapser
{
    public class BaseWebsocketConnectionManager : MonoBehaviour
    {
        protected WebSocket Conn;
        
        public event UnityAction OnConnectionOpened;
        public event UnityAction OnConnectionClosed;
        public event UnityAction OnConnectionError;

        public void Awake()
        {
            enabled = false;
            DontDestroyOnLoad(this);
        }
        
        public void OnDestroy()
        {
            enabled = false;
            if (Conn != null && Conn.State == WebSocketState.Open)
                Conn.Close();
        }
        
        public void OnApplicationQuit()
        {
            enabled = false;
            if (Conn != null && Conn.State == WebSocketState.Open)
                Conn.Close();
        }
        
        public void FixedUpdate()
        {
            if (Conn.State == WebSocketState.Open)
                Conn.DispatchMessageQueue();
        }
        
        public void Connect(string address)
        {
            Debug.Log($"Connecting to {address}...");
            Conn = new WebSocket(address);
            
            // Ride that monobehavior update loop baby
            //WebsocketUpdater.Initialize(conn);
            enabled = true;
            
            RegisterDefaultHandlers();
            RegisterMessageHandlers();

            Conn.Connect();
        }
        
        public bool IsConnected()
        {
            return Conn is { State: WebSocketState.Open };
        }
        
        public void Disconnect()
        {
            Conn.Close();
        }

        public virtual void RegisterDefaultHandlers()
        {
            Conn.OnOpen += () =>
            {
                OnConnectionOpened?.Invoke();
                Debug.Log("Connection opened!");
            };

            Conn.OnClose += e =>
            {
                OnConnectionClosed?.Invoke();
                Debug.Log("WebSocket closed! " + e);
            };

            Conn.OnError += e =>
            {
                OnConnectionError?.Invoke();
                Debug.LogError("WebSocket error! " + e.ToString());
            };
        }

        public virtual void RegisterMessageHandlers()
        {
            Conn.OnMessage += message =>
            {
                Debug.Log("received message: " + message);
            };
        }
    }
}