using System;
using System.Globalization;
using System.IO;
using System.Text;
using MelonLoader;
using TMPro;
using TimingShow.Bootstrap;
using TimingShow.Update;
using UnityEngine;
using UnityEngine.UI;

namespace TimingShow.Melon
{
    internal sealed class RepairPage
    {
        private const string ReleaseId = "TimingShow.Melon";
        private const string LoaderFileName = "TimingShow.Loader.Melon.dll";
        private const string SettingsFileName = "Settings.json";
        private const string ConfigKeyName = "ConfigKey";
        private const int WindowId = 7331001;
        private const float WindowWidth = 760f;
        private const float WindowHeight = 480f;
        private const float DetailsHeight = 220f;
        private const float MarginX = 48f;
        private const float MarginY = 40f;
        private const float TextWidth = 1400f;
        private const int FontSize = 26;
        private const int CanvasSortingOrder = 20000;
        private static readonly KeyCode FallbackConfigKey = KeyCode.F9;

        private readonly RepairInstaller _installer;
        private readonly StartupIntegrityReport _integrity;
        private readonly KeyCode _configKey;
        private bool _readyToLoad;
        private bool _repairAbandoned;
        private bool _loadFailed;
        private bool _runtimeFailure;
        private string _failureDetails = string.Empty;
        private string _loadFailureMessage;
        private GameObject _hintObject;
        private TextMeshProUGUI _hint;
        private string _shownText;
        private float _nextBuildTime;
        private float _nextFontCheck;
        private bool _gameFontResolved;
        private bool _detailsVisible;
        private Rect _windowRect = new Rect(80f, 80f, WindowWidth, WindowHeight);
        private Vector2 _detailsScroll;
        private GUIStyle _detailStyle;

        public RepairPage(string modPath, string userDataDirectory, string pluginsDirectory, StartupIntegrityReport integrity, string failureDetails, bool runtimeFailure)
        {
            _integrity = integrity;
            _configKey = ReadConfigKey(modPath, userDataDirectory);
            _runtimeFailure = runtimeFailure;
            _failureDetails = failureDetails ?? string.Empty;
            string updateRoot = Path.Combine(userDataDirectory, "TimingShow", "Update");
            _installer = new RepairInstaller(
                modPath,
                pluginsDirectory,
                true,
                ReleaseId,
                null,
                Path.Combine(userDataDirectory, "TimingShow", SettingsFileName),
                updateRoot,
                LoaderFileName,
                message => MelonLogger.Msg("[TimingShow] " + message),
                message => MelonLogger.Error("[TimingShow] " + message));
            if (failureDetails == null) _installer.Start();
        }

        public bool IsReadyToLoad
        {
            get { return _readyToLoad && !_loadFailed; }
        }

        public bool ContinueRequested { get; private set; }

        public bool ShouldLoadCore
        {
            get { return IsReadyToLoad || (_repairAbandoned && !_loadFailed && _integrity != null && _integrity.CoreUsable); }
        }

        public void Tick()
        {
            ShowHint();
            if (Input.GetKeyDown(_configKey)) _detailsVisible = !_detailsVisible;
            if (_readyToLoad || _loadFailed) return;

            RepairState state = _installer.State;
            _repairAbandoned = state == RepairState.Failed || state == RepairState.Cancelled;
            if (state == RepairState.ReadyToLoad) _readyToLoad = true;
        }

        public void MarkLoadFailed(string details)
        {
            _readyToLoad = false;
            _loadFailed = true;
            _loadFailureMessage = details;
        }

        public void OnGUI()
        {
            if (!_detailsVisible) return;

            _windowRect.x = Mathf.Clamp(_windowRect.x, 0f, Mathf.Max(0f, Screen.width - _windowRect.width));
            _windowRect.y = Mathf.Clamp(_windowRect.y, 0f, Mathf.Max(0f, Screen.height - _windowRect.height));
            _windowRect = GUILayout.Window(WindowId, _windowRect, DrawWindow, "TimingShow", GUILayout.Width(WindowWidth), GUILayout.Height(WindowHeight));
        }

        public void Dispose()
        {
            _detailsVisible = false;
            if (_hintObject != null) UnityEngine.Object.Destroy(_hintObject);
            _hintObject = null;
            _hint = null;
            _shownText = null;
            _nextBuildTime = 0f;
            _gameFontResolved = false;
            _nextFontCheck = 0f;
        }

        private void DrawWindow(int id)
        {
            GUILayout.Label(HeaderText(), DetailStyle);
            GUILayout.Label(StatusText(), DetailStyle);
            DrawButtons();

            string details = BuildDetails();
            if (details.Length > 0)
            {
                _detailsScroll = GUILayout.BeginScrollView(_detailsScroll, GUILayout.Height(DetailsHeight));
                GUILayout.Label(details, DetailStyle);
                GUILayout.EndScrollView();
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                _detailsVisible = false;
                Event.current.Use();
            }

            GUI.DragWindow(new Rect(0f, 0f, _windowRect.width, 20f));
        }

        private void DrawButtons()
        {
            RepairState state = _installer.State;
            GUILayout.BeginHorizontal();
            if (_installer.IsRunning)
            {
                if (GUILayout.Button("Cancel", GUILayout.Width(120f))) _installer.Cancel();
            }
            else if (!RestartPending(state) && GUILayout.Button("Repair", GUILayout.Width(120f)))
            {
                Repair();
            }

            if (_runtimeFailure && !_installer.IsRunning && GUILayout.Button("Continue anyway", GUILayout.Width(150f))) Continue();
            GUILayout.FlexibleSpace();
            if (HasDiagnosticError() && GUILayout.Button("Copy error", GUILayout.Width(120f))) CopyError();
            if (GUILayout.Button("Close", GUILayout.Width(120f))) _detailsVisible = false;
            GUILayout.EndHorizontal();
        }

        private void Continue()
        {
            ContinueRequested = true;
            _detailsVisible = false;
        }

        private bool HasDiagnosticError()
        {
            if (_failureDetails.Length > 0) return true;
            if (!string.IsNullOrEmpty(_installer.ErrorMessage)) return true;
            return _integrity != null && _integrity.Problems.Count > 0;
        }

        private void CopyError()
        {
            GUIUtility.systemCopyBuffer = BuildDetails();
            MelonLogger.Msg("[TimingShow] Error details copied to the clipboard.");
        }

        private bool RestartPending(RepairState state)
        {
            return _loadFailed || state == RepairState.RestartRequired;
        }

        private void Repair()
        {
            _readyToLoad = false;
            _repairAbandoned = false;
            _loadFailed = false;
            _runtimeFailure = false;
            _failureDetails = string.Empty;
            _loadFailureMessage = null;
            _installer.Start();
        }

        private string HeaderText()
        {
            return _runtimeFailure ? "TimingShow was stopped after a runtime error." : "TimingShow is not running.";
        }

        private string StatusText()
        {
            RepairState state = _installer.State;
            if (_loadFailed) return "TimingShow was updated, but it could not start. Restart the game to load it.";
            if (_runtimeFailure) return "Repair it, continue anyway, or restart the game.";
            if (_failureDetails.Length > 0) return "TimingShow could not start. Click Repair to download a fresh copy.";
            if (state == RepairState.RestartRequired) return "Restart the game to finish repairing.";

            switch (state)
            {
                case RepairState.Checking:
                    return "Checking the latest release.";
                case RepairState.Downloading:
                    return "Downloading the latest package (" + FormatPercent() + ").";
                case RepairState.Verifying:
                    return "Verifying the downloaded package.";
                case RepairState.Installing:
                    return "Installing the downloaded files.";
                case RepairState.ReadyToLoad:
                    return "Starting the downloaded core.";
                case RepairState.Failed:
                    return "The repair download failed: " + _installer.ErrorMessage;
                case RepairState.Cancelled:
                    return "The repair download was cancelled.";
                default:
                    return _integrity != null ? "The installed files need a repair." : "TimingShow.dll is missing.";
            }
        }

        private string BuildDetails()
        {
            StringBuilder builder = new StringBuilder();
            if (!string.IsNullOrEmpty(_installer.Version)) builder.AppendLine("Version: " + _installer.Version);
            if (_integrity != null)
            {
                string check = _integrity.Enabled ? (_integrity.Problems.Count == 0 ? "passed" : "failed") : "disabled";
                builder.AppendLine("File check: " + check);
                foreach (string problem in _integrity.Problems) builder.AppendLine("  " + problem);
            }
            if (!string.IsNullOrEmpty(_installer.ErrorMessage)) builder.AppendLine("Download error: " + _installer.ErrorMessage);
            if (_failureDetails.Length > 0) builder.AppendLine(_failureDetails);
            if (!string.IsNullOrEmpty(_loadFailureMessage)) builder.AppendLine(_loadFailureMessage);
            return builder.ToString().Trim();
        }

        private GUIStyle DetailStyle
        {
            get
            {
                if (_detailStyle == null) _detailStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, richText = false };
                return _detailStyle;
            }
        }

        private void ShowHint()
        {
            if (_hint == null)
            {
                if (Time.unscaledTime < _nextBuildTime) return;
                _nextBuildTime = Time.unscaledTime + 1f;
                if (!BuildHint()) return;
            }

            string text = HintText();
            if (!string.Equals(_shownText, text, StringComparison.Ordinal))
            {
                _shownText = text;
                _hint.text = text;
            }

            TryUseGameFont();
        }

        private string HintText()
        {
            string text = "TimingShow is not running.";
            if (_runtimeFailure) text = "TimingShow core crashed.";
            else if (_failureDetails.Length > 0) text = "TimingShow core failed to start.";
            else if (_loadFailed) text = "TimingShow core failed to load.";
            if (_installer.IsRunning) text += " Downloading... " + FormatPercent();
            return text + " Press " + _configKey + " for details.";
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
                _hintObject = new GameObject("TimingShow_RepairHint");
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
                MelonLogger.Error("[TimingShow] Failed to display repair hint: " + e.Message);
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

        private static KeyCode ReadConfigKey(string modPath, string userDataDirectory)
        {
            KeyCode key = ReadStoredKey(Path.Combine(userDataDirectory, "TimingShow", SettingsFileName));
            if (key == KeyCode.None && !string.IsNullOrEmpty(modPath)) key = ReadStoredKey(Path.Combine(modPath, SettingsFileName));
            return key == KeyCode.None ? FallbackConfigKey : key;
        }

        private static KeyCode ReadStoredKey(string path)
        {
            try
            {
                if (!File.Exists(path)) return KeyCode.None;
                object value;
                if (!JsonText.ParseObject(File.ReadAllText(path)).TryGetValue(ConfigKeyName, out value)) return KeyCode.None;
                return ParseKeyCode(value);
            }
            catch (Exception)
            {
                return KeyCode.None;
            }
        }

        private static KeyCode ParseKeyCode(object value)
        {
            if (value is long)
            {
                long code = (long)value;
                return code > 0 && code <= int.MaxValue ? (KeyCode)(int)code : KeyCode.None;
            }

            string text = value as string;
            if (string.IsNullOrEmpty(text)) return KeyCode.None;
            KeyCode parsed;
            return Enum.TryParse(text, true, out parsed) ? parsed : KeyCode.None;
        }

        private string FormatPercent()
        {
            return (_installer.Progress * 100f).ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }
    }
}
