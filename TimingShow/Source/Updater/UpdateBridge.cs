using System;
using System.IO;
using System.Reflection;

namespace TimingShow.Update
{
    public static class UpdateBridge
    {
        public const string ServiceAssemblyFileName = "TimingShow.Updater.dll";
        public const string DefaultRepositoryUrl = "https://raw.githubusercontent.com/SleepingCui/TimingShow/refs/heads/master/Repository.json";

        private const string ServiceTypeName = "TimingShow.Update.UpdateService";

        private static IUpdateService _service;

        public static event Action Changed;

        public static bool IsAvailable
        {
            get { return _service != null; }
        }

        public static bool Supported
        {
            get { return _service != null && _service.Supported; }
        }

        public static string Suffix
        {
            get { return _service == null ? string.Empty : _service.Suffix; }
        }

        public static void Initialize(string modPath, string updateRoot, string repositoryUrl, string version, UpdateTarget target, bool autoApplyAvailable, IModLogger logger)
        {
            _service = LoadService(modPath, logger);
            if (_service == null) return;

            _service.Changed += OnChanged;
            try
            {
                _service.Initialize(modPath, updateRoot, repositoryUrl, version, target, autoApplyAvailable, logger);
            }
            catch (Exception e)
            {
                logger?.Error("Failed to initialize automatic updates: " + e.Message);
                _service.Changed -= OnChanged;
                _service = null;
            }
        }

        public static void DrawBanner()
        {
            if (_service != null) _service.DrawBanner();
        }

        public static void DrawSettings()
        {
            if (_service != null) _service.DrawSettings();
        }

        public static void CheckNow()
        {
            if (_service != null) _service.CheckNow();
        }

        private static void OnChanged()
        {
            Action handler = Changed;
            if (handler != null) handler();
        }

        private static IUpdateService LoadService(string modPath, IModLogger logger)
        {
            if (string.IsNullOrEmpty(modPath)) return null;

            string path = Path.Combine(modPath, ServiceAssemblyFileName);
            if (!File.Exists(path))
            {
                logger?.Log(ServiceAssemblyFileName + " was not found; automatic updates are disabled.");
                return null;
            }

            try
            {
                Assembly assembly = Assembly.LoadFrom(path);
                Type type = assembly.GetType(ServiceTypeName, false);
                if (type == null)
                {
                    logger?.Error(ServiceAssemblyFileName + " does not contain " + ServiceTypeName + "; automatic updates are disabled.");
                    return null;
                }

                IUpdateService service = Activator.CreateInstance(type) as IUpdateService;
                if (service == null)
                {
                    logger?.Error(ServiceTypeName + " in " + ServiceAssemblyFileName + " does not implement " + typeof(IUpdateService).FullName + "; automatic updates are disabled.");
                }
                return service;
            }
            catch (Exception e)
            {
                logger?.Error("Failed to load " + ServiceAssemblyFileName + ": " + e.Message + "; automatic updates are disabled.");
                return null;
            }
        }
    }
}
