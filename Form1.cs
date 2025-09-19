using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Threading;

namespace eGPUManager
{
    public partial class Form1 : Form
    {
        // Variables for the system tray icon
        private NotifyIcon notifyIcon;
        private ContextMenuStrip contextMenu;
        private ToolStripMenuItem showMenuItem;
        private ToolStripMenuItem enabledMenuItem;
        private ToolStripMenuItem exitMenuItem;
        private Icon iconActive;
        private Icon iconInactive;

        public Form1()
        {
            InitializeComponent();
            SetupApplication();
        }

        private void SetupApplication()
        {
            // Load icons (ideally, add them as project resources)
            // For simplicity, we create them programmatically.
            iconActive = CreateIcon(Brushes.LimeGreen);
            iconInactive = CreateIcon(Brushes.Red);

            // Setup the context menu for the tray icon
            contextMenu = new ContextMenuStrip();
            showMenuItem = new ToolStripMenuItem("Show Window");
            showMenuItem.Click += ShowMenuItem_Click;
            enabledMenuItem = new ToolStripMenuItem("Enable Management");
            enabledMenuItem.Checked = false;
            enabledMenuItem.CheckOnClick = true;
            enabledMenuItem.CheckedChanged += EnabledMenuItem_CheckedChanged;
            exitMenuItem = new ToolStripMenuItem("Exit");
            exitMenuItem.Click += ExitMenuItem_Click;

            contextMenu.Items.AddRange(new ToolStripItem[] { showMenuItem, enabledMenuItem, new ToolStripSeparator(), exitMenuItem });

            // Setup the system tray icon itself
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = iconInactive;
            notifyIcon.Text = "eGPU Manager (Inactive)";
            notifyIcon.Visible = true;
            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.DoubleClick += ShowMenuItem_Click;

            // Load the saved InstanceId from settings
            txtInstanceId.Text = Properties.Settings.Default.DeviceInstanceId;
        }

        // --- UI Event Handlers ---

        private void EnabledMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            // Sync the menu's checked state with the checkbox in the main window
            chkEnableManagement.Checked = enabledMenuItem.Checked;
        }

        private void chkEnableManagement_CheckedChanged(object sender, EventArgs e)
        {
            bool isEnabled = chkEnableManagement.Checked;

            if (string.IsNullOrWhiteSpace(txtInstanceId.Text))
            {
                MessageBox.Show("Please enter a valid InstanceId before enabling management.", "ID Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                chkEnableManagement.Checked = false;
                return;
            }

            // Save the ID when the user enables management for the first time
            Properties.Settings.Default.DeviceInstanceId = txtInstanceId.Text;
            Properties.Settings.Default.Save();

            // Enable/disable the UI elements
            txtInstanceId.Enabled = !isEnabled;
            enabledMenuItem.Checked = isEnabled; // Sync the context menu

            if (isEnabled)
            {
                Log("Management ENABLED. Listening for system events...");
                notifyIcon.Icon = iconActive;
                notifyIcon.Text = "eGPU Manager (Active)";
                // Subscribe to system power and session events
                SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
                SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
            }
            else
            {
                Log("Management DISABLED.");
                notifyIcon.Icon = iconInactive;
                notifyIcon.Text = "eGPU Manager (Inactive)";
                // Unsubscribe to prevent memory leaks
                SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
                SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
            }
        }

        // --- System Event Handlers ---

        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            switch (e.Reason)
            {
                case SessionSwitchReason.SessionLock:
                    Log("EVENT: Session locked. Disabling eGPU...");
                    SetDeviceState(txtInstanceId.Text, false); // Disable
                    break;
                case SessionSwitchReason.SessionUnlock:
                    Log("EVENT: Session unlocked. Enabling eGPU...");
                    // Add a delay to give the system time to stabilize
                    Thread.Sleep(3000);
                    SetDeviceState(txtInstanceId.Text, true); // Enable
                    break;
            }
        }

        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Suspend:
                    Log("EVENT: System is suspending. Disabling eGPU...");
                    SetDeviceState(txtInstanceId.Text, false); // Disable
                    break;
                case PowerModes.Resume:
                    Log("EVENT: System is resuming. Enabling eGPU...");
                    // Add a longer delay after resuming from sleep
                    Thread.Sleep(5000);
                    SetDeviceState(txtInstanceId.Text, true); // Enable
                    break;
            }
        }

        // --- Window and Tray Icon Management ---

        private void Form1_Resize(object sender, EventArgs e)
        {
            // When the form is minimized, hide it and show a balloon tip
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                notifyIcon.ShowBalloonTip(1000, "eGPU Manager", "The application is now running in the background.", ToolTipIcon.Info);
            }
        }

        private void ShowMenuItem_Click(object sender, EventArgs e)
        {
            // Show the main window
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        }



        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            // Close the application
            this.Close();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Cleanup resources before closing
            SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
            SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
            notifyIcon?.Dispose();
        }

        // --- Logging and Utility Functions ---

        private void Log(string message)
        {
            // Thread-safe method to append text to the log box
            if (rtbLog.InvokeRequired)
            {
                rtbLog.Invoke(new Action<string>(Log), message);
                return;
            }
            rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            rtbLog.ScrollToCaret();
        }

        private Icon CreateIcon(Brush color)
        {
            // Programmatically creates a simple colored circle icon
            using (Bitmap bmp = new Bitmap(32, 32))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.FillEllipse(color, 0, 0, 31, 31);
                return Icon.FromHandle(bmp.GetHicon());
            }
        }

        #region P/Invoke for Device Management
        // This section contains complex code to interact with low-level Windows APIs
        // for enabling and disabling hardware devices.

        private bool SetDeviceState(string instanceId, bool enable)
        {
            try
            {
                Guid guid = Guid.Empty; // An empty Guid searches all devices
                IntPtr hDevInfo = SetupDiGetClassDevs(ref guid, instanceId, IntPtr.Zero, DIGCF_ALLCLASSES | DIGCF_DEVICEINTERFACE);
                if (hDevInfo == (IntPtr)INVALID_HANDLE_VALUE)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                SP_DEVINFO_DATA devInfoData = new SP_DEVINFO_DATA();
                devInfoData.cbSize = (uint)Marshal.SizeOf(devInfoData);

                if (!SetupDiEnumDeviceInfo(hDevInfo, 0, ref devInfoData))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                SP_PROPCHANGE_PARAMS propChangeParams = new SP_PROPCHANGE_PARAMS();
                propChangeParams.ClassInstallHeader.cbSize = (uint)Marshal.SizeOf(typeof(SP_CLASSINSTALL_HEADER));
                propChangeParams.ClassInstallHeader.InstallFunction = DIF_PROPERTYCHANGE;
                propChangeParams.StateChange = enable ? DICS_ENABLE : DICS_DISABLE;
                propChangeParams.Scope = DICS_FLAG_GLOBAL;
                propChangeParams.HwProfile = 0;

                if (!SetupDiSetClassInstallParams(hDevInfo, ref devInfoData, ref propChangeParams, (uint)Marshal.SizeOf(propChangeParams)))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                if (!SetupDiCallClassInstaller(DIF_PROPERTYCHANGE, hDevInfo, ref devInfoData))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                Log($"SUCCESS: Device has been {(enable ? "enabled" : "disabled")}.");
                return true;
            }
            catch (Exception ex)
            {
                Log($"ERROR while changing device state: {ex.Message}");
                return false;
            }
        }

        // P/Invoke constants and structures
        const int INVALID_HANDLE_VALUE = -1;
        const uint DIGCF_ALLCLASSES = 0x00000004;
        const uint DIGCF_DEVICEINTERFACE = 0x00000010;
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

        // P/Invoke method signatures
        [DllImport("setupapi.dll", SetLastError = true)]
        static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, string Enumerator, IntPtr hwndParent, uint Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiEnumDeviceInfo(IntPtr DeviceInfoSet, uint MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiSetClassInstallParams(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, ref SP_PROPCHANGE_PARAMS ClassInstallParams, uint ClassInstallParamsSize);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiCallClassInstaller(uint InstallFunction, IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData);

        #endregion
    }
}

