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

    }
}
