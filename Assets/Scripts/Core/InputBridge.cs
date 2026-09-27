using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace NoLightBelow.Core
{
    public static class InputBridge
    {
        public static Vector2 GetMoveVector()
        {
            Vector2 move = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;
            }

            var pad = Gamepad.current;
            if (pad != null)
            {
                Vector2 stick = pad.leftStick.ReadValue();
                if (stick.sqrMagnitude > move.sqrMagnitude) move = stick;
            }

            return move.sqrMagnitude > 1f ? move.normalized : move;
#else
            try
            {
                move.x = Input.GetAxisRaw("Horizontal");
                move.y = Input.GetAxisRaw("Vertical");
            }
            catch { }
            return move.sqrMagnitude > 1f ? move.normalized : move;
#endif
        }

        public static Vector2 GetLookDelta()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 delta = mouse.delta.ReadValue();
                return delta * 0.08f;
            }

            var pad = Gamepad.current;
            if (pad != null)
            {
                return pad.rightStick.ReadValue() * 2.5f;
            }

            return Vector2.zero;
#else
            try
            {
                return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            }
            catch
            {
                return Vector2.zero;
            }
#endif
        }

        public static bool IsSprinting()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed)) return true;

            var pad = Gamepad.current;
            if (pad != null && pad.leftStickButton.isPressed) return true;

            return false;
#else
            try
            {
                return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            }
            catch
            {
                return false;
            }
#endif
        }

        public static bool IsJumpDown()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame) return true;

            var pad = Gamepad.current;
            if (pad != null && pad.buttonSouth.wasPressedThisFrame) return true;

            return false;
#else
            try
            {
                return Input.GetKeyDown(KeyCode.Space);
            }
            catch
            {
                return false;
            }
#endif
        }

        public static bool IsDodgeDown()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.leftAltKey.wasPressedThisFrame || kb.leftCtrlKey.wasPressedThisFrame || kb.fKey.wasPressedThisFrame)) return true;

            var pad = Gamepad.current;
            if (pad != null && pad.buttonEast.wasPressedThisFrame) return true;

            return false;
#else
            try
            {
                return Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.F);
            }
            catch
            {
                return false;
            }
#endif
        }

        public static bool IsAttackDown()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;

            var pad = Gamepad.current;
            if (pad != null && (pad.rightTrigger.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame)) return true;

            return false;
#else
            try
            {
                return Input.GetMouseButtonDown(0);
            }
            catch
            {
                return false;
            }
#endif
        }

        public static bool IsBlockHeld()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed) return true;

            var pad = Gamepad.current;
            if (pad != null && pad.leftTrigger.isPressed) return true;

            return false;
#else
            try
            {
                return Input.GetMouseButton(1);
            }
            catch
            {
                return false;
            }
#endif
        }

        public static bool IsRangedDown()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.wasPressedThisFrame) return true;

            return false;
#else
            try
            {
                return Input.GetMouseButtonDown(1);
            }
            catch
            {
                return false;
            }
#endif
        }

        public static bool IsCardMenuDown()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.cKey.wasPressedThisFrame) return true;

            return false;
#else
            try
            {
                return Input.GetKeyDown(KeyCode.C);
            }
            catch
            {
                return false;
            }
#endif
        }

        public static bool IsEscapeDown()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;

            var pad = Gamepad.current;
            if (pad != null && pad.startButton.wasPressedThisFrame) return true;

            return false;
#else
            try
            {
                return Input.GetKeyDown(KeyCode.Escape);
            }
            catch
            {
                return false;
            }
#endif
        }
    }
}
