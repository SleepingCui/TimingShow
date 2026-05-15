//全是问题

using BlendModes;
using UnityEngine;
using UnityEngine.UI;

namespace TimingShow
{
    public class TextUI : MonoBehaviour
    {
        public GameObject TextObject;
        public Text text;
        public Shadow shadowText;
        public RectTransform rectTransform;

        public void SetSize(int size)
        {
            if (this.text != null)
                this.text.fontSize = size;
        }

        public void SetText(string textStr)
        {
            if (this.text != null)
                this.text.text = textStr;
        }

        public void SetPosition(float x, float y)
        {
            if (this.rectTransform != null)
            {
                Vector2 vector = new Vector2(x + 0.5f, y - 0.5f);
                this.rectTransform.anchorMin = vector;
                this.rectTransform.anchorMax = vector;
                this.rectTransform.pivot = vector;
            }
        }

        public TextAnchor ToAlign(int align)
        {
            if (align == 0)
                return TextAnchor.UpperLeft;
            else if (align == 1)
                return TextAnchor.UpperCenter;
            else
                return TextAnchor.UpperRight;
        }

        private void Awake()
        {
            Canvas canvas = base.gameObject.AddComponent<Canvas>();
            canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10001;

            CanvasScaler canvasScaler = base.gameObject.AddComponent<CanvasScaler>();
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);

            this.TextObject = new GameObject("TimingTextObject");
            this.TextObject.transform.SetParent(base.transform);
            this.TextObject.AddComponent<Canvas>();
            this.rectTransform = this.TextObject.GetComponent<RectTransform>();

            GameObject gameObject = new GameObject("TextComponent");
            gameObject.transform.SetParent(this.TextObject.transform);
            this.text = gameObject.AddComponent<Text>();
            this.text.font = RDString.GetFontDataForLanguage(RDString.language).font;
            this.text.alignment = this.ToAlign(Main.Settings.HUD_align);
            this.text.fontSize = (int)(24 * Main.Settings.HUD_scale);
            this.text.color = Color.white;
            this.text.horizontalOverflow = HorizontalWrapMode.Overflow;

            this.shadowText = gameObject.AddComponent<Shadow>();
            this.shadowText.effectColor = new Color(0f, 0f, 0f, 0.45f);
            this.shadowText.effectDistance = new Vector2(2f, -2f);

            float width = canvas.GetComponent<RectTransform>().rect.width;
            float height = canvas.GetComponent<RectTransform>().rect.height;
            Vector2 vector = new Vector2(Main.Settings.HUD_x + 0.5f, Main.Settings.HUD_y - 0.5f);
            this.rectTransform.anchorMin = vector;
            this.rectTransform.anchorMax = vector;
            this.rectTransform.pivot = vector;
            this.text.rectTransform.sizeDelta = new Vector2(width, height);
            this.rectTransform.anchoredPosition = Vector2.zero;
        }
    }
}