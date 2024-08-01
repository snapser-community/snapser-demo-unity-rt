using System;
using System.ComponentModel.DataAnnotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class PlayerUI : MonoBehaviour
    {
        [SerializeField, Required] private Image image;
        [SerializeField, Required] private TextMeshProUGUI label;
        [SerializeField, Required] private GameObject eliminationAnimation, victoryEffect, victoryCrown;
        [SerializeField, Required] private Sprite eliminatedIcon;

        private GameObject victoryEffectInstance;
        
        private void Start()
        {
            victoryCrown.SetActive(false);
        }

        private void OnDestroy()
        {
            Destroy(victoryEffectInstance);
        }

        public void SetPlayerColor(Color uColor)
        {
            image.color = uColor;
        }

        public void SetPlayerName(string uName)
        {
            label.text = uName;
        }

        public void OnPlayerEliminated()
        {
            Instantiate(eliminationAnimation, image.transform.position, Quaternion.identity);
            image.color = Color.red;
            image.sprite = eliminatedIcon;
        }

        public void OnPlayerVictory()
        {
            victoryEffectInstance = Instantiate(victoryEffect, image.transform.position, Quaternion.identity);
            victoryCrown.SetActive(true);
        }

    }
}