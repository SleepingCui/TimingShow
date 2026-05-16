using UnityEngine;
using UnityEngine.UI;

namespace TimingShow
{
    public class TextUI : MonoBehaviour
    {
        public Canvas canvas;
        public RectTransform rootRect;
        public GameObject textObject;
        public Text text;
        public Shadow shadow;

        private void Awake()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10001;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            rootRect = gameObject.GetComponent<RectTransform>();
            textObject = new GameObject("TextComponent");
            textObject.transform.SetParent(transform,false);

            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(1000f, 200f);

            text = textObject.AddComponent<Text>();
            text.raycastTarget = false;
            text.supportRichText = true;
            text.font =RDString.GetFontDataForLanguage(RDString.language).font;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            shadow = textObject.AddComponent<Shadow>();
            shadow.effectColor =new Color(0f,0f,0f,0.45f);
            shadow.effectDistance = new Vector2(2f, -2f);
        }

        public void SetText(string value)
        {
            if (text != null)
                text.text = value;
        }

        public void SetSize(int size)
        {
            if (text != null)
                text.fontSize = size;
        }

        public void SetPosition(float x, float y)
        {
            if (textObject == null) return;
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(x * Screen.width,y * Screen.height);
        }

        public TextAnchor ToAlign(int align)
        {
            switch (align)
            {
                case 0: return TextAnchor.MiddleLeft;
                case 1: return TextAnchor.MiddleCenter;
                case 2: return TextAnchor.MiddleRight;
                default:return TextAnchor.MiddleCenter;
            }
        }
    }
}