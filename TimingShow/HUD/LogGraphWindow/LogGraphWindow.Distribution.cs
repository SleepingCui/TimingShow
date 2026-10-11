using UnityEngine;


namespace TimingShow.HUD.LogGraphWindow
{
    public partial class LogGraphWindow
    {
        private void PushDistributionData()
        {
            if (_distributionDrawer == null) return;
            _distributionDrawer.SetData(_distributionStats, _data != null && _data.IsAngle);
        }
        
        private void SetDistributionView(bool distribution)
        {
            if (_distributionView == distribution) return;
            _distributionView = distribution;

            if (_plotAreaRect != null) _plotAreaRect.gameObject.SetActive(!distribution);
            if (_distributionRect != null) _distributionRect.gameObject.SetActive(distribution);

            if (distribution && _graphPanActive)
            {
                _graphPanActive = false;
                _graphPanRunning = false;
                if (_drawer != null) _drawer.EndPan();
            }

            if (_panel != null)
            {
                _panel.SetViewDistribution(distribution);
                _panel.SetViewHover(-1);
                _panel.MarkDirty();
            }

            SyncSettingsInteraction();
            RefreshStats();

            if (_windowRect != null)
            {
                Vector2 size = _windowRect.sizeDelta;
                ApplyViewToggleLayout(size, ViewToggleFits(size.x));
                RefreshButtonHover(Input.mousePosition);
            }
        }
        
    }
}
