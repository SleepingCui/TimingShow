using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TimingShow.HUD
{
    public sealed class StartupHint : MonoBehaviour
    {
        private const float HoldSeconds = 3f;
        private const float FadeSeconds = 0.6f;
        private const float RevealTimeoutSeconds = 6f;
        private const float MarginX = 48f;
        private const float MarginY = 40f;
        private const float TextWidth = 820f;
        private const int FontSize = 26;
        private const int CanvasSortingOrder = 20000;

        private static StartupHint _host;

        private string _message;
        private CanvasGroup _group;
        private float _createdAt;
        private float _shownAt = -1f;

        public static void Schedule(string message)
        {
            if (_host != null || string.IsNullOrEmpty(message)) return;

            try
            {
                GameObject go = new GameObject("TimingShow_StartupHint");
                go.hideFlags = HideFlags.HideAndDontSave;
                Object.DontDestroyOnLoad(go);
                _host = go.AddComponent<StartupHint>();
                _host._message = message;
                SceneManager.sceneLoaded += _host.OnSceneLoaded;
            }
            catch (System.Exception e)
            {
                ModContext.Logger?.Error("Failed to create startup hint: " + e.Message);
                if (_host != null) Destroy(_host.gameObject);
                _host = null;
            }
        }

        private void Awake()
        {
            _createdAt = Time.unscaledTime;
        }

        private void Update()
        {
            if (_shownAt < 0f)
            {
                if (Time.unscaledTime - _createdAt >= RevealTimeoutSeconds) Reveal();
                return;
            }

            float elapsed = Time.unscaledTime - _shownAt;
            if (elapsed <= HoldSeconds) return;

            float alpha = 1f - (elapsed - HoldSeconds) / FadeSeconds;
            if (_group != null) _group.alpha = Mathf.Clamp01(alpha);
            if (alpha > 0f) return;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            Destroy(gameObject);
            _host = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Patches.PlayStatePatches.IsPlayableScene()) return;
            Reveal();
        }

        private void Reveal()
        {
            if (_shownAt >= 0f) return;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            try
            {
                Build();
                _shownAt = Time.unscaledTime;
            }
            catch (System.Exception e)
            {
                ModContext.Logger?.Error("Failed to build startup hint: " + e.Message);
                Destroy(gameObject);
                _host = null;
            }
        }

        private void Build()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _group = gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;

            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(transform, false);

            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(MarginX, MarginY);
            rect.sizeDelta = new Vector2(TextWidth, FontSize + 14f);

            TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.BottomLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.fontSize = FontSize;
            text.color = new Color(1f, 1f, 1f, 0.94f);

            TMP_FontAsset font = TextUI.FindGameFont();
            if (font == null) font = LogGraphFont.Resolve();
            if (font != null)
            {
                text.font = font;
                LogGraphFont.EnsureGlyphs(font, _message);
            }

            Shadow shadow = textObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(2f, -2f);

            text.text = _message;
        }
    }
}
