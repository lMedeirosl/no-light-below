using System.Collections;
using UnityEngine;
using NoLightBelow.Core;
using NoLightBelow.UI;
using NoLightBelow.Player;
using NoLightBelow.Environment;

namespace NoLightBelow.Dungeon
{
    public class CardRewardChest : MonoBehaviour, IInteractable
    {
        [Header("References")]
        [SerializeField] private Transform chestLid;
        [SerializeField] private Light innerGlowLight;
        [SerializeField] private TextMesh promptText;
        [SerializeField] private float interactRadius = 2.8f;

        public string PromptMessage => "Abrir Baú de Dádivas";
        public bool CanInteract => !_isOpened;
        public Transform Transform => transform;

        public void Interact(GameObject user)
        {
            OpenChest();
        }

        private Transform _playerTransform;
        private bool _isOpened;
        private bool _isPlayerInRange;

        private void Start()
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null) _playerTransform = player.transform;

            if (promptText != null) promptText.gameObject.SetActive(false);
            if (innerGlowLight != null) innerGlowLight.intensity = 0.5f;
        }

        private void Update()
        {
            if (_isOpened || _playerTransform == null) return;

            float dist = Vector3.Distance(transform.position, _playerTransform.position);
            bool inRange = dist <= interactRadius;

            if (inRange != _isPlayerInRange)
            {
                _isPlayerInRange = inRange;
                if (promptText != null) promptText.gameObject.SetActive(inRange);
            }

            if (inRange)
            {
                if (promptText != null && Camera.main != null)
                {
                    promptText.transform.rotation = Camera.main.transform.rotation;
                }

                // Check [E] key
                bool interactPressed = false;
#if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
                {
                    interactPressed = true;
                }
#else
                if (Input.GetKeyDown(KeyCode.E))
                {
                    interactPressed = true;
                }
#endif
                if (interactPressed)
                {
                    OpenChest();
                }
            }
        }

        public void OpenChest()
        {
            if (_isOpened) return;
            _isOpened = true;

            if (promptText != null) promptText.gameObject.SetActive(false);
            StartCoroutine(OpenLidRoutine());
        }

        private IEnumerator OpenLidRoutine()
        {
            // Open Lid animation
            Quaternion startRot = chestLid != null ? chestLid.localRotation : Quaternion.identity;
            Quaternion targetRot = startRot * Quaternion.Euler(-95f, 0f, 0f);

            float elapsed = 0f;
            float duration = 0.65f;

            if (innerGlowLight != null) innerGlowLight.intensity = 3.5f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                if (chestLid != null)
                {
                    chestLid.localRotation = Quaternion.Slerp(startRot, targetRot, Mathf.SmoothStep(0f, 1f, t));
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (chestLid != null) chestLid.localRotation = targetRot;

            yield return new WaitForSeconds(0.2f);

            // Pop Card Selection
            if (CardRewardModal.Instance != null)
            {
                CardRewardModal.Instance.OpenRewardChoice();
            }
        }

        public static GameObject CreateChest(Vector3 position, Transform parent = null)
        {
            GameObject chestRoot = new GameObject("Card_Reward_Chest");
            if (parent != null) chestRoot.transform.SetParent(parent, false);
            chestRoot.transform.position = position;

            Material woodMat = DungeonMaterialFactory.CreateDummyMaterial();
            Material ironMat = DungeonMaterialFactory.CreateRustyIronMaterial();

            // Chest Base
            GameObject baseBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseBox.name = "Chest_Base";
            baseBox.transform.SetParent(chestRoot.transform, false);
            baseBox.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            baseBox.transform.localScale = new Vector3(1.3f, 0.7f, 0.9f);
            baseBox.GetComponent<Renderer>().sharedMaterial = woodMat;

            // Iron Corner Straps
            GameObject ironBand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ironBand.name = "Chest_IronBand";
            ironBand.transform.SetParent(baseBox.transform, false);
            ironBand.transform.localPosition = Vector3.zero;
            ironBand.transform.localScale = new Vector3(1.02f, 1.02f, 0.3f);
            ironBand.GetComponent<Renderer>().sharedMaterial = ironMat;
            DestroyImmediate(ironBand.GetComponent<Collider>());

            // Lid Pivot
            GameObject lidPivot = new GameObject("Chest_LidPivot");
            lidPivot.transform.SetParent(chestRoot.transform, false);
            lidPivot.transform.localPosition = new Vector3(0f, 0.7f, -0.45f);

            GameObject lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lid.name = "Chest_Lid";
            lid.transform.SetParent(lidPivot.transform, false);
            lid.transform.localPosition = new Vector3(0f, 0.15f, 0.45f);
            lid.transform.localScale = new Vector3(1.35f, 0.3f, 0.95f);
            lid.GetComponent<Renderer>().sharedMaterial = woodMat;
            DestroyImmediate(lid.GetComponent<Collider>());

            // Inner Light
            GameObject lightObj = new GameObject("Chest_InnerGlow");
            lightObj.transform.SetParent(chestRoot.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.6f, 0f);

            Light glow = lightObj.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1.0f, 0.78f, 0.28f);
            glow.intensity = 1.0f;
            glow.range = 7f;

            // Prompt Text
            GameObject textObj = new GameObject("Prompt_Text");
            textObj.transform.SetParent(chestRoot.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 1.4f, 0f);

            TextMesh tm = textObj.AddComponent<TextMesh>();
            tm.text = "[E] Abrir Baú de Dádivas";
            tm.fontSize = 36;
            tm.characterSize = 0.18f;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(1.0f, 0.88f, 0.45f);

            var comp = chestRoot.AddComponent<CardRewardChest>();
            comp.chestLid = lidPivot.transform;
            comp.innerGlowLight = glow;
            comp.promptText = tm;

            return chestRoot;
        }
    }
}
