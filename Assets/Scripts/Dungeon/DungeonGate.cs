using System.Collections;
using UnityEngine;
using NoLightBelow.Player;

namespace NoLightBelow.Dungeon
{
    public class DungeonGate : MonoBehaviour
    {
        [Header("Gate Settings")]
        [SerializeField] private Transform gateMovingBars;
        [SerializeField] private float closedY = 0f;
        [SerializeField] private float openY = 3.6f;
        [SerializeField] private float moveSpeed = 6f;

        private bool _isOpen = true;
        private Coroutine _moveRoutine;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            if (gateMovingBars == null) gateMovingBars = transform;
        }

        public void Initialize(Transform movingBars, float cY, float oY)
        {
            gateMovingBars = movingBars;
            closedY = cY;
            openY = oY;
        }

        public void OpenGate()
        {
            _isOpen = true;
            if (_moveRoutine != null) StopCoroutine(_moveRoutine);
            _moveRoutine = StartCoroutine(MoveGateRoutine(openY, false));
        }

        public void CloseGate()
        {
            _isOpen = false;
            if (_moveRoutine != null) StopCoroutine(_moveRoutine);
            _moveRoutine = StartCoroutine(MoveGateRoutine(closedY, true));
        }

        private IEnumerator MoveGateRoutine(float targetY, bool isSlammingDown)
        {
            float speed = isSlammingDown ? moveSpeed * 2.2f : moveSpeed;
            Vector3 pos = gateMovingBars.localPosition;

            while (Mathf.Abs(gateMovingBars.localPosition.y - targetY) > 0.02f)
            {
                pos.y = Mathf.MoveTowards(pos.y, targetY, speed * Time.deltaTime);
                gateMovingBars.localPosition = pos;
                yield return null;
            }

            pos.y = targetY;
            gateMovingBars.localPosition = pos;

            // Slam impact screenshake if closing
            if (isSlammingDown && ThirdPersonCameraController.Instance != null)
            {
                ThirdPersonCameraController.Instance.AddShake(0.22f);
            }
        }
    }
}
