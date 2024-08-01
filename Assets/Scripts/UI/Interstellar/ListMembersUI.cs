using System.Collections.Generic;
using Lobbies;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ListMembersUI : MonoBehaviour
    {
        [SerializeField] private ListMembersEntryUI listMembersEntryUI;

        private readonly Dictionary<string, ListMembersEntryUI> _entryUis = new();

        public void AddMemberEntry(Member member, System.Action<Member> onJoinMemberButtonPressed)
        {
            if (!_entryUis.ContainsKey(member.Id))
            {
                ListMembersEntryUI entryUI = Instantiate(listMembersEntryUI, transform);
                _entryUis.Add(member.Id, entryUI);
                entryUI.Initialize(member, () => onJoinMemberButtonPressed?.Invoke(member));
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
            }
        }
        
        public void ClearMemberEntries()
        {
            foreach (ListMembersEntryUI entryUI in _entryUis.Values)
            {
                DestroyImmediate(entryUI.gameObject);
            }
            _entryUis.Clear();
        }
    }
}