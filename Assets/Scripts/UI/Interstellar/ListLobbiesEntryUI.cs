using System;
using Lobbies;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ListLobbiesEntryUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI lobbyName, lobbyOwner, lobbyMembersCount ;
        [SerializeField] private Button joinLobbyButton;
        
        public void Initialize(Lobby lobby, Action onJoinLobbyButtonPressed)
        {
            lobbyName.text = lobby.Name;
            lobbyOwner.text = "fixme";
            lobbyMembersCount.text = lobby.Members.Count.ToString();
            
            joinLobbyButton.onClick.AddListener(() => onJoinLobbyButtonPressed?.Invoke());
        }
    }
}