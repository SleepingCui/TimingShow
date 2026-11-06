using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TimingShow.HUD.LogGraphWindow;
using UnityFileDialog;

namespace TimingShow.HUD.LogGraphSettingsUI
{
    public sealed partial class LogGraphSettingsUI
    {
        private bool _capturingReportKey;

        private List<Row> BuildMiscRows()
        {
            List<Row> rows = new List<Row>(8);

            rows.Add(Toggle("Toggle_CustomFont",
                () => _draft.UseCustomFont,
                v => _draft.UseCustomFont = v));

            rows.Add(Text("Label_FontPath"));
            rows.Add(new Row { Kind = RowKind.TextField });

            rows.Add(new Row
            {
                Kind = RowKind.Button,
                LabelKey = "Btn_SelectFont",
                OnPress = PickFontFile
            });

            rows.Add(new Row
            {
                Kind = RowKind.Button,
                LabelKey = "Label_ReportKey",
                LabelText = ReportKeyButtonText(),
                OnPress = ToggleReportKeyCapture
            });

            rows.Add(new Row
            {
                Kind = RowKind.Button,
                LabelKey = "Btn_ResetReportKey",
                IsEnabled = () => _draft.ReportKey != KeyCode.F10,
                OnPress = ResetReportKey
            });

            rows.Add(new Row
            {
                Kind = RowKind.Button,
                LabelKey = "Btn_ViewLastReport",
                IsEnabled = () => SessionReport.LastSessionReport.HasReport,
                OnPress = () => SessionReport.LastSessionReport.OpenWindow()
            });

            rows.Add(Text("LogGraph_ReportKeyHint"));
            return rows;
        }

        private string ReportKeyButtonText()
        {
            string key;
            if (_capturingReportKey) key = i18n.T("Btn_PressKey");
            else if (_draft.ReportKey == KeyCode.None) key = i18n.T("Status_Disabled");
            else key = _draft.ReportKey.ToString();

            return i18n.T("Label_ReportKey") + key;
        }

        private void ToggleReportKeyCapture()
        {
            bool capture = !_capturingReportKey;
            SetReportKeyCapture(capture);
            if (capture) EndEditingFields();

            RebuildRows();
            MarkDirty();
        }

        private void ResetReportKey()
        {
            _draft.ReportKey = KeyCode.F10;
            _draftDirty = true;
            SetReportKeyCapture(false);
            RebuildRows();
            MarkDirty();
        }

        private void SetReportKeyCapture(bool capture)
        {
            _capturingReportKey = capture;
            CapturingReportKey = capture;
        }

        private void TickReportKeyCapture()
        {
            if (!_capturingReportKey) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetReportKeyCapture(false);
                RebuildRows();
                MarkDirty();
                return;
            }

            if (!Input.anyKeyDown) return;

            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if (key == KeyCode.None || key == KeyCode.Escape) continue;
                if (key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6) continue;
                if (!Input.GetKeyDown(key)) continue;

                _draft.ReportKey = key;
                _draftDirty = true;
                _suppressHotkeyFrame = Time.frameCount;
                SetReportKeyCapture(false);
                RebuildRows();
                MarkDirty();
                return;
            }
        }

        private void OnFontPathChanged(string path)
        {
            _draft.FontPath = path ?? "";
            _draftDirty = true;
        }

        private void OnFontPathCommitted(LogRangeField field)
        {
            if (field != null) _draft.FontPath = field.Value ?? "";
        }

        private void PickFontFile()
        {
            string initialDirectory = "";
            try
            {
                if (!string.IsNullOrWhiteSpace(_draft.FontPath))
                {
                    string folder = Path.GetDirectoryName(_draft.FontPath);
                    if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder)) initialDirectory = folder;
                }
            }
            catch (Exception)
            {
            }

            string selected;
            try
            {
                selected = FileBrowser.PickFile(initialDirectory, "Font", new[] { "ttf", "otf" }, i18n.T("Btn_SelectFont"));
            }
            catch (Exception e)
            {
                ModContext.Logger?.Error("[LogGraphSettings] Font dialog failed: " + e);
                return;
            }

            if (string.IsNullOrWhiteSpace(selected)) return;

            _draft.FontPath = selected;
            _draft.UseCustomFont = true;
            _draftDirty = true;
            RebuildRows();
            MarkDirty();
        }
    }
}
