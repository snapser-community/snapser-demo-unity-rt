using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class PlayersUI : MonoBehaviour
    {
        [SerializeField, Required] private PlayerUI playerUI;
        [SerializeField, Required] private RectTransform rectTransform;
        [SerializeField, Required] private GameObject playersUILineSeparatorGameObject;

        private Dictionary<string, PlayerUI> playerUiDictionary = new Dictionary<string, PlayerUI>();
        
        private void Start()
        {
            Spaceship.OnUpdatePlayerUI += OnUpdatePlayerUI;
            Spaceship.OnPlayerEliminated += OnPlayerEliminated;
            Spaceship.OnPlayerWon += OnPlayerWon;
        }

        private void OnDestroy()
        {
            Spaceship.OnUpdatePlayerUI -= OnUpdatePlayerUI;
            Spaceship.OnPlayerEliminated -= OnPlayerEliminated;
            Spaceship.OnPlayerWon -= OnPlayerWon;
        }

        private void OnPlayerWon(string uName, bool isLocalPlayer)
        {
            if (playerUiDictionary.ContainsKey(uName))
            {
                playerUiDictionary[uName].OnPlayerVictory();
            }
        }

        private void OnPlayerEliminated(string uName, bool isLocalPlayer)
        {
            if (playerUiDictionary.ContainsKey(uName))
            {
                playerUiDictionary[uName].OnPlayerEliminated();
            }
        }

        private void OnUpdatePlayerUI(string uName, Color uColor)
        {
            PlayerUI ui;
            if (!playerUiDictionary.ContainsKey(uName))
            {
                ui = Instantiate(playerUI, rectTransform);
                playerUiDictionary.Add(uName, ui);
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                
                if(playerUiDictionary.Count > 0)
                    playersUILineSeparatorGameObject.SetActive(true);
            }
            else
            {
                ui = playerUiDictionary[uName];
            }
            
            ui.SetPlayerColor(uColor);
            ui.SetPlayerName(uName);
        }

        public void DestroyAllPlayerUis()
        {
            foreach (PlayerUI ui in playerUiDictionary.Values)
            {
                Destroy(ui.gameObject);
            }
            playerUiDictionary.Clear();
        }
    }
}