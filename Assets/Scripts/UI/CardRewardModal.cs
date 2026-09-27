using System.Collections.Generic;
using UnityEngine;
using NoLightBelow.Cards;
using NoLightBelow.Combat;
using NoLightBelow.Core;

namespace NoLightBelow.UI
{
    public class CardRewardModal : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject modalRoot;
        [SerializeField] private Transform cardsContainer;
        [SerializeField] private CardUI cardPrefab;
        [SerializeField] private PlayerStats targetPlayerStats;

        [Header("Card Database")]
        [SerializeField] private List<CardData> cardPool = new List<CardData>();

        private bool _isOpen;
        private readonly List<CardUI> _spawnedCards = new List<CardUI>();

        public static CardRewardModal Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }
        }

        private void Start()
        {
            if (targetPlayerStats == null)
            {
                targetPlayerStats = FindAnyObjectByType<PlayerStats>();
            }

            TrainingDummy.OnDummyDefeated += HandleDummyDefeated;
        }

        private void OnDestroy()
        {
            TrainingDummy.OnDummyDefeated -= HandleDummyDefeated;
        }

        private void Update()
        {
            // Debug shortcut: press 'C' to open card reward choice
            if (InputBridge.IsCardMenuDown() && !_isOpen)
            {
                OpenRewardChoice();
            }
        }

        private void HandleDummyDefeated()
        {
            OpenRewardChoice();
        }

        public void OpenRewardChoice()
        {
            if (_isOpen) return;
            _isOpen = true;

            if (modalRoot != null)
            {
                modalRoot.SetActive(true);
            }

            // Unlock cursor and slow down game slightly
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0.05f;

            PopulateCards();
        }

        private void PopulateCards()
        {
            foreach (var card in _spawnedCards)
            {
                if (card != null) Destroy(card.gameObject);
            }
            _spawnedCards.Clear();

            List<CardData> selectedDraft = DrawRandomCards(3);

            foreach (var cardData in selectedDraft)
            {
                if (cardPrefab != null && cardsContainer != null)
                {
                    CardUI cardUI = Instantiate(cardPrefab, cardsContainer);
                    cardUI.Bind(cardData, OnCardSelected);
                    _spawnedCards.Add(cardUI);
                }
            }
        }

        private List<CardData> DrawRandomCards(int count)
        {
            var result = new List<CardData>();
            if (cardPool.Count == 0)
            {
                // Fallback procedural sample cards if pool empty
                return GenerateSampleCards();
            }

            List<CardData> poolCopy = new List<CardData>(cardPool);
            for (int i = 0; i < count && poolCopy.Count > 0; i++)
            {
                int idx = Random.Range(0, poolCopy.Count);
                result.Add(poolCopy[idx]);
                poolCopy.RemoveAt(idx);
            }

            return result;
        }

        private List<CardData> GenerateSampleCards()
        {
            var list = new List<CardData>();

            var c1 = ScriptableObject.CreateInstance<CardData>();
            c1.CardTitle = "Lâmina Enferrujada";
            c1.Rarity = CardRarity.Common;
            c1.Category = CardCategory.Offensive;
            c1.LoreQuote = "O sangue de outros ainda corrói o ferro batido.";
            c1.Modifiers.Add(new StatModifier { TargetStat = StatType.AttackPower, PercentMultiplier = 0.20f });
            c1.TriggersBleedOnHit = true;
            c1.BleedChance = 0.25f;
            list.Add(c1);

            var c2 = ScriptableObject.CreateInstance<CardData>();
            c2.CardTitle = "Passo do Espectro";
            c2.Rarity = CardRarity.Rare;
            c2.Category = CardCategory.Agility;
            c2.LoreQuote = "A dungeon não pode segurar aquilo que não tem peso.";
            c2.Modifiers.Add(new StatModifier { TargetStat = StatType.MoveSpeed, PercentMultiplier = 0.15f });
            c2.Modifiers.Add(new StatModifier { TargetStat = StatType.AttackSpeed, PercentMultiplier = 0.25f });
            c2.ShockwaveOnDodge = true;
            list.Add(c2);

            var c3 = ScriptableObject.CreateInstance<CardData>();
            c3.CardTitle = "Pacto da Cripta";
            c3.Rarity = CardRarity.Cursed;
            c3.Category = CardCategory.Occult;
            c3.LoreQuote = "Toda ferida se fecha se você sangrar o que estiver à sua frente.";
            c3.Modifiers.Add(new StatModifier { TargetStat = StatType.AttackPower, PercentMultiplier = 0.40f });
            c3.Modifiers.Add(new StatModifier { TargetStat = StatType.MaxHealth, PercentMultiplier = -0.20f });
            c3.TriggersLifestealOnHit = true;
            c3.LifestealPercent = 0.12f;
            list.Add(c3);

            return list;
        }

        private void OnCardSelected(CardData selectedCard)
        {
            if (targetPlayerStats != null)
            {
                targetPlayerStats.AddCard(selectedCard);
            }

            CloseModal();
        }

        public void CloseModal()
        {
            _isOpen = false;
            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1.0f;
        }

        public void SetReferences(GameObject root, Transform container, CardUI prefab, PlayerStats player, List<CardData> pool)
        {
            modalRoot = root;
            cardsContainer = container;
            cardPrefab = prefab;
            targetPlayerStats = player;
            cardPool = pool;
        }
    }
}
