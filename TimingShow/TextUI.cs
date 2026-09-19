using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TimingShow
{
    public class TextUI : MonoBehaviour
    {
        public Canvas canvas;
        public RectTransform rootRect;
        public GameObject textObject;
        public TMP_Text text;
        public Shadow shadow;

        private string _fontConfigKey;
        private TMP_FontAsset _customFontAsset;
        private string _customFontPath;
        private Material _shadowMaterial;

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
            textObject.transform.SetParent(transform, false);

            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(1000f, 200f);

            text = textObject.AddComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.richText = true;
            text.font = TMP_Settings.defaultFontAsset;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;

            shadow = textObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
            shadow.effectDistance = new Vector2(2f, -2f);
            shadow.enabled = false;
            ApplyTMPShadow();
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
            rect.anchoredPosition = new Vector2(x * Screen.width, y * Screen.height);
        }

        public void ApplyFont(bool useCustomFont, string fontPath)
        {
            if (text == null) return;

            string normalizedPath = string.IsNullOrWhiteSpace(fontPath) ? "" : Path.GetFullPath(fontPath);
            string configKey = useCustomFont ? "custom:" + normalizedPath : "game";
            if (configKey == _fontConfigKey) return;

            TMP_FontAsset fontAsset;
            if (useCustomFont)
            {
                if (_customFontPath != normalizedPath || _customFontAsset == null)
                {
                    _customFontPath = normalizedPath;
                    _customFontAsset = LoadCustomFont(normalizedPath);
                }
                fontAsset = _customFontAsset;
            }
            else
            {
                fontAsset = FindGameFont();
            }
            if (fontAsset == null) fontAsset = TMP_Settings.defaultFontAsset;
            if (fontAsset == null)
            {
                _fontConfigKey = configKey;
                return;
            }

            text.font = fontAsset;
            ApplyTMPShadow();
            text.SetVerticesDirty();
            text.SetLayoutDirty();
            _fontConfigKey = configKey;
        }

        private void ApplyTMPShadow()
        {
            if (text == null || text.fontSharedMaterial == null) return;

            if (_shadowMaterial != null)
                Object.Destroy(_shadowMaterial);

            _shadowMaterial = new Material(text.fontSharedMaterial);
            _shadowMaterial.name = "TimingShow_TMP_ShadowMaterial";
            if (_shadowMaterial.HasProperty(ShaderUtilities.ID_UnderlayColor))
            {
                _shadowMaterial.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                _shadowMaterial.SetColor(
                    ShaderUtilities.ID_UnderlayColor,
                    new Color(0f, 0f, 0f, 0.45f));
                _shadowMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.1f);
                _shadowMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.1f);
                _shadowMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.1f);
                _shadowMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.05f);
                text.fontMaterial = _shadowMaterial;
            }
        }

        private static TMP_FontAsset FindGameFont()
        {
            try
            {
                scrHitTextMesh[] hitTexts = Resources.FindObjectsOfTypeAll<scrHitTextMesh>();
                for (int i = 0; i < hitTexts.Length; i++)
                {
                    if (hitTexts[i] != null && hitTexts[i].text != null && hitTexts[i].text.font != null)
                        return hitTexts[i].text.font;
                }
            }
            catch (System.Exception e)
            {
                ModContext.Logger?.Log("Failed to find game HUD font: " + e.Message);
            }

            return TMP_Settings.defaultFontAsset;
        }

        private static TMP_FontAsset LoadCustomFont(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                ModContext.Logger?.Log("HUD font file does not exist: " + path);
                return null;
            }

            string extension = Path.GetExtension(path);
            if (!string.Equals(extension, ".ttf", System.StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".otf", System.StringComparison.OrdinalIgnoreCase))
            {
                ModContext.Logger?.Log("Unsupported HUD font file: " + path);
                return null;
            }

            try
            {
                Font sourceFont = new Font();
                MethodInfo loadFromPath = typeof(Font).GetMethod(
                    "Internal_CreateFontFromPath",
                    BindingFlags.Static | BindingFlags.NonPublic);
                if (loadFromPath == null)
                {
                    ModContext.Logger?.Log("Unity does not expose font loading from path");
                    return null;
                }

                loadFromPath.Invoke(null, new object[] { sourceFont, path });
                TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
                if (fontAsset != null)
                {
                    fontAsset.name = "TimingShow_CustomFont_" + Path.GetFileNameWithoutExtension(path);
                    Object.DontDestroyOnLoad(fontAsset);
                }
                return fontAsset;
            }
            catch (System.Exception e)
            {
                ModContext.Logger?.Log("Failed to load HUD font: " + e.Message);
                return null;
            }
        }

        public void SetAlignment(int align)
        {
            if (text == null) return;
            switch (align)
            {
                case 0: text.alignment = TextAlignmentOptions.MidlineLeft; break;
                case 1: text.alignment = TextAlignmentOptions.Center; break;
                case 2: text.alignment = TextAlignmentOptions.MidlineRight; break;
                default: text.alignment = TextAlignmentOptions.Center; break;
            }
        }

        public void SetBold(bool bold)
        {
            if (text != null)
                text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        }
    }
}
