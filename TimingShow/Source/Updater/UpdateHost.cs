using System;
using UnityEngine;

namespace TimingShow.Update
{
    internal sealed class UpdateHost : MonoBehaviour
    {
        private static GameObject _host;

        public static void EnsureHost()
        {
            if (_host != null) return;
            try
            {
                _host = new GameObject("TimingShow_UpdateHost");
                _host.hideFlags = HideFlags.HideAndDontSave;
                UnityEngine.Object.DontDestroyOnLoad(_host);
                _host.AddComponent<UpdateHost>();
            }
            catch (Exception e)
            {
                ModContext.Logger?.Error("Failed to create update host: " + e.Message);
                if (_host != null) UnityEngine.Object.Destroy(_host);
                _host = null;
            }
        }

        private void Update()
        {
            Updater.Tick();
        }
    }
}
