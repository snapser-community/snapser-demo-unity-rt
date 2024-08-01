using TMPro;
using UnityEngine;
using Utilities;

namespace UI
{
    public class LeaderboardEntryUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI playerRank, playerName, playerScore;
        
        public void Initialize(string pRank, string pName, string pScore)
        {
            playerRank.text = pRank;
            playerName.text = "Snapship Player";
            playerScore.text = pScore;
            
            SnapserManager.Instance.GetUserProfileAsync(OnGetUserProfileCallComplete, pName);
        }

        void OnGetUserProfileCallComplete(SnapserServiceResponse response)
        {
            if (response.Success && response.Data is SnapserProfile profile && !profile.gamertag.IsEmptyOrNull())
                playerName.text = profile.gamertag;
        }
        
    }
}