using System;
using UnityEngine;
using UnityEngine.Events;
using NoLightBelow.Core;
using NoLightBelow.Cards;

namespace NoLightBelow.Player
{
    /// <summary>
    /// Gerencia os pontos de vida (HP), invulnerabilidade temporária (i-frames) e absorção de dano do jogador.
    /// Implementa IDamageable e dispara eventos UnityEvent para a interface HUD e sistemas de feedback.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Health Configuration")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;
        [SerializeField] private float invulnerabilityDuration = 0.45f;
        [Range(0f, 1f)]
        [SerializeField] private float blockDamageReduction = 0.85f; // 85% de dano bloqueado pelo escudo

        [Header("Unity Events (UI & Game Feel)")]
        [Tooltip("Disparado quando a vida muda. Parâmetros: (vidaAtual, vidaMaxima)")]
        public UnityEvent<float, float> OnHealthChanged;

        [Tooltip("Disparado quando o jogador recebe dano efetivo na carne.")]
        public UnityEvent<DamageData> OnDamaged;

        [Tooltip("Disparado quando o dano foi defendido com sucesso pelo escudo.")]
        public UnityEvent<DamageData> OnDamageBlocked;

        [Tooltip("Disparado quando o jogador atinge 0 de vida e sucumbe.")]
        public UnityEvent OnDeath;

        private PlayerController _controller;
        private PlayerStats _stats;
        private float _lastDamageTime = -999f;
        private bool _isDead;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => _isDead;
        public Transform Transform => transform;
        public bool IsInvulnerable => Time.time < _lastDamageTime + invulnerabilityDuration;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _stats = GetComponent<PlayerStats>();

            if (_stats != null)
            {
                maxHealth = _stats.MaxHealth > 0 ? _stats.MaxHealth : maxHealth;
            }

            currentHealth = maxHealth;
        }

        private void Start()
        {
            // Dispara atualização inicial de vida para a UI
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (_stats != null)
            {
                _stats.OnHealthChanged += HandleStatsHealthChanged;
            }
        }

        private void OnDestroy()
        {
            if (_stats != null)
            {
                _stats.OnHealthChanged -= HandleStatsHealthChanged;
            }
        }

        private void HandleStatsHealthChanged(float cur, float max)
        {
            maxHealth = max;
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void TakeDamage(DamageData data)
        {
            if (_isDead || IsInvulnerable) return;

            // 1. Checagem de Defesa com Escudo (BlockState)
            bool isBlocking = _controller != null && _controller.IsBlocking;
            if (isBlocking)
            {
                // Verifica se o dano veio pela frente do escudo (arco frontal de 140 graus)
                Vector3 toSource = data.Source != null 
                    ? (data.Source.transform.position - transform.position).normalized 
                    : -data.ImpactNormal;
                toSource.y = 0f;

                float forwardDot = Vector3.Dot(transform.forward, toSource.normalized);
                if (forwardDot > 0.15f)
                {
                    // Bloqueio bem-sucedido!
                    data.IsBlocked = true;
                    float absorbed = data.Amount * blockDamageReduction;
                    data.Amount -= absorbed;

                    // Consome estamina pelo impacto
                    if (_stats != null)
                    {
                        _stats.ConsumeStamina(15f);
                    }

                    _lastDamageTime = Time.time;
                    currentHealth = Mathf.Max(1f, currentHealth - data.Amount);

                    OnHealthChanged?.Invoke(currentHealth, maxHealth);
                    OnDamageBlocked?.Invoke(data);

                    if (_controller != null)
                    {
                        _controller.TriggerShieldBlockFeedback(data);
                    }

                    return;
                }
            }

            // 2. Dano Não Bloqueado (Impacto na carne / armadura)
            _lastDamageTime = Time.time;
            currentHealth = Mathf.Max(0f, currentHealth - data.Amount);

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnDamaged?.Invoke(data);

            if (currentHealth <= 0f)
            {
                Die();
            }
            else
            {
                if (_controller != null)
                {
                    _controller.TriggerHurt(data);
                }
            }
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            TakeDamage((DamageData)damageInfo);
        }

        public void Heal(float amount)
        {
            if (_isDead || amount <= 0f) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void SetMaxHealth(float newMax, bool healDifference = true)
        {
            float diff = newMax - maxHealth;
            maxHealth = Mathf.Max(1f, newMax);

            if (healDifference && diff > 0f)
            {
                currentHealth += diff;
            }
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            Debug.Log("<color=#8B0000>[PlayerHealth]</color> O Cavaleiro sucumbiu às sombras da cripta...");
            OnDeath?.Invoke();

            if (_controller != null)
            {
                _controller.TriggerDeath();
            }
        }
    }
}
