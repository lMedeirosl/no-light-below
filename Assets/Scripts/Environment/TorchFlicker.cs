using UnityEngine;

namespace NoLightBelow.Environment
{
    [RequireComponent(typeof(Light))]
    public class TorchFlicker : MonoBehaviour
    {
        [Header("Flicker Settings")]
        [SerializeField] private float minIntensity = 1.8f;
        [SerializeField] private float maxIntensity = 3.2f;
        [SerializeField] private float flickerSpeed = 12f;
        [SerializeField] private float positionJitter = 0.04f;

        private Light _light;
        private Vector3 _basePosition;
        private float _randomOffset;

        private void Awake()
        {
            _light = GetComponent<Light>();
            _basePosition = transform.localPosition;
            _randomOffset = Random.Range(0f, 100f);
        }

        private void Update()
        {
            if (_light == null) return;

            float noise = Mathf.PerlinNoise((Time.time * flickerSpeed) + _randomOffset, 0f);
            _light.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);

            float jitterX = (Mathf.PerlinNoise(Time.time * 8f + _randomOffset, 0f) - 0.5f) * positionJitter;
            float jitterY = (Mathf.PerlinNoise(0f, Time.time * 8f + _randomOffset) - 0.5f) * positionJitter;
            transform.localPosition = _basePosition + new Vector3(jitterX, jitterY, 0f);
        }
    }
}
