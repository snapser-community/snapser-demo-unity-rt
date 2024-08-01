using System.Collections.Generic;
using Lobbies;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ListLobbiesUI : MonoBehaviour
    {
        [SerializeField] private ListLobbiesEntryUI listLobbiesEntryUI;

        private readonly Dictionary<string, ListLobbiesEntryUI> _entryUis = new();

        public void AddLobbyEntry(Lobby lobby, System.Action<Lobby> onJoinLobbyButtonPressed)
        {
            if (!_entryUis.ContainsKey(lobby.Id))
            {
                ListLobbiesEntryUI entryUI = Instantiate(listLobbiesEntryUI, transform);
                _entryUis.Add(lobby.Id, entryUI);
                entryUI.Initialize(lobby, () => onJoinLobbyButtonPressed?.Invoke(lobby));
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
            }
        }
        
        public void ClearLobbyEntries()
        {
            foreach (ListLobbiesEntryUI entryUI in _entryUis.Values)
            {
                DestroyImmediate(entryUI.gameObject);
            }
            _entryUis.Clear();
        }
    }
}