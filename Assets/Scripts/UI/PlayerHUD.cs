using UnityEngine;
using UnityEngine.UI;
using NoLightBelow.Cards;
using NoLightBelow.Player;

namespace NoLightBelow.UI
{
    public class PlayerHUD : MonoBehaviour
    {
        [Header("Bars")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Slider staminaSlider;
        [SerializeField] private Image healthFill;
        [SerializeField] private Image staminaFill;

        [Header("Texts")]
        [SerializeField] private Text healthText;
        [SerializeField] private Text staminaText;
        [SerializeField] private Text cardCountText;

        [Header("Crosshair")]
        [SerializeField] private Image crosshairImage;

        [Header("Target Player")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerHealth playerHealth;

        private void Start()
        {
            if (playerStats == null)
            {
                playerStats = FindAnyObjectByType<PlayerStats>();
            }

            if (playerHealth == null && playerStats != null)
            {
                playerHealth = playerStats.GetComponent<PlayerHealth>();
            }
            if (playerHealth == null)
            {
                playerHealth = FindAnyObjectByType<PlayerHealth>();
            }

            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged.AddListener(HandleHealthChanged);
                HandleHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }
            else if (playerStats != null)
            {
                playerStats.OnHealthChanged += HandleHealthChanged;
                HandleHealthChanged(playerStats.CurrentHealth, playerStats.MaxHealth);
            }

            if (playerStats != null)
            {
                playerStats.OnStaminaChanged += HandleStaminaChanged;
                playerStats.OnCardAcquired += HandleCardAcquired;
                HandleStaminaChanged(playerStats.CurrentStamina, playerStats.MaxStamina);
                UpdateCardCount();
            }
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged.RemoveListener(HandleHealthChanged);
            }

            if (playerStats != null)
            {
                playerStats.OnHealthChanged -= HandleHealthChanged;
                playerStats.OnStaminaChanged -= HandleStaminaChanged;
                playerStats.OnCardAcquired -= HandleCardAcquired;
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (healthSlider != null)
            {
                healthSlider.maxValue = max;
                healthSlider.value = current;
            }

            if (healthText != null)
            {
                healthText.text = $"{current:0} / {max:0}";
            }
        }

        private void HandleStaminaChanged(float current, float max)
        {
            if (staminaSlider != null)
            {
                staminaSlider.maxValue = max;
                staminaSlider.value = current;
            }

            if (staminaText != null)
            {
                staminaText.text = $"{current:0} / {max:0}";
            }
        }

        private void HandleCardAcquired(CardData card)
        {
            UpdateCardCount();
        }

        private void UpdateCardCount()
        {
            if (cardCountText != null && playerStats != null)
            {
                cardCountText.text = $"CARTAS: {playerStats.CollectedCards.Count}";
            }
        }

        public void SetReferences(Slider hpSlider, Slider stamSlider, Text hpText, Text stamText, Text cardsText, Image crosshair, PlayerStats player, PlayerHealth health = null)
        {
            healthSlider = hpSlider;
            staminaSlider = stamSlider;
            healthText = hpText;
            staminaText = stamText;
            cardCountText = cardsText;
            crosshairImage = crosshair;
            playerStats = player;
            playerHealth = health != null ? health : (player != null ? player.GetComponent<PlayerHealth>() : null);
        }
    }
}
