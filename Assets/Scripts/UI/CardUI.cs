using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using NoLightBelow.Cards;

namespace NoLightBelow.UI
{
    public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("UI Elements")]
        [SerializeField] private Image cardFrame;
        [SerializeField] private Image cardBackground;
        [SerializeField] private Image artworkImage;
        [SerializeField] private Text titleText;
        [SerializeField] private Text categoryText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text loreText;

        [Header("3D Tilt Effect")]
        [SerializeField] private float maxTiltAngle = 14f;
        [SerializeField] private float tiltSpeed = 15f;
        [SerializeField] private float hoverScale = 1.08f;

        private CardData _data;
        private RectTransform _rectTransform;
        private Vector3 _originalScale;
        private bool _isHovered;
        private System.Action<CardData> _onSelectedCallback;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _originalScale = transform.localScale;
        }

        public void Bind(CardData data, System.Action<CardData> onSelected)
        {
            _data = data;
            _onSelectedCallback = onSelected;

            if (data == null) return;

            if (titleText != null) titleText.text = data.CardTitle;
            if (categoryText != null) categoryText.text = data.Category.ToString().ToUpper();
            if (descriptionText != null) descriptionText.text = data.BuildFormattedDescription();
            if (loreText != null) loreText.text = $"\"{data.LoreQuote}\"";

            Color rarityCol = data.GetRarityColor();
            if (cardFrame != null) cardFrame.color = rarityCol;
            if (titleText != null) titleText.color = rarityCol;

            if (artworkImage != null && data.Artwork != null)
            {
                artworkImage.sprite = data.Artwork;
                artworkImage.color = Color.white;
            }
        }

        private void Update()
        {
            if (_isHovered)
            {
                Vector2 mouseScreenPos = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Mouse.current != null)
                {
                    mouseScreenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                }
#else
                mouseScreenPos = Input.mousePosition;
#endif
                // Calculate mouse position relative to card center for 3D tilt
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rectTransform, 
                    mouseScreenPos, 
                    null, 
                    out Vector2 localPoint
                );

                Vector2 normalized = new Vector2(
                    localPoint.x / (_rectTransform.rect.width * 0.5f),
                    localPoint.y / (_rectTransform.rect.height * 0.5f)
                );
                normalized.x = Mathf.Clamp(normalized.x, -1f, 1f);
                normalized.y = Mathf.Clamp(normalized.y, -1f, 1f);

                // Tilt: mouse to right tilts card right (yaw), mouse up tilts card back (pitch)
                Quaternion targetRot = Quaternion.Euler(-normalized.y * maxTiltAngle, normalized.x * maxTiltAngle, 0f);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.unscaledDeltaTime * tiltSpeed);
                transform.localScale = Vector3.Lerp(transform.localScale, _originalScale * hoverScale, Time.unscaledDeltaTime * tiltSpeed);
            }
            else
            {
                transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.identity, Time.unscaledDeltaTime * tiltSpeed);
                transform.localScale = Vector3.Lerp(transform.localScale, _originalScale, Time.unscaledDeltaTime * tiltSpeed);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_data != null)
            {
                _onSelectedCallback?.Invoke(_data);
            }
        }

        public void SetReferences(Image frame, Image bg, Image art, Text title, Text cat, Text desc, Text lore)
        {
            cardFrame = frame;
            cardBackground = bg;
            artworkImage = art;
            titleText = title;
            categoryText = cat;
            descriptionText = desc;
            loreText = lore;
        }
    }
}
