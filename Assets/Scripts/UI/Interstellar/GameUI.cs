using System;
using System.ComponentModel.DataAnnotations;
using Mirror;
using Snapser.Handlers;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Utilities;

namespace Snapser.UI
{
    public class GameUI : MonoBehaviour
    {
        [SerializeField, Required] private PlayersUI playersUI;
        [SerializeField, Required] CanvasGroup usernameCanvasGroup, matchmakingCanvasGroup, leaveButtonCanvasGroup, findingMatchCanvasGroup;
        [SerializeField, Required] private TextMeshProUGUI messageLabel;
        [SerializeField, Required] private TMP_InputField usernameInputField;
        [SerializeField, Required] private Button submitButton;
        [SerializeField, Required] private Button leaveButton;
        [SerializeField, Required] private Toggle rememberUsernameToggle;
        [SerializeField, Required] private GameObject serverLeaveCautionLabelGameObject;
        
        // Test UI Elements
        [SerializeField, Required] private CanvasGroup lobbiesUICanvasGroup, partiesUICanvasGroup;
        
        [SerializeField] private string relayConnectedMessage = "Connection Successful!";
        [SerializeField] private string relayDisconnectedMessage = "Connection Disconnected!";
        [SerializeField] private string onMatchStartedMessage = "Player Found! Match Started";
        [SerializeField] private string onMatchEndedMessage = "Match Ended";

        // private HermesTransport transport;
        // private SpaceshipNetworkManager spaceshipNetworkManager;
        private SnapserNetworkManager _snapserNetworkManager;
        private TitanTransport _transport;
        private HermesWebsocketConnectionManager _hermesWebsocketConnection;

        private string remeberUsernameKey = "savedUserName";
        private string remeberUsernameToggleKey = "savedRememberUsernameToggle";


        public static event UnityAction OnInitializeUI;
        
        private void Start()
        {
            // spaceshipNetworkManager = SpaceshipNetworkManager.singleton;
            // transport = spaceshipNetworkManager.HermesTransport;
            _snapserNetworkManager = SnapserNetworkManager.singleton;
            _transport = _snapserNetworkManager.titanTransport;
            _hermesWebsocketConnection = _snapserNetworkManager.hermesWebsocketConnection;

            InitializeGameUI();

            _hermesWebsocketConnection.OnConnectionOpened += OnRelayConnectionOpened;
            _hermesWebsocketConnection.OnConnectionClosed += OnRelayConnectionClosed;
            _hermesWebsocketConnection.OnConnectionError += OnRelayConnectionClosed;

            TitanMessageHandler.Instance.OnMatchReady += OnMatchReady;
            TitanMessageHandler.Instance.OnMatchOver += OnMatchOver;
            
            // MatchHandler.Instance.OnMatchStarted += OnMatchStarted;
            // MatchHandler.Instance.OnMatchEnded += OnMatchEnded;
            Spaceship.OnPlayerWon += ShowLeaveButton;
            Spaceship.OnPlayerEliminated += ShowLeaveButton;
        }

        private void OnDestroy()
        {
            _hermesWebsocketConnection.OnConnectionOpened -= OnRelayConnectionOpened;
            _hermesWebsocketConnection.OnConnectionClosed -= OnRelayConnectionClosed;
            _hermesWebsocketConnection.OnConnectionError -= OnRelayConnectionClosed;
            
            TitanMessageHandler.Instance.OnMatchReady -= OnMatchReady;
            TitanMessageHandler.Instance.OnMatchOver -= OnMatchOver;
            
            // MatchHandler.Instance.OnMatchStarted -= OnMatchStarted;
            // MatchHandler.Instance.OnMatchEnded -= OnMatchEnded;
            Spaceship.OnPlayerWon -= ShowLeaveButton;
            Spaceship.OnPlayerEliminated -= ShowLeaveButton;
            usernameInputField.onValueChanged.RemoveListener(delegate { OnUsernameInputValueChanged();  });
        }

        private void OnRelayConnectionOpened()
        {
            messageLabel.gameObject.SetActive(true);
            messageLabel.text = relayConnectedMessage;
            matchmakingCanvasGroup.Show();
            leaveButtonCanvasGroup.Hide();
            SnapserManager.Instance.UpsertProfileAsync(_snapserNetworkManager.username);
        }
        
        private void OnRelayConnectionClosed()
        {
            InitializeGameUI();
            messageLabel.gameObject.SetActive(true);
            messageLabel.text = relayDisconnectedMessage;
        }

        private void InitializeGameUI()
        {
            OnInitializeUI?.Invoke();
            leaveButtonCanvasGroup.Hide();
            rememberUsernameToggle.isOn = PlayerPrefs.GetInt(remeberUsernameToggleKey) == 1;
            
            if (!(_snapserNetworkManager.HermesConnected && _snapserNetworkManager.IsAuthenticated))
            {
                usernameCanvasGroup.Show();
                matchmakingCanvasGroup.Hide();

                if (PlayerPrefs.HasKey(remeberUsernameKey))
                    usernameInputField.text = PlayerPrefs.GetString(remeberUsernameKey);
                
                OnUsernameInputValueChanged();
                usernameInputField.onValueChanged.AddListener(delegate { OnUsernameInputValueChanged();  });
            }
            else
            {
                // if (_snapserNetworkManager.Username.IsEmptyOrNull())
                    // transport.SetUsername(PlayerPrefs.GetString(remeberUsernameKey));
                    
                
                usernameCanvasGroup.Hide();
                matchmakingCanvasGroup.Show();
            }
        }

        private void OnMatchReady(object sender, OnMatchReadyArgs ev)
        {
            matchmakingCanvasGroup.Hide();
            findingMatchCanvasGroup.Hide();
            messageLabel.text = onMatchStartedMessage; 
        }
            
        private void OnMatchOver(object sender, OnMatchOverArgs e)
        {
            OnMatchEnded();
        }

        private void OnMatchEnded()
        {
            InitializeGameUI();
            messageLabel.text = onMatchEndedMessage;
            playersUI.DestroyAllPlayerUis();
        }

        public void OnToggleValueChanged(bool value)
        {
            PlayerPrefs.SetInt(remeberUsernameToggleKey, value ? 1 :0);
        }

        public void OnUsernameSubmitButtonPressed()
        {
            if (PlayerPrefs.GetInt(remeberUsernameToggleKey) == 1 && (!PlayerPrefs.HasKey(remeberUsernameKey) || (PlayerPrefs.HasKey(remeberUsernameKey) && !PlayerPrefs.GetString(remeberUsernameKey).Equals(usernameInputField.text))))
            {
                PlayerPrefs.SetString(remeberUsernameKey, usernameInputField.text);
            }
            else if (!rememberUsernameToggle.isOn && PlayerPrefs.HasKey(remeberUsernameKey))
            {
                PlayerPrefs.DeleteKey(remeberUsernameKey);
            }
            PlayerPrefs.Save();
            
            usernameCanvasGroup.Hide();
            // transport.Authenticate(usernameInputField.text);
            _snapserNetworkManager.Login(usernameInputField.text);
        }
        
        public void OnFindMatchButtonPressed()
        {
            matchmakingCanvasGroup.Hide();
            findingMatchCanvasGroup.Show();
            // transport.QueueMatchmaking();
            _snapserNetworkManager.QueueMatchmaking();
        }
        
        public void OnLobbiesUIButtonPressed()
        {   
            matchmakingCanvasGroup.Hide();
            lobbiesUICanvasGroup.Show();
        }

        public void OnPartiesUIButtonPressed()
        {
            matchmakingCanvasGroup.Hide();
            partiesUICanvasGroup.Show();
        }

        public void OnExitLobbiesButtonPressed()
        {
            lobbiesUICanvasGroup.Hide();
            matchmakingCanvasGroup.Show();
        }

        public void OnCreatePartyButtonPressed()
        {
            Debug.Log("Pressed the create party button");
            _snapserNetworkManager.PartiesHandler.CreateParty();
        }

        public void OnFindPartyButtonPressed()
        {
            Debug.Log("Pressed the find party button");
            _snapserNetworkManager.PartiesHandler.SearchParties();
        }

        public void OnDeletePartyButtonPressed()
        {
            Debug.Log("Pressed the delete party button");
            _snapserNetworkManager.PartiesHandler.DeleteParty();
        }

        public void OnQueuePartyButtonPressed()
        {
            Debug.Log("Pressed the queue party button");
            _snapserNetworkManager.PartiesHandler.QueueParty();
        }
        
        public void OnDequeuePartyButtonPressed()
        {
            Debug.Log("Pressed the dequeue party button");
            _snapserNetworkManager.PartiesHandler.DequeueParty();
        }
        
        private void ShowLeaveButton(string userName, bool isLocalPlayer)
        {
            if (isLocalPlayer)
            {
                leaveButtonCanvasGroup.Show();
                bool isServer = _snapserNetworkManager.titanTransport.IsServer();
                serverLeaveCautionLabelGameObject.SetActive(isServer);
                leaveButton.gameObject.SetActive(!isServer);
                leaveButton.interactable = !isServer;
            }
        }

        public void OnLeaveButtonPressed()
        {
            // transport.SendPlayerLeave();
            // transport.OnMatchEnded();
            OnMatchEnded();
            NetworkClient.DestroyAllClientObjects();
        }

        private void OnUsernameInputValueChanged()
        {
            submitButton.interactable = usernameInputField.text.Length > 0;
        }
    }
}