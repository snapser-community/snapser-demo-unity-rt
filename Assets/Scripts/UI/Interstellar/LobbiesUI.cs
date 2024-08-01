
using System;
using System.ComponentModel.DataAnnotations;
using Lobbies;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using Utilities;
using Button = UnityEngine.UI.Button;

namespace Snapser.UI
{
    public class LobbiesUI : MonoBehaviour
    {
        [SerializeField, Required] private TMP_Text lobbyNameLabel;
        
        [SerializeField, Required] private TMP_InputField createLobbyNameInput;
        [SerializeField, Required] private TMP_InputField joinLobbyIdInput;
        [SerializeField, Required] private TMP_InputField listLobbiesInput;
        
        [SerializeField, Required] private Button deleteLobbyButton;
        [SerializeField, Required] private Button leaveLobbyButton;
        [SerializeField, Required] private Button startMatchButton;
        [SerializeField, Required] private Button readyCheckButton;
        [SerializeField, Required] private Button updateMetadataButton;
        

        private SnapserNetworkManager _snapserNetworkManager;
        
        private void Start()
        {
            _snapserNetworkManager = SnapserNetworkManager.singleton;
        }

        private void Update()
        {
            if (_snapserNetworkManager.LobbiesHandler == null)
            {
                return;
            }
            
            UpdateForLobbyScreen();
            
            if (_snapserNetworkManager.LobbiesHandler.CurrentLobby != null)
            {
                if (_snapserNetworkManager.LobbiesHandler.CurrentLobby.Owner == _snapserNetworkManager.userId)
                {
                    leaveLobbyButton.interactable = false;
                    
                    deleteLobbyButton.interactable = true;
                    startMatchButton.interactable = _snapserNetworkManager.LobbiesHandler.CurrentLobby.Members.Count > 1;
                }
                else
                {
                    deleteLobbyButton.interactable = false;
                    startMatchButton.interactable = false;
                    
                    leaveLobbyButton.interactable = true;
                }
                
                readyCheckButton.interactable = true;
                updateMetadataButton.interactable = true;
            }
            else
            {
                deleteLobbyButton.interactable = false;
                startMatchButton.interactable = false;
                readyCheckButton.interactable = false;
                leaveLobbyButton.interactable = false;
                updateMetadataButton.interactable = false;
            }
        }

        private void UpdateForLobbyScreen()
        {
            if (_snapserNetworkManager.LobbiesHandler.CurrentLobby != null)
            {
                lobbyNameLabel.text = _snapserNetworkManager.LobbiesHandler.CurrentLobby.Name;
                // set members
                
            }
            else
            {
                lobbyNameLabel.text = "Join or Create Lobby";
            }
        }

        public void UpdateForMatch()
        {
            deleteLobbyButton.interactable = false;
            startMatchButton.interactable = false;
            readyCheckButton.interactable = false;
            
            var myCanvasGroup = GetComponent<CanvasGroup>();
            myCanvasGroup.Hide();
        }

        public void OnCreateLobbyButtonPressed()
        {
            var partyName = createLobbyNameInput.text;
            
            Debug.Log($"Pressed the create lobby button with name: {partyName}");

            _snapserNetworkManager.LobbiesHandler.CreateLobby(partyName);
        }
        
        public void OnListLobbiesButtonPressed()
        {
            Debug.Log("Pressed the list lobbies button");
            _snapserNetworkManager.LobbiesHandler.ListLobbies("Interstellar Lobby");
        }
        
        public void OnJoinLobbyButtonPressed()
        {
            var lobbyId = joinLobbyIdInput.text;
            
            Debug.Log($"Pressed the join lobby button with id: {lobbyId}");
            
            _snapserNetworkManager.LobbiesHandler.JoinLobby(lobbyId);
        }
        
        public void OnLeaveLobbyButtonPressed()
        {
            Debug.Log("Pressed the leave lobby button");
            _snapserNetworkManager.LobbiesHandler.LeaveLobby();
            
            leaveLobbyButton.interactable = false;
        }
        
        public void OnDeleteLobbyButtonPressed()
        {
            Debug.Log("Pressed the delete lobby button");
            _snapserNetworkManager.LobbiesHandler.DeleteLobby();
            
            deleteLobbyButton.interactable = false;
        }
        
        public void OnReadyCheckButtonPressed()
        {
            Debug.Log("Pressed the ready check button");
            _snapserNetworkManager.LobbiesHandler.ReadyCheck();
        }
        
        public void OnUpdateMetadataButtonPressed()
        {
            Debug.Log("Pressed the update metadata button");
            _snapserNetworkManager.LobbiesHandler.UpdateMetadata();
        }
        
        public void OnStartMatchButtonPressed()
        {
            Debug.Log("Pressed the start match button");
            _snapserNetworkManager.LobbiesHandler.StartMatch();
        }
    }
}