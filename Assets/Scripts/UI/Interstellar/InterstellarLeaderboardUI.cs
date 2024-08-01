using System;
using System.Collections.Generic;
using System.Globalization;
using Snapser.Model;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

namespace UI
{
    public class InterstellarLeaderboardUI : MonoBehaviour
    {
        [SerializeField] private LeaderboardEntryUI leaderboardEntryUI;

        private readonly Dictionary<string, LeaderboardEntryUI> _entryUis = new Dictionary<string, LeaderboardEntryUI>();


        private void Start()
        {
            Spaceship.OnPlayerWon += OnPlayerWon;
            Spaceship.OnPlayerEliminated += GetLeaderboard;
        }

        private void OnDestroy()
        {
            Spaceship.OnPlayerWon -= OnPlayerWon;
            Spaceship.OnPlayerEliminated -= GetLeaderboard;
        }

        #region UI

        private void AddLeaderboardEntry(List<LeaderboardsUserScore> entries)
        {
            foreach (LeaderboardsUserScore entry in entries)
            {
                if (!_entryUis.ContainsKey(entry.UserId))
                {
                    LeaderboardEntryUI entryUI = Instantiate(leaderboardEntryUI, transform);
                    _entryUis.Add(entry.UserId, entryUI);
                    entryUI.Initialize(entry.Rank.ToString(), entry.UserId, entry.Score.ToString(CultureInfo.InvariantCulture));
                    LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
                }
            }
        }

        private void ResetLeaderboardUI()
        {
            foreach (LeaderboardEntryUI entryUI in _entryUis.Values)
            {
                DestroyImmediate(entryUI.gameObject);
            }
            _entryUis.Clear();
        }

        #endregion

        #region Snapser Calls & Handlers
        
        private void OnPlayerWon(string userName, bool isLocalPlayer)
        {
            if (isLocalPlayer)
                SnapserManager.Instance.IncrementLeaderboardScoreAsync(GameConstants.InterstellarLeaderboardName, 1, OnSetLeaderboardScoreCallComplete);
            else
                GetLeaderboard();
        }

        void OnSetLeaderboardScoreCallComplete(SnapserServiceResponse response)
        {
            if (response.Success)
                GetLeaderboard();
        }

        private void GetLeaderboard(string userName, bool isLocalPlayer)
        {
            if (isLocalPlayer)
                GetLeaderboard();
        }

        private void GetLeaderboard()
        {
            ResetLeaderboardUI();

            if (SnapserManager.Instance.HasLeaderboardsEnabled)
            {
                SnapserManager.Instance.GetLeaderboardAsync(GameConstants.InterstellarLeaderboardName, GameConstants.LeaderboardTopRange, GameConstants.LeaderboardEntryCount, OnGetLeaderboardCallComplete);
            }
        }

        void OnGetLeaderboardCallComplete(SnapserServiceResponse response)
        {
            if (response.Success && response.Data is List<LeaderboardsUserScore> leaderboardScores)
            {
                AddLeaderboardEntry(leaderboardScores);
            }
        }

        #endregion
    }
}