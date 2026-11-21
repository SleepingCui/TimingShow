using System;
using System.Globalization;
using System.IO;
using MelonLoader;
using TMPro;
using TimingShow.Bootstrap;
using UnityEngine;
using UnityEngine.UI;

namespace TimingShow.Melon
{
    internal sealed class MissingCoreFallback
    {
        private const string ReleaseId = "TimingShow.Melon";
        private const string LoaderFileName = "TimingShow.Loader.Melon.dll";
        private const float MarginX = 48f;
        private const float MarginY = 40f;
        private const float TextWidth = 1400f;
        private const int FontSize = 26;
        private const int CanvasSortingOrder = 20000;

        private readonly MissingCoreInstaller _installer;
        private bool _readyToLoad;
        private bool _loadFailed;
        private string _loadFailureMessage;
        private GameObject _hintObject;
        private TextMeshProUGUI _hint;
        private string _shownText;
        private float _nextBuildTime;
        private float _nextFontCheck;
        private bool _gameFontResolved;

        public MissingCoreFallback(string modPath, string userDataDirectory, string pluginsDirectory)
        {
            string updateRoot = Path.Combine(userDataDirectory, "TimingShow", "Update");
            _installer = new MissingCoreInstaller(
                modPath,
                pluginsDirectory,
                true,
                ReleaseId,
                null,
                updateRoot,
                LoaderFileName,
                message => MelonLogger.Msg("[TimingShow] " + message),
                message => MelonLogger.Error("[TimingShow] " + message));
            _installer.Start();
        }

        public bool IsReadyToLoad
        {
            get { return _readyToLoad && !_loadFailed; }
        }

        public void Tick()
        {
            ShowHint();
            if (_readyToLoad || _loadFailed) return;

            MissingCoreState state = _installer.State;
            if (state == MissingCoreState.ReadyToLoad)
            {
                _readyToLoad = true;
                return;
            }
            if (state == MissingCoreState.RestartRequired) return;

            if (!Input.GetKeyDown(KeyCode.F6)) return;
            if (IsRunning(state)) _installer.Cancel();
            else if (state == MissingCoreState.Failed || state == MissingCoreState.Cancelled) _installer.Start();
        }

        public void MarkLoadFailed(string message)
        {
            _readyToLoad = false;
            _loadFailed = true;
            _loadFailureMessage = message;
        }

        public void Dispose()
        {
            if (_hintObject != null) UnityEngine.Object.Destroy(_hintObject);
            _hintObject = null;
            _hint = null;
            _shownText = null;
            _nextBuildTime = 0f;
            _gameFontResolved = false;
            _nextFontCheck = 0f;
        }

        private void ShowHint()
        {
            if (_hint == null)
            {
                if (Time.unscaledTime < _nextBuildTime) return;
                _nextBuildTime = Time.unscaledTime + 1f;
                if (!BuildHint()) return;
            }

            string text = StatusText();
            if (!string.Equals(_shownText, text, StringComparison.Ordinal))
            {
                _shownText = text;
                _hint.text = text;
            }

            TryUseGameFont();
        }

        private void TryUseGameFont()
        {
            if (_gameFontResolved || _hint == null) return;
            if (Time.unscaledTime < _nextFontCheck) return;
            _nextFontCheck = Time.unscaledTime + 0.5f;

            TMP_FontAsset font = FindGameFont();
            if (font == null) return;
            _gameFontResolved = true;
            _hint.font = font;
        }

        private bool BuildHint()
        {
            try
            {
                _hintObject = new GameObject("TimingShow_MissingCoreHint");
                _hintObject.hideFlags = HideFlags.HideAndDontSave;
                UnityEngine.Object.DontDestroyOnLoad(_hintObject);

                Canvas canvas = _hintObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = CanvasSortingOrder;

                CanvasScaler scaler = _hintObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                GameObject textObject = new GameObject("Text");
                textObject.transform.SetParent(_hintObject.transform, false);

                RectTransform rect = textObject.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.pivot = Vector2.zero;
                rect.anchoredPosition = new Vector2(MarginX, MarginY);
                rect.sizeDelta = new Vector2(TextWidth, FontSize + 12f);

                _hint = textObject.AddComponent<TextMeshProUGUI>();
                _hint.raycastTarget = false;
                _hint.alignment = TextAlignmentOptions.BottomLeft;
                _hint.textWrappingMode = TextWrappingModes.NoWrap;
                _hint.overflowMode = TextOverflowModes.Overflow;
                _hint.fontSize = FontSize;
                _hint.color = new Color(1f, 1f, 1f, 0.94f);

                if (TMP_Settings.defaultFontAsset != null) _hint.font = TMP_Settings.defaultFontAsset;

                Shadow shadow = textObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
                shadow.effectDistance = new Vector2(2f, -2f);
                return true;
            }
            catch (Exception e)
            {
                MelonLogger.Error("[TimingShow] Failed to create the missing-core hint: " + e.Message);
                Dispose();
                return false;
            }
        }

        private static TMP_FontAsset FindGameFont()
        {
            try
            {
                TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
                for (int i = 0; i < texts.Length; i++)
                {
                    TMP_Text candidate = texts[i];
                    if (candidate == null || candidate.font == null) continue;
                    if (candidate.font.name.IndexOf("LiberationSans", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                    return candidate.font;
                }
            }
            catch (Exception)
            {
            }
            return TMP_Settings.defaultFontAsset;
        }

        private static bool IsRunning(MissingCoreState state)
        {
            return state == MissingCoreState.Checking || state == MissingCoreState.Downloading || state == MissingCoreState.Verifying || state == MissingCoreState.Installing;
        }

        private string StatusText()
        {
            if (_loadFailed) return "Core downloaded, but it could not start. Restart the game." + (string.IsNullOrEmpty(_loadFailureMessage) ? string.Empty : " " + _loadFailureMessage);

            MissingCoreState state = _installer.State;
            switch (state)
            {
                case MissingCoreState.Checking:
                    return "Core missing! Checking for updates...";
                case MissingCoreState.Downloading:
                    return "Core missing! Downloading... " + FormatPercent() + "  Press F6 to cancel";
                case MissingCoreState.Verifying:
                    return "Core missing! Verifying the download...  Press F6 to cancel";
                case MissingCoreState.Installing:
                    return "Core missing! Installing...  Press F6 to cancel";
                case MissingCoreState.ReadyToLoad:
                    return "Core downloaded. Loading TimingShow...";
                case MissingCoreState.RestartRequired:
                    return "Core downloaded. Restart the game to load TimingShow.";
                case MissingCoreState.Failed:
                    return "Core download failed: " + _installer.ErrorMessage + "  Press F6 to retry";
                case MissingCoreState.Cancelled:
                    return "Download cancelled. Press F6 to retry.";
                default:
                    return "Core missing! Preparing the download...";
            }
        }

        private string FormatPercent()
        {
            return (_installer.Progress * 100f).ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }
    }
}
