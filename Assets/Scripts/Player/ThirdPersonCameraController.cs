using UnityEngine;
using NoLightBelow.Core;

namespace NoLightBelow.Player
{
    public class ThirdPersonCameraController : MonoBehaviour
    {
        [Header("Target & Offsets")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 shoulderOffset = new Vector3(0.5f, 1.4f, 0f); // Slight right shoulder over-the-shoulder
        [SerializeField] private float defaultDistance = 3.6f;
        [SerializeField] private float minDistance = 0.8f;

        [Header("Rotation Settings")]
        [SerializeField] private float mouseSensitivityX = 2.5f;
        [SerializeField] private float mouseSensitivityY = 2.2f;
        [SerializeField] private float minPitch = -35f;
        [SerializeField] private float maxPitch = 70f;
        [SerializeField] private bool invertY = false;

        [Header("Wall Collision")]
        [SerializeField] private LayerMask collisionLayers = ~0; // Everything except player/triggers
        [SerializeField] private float cameraCollisionRadius = 0.25f;
        [SerializeField] private float collisionDamping = 12f;

        [Header("Screen Shake")]
        [SerializeField] private float shakeTraumaDecay = 1.5f;

        private float _yaw;
        private float _pitch = 15f;
        private float _currentDistance;
        private float _shakeTrauma;
        private Vector3 _smoothVelocity;

        public static ThirdPersonCameraController Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            _currentDistance = defaultDistance;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Ensure strictly one AudioListener in the scene
            AudioListener[] listeners = FindObjectsByType<AudioListener>();
            if (listeners.Length > 1)
            {
                AudioListener myListener = GetComponent<AudioListener>();
                for (int i = 0; i < listeners.Length; i++)
                {
                    if (myListener != null && listeners[i] != myListener)
                    {
                        Destroy(listeners[i]);
                    }
                    else if (myListener == null && i > 0)
                    {
                        Destroy(listeners[i]);
                    }
                }
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                _yaw = target.eulerAngles.y;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            HandleInput();
            HandleCollisionAndPosition();
            HandleScreenShake();
        }

        private void HandleInput()
        {
            // Toggle cursor lock with Escape
            if (InputBridge.IsEscapeDown())
            {
                Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
            }

            // Click inside game window to re-lock cursor
            if (Cursor.lockState != CursorLockMode.Locked && InputBridge.IsAttackDown())
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (Cursor.lockState != CursorLockMode.Locked) return;

            Vector2 look = InputBridge.GetLookDelta();
            float mouseX = look.x * mouseSensitivityX;
            float mouseY = look.y * mouseSensitivityY * (invertY ? -1f : 1f);

            _yaw += mouseX;
            _pitch = Mathf.Clamp(_pitch - mouseY, minPitch, maxPitch);
        }

        private void HandleCollisionAndPosition()
        {
            Quaternion cameraRotation = Quaternion.Euler(_pitch, _yaw, 0f);

            // Pivot point is at character center + shoulder offset rotated
            Vector3 pivot = target.position + (cameraRotation * shoulderOffset);
            Vector3 desiredDirection = - (cameraRotation * Vector3.forward);
            float targetDist = defaultDistance;

            // SphereCast to avoid clipping dungeon walls/pillars
            if (Physics.SphereCast(pivot, cameraCollisionRadius, desiredDirection, out RaycastHit hit, defaultDistance, collisionLayers, QueryTriggerInteraction.Ignore))
            {
                targetDist = Mathf.Clamp(hit.distance - 0.1f, minDistance, defaultDistance);
            }

            _currentDistance = Mathf.Lerp(_currentDistance, targetDist, Time.deltaTime * collisionDamping);

            Vector3 finalPosition = pivot + (desiredDirection * _currentDistance);

            transform.position = finalPosition;
            transform.rotation = cameraRotation;
        }

        private void HandleScreenShake()
        {
            if (_shakeTrauma > 0f)
            {
                float shakeMagnitude = _shakeTrauma * _shakeTrauma * 0.18f;
                Vector3 shakeOffset = new Vector3(
                    (Mathf.PerlinNoise(Time.time * 30f, 0f) - 0.5f) * 2f * shakeMagnitude,
                    (Mathf.PerlinNoise(0f, Time.time * 30f) - 0.5f) * 2f * shakeMagnitude,
                    0f
                );
                transform.position += transform.rotation * shakeOffset;

                _shakeTrauma = Mathf.Clamp01(_shakeTrauma - Time.deltaTime * shakeTraumaDecay);
            }
        }

        public void AddShake(float trauma)
        {
            _shakeTrauma = Mathf.Clamp01(_shakeTrauma + trauma);
        }

        public Vector3 GetForwardFlat()
        {
            Vector3 fwd = transform.forward;
            fwd.y = 0f;
            return fwd.normalized;
        }

        public Vector3 GetRightFlat()
        {
            Vector3 right = transform.right;
            right.y = 0f;
            return right.normalized;
        }
    }
}
