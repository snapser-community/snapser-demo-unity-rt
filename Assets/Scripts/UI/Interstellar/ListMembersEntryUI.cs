using Lobbies;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ListMembersEntryUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI memberName;
        [SerializeField] private Button joinMemberButton;
        
        public void Initialize(Member member, System.Action onJoinMemberButtonPressed)
        {
            memberName.text = member.Metadata.Fields["username"].ToString();
            joinMemberButton.onClick.AddListener(() => onJoinMemberButtonPressed?.Invoke());
        }
    }
}