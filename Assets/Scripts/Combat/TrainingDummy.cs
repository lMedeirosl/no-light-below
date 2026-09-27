using System.Collections;
using UnityEngine;
using NoLightBelow.Core;
using NoLightBelow.Cards;

namespace NoLightBelow.Combat
{
    public class TrainingDummy : MonoBehaviour, IDamageable
    {
        [Header("Stats")]
        [SerializeField] private float maxHealth = 150f;
        [SerializeField] private float currentHealth;
        [SerializeField] private float respawnTime = 2.5f;

        [Header("Physical Wobble Reaction")]
        [SerializeField] private Transform dummyMeshTransform;
        [SerializeField] private float wobbleAmount = 25f;
        [SerializeField] private float wobbleSpeed = 16f;

        [Header("Visual Feedback")]
        [SerializeField] private Renderer meshRenderer;
        [SerializeField] private Color hitFlashColor = new Color(0.9f, 0.2f, 0.2f);

        private Color _originalColor;
        private Quaternion _originalMeshRotation;
        private float _currentWobble;
        private Vector3 _wobbleAxis;
        private bool _isDead;

        public bool IsDead => _isDead;
        public Transform Transform => transform;

        public static event System.Action OnDummyDefeated;

        private void Awake()
        {
            currentHealth = maxHealth;
            if (dummyMeshTransform != null)
            {
                _originalMeshRotation = dummyMeshTransform.localRotation;
            }
            if (meshRenderer != null && meshRenderer.material.HasProperty("_BaseColor"))
            {
                _originalColor = meshRenderer.material.color;
            }
        }

        public void SetReferences(Transform meshT, Renderer r)
        {
            dummyMeshTransform = meshT;
            meshRenderer = r;
            if (dummyMeshTransform != null) _originalMeshRotation = dummyMeshTransform.localRotation;
            if (meshRenderer != null && meshRenderer.sharedMaterial != null) _originalColor = meshRenderer.sharedMaterial.color;
        }

        private void Update()
        {
            if (_currentWobble > 0.01f && dummyMeshTransform != null)
            {
                _currentWobble = Mathf.Lerp(_currentWobble, 0f, Time.deltaTime * wobbleSpeed);
                float angle = Mathf.Sin(Time.time * wobbleSpeed) * _currentWobble;
                dummyMeshTransform.localRotation = _originalMeshRotation * Quaternion.AngleAxis(angle, _wobbleAxis);
            }
            else if (dummyMeshTransform != null)
            {
                dummyMeshTransform.localRotation = _originalMeshRotation;
            }
        }

        public void TakeDamage(DamageData data)
        {
            TakeDamage((DamageInfo)data);
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (_isDead) return;

            currentHealth -= damageInfo.Amount;
            Debug.Log($"<color=#FF6347>[Boneco de Treino]</color> Sofreu <b>{damageInfo.Amount:0.#}</b> de dano! (Tipo: {damageInfo.Type}, Crítico: {damageInfo.IsCritical})");

            // Wobble in direction of hit
            _wobbleAxis = Vector3.Cross(damageInfo.Direction, Vector3.up).normalized;
            if (_wobbleAxis == Vector3.zero) _wobbleAxis = Vector3.right;
            _currentWobble = wobbleAmount * (damageInfo.IsCritical ? 1.6f : 1.0f);

            // Flash material
            StartCoroutine(FlashRoutine());

            // Spawn floating damage text
            SpawnDamageText(damageInfo);

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        private void SpawnDamageText(DamageInfo info)
        {
            GameObject textObj = new GameObject("DamageText");
            textObj.transform.position = transform.position + Vector3.up * 1.8f + Random.insideUnitSphere * 0.3f;

            var textMesh = textObj.AddComponent<TextMesh>();
            textMesh.text = info.IsCritical ? $"{info.Amount:0}!" : $"{info.Amount:0}";
            textMesh.characterSize = info.IsCritical ? 0.28f : 0.2f;
            textMesh.fontSize = 48;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.color = info.IsCritical ? new Color(1f, 0.85f, 0.1f) : Color.white;

            textObj.AddComponent<FloatingDamageNumber>();
        }

        private IEnumerator FlashRoutine()
        {
            if (meshRenderer != null)
            {
                meshRenderer.material.color = hitFlashColor;
                yield return new WaitForSeconds(0.12f);
                meshRenderer.material.color = _originalColor;
            }
        }

        private void Die()
        {
            _isDead = true;
            Debug.Log("<color=#32CD32>[Recompensa]</color> Boneco destruído! Invocando escolha de cartas!");
            OnDummyDefeated?.Invoke();

            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            if (meshRenderer != null) meshRenderer.enabled = false;
            yield return new WaitForSeconds(respawnTime);

            currentHealth = maxHealth;
            _isDead = false;
            if (meshRenderer != null) meshRenderer.enabled = true;
        }
    }

    public class FloatingDamageNumber : MonoBehaviour
    {
        private float _lifetime = 0.9f;
        private float _elapsed;
        private Vector3 _velocity;
        private TextMesh _textMesh;

        private void Awake()
        {
            _velocity = new Vector3(Random.Range(-0.8f, 0.8f), 2.2f, Random.Range(-0.8f, 0.8f));
            _textMesh = GetComponent<TextMesh>();
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            transform.position += _velocity * Time.deltaTime;
            _velocity.y = Mathf.Lerp(_velocity.y, 0.5f, Time.deltaTime * 3f);

            // Face camera
            if (Camera.main != null)
            {
                transform.rotation = Camera.main.transform.rotation;
            }

            if (_textMesh != null)
            {
                Color c = _textMesh.color;
                c.a = Mathf.Clamp01(1f - (_elapsed / _lifetime));
                _textMesh.color = c;
            }

            if (_elapsed >= _lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
