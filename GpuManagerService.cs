using System;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Runtime.InteropServices;

namespace eGPUManager
{
    internal sealed class GpuManagerService : ServiceBase
    {
        private readonly HeadlessManager manager;

        public GpuManagerService(string? pciId, bool enableMode)
        {
            ServiceName = "eGPUManagerService";
            CanStop = true;
            CanPauseAndContinue = false;
            AutoLog = false;
            manager = new HeadlessManager(pciId, enableMode);
        }

        protected override void OnStart(string[] args)
        {
            manager.Start();
            base.OnStart(args);
        }

        protected override void OnStop()
        {
            manager.Stop();
            base.OnStop();
        }
    }

    internal sealed class HeadlessManager
    {
        private readonly string? initialPciId;
        private readonly bool enableMode;
        private readonly System.Timers.Timer autoscanTimer;
        private bool isAutoscanMode;
        private string? currentInstanceId;
        private readonly string logFilePath;

        public HeadlessManager(string? pciId, bool enableMode)
        {
            initialPciId = pciId;
            this.enableMode = enableMode;
            autoscanTimer = new System.Timers.Timer(5000);
            autoscanTimer.Elapsed += AutoscanTimer_Elapsed;
            autoscanTimer.AutoReset = true;
            logFilePath = InitializeLogFile();
        }

        public void Start()
        {
            Log("Service mode starting...");

            if (!string.IsNullOrWhiteSpace(initialPciId))
            {
                currentInstanceId = initialPciId;
                Properties.Settings.Default.DeviceInstanceId = currentInstanceId;
                Properties.Settings.Default.Save();
                Log($"Using provided PCI ID: {currentInstanceId}");
                if (enableMode)
                {
                    TryEnableDevice(currentInstanceId);
                }
                isAutoscanMode = false;
            }
            else
            {
                isAutoscanMode = true;
                Log("Autoscan mode enabled for service.");
                string savedId = Properties.Settings.Default.DeviceInstanceId;
                if (!string.IsNullOrWhiteSpace(savedId))
                {
                    currentInstanceId = savedId;
                    Log($"Loaded saved InstanceId: {savedId}");
                    if (enableMode)
                    {
                        TryEnableDevice(savedId);
                    }
                }
                else
                {
                    string? detectedId = DetectEgpuDeviceInstanceId();
                    if (!string.IsNullOrEmpty(detectedId))
                    {
                        currentInstanceId = detectedId;
                        Properties.Settings.Default.DeviceInstanceId = detectedId;
                        Properties.Settings.Default.Save();
                        Log($"Detected eGPU InstanceId: {detectedId}");
                        if (enableMode)
                        {
                            TryEnableDevice(detectedId);
                        }
                    }
                    else
                    {
                        Log("No eGPU device detected at service start.");
                    }
                }

                autoscanTimer.Start();
                Log("Autoscan timer started.");
            }
        }

        public void Stop()
        {
            autoscanTimer.Stop();
            Log("Service mode stopped.");
        }

        private void AutoscanTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (!isAutoscanMode)
                return;

            try
            {
                string? detectedId = DetectEgpuDeviceInstanceId();
                if (!string.IsNullOrEmpty(detectedId) && !string.Equals(detectedId, currentInstanceId, StringComparison.OrdinalIgnoreCase))
                {
                    currentInstanceId = detectedId;
                    Properties.Settings.Default.DeviceInstanceId = detectedId;
                    Properties.Settings.Default.Save();
                    Log($"Autoscan detected eGPU InstanceId: {detectedId}");
                    if (enableMode)
                    {
                        TryEnableDevice(detectedId);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Autoscan error: {ex.Message}");
            }
        }

        private void TryEnableDevice(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
                return;

            Log($"Attempting to enable device: {instanceId}");
            bool success = SetDeviceState(instanceId, true);
            if (success)
            {
                Log("Device enable request completed successfully.");
            }
            else
            {
                Log("Device enable request failed.");
            }
        }

        private string InitializeLogFile()
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "eGPUManager", "logs");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"eGPUManager_service_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Service log started{Environment.NewLine}");
            return path;
        }

        private void Log(string message)
        {
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
            try
            {
                File.AppendAllText(logFilePath, logEntry);
            }
            catch { }
        }

        private string? DetectEgpuDeviceInstanceId()
        {
            string[] searchPatterns = new[]
            {
                "PCI\\VEN_1002&DEV_1479",
                "PCI\\VEN_1002&DEV_"
            };

            foreach (string pattern in searchPatterns)
            {
                string? deviceId = FindDeviceInstanceIdByHardwarePattern(pattern);
                if (!string.IsNullOrEmpty(deviceId))
                {
                    return deviceId;
                }
            }

            return null;
        }

        private string? FindDeviceInstanceIdByHardwarePattern(string hardwarePattern)
        {
            Guid guid = Guid.Empty;
            IntPtr hDevInfo = SetupDiGetClassDevs(ref guid, null, IntPtr.Zero, DIGCF_ALLCLASSES | DIGCF_PRESENT);
            if (hDevInfo == (IntPtr)INVALID_HANDLE_VALUE)
                return null;

            try
            {
                SP_DEVINFO_DATA devInfoData = new SP_DEVINFO_DATA();
                devInfoData.cbSize = (uint)Marshal.SizeOf(devInfoData);

                for (uint i = 0; SetupDiEnumDeviceInfo(hDevInfo, i, ref devInfoData); i++)
                {
                    string[]? hardwareIds = GetDeviceHardwareIds(hDevInfo, ref devInfoData);
                    if (hardwareIds == null)
                        continue;

                    foreach (string hwid in hardwareIds)
                    {
                        if (hwid.IndexOf(hardwarePattern, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            string? instanceId = GetDeviceInstanceId(hDevInfo, ref devInfoData);
                            if (!string.IsNullOrEmpty(instanceId))
                                return instanceId;
                        }
                    }
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(hDevInfo);
            }

            return null;
        }

        private string[]? GetDeviceHardwareIds(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA devInfoData)
        {
            uint requiredSize = 0;
            uint regType;
            SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref devInfoData, SPDRP_HARDWAREID, out regType, null, 0, out requiredSize);
            if (requiredSize == 0)
                return null;

            byte[] buffer = new byte[requiredSize];
            if (!SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref devInfoData, SPDRP_HARDWAREID, out regType, buffer, requiredSize, out requiredSize))
                return null;

            string raw = Encoding.Unicode.GetString(buffer, 0, (int)requiredSize);
            return raw.TrimEnd('\0').Split('\0', StringSplitOptions.RemoveEmptyEntries);
        }

        private string? GetDeviceInstanceId(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA devInfoData)
        {
            const uint bufferSize = 512;
            StringBuilder instanceId = new StringBuilder((int)bufferSize);
            if (SetupDiGetDeviceInstanceId(deviceInfoSet, ref devInfoData, instanceId, bufferSize, out _))
                return instanceId.ToString();

            return null;
        }

        private bool SetDeviceState(string instanceId, bool enable)
        {
            try
            {
                Guid guid = Guid.Empty;
                IntPtr hDevInfo = SetupDiGetClassDevs(ref guid, instanceId, IntPtr.Zero, DIGCF_ALLCLASSES | DIGCF_DEVICEINTERFACE);
                if (hDevInfo == (IntPtr)INVALID_HANDLE_VALUE)
                {
                    int err = Marshal.GetLastWin32Error();
                    Log($"ERROR: SetupDiGetClassDevs failed. Win32Error={err}");
                    return false;
                }

                SP_DEVINFO_DATA devInfoData = new SP_DEVINFO_DATA();
                devInfoData.cbSize = (uint)Marshal.SizeOf(devInfoData);

                if (!SetupDiEnumDeviceInfo(hDevInfo, 0, ref devInfoData))
                {
                    int err = Marshal.GetLastWin32Error();
                    Log($"ERROR: SetupDiEnumDeviceInfo failed. Win32Error={err}");
                    return false;
                }

                SP_PROPCHANGE_PARAMS propChangeParams = new SP_PROPCHANGE_PARAMS();
                propChangeParams.ClassInstallHeader.cbSize = (uint)Marshal.SizeOf(typeof(SP_CLASSINSTALL_HEADER));
                propChangeParams.ClassInstallHeader.InstallFunction = DIF_PROPERTYCHANGE;
                propChangeParams.StateChange = enable ? DICS_ENABLE : DICS_DISABLE;
                propChangeParams.Scope = DICS_FLAG_GLOBAL;
                propChangeParams.HwProfile = 0;

                if (!SetupDiSetClassInstallParams(hDevInfo, ref devInfoData, ref propChangeParams, (uint)Marshal.SizeOf(propChangeParams)))
                {
                    int err = Marshal.GetLastWin32Error();
                    Log($"ERROR: SetupDiSetClassInstallParams failed. Win32Error={err}");
                    return false;
                }

                bool called = SetupDiCallClassInstaller(DIF_PROPERTYCHANGE, hDevInfo, ref devInfoData);
                if (!called)
                {
                    int err = Marshal.GetLastWin32Error();
                    Log($"ERROR: SetupDiCallClassInstaller failed. Win32Error={err}");
                    return false;
                }

                Log($"SUCCESS: Device has been {(enable ? "enabled" : "disabled")}. ");
                return true;
            }
            catch (Exception ex)
            {
                Log($"ERROR while changing device state: {ex.Message}");
                return false;
            }
        }

        const int INVALID_HANDLE_VALUE = -1;
        const uint DIGCF_ALLCLASSES = 0x00000004;
        const uint DIGCF_PRESENT = 0x00000002;
        const uint DIGCF_DEVICEINTERFACE = 0x00000010;
        const uint SPDRP_HARDWAREID = 0x00000001;
        const uint DIF_PROPERTYCHANGE = 0x00000012;
        const uint DICS_ENABLE = 0x00000001;
        const uint DICS_DISABLE = 0x00000002;
        const uint DICS_FLAG_GLOBAL = 0x00000001;

        [StructLayout(LayoutKind.Sequential)]
        struct SP_DEVINFO_DATA
        {
            public uint cbSize;
            public Guid ClassGuid;
            public uint DevInst;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct SP_CLASSINSTALL_HEADER
        {
            public uint cbSize;
            public uint InstallFunction;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct SP_PROPCHANGE_PARAMS
        {
            public SP_CLASSINSTALL_HEADER ClassInstallHeader;
            public uint StateChange;
            public uint Scope;
            public uint HwProfile;
        }

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, string? Enumerator, IntPtr hwndParent, uint Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiEnumDeviceInfo(IntPtr DeviceInfoSet, uint MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool SetupDiGetDeviceRegistryProperty(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, uint Property, out uint PropertyRegDataType, byte[]? PropertyBuffer, uint PropertyBufferSize, out uint RequiredSize);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool SetupDiGetDeviceInstanceId(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, StringBuilder DeviceInstanceId, uint DeviceInstanceIdSize, out uint RequiredSize);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiSetClassInstallParams(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, ref SP_PROPCHANGE_PARAMS ClassInstallParams, uint ClassInstallParamsSize);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiCallClassInstaller(uint InstallFunction, IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData);
    }
}
