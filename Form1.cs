using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using System.ComponentModel;
using System.Security.Principal;

namespace eGPUManager
{
    public partial class Form1 : Form
    {
        // Variables for the system tray icon
        private NotifyIcon notifyIcon = null!;
        private ContextMenuStrip contextMenu = null!;
        private ToolStripMenuItem showMenuItem = null!;
        private ToolStripMenuItem enabledMenuItem = null!;
        private ToolStripMenuItem rediscoverMenuItem = null!;
        private ToolStripMenuItem exitMenuItem = null!;
        private Icon iconActive = null!;
        private Icon iconInactive = null!;
        private readonly string langCode = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        private ToolStripMenuItem installServiceMenuItem = null!;
        private ToolStripMenuItem serviceStatusMenuItem = null!;
        private ToolStripMenuItem startServiceMenuItem = null!;
        private ToolStripMenuItem stopServiceMenuItem = null!;
        private ToolStripMenuItem runAsAdminMenuItem = null!;
        private string logFilePath = null!;
        private readonly object logFileLock = new object();
        private System.Windows.Forms.Timer? autoscanTimer = null;
        private bool isAutoscanMode = false;

        private string? commandLinePciId = null;
        private bool? commandLineEnableMode = null;

        public Form1()
        {
            InitializeComponent();
            SetupApplication();
        }

        public Form1(string? pciId, bool enableMode)
        {
            InitializeComponent();
            commandLinePciId = pciId;
            commandLineEnableMode = enableMode;
            SetupApplication();
        }

        private void SetupApplication()
        {
            // Load icons (ideally, add them as project resources)
            // For simplicity, we create them programmatically.
            iconActive = CreateIcon(Color.LimeGreen);
            iconInactive = CreateIcon(Color.Red);

            // Setup the context menu for the tray icon
            contextMenu = new ContextMenuStrip();
            contextMenu.Opening += ContextMenu_Opening;
            showMenuItem = new ToolStripMenuItem(L("Show eGPU Manager", "Afficher le gestionnaire eGPU", "eGPU-Manager anzeigen", "Mostra il gestore eGPU", "Mostrar el administrador de eGPU"));
            showMenuItem.Click += ShowMenuItem_Click;
            enabledMenuItem = new ToolStripMenuItem(L("Enable Management", "Activer la gestion", "Verwaltung aktivieren", "Abilita gestione", "Habilitar gestión"));
            enabledMenuItem.Checked = false;
            enabledMenuItem.CheckOnClick = true;
            enabledMenuItem.CheckedChanged += EnabledMenuItem_CheckedChanged;
            rediscoverMenuItem = new ToolStripMenuItem(L("Rediscover eGPU", "Redécouvrir eGPU", "eGPU neu erkennen", "Riscopri eGPU", "Redescubrir eGPU"));
            rediscoverMenuItem.Click += RediscoverDeviceMenuItem_Click;
            installServiceMenuItem = new ToolStripMenuItem(L("Install as service", "Installer en tant que service", "Als Dienst installieren", "Installa come servizio", "Instalar como servicio"));
            installServiceMenuItem.Click += InstallServiceMenuItem_Click;
            serviceStatusMenuItem = new ToolStripMenuItem(L("Service status", "État du service", "Dienststatus", "Stato del servizio", "Estado del servicio"));
            serviceStatusMenuItem.Click += ServiceStatusMenuItem_Click;
            startServiceMenuItem = new ToolStripMenuItem(L("Start service", "Démarrer le service", "Dienst starten", "Avvia servizio", "Iniciar servicio"));
            startServiceMenuItem.Click += StartServiceMenuItem_Click;
            stopServiceMenuItem = new ToolStripMenuItem(L("Stop service", "Arrêter le service", "Dienst stoppen", "Interrompi servizio", "Detener servicio"));
            stopServiceMenuItem.Click += StopServiceMenuItem_Click;
            runAsAdminMenuItem = new ToolStripMenuItem(L("Run as administrator", "Exécuter en tant qu'administrateur", "Als Administrator ausführen", "Esegui come amministratore", "Ejecutar como administrador"));
            runAsAdminMenuItem.Click += RunAsAdminMenuItem_Click;
            exitMenuItem = new ToolStripMenuItem(L("Exit", "Quitter", "Beenden", "Esci", "Salir"));
            exitMenuItem.Click += ExitMenuItem_Click;

            chkEnableManagement.CheckedChanged += chkEnableManagement_CheckedChanged;

            contextMenu.Items.AddRange(new ToolStripItem[] { showMenuItem, enabledMenuItem, rediscoverMenuItem, installServiceMenuItem, serviceStatusMenuItem, startServiceMenuItem, stopServiceMenuItem, runAsAdminMenuItem, new ToolStripSeparator(), exitMenuItem });

            // Apply localized UI strings
            ApplyLocalization();
            UpdateAdminMenuState();

            // Setup the system tray icon itself
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = iconInactive;
            notifyIcon.Text = L("eGPU Manager (Inactive)", "Gestionnaire eGPU (Inactif)", "eGPU-Manager (Inaktiv)", "Gestore eGPU (Inattivo)", "Gestor de eGPU (Inactivo)");
            notifyIcon.Visible = true;
            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.DoubleClick += ShowMenuItem_Click;

            // Initialize persistent file logging
            InitializeFileLogging();

            // Log service installation/status at startup
            try
            {
                var svcStatus = GetServiceStatus();
                if (svcStatus.HasValue)
                {
                    string svcStatusText = svcStatus.Value switch
                    {
                        ServiceControllerStatus.Running => L("Running", "En cours d'exécution"),
                        ServiceControllerStatus.Stopped => L("Stopped", "Arrêté"),
                        ServiceControllerStatus.Paused => L("Paused", "En pause"),
                        ServiceControllerStatus.ContinuePending => L("Resuming", "Reprise en cours"),
                        ServiceControllerStatus.PausePending => L("Pausing", "Pause en cours"),
                        ServiceControllerStatus.StartPending => L("Starting", "Démarrage en cours"),
                        ServiceControllerStatus.StopPending => L("Stopping", "Arrêt en cours"),
                        _ => svcStatus.Value.ToString()
                    };
                    Log($"Service {ServiceName} is installed. Status: {svcStatusText}");
                    try { lblServiceStatus.Text = L("Installed", "Installé") + " — " + svcStatusText; } catch { }
                    try { notifyIcon.ShowBalloonTip(3000, L("Service status", "État du service"), L("Service is installed.", "Le service est installé.") + " (" + svcStatusText + ")", ToolTipIcon.Info); } catch { }
                }
                else
                {
                    Log($"Service {ServiceName} is not installed.");
                    try { lblServiceStatus.Text = L("Not installed", "Non installé"); } catch { }
                    try { notifyIcon.ShowBalloonTip(3000, L("Service status", "État du service"), L("Service is not installed.", "Le service n'est pas installé."), ToolTipIcon.Info); } catch { }
                }
            }
            catch (Exception ex)
            {
                Log($"Failed to query service status: {ex.Message}");
            }

            // Apply command-line arguments if provided
            if (!string.IsNullOrWhiteSpace(commandLinePciId))
            {
                txtInstanceId.Text = commandLinePciId;
                Properties.Settings.Default.DeviceInstanceId = commandLinePciId;
                Properties.Settings.Default.Save();
                Log($"Applied command-line PCI ID: {commandLinePciId}");
                UpdateGpuInfoDisplay(commandLinePciId);
                isAutoscanMode = false;
            }
            else
            {
                isAutoscanMode = true;
                Log("Autoscan mode enabled (no command-line PCI ID provided).");
                
                // Load the saved InstanceId from settings or detect the attached eGPU automatically
                string savedId = Properties.Settings.Default.DeviceInstanceId;
                if (!string.IsNullOrWhiteSpace(savedId))
                {
                    txtInstanceId.Text = savedId;
                    Log($"Loaded saved InstanceId: {savedId}");
                    UpdateGpuInfoDisplay(savedId);
                }
                else
                {
                    string? detectedId = DetectEgpuDeviceInstanceId();
                    if (!string.IsNullOrEmpty(detectedId))
                    {
                        txtInstanceId.Text = detectedId;
                        Properties.Settings.Default.DeviceInstanceId = detectedId;
                        Properties.Settings.Default.Save();
                        Log($"Detected eGPU InstanceId: {detectedId}");
                        UpdateGpuInfoDisplay(detectedId);
                    }
                    else
                    {
                        lblGpuInfo.Text = L("Unknown GPU", "GPU inconnu", "Unbekannte GPU", "GPU sconosciuta", "GPU desconocida");
                        Log("No eGPU device detected automatically.");
                    }
                }
                
                // Start autoscan timer to periodically check for eGPU
                StartAutoscan();
            }

            // Apply command-line enable mode if provided
            if (commandLineEnableMode.HasValue)
            {
                if (commandLineEnableMode.Value && IsRunningAsAdministrator())
                {
                    chkEnableManagement.Checked = true;
                    Log("Applied command-line enable mode: true");
                }
                else if (!commandLineEnableMode.Value)
                {
                    chkEnableManagement.Checked = false;
                    Log("Applied command-line enable mode: false");
                }
                else if (commandLineEnableMode.Value && !IsRunningAsAdministrator())
                {
                    Log("NOTE: Command-line enable mode requested but application is not running as administrator.");
                }
            }

            // Show hint in tray menu if not running as admin
            if (!IsRunningAsAdministrator())
            {
                Log("NOTE: Application is not running with administrative privileges. Device enable/disable requires elevation.");
            }
        }

        private async void RediscoverDeviceMenuItem_Click(object? sender, EventArgs e)
        {
            if (!IsRunningAsAdministrator())
            {
                Log("ERROR: Rediscovery requires administrator privileges. Please run the app as administrator.");
                MessageBox.Show(
                    L("Rediscovery requires administrator privileges. Please restart the app as administrator.", "La redécouverte nécessite des droits d'administrateur. Veuillez relancer l'application en tant qu'administrateur.", "Die Umwidmung erfordert Administratorrechte. Bitte starten Sie die App als Administrator neu.", "La riscoperta richiede privilegi di amministratore. Riavvia l'app come amministratore.", "El redescubrimiento requiere privilegios de administrador. Reinicie la aplicación como administrador."),
                    L("Administrator required", "Administrateur requis", "Administrator erforderlich", "Amministratore richiesto", "Se requiere administrador"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            Log("Rediscovery requested from menu...");
            bool ok = RequestDeviceRediscovery();
            if (ok)
            {
                Log("Device rediscovery triggered. Waiting for Windows to refresh...");
                await Task.Delay(3000);
                RescanDevices_Click(sender, e);
            }
        }

        private bool RequestDeviceRediscovery()
        {
            if (!IsRunningAsAdministrator())
            {
                Log("ERROR: Rediscovery requires administrator privileges. Please run the app as administrator.");
                return false;
            }

            string instanceId = txtInstanceId.Text.Trim();
            if (!string.IsNullOrEmpty(instanceId) && TryLocateDevNode(instanceId, out uint devInst))
            {
                Log($"Reenumerating device node for InstanceId '{instanceId}' (DevInst={devInst})...");
                if (TryReenumerateDevNode(devInst, "device-specific"))
                    return true;

                if (TryGetParentDevNode(devInst, out uint parentDevInst))
                {
                    Log($"Reenumerating parent device node (DevInst={parentDevInst})...");
                    if (TryReenumerateDevNode(parentDevInst, "parent"))
                        return true;
                }

                Log("WARN: Specific and parent rediscovery both failed. Falling back to root rediscovery.");
            }
            else
            {
                Log("Unable to locate device devnode by InstanceId; falling back to root rediscovery.");
            }

            if (TryReenumerateDevNode(0, "root"))
                return true;

            Log($"ERROR: Rediscovery failed after trying device-specific, parent, and root paths.");
            return false;
        }

        private bool TryReenumerateDevNode(uint devInst, string label)
        {
            uint flags = CM_REENUMERATE_SYNCHRONOUS | CM_REENUMERATE_RETRY_INSTALL;
            if (devInst == 0)
                flags |= CM_REENUMERATE_ROOT;

            int result = CM_Reenumerate_DevNode(devInst, flags);
            if (result == CR_SUCCESS)
            {
                Log($"{label} rediscovery request succeeded.");
                return true;
            }

            if (result == CR_ACCESS_DENIED)
            {
                Log($"ERROR: {label} rediscovery request failed with access denied. Administrator privileges are required.");
                return false;
            }

            Log($"WARN: {label} rediscovery request failed with code {result}.");
            return false;
        }

        private bool TryGetParentDevNode(uint devInst, out uint parentDevInst)
        {
            parentDevInst = 0;
            int result = CM_Get_Parent(out parentDevInst, devInst, 0);
            if (result == CR_SUCCESS)
            {
                return true;
            }

            Log($"Unable to locate parent devnode for DevInst={devInst}. CM_Get_Parent returned {result}.");
            return false;
        }

        private bool TryLocateDevNode(string instanceId, out uint devInst)
        {
            devInst = 0;
            int result = CM_Locate_DevNode(out devInst, instanceId, CM_LOCATE_DEVNODE_NORMAL);
            if (result == CR_SUCCESS)
            {
                return true;
            }

            Log($"Unable to locate devnode for InstanceId '{instanceId}'. CM_Locate_DevNode returned {result}.");
            return false;
        }

        private void InitializeFileLogging()
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "eGPUManager", "logs");
                Directory.CreateDirectory(dir);
                logFilePath = Path.Combine(dir, $"eGPUManager_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                lock (logFileLock)
                {
                    File.AppendAllText(logFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Log started{Environment.NewLine}");
                }
            }
            catch (Exception ex)
            {
                try { Debug.WriteLine("Failed to initialize file logging: " + ex.Message); } catch { }
            }
        }

        // --- UI Event Handlers ---

        private void EnabledMenuItem_CheckedChanged(object? sender, EventArgs e)
        {
            // Sync the menu's checked state with the checkbox in the main window
            chkEnableManagement.Checked = enabledMenuItem!.Checked;
        }

        private void chkEnableManagement_CheckedChanged(object? sender, EventArgs e)
        {
            bool isEnabled = chkEnableManagement.Checked;

            if (string.IsNullOrWhiteSpace(txtInstanceId.Text))
            {
                MessageBox.Show(L("Please enter a valid InstanceId before enabling management.", "Veuillez saisir un InstanceId valide avant d'activer la gestion.", "Bitte geben Sie eine gültige InstanceId ein, bevor Sie die Verwaltung aktivieren.", "Inserisci un InstanceId valido prima di abilitare la gestione.", "Ingrese un InstanceId válido antes de habilitar la gestión."), L("ID Missing", "ID manquant", "ID fehlt", "ID mancante", "ID faltante"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                chkEnableManagement.Checked = false;
                return;
            }

            // If enabling management and not elevated, prompt to restart as admin
            if (isEnabled && !IsRunningAsAdministrator())
            {
                var result = MessageBox.Show(L("Administrative privileges are required to manage devices. Restart as administrator now?", "Les privilèges administratifs sont requis pour gérer les périphériques. Redémarrer en tant qu'administrateur maintenant?", "Administratorrechte sind erforderlich, um Geräte zu verwalten. Jetzt als Administrator neu starten?", "I privilegi amministrativi sono necessari per gestire i dispositivi. Riavviare come amministratore ora?", "Se requieren privilegios de administrador para gestionar dispositivos. ¿Reiniciar como administrador ahora?"), L("Elevation Required", "Élévation requise", "Erhöhung erforderlich", "Elevazione richiesta", "Elevación requerida"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    Log("Requesting elevation and relaunching as administrator...");
                    bool relaunched = RelaunchAsAdministrator();
                    if (relaunched)
                    {
                        Application.Exit();
                        return;
                    }
                    else
                    {
                        MessageBox.Show(L("Failed to restart as administrator.", "Échec du redémarrage en tant qu'administrateur.", "Fehler beim Neustart als Administrator.", "Impossibile riavviare come amministratore.", "Error al reiniciar como administrador."), L("Error", "Erreur", "Fehler", "Errore", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                        chkEnableManagement.Checked = false;
                        return;
                    }
                }
                else
                {
                    chkEnableManagement.Checked = false;
                    return;
                }
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
                    // Kick off a background retry-enable routine to handle slow re-enumeration
                    string currentId = txtInstanceId.Text;
                    Task.Run(() => AttemptEnableWithRetry(currentId));
                    break;
            }
        }

        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            // Diagnostic log to confirm the event was received and the mode
            Log($"EVENT: PowerModeChanged -> {e.Mode}");
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
                    // Kick off a background retry-enable routine to handle slow re-enumeration
                    string resumedId = txtInstanceId.Text;
                    Task.Run(() => AttemptEnableWithRetry(resumedId));
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
                notifyIcon.ShowBalloonTip(1000, L("eGPU Manager", "Gestionnaire eGPU", "eGPU-Manager", "Gestore eGPU", "Gestor de eGPU"), L("The application is now running in the background.", "L'application fonctionne maintenant en arrière-plan.", "Die Anwendung läuft jetzt im Hintergrund.", "L'applicazione è ora in esecuzione in background.", "La aplicación se está ejecutando en segundo plano."), ToolTipIcon.Info);
            }
        }

        private void ShowMenuItem_Click(object? sender, EventArgs e)
        {
            // Show the main window
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            try
            {
                int w = Properties.Settings.Default.FormWidth;
                int h = Properties.Settings.Default.FormHeight;
                if (w > 0 && h > 0)
                {
                    this.ClientSize = new Size(w, h);
                }

                string ws = Properties.Settings.Default.WindowState;
                if (!string.IsNullOrEmpty(ws) && Enum.TryParse(ws, out FormWindowState state))
                {
                    // Avoid starting minimized
                    if (state != FormWindowState.Minimized)
                        this.WindowState = state;
                }
            }
            catch { }
        }

        private void RunAsAdminMenuItem_Click(object? sender, EventArgs e)
        {
            if (IsRunningAsAdministrator())
            {
                MessageBox.Show(L("Already running as administrator.", "Déjà exécuté en tant qu'administrateur.", "Wird bereits als Administrator ausgeführt.", "Già in esecuzione come amministratore.", "Ya se ejecuta como administrador."), L("Information", "Information", "Information", "Informazione", "Información"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var relaunched = RelaunchAsAdministrator();
            if (relaunched)
            {
                Application.Exit();
            }
            else
            {
                MessageBox.Show(L("Failed to restart as administrator.", "Échec du redémarrage en tant qu'administrateur."), L("Error", "Erreur"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RescanDevices_Click(object? sender, EventArgs e)
        {
            Log("Manual rescan started...");
            string? newId = DetectEgpuDeviceInstanceId();
            if (!string.IsNullOrEmpty(newId))
            {
                txtInstanceId.Text = newId;
                Properties.Settings.Default.DeviceInstanceId = newId;
                Properties.Settings.Default.Save();
                Log($"Detected eGPU InstanceId: {newId}");
                if (chkEnableManagement.Checked && IsRunningAsAdministrator())
                {
                    Log("Management enabled — enabling detected device...");
                    SetDeviceState(newId, true);
                }
            }
            else
            {
                lblGpuInfo.Text = L("Unknown GPU", "GPU inconnu");
                Log("No eGPU device found during rescan.");
            }
        }

        // Called from designer NumericUpDown ValueChanged lambdas
        public void NudRetryCount_ValueChanged(object? sender, EventArgs e)
        {
            try
            {
                if (sender is NumericUpDown nud)
                {
                    Properties.Settings.Default.RetryCount = (int)nud.Value;
                    Properties.Settings.Default.Save();
                    Log($"Settings: RetryCount set to {nud.Value}");
                }
            }
            catch { }
        }

        // Called from designer NumericUpDown ValueChanged lambdas
        public void NudBackoffSeconds_ValueChanged(object? sender, EventArgs e)
        {
            try
            {
                if (sender is NumericUpDown nud)
                {
                    int ms = (int)nud.Value * 1000;
                    Properties.Settings.Default.InitialBackoffMs = ms;
                    Properties.Settings.Default.Save();
                    Log($"Settings: InitialBackoffMs set to {ms}ms");
                }
            }
            catch { }
        }



        private void ExitMenuItem_Click(object? sender, EventArgs e)
        {
            // Close the application
            this.Close();
        }

        private void StartAutoscan()
        {
            if (autoscanTimer != null)
                return;

            autoscanTimer = new System.Windows.Forms.Timer();
            autoscanTimer.Interval = 5000; // Scan every 5 seconds
            autoscanTimer.Tick += (sender, e) => PerformAutoscan();
            autoscanTimer.Start();
            Log("Autoscan timer started (interval: 5 seconds).");
        }

        private void StopAutoscan()
        {
            if (autoscanTimer != null)
            {
                autoscanTimer.Stop();
                autoscanTimer.Dispose();
                autoscanTimer = null;
                Log("Autoscan timer stopped.");
            }
        }

        private void PerformAutoscan()
        {
            if (!isAutoscanMode)
                return;

            string currentId = txtInstanceId.Text.Trim();
            string? detectedId = DetectEgpuDeviceInstanceId();

            if (!string.IsNullOrEmpty(detectedId) && !string.Equals(currentId, detectedId, StringComparison.OrdinalIgnoreCase))
            {
                Log($"Autoscan detected new/different eGPU: {detectedId}");
                try
                {
                    Invoke(new Action(() =>
                    {
                        txtInstanceId.Text = detectedId;
                        Properties.Settings.Default.DeviceInstanceId = detectedId;
                        Properties.Settings.Default.Save();
                        UpdateGpuInfoDisplay(detectedId);
                        Log($"Autoscan updated device to: {detectedId}");
                    }));
                }
                catch { }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Stop autoscan timer if running
            StopAutoscan();

            // Persist window size/state
            try
            {
                if (this.WindowState == FormWindowState.Normal)
                {
                    Properties.Settings.Default.FormWidth = this.ClientSize.Width;
                    Properties.Settings.Default.FormHeight = this.ClientSize.Height;
                }
                else
                {
                    // Use RestoreBounds for maximized/minimized cases
                    Properties.Settings.Default.FormWidth = this.RestoreBounds.Width;
                    Properties.Settings.Default.FormHeight = this.RestoreBounds.Height;
                }
                Properties.Settings.Default.WindowState = this.WindowState.ToString();
                Properties.Settings.Default.Save();
            }
            catch { }

            // Cleanup resources before closing
            SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
            SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
            notifyIcon?.Dispose();
        }

        // --- Logging and Utility Functions ---

        private void Log(string message)
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
            // Write to file first (ignore file errors)
            try
            {
                if (!string.IsNullOrEmpty(logFilePath))
                {
                    lock (logFileLock)
                    {
                        File.AppendAllText(logFilePath, line);
                    }
                }
            }
            catch { }

            // Thread-safe UI append
            if (rtbLog.InvokeRequired)
            {
                rtbLog.Invoke(new Action(() =>
                {
                    rtbLog.AppendText(line);
                    rtbLog.ScrollToCaret();
                }));
                return;
            }

            rtbLog.AppendText(line);
            rtbLog.ScrollToCaret();
        }

        private Icon CreateIcon(Color color)
        {
            // Creates a stylized eGPU card icon
            using (Bitmap bmp = new Bitmap(32, 32))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Draw outer card border
                using (Pen borderPen = new Pen(Color.Gray, 1.5f))
                {
                    g.DrawRectangle(borderPen, 2, 6, 28, 18);
                }

                // Fill the card body with the color
                using (Brush cardBrush = new SolidBrush(color))
                {
                    g.FillRectangle(cardBrush, 3, 7, 26, 16);
                }

                // Draw connector pins at the bottom
                using (Pen pinPen = new Pen(color, 1.5f))
                {
                    // Left pin
                    g.DrawLine(pinPen, 8, 23, 8, 29);
                    // Middle pin
                    g.DrawLine(pinPen, 16, 23, 16, 29);
                    // Right pin
                    g.DrawLine(pinPen, 24, 23, 24, 29);
                }

                // Draw chip/circuit pattern on the card
                using (Brush chipBrush = new SolidBrush(Color.FromArgb(100, 255, 255, 255)))
                {
                    g.FillRectangle(chipBrush, 6, 10, 8, 8);
                    g.FillRectangle(chipBrush, 18, 10, 8, 8);
                }

                // Draw small dots as circuit elements
                using (Brush dotBrush = new SolidBrush(Color.White))
                {
                    g.FillEllipse(dotBrush, 6, 10, 2, 2);
                    g.FillEllipse(dotBrush, 12, 10, 2, 2);
                    g.FillEllipse(dotBrush, 18, 10, 2, 2);
                    g.FillEllipse(dotBrush, 24, 10, 2, 2);
                }

                return Icon.FromHandle(bmp.GetHicon());
            }
        }

        private string L(string english, string french, string german = "", string italian = "", string spanish = "")
        {
            // Use provided translations or fallback to English
            return langCode switch
            {
                "fr" => !string.IsNullOrEmpty(french) ? french : english,
                "de" => !string.IsNullOrEmpty(german) ? german : english,
                "it" => !string.IsNullOrEmpty(italian) ? italian : english,
                "es" => !string.IsNullOrEmpty(spanish) ? spanish : english,
                _ => english
            };
        }

        private string L(string english, string french)
        {
            // Backward compatibility for 2-parameter calls
            return L(english, french, "", "", "");
        }
        private void ApplyLocalization()
        {
            string appLabel = L("eGPU Manager", "Gestionnaire eGPU", "eGPU-Manager", "Gestore eGPU", "Gestor de eGPU");
            label1.Text = L("Device InstanceId:", "ID d'instance du périphérique :", "Geräte-InstanceId:", "ID istanza dispositivo:", "ID de instancia del dispositivo:");
            labelGpuInfo.Text = L("GPU:", "GPU :", "GPU:", "GPU:", "GPU:");
            chkEnableManagement.Text = L("Enable Automatic Management", "Activer la gestion automatique", "Automatische Verwaltung aktivieren", "Abilita gestione automatica", "Habilitar gestión automática");
            showMenuItem.Text = L("Show eGPU Manager", "Afficher le gestionnaire eGPU", "eGPU-Manager anzeigen", "Mostra il gestore eGPU", "Mostrar el administrador de eGPU");
            enabledMenuItem.Text = L("Enable Management", "Activer la gestion", "Verwaltung aktivieren", "Abilita gestione", "Habilitar gestión");
            rediscoverMenuItem.Text = L("Rediscover eGPU", "Redécouvrir eGPU", "eGPU neu erkennen", "Riscopri eGPU", "Redescubrir eGPU");
            installServiceMenuItem.Text = L("Install as service", "Installer en tant que service", "Als Dienst installieren", "Installa come servizio", "Instalar como servicio");
            serviceStatusMenuItem.Text = L("Service status", "État du service", "Dienststatus", "Stato del servizio", "Estado del servicio");
            startServiceMenuItem.Text = L("Start service", "Démarrer le service", "Dienst starten", "Avvia servizio", "Iniciar servicio");
            stopServiceMenuItem.Text = L("Stop service", "Arrêter le service", "Dienst stoppen", "Interrompi servizio", "Detener servicio");
            exitMenuItem.Text = L("Exit", "Quitter", "Beenden", "Esci", "Salir");
            Text = appLabel;
        }

        private void UpdateGpuInfoDisplay(string instanceId)
        {
            string info = GetGpuDisplayName(instanceId) ?? L("Unknown GPU", "GPU inconnu", "Unbekannte GPU", "GPU sconosciuta", "GPU desconocida");
            if (lblGpuInfo.InvokeRequired)
            {
                lblGpuInfo.Invoke(new Action(() => lblGpuInfo.Text = info));
            }
            else
            {
                lblGpuInfo.Text = info;
            }
        }

        private string? GetGpuDisplayName(string instanceId)
        {
            string? label = GetDevicePropertyForInstanceId(instanceId);
            if (!string.IsNullOrEmpty(label) && !IsLikelyPciBridgeLabel(label))
                return label;

            string? hardwarePattern = GetHardwarePatternFromInstanceId(instanceId);
            if (!string.IsNullOrEmpty(hardwarePattern))
            {
                string? displayId = FindDeviceInstanceIdByHardwarePattern(GUID_DEVCLASS_DISPLAY, hardwarePattern);
                if (!string.IsNullOrEmpty(displayId))
                {
                    label = GetDevicePropertyForInstanceId(displayId);
                    if (!string.IsNullOrEmpty(label))
                        return label;
                }
            }

            return label;
        }

        private bool IsLikelyPciBridgeLabel(string label)
        {
            string normalized = label.ToLowerInvariant();
            return normalized.Contains("pci") && (normalized.Contains("bridge") || normalized.Contains("host") || normalized.Contains("root"));
        }

        private string? GetHardwarePatternFromInstanceId(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
                return null;

            Match match = Regex.Match(instanceId, @"PCI\\VEN_[0-9A-Fa-f]{4}&DEV_[0-9A-Fa-f]{4}");
            return match.Success ? match.Value : null;
        }

        private string? GetDevicePropertyForInstanceId(string instanceId)
        {
            string? label = GetDevicePropertyStringByInstanceId(instanceId, SPDRP_FRIENDLYNAME);
            if (!string.IsNullOrEmpty(label))
                return label;

            return GetDevicePropertyStringByInstanceId(instanceId, SPDRP_DEVICEDESC);
        }

        private string? GetDevicePropertyStringByInstanceId(string instanceId, uint property)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
                return null;

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
                    string? id = GetDeviceInstanceId(hDevInfo, ref devInfoData);
                    if (string.IsNullOrEmpty(id) || !string.Equals(id, instanceId, StringComparison.OrdinalIgnoreCase))
                        continue;

                    uint requiredSize = 0;
                    uint regType;
                    SetupDiGetDeviceRegistryProperty(hDevInfo, ref devInfoData, property, out regType, null, 0, out requiredSize);
                    if (requiredSize == 0)
                        return null;

                    byte[] buffer = new byte[requiredSize];
                    if (SetupDiGetDeviceRegistryProperty(hDevInfo, ref devInfoData, property, out regType, buffer, requiredSize, out requiredSize))
                    {
                        string raw = Encoding.Unicode.GetString(buffer, 0, (int)requiredSize);
                        return raw.TrimEnd('\0');
                    }

                    return null;
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(hDevInfo);
            }

            return null;
        }

        private void UpdateAdminMenuState()
        {
            bool isAdmin = IsRunningAsAdministrator();
            ServiceControllerStatus? status = GetServiceStatus();
            bool serviceInstalled = status.HasValue;
            bool serviceRunning = status == ServiceControllerStatus.Running;

            runAsAdminMenuItem.Enabled = !isAdmin;
            rediscoverMenuItem.Enabled = isAdmin;
            enabledMenuItem.Enabled = isAdmin;
            installServiceMenuItem.Enabled = isAdmin && !serviceInstalled;
            serviceStatusMenuItem.Enabled = serviceInstalled;
            startServiceMenuItem.Enabled = isAdmin && serviceInstalled && !serviceRunning;
            stopServiceMenuItem.Enabled = isAdmin && serviceInstalled && serviceRunning;
            runAsAdminMenuItem.Text = isAdmin
                ? L("Running as administrator", "Exécuté en tant qu'administrateur", "Wird als Administrator ausgeführt", "In esecuzione come amministratore", "Ejecutándose como administrador")
                : L("Run as administrator", "Exécuter en tant qu'administrateur", "Als Administrator ausführen", "Esegui come amministratore", "Ejecutar como administrador");
        }

        private const string ServiceName = "eGPUManager";
        private const string ServiceDisplayName = "eGPU Manager Service";

        private void ContextMenu_Opening(object? sender, CancelEventArgs e)
        {
            UpdateAdminMenuState();
        }

        private bool IsServiceInstalled()
        {
            try
            {
                return ServiceController.GetServices().Any(s => string.Equals(s.ServiceName, ServiceName, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        private void InstallServiceMenuItem_Click(object? sender, EventArgs e)
        {
            if (!IsRunningAsAdministrator())
            {
                MessageBox.Show(
                    L("Installing a Windows service requires administrator privileges.", "L'installation d'un service Windows nécessite des privilèges administrateur.", "Zum Installieren eines Windows-Dienstes sind Administratorrechte erforderlich.", "L'installazione di un servizio Windows richiede privilegi di amministratore.", "La instalación de un servicio de Windows requiere privilegios de administrador."),
                    L("Administrator required", "Administrateur requis", "Administrator erforderlich", "Amministratore richiesto", "Se requiere administrador"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (IsServiceInstalled())
            {
                MessageBox.Show(
                    L("The service is already installed.", "Le service est déjà installé.", "Der Dienst ist bereits installiert.", "Il servizio è già installato.", "El servicio ya está instalado."),
                    L("Information", "Information", "Information", "Informazione", "Información"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? Application.ExecutablePath;
            string innerBinPath = $"\"{exePath}\" --service";
            string args = $"create {ServiceName} binPath= \"{innerBinPath}\" start= auto DisplayName= \"{ServiceDisplayName}\"";
            Log($"Installing service with command: sc {args}");

            var psi = new ProcessStartInfo("sc", args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try
            {
                using var proc = Process.Start(psi);
                if (proc == null)
                    throw new InvalidOperationException("Failed to start sc.exe");

                string output = proc.StandardOutput.ReadToEnd();
                string error = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                Log($"sc exit code: {proc.ExitCode}");
                Log($"sc output: {output}");
                if (!string.IsNullOrWhiteSpace(error))
                    Log($"sc error: {error}");

                if (proc.ExitCode == 0)
                {
                    MessageBox.Show(
                        L("Service installed successfully.", "Service installé avec succès.", "Dienst erfolgreich installiert.", "Servizio installato con successo.", "Servicio instalado correctamente."),
                        L("Success", "Succès", "Erfolg", "Successo", "Éxito"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    string message = L("Failed to install the service.", "Échec de l'installation du service.", "Die Installation des Dienstes ist fehlgeschlagen.", "Impossibile installare il servizio.", "No se pudo instalar el servicio.");
                    if (!string.IsNullOrWhiteSpace(error))
                        message += $"\n{error}";
                    if (!string.IsNullOrWhiteSpace(output))
                        message += $"\n{output}";

                    MessageBox.Show(
                        message,
                        L("Error", "Erreur", "Fehler", "Errore", "Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    L("Failed to install the service.", "Échec de l'installation du service.", "Die Installation des Dienstes ist fehlgeschlagen.", "Impossibile installare il servizio.", "No se pudo instalar el servicio.") + $"\n{ex.Message}",
                    L("Error", "Erreur", "Fehler", "Errore", "Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ServiceStatusMenuItem_Click(object? sender, EventArgs e)
        {
            ServiceControllerStatus? status = GetServiceStatus();
            if (!status.HasValue)
            {
                MessageBox.Show(
                    L("The service is not installed.", "Le service n'est pas installé.", "Der Dienst ist nicht installiert.", "Il servizio non è installato.", "El servicio no está instalado."),
                    L("Service status", "État du service", "Dienststatus", "Stato del servizio", "Estado del servicio"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            MessageBox.Show(
                L("Service is installed.", "Le service est installé.", "Der Dienst ist installiert.", "Il servizio è installato.", "El servicio está instalado.") + $"\n{L("Status:", "Statut :", "Status:", "Stato:", "Estado:")} {status}",
                L("Service status", "État du service", "Dienststatus", "Stato del servizio", "Estado del servicio"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void StartServiceMenuItem_Click(object? sender, EventArgs e)
        {
            TryChangeServiceState(ServiceControllerStatus.Running, L("Starting service...", "Démarrage du service...", "Dienst wird gestartet...", "Avvio del servizio...", "Iniciando servicio..."));
        }

        private void StopServiceMenuItem_Click(object? sender, EventArgs e)
        {
            TryChangeServiceState(ServiceControllerStatus.Stopped, L("Stopping service...", "Arrêt du service...", "Dienst wird gestoppt...", "Arresto del servizio...", "Deteniendo servicio..."));
        }

        private void TryChangeServiceState(ServiceControllerStatus desiredState, string actionMessage)
        {
            if (!IsServiceInstalled())
            {
                MessageBox.Show(
                    L("The service is not installed.", "Le service n'est pas installé.", "Der Dienst ist nicht installiert.", "Il servizio non è installato.", "El servicio no está instalado."),
                    L("Service status", "État du service", "Dienststatus", "Stato del servizio", "Estado del servicio"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (!IsRunningAsAdministrator())
            {
                MessageBox.Show(
                    L("Changing service state requires administrator privileges.", "La modification de l'état du service nécessite des privilèges administrateur.", "Das Ändern des Dienststatus erfordert Administratorrechte.", "La modifica dello stato del servizio richiede privilegi di amministratore.", "Cambiar el estado del servicio requiere privilegios de administrador."),
                    L("Administrator required", "Administrateur requis", "Administrator erforderlich", "Amministratore richiesto", "Se requiere administrador"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using var controller = new ServiceController(ServiceName);
                controller.Refresh();
                if (desiredState == ServiceControllerStatus.Running)
                {
                    if (controller.Status == ServiceControllerStatus.Running)
                    {
                        MessageBox.Show(L("Service is already running.", "Le service est déjà en cours d'exécution.", "Der Dienst läuft bereits.", "Il servizio è già in esecuzione.", "El servicio ya está en ejecución."), L("Information", "Information", "Information", "Informazione", "Información"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    controller.Start();
                }
                else
                {
                    if (controller.Status == ServiceControllerStatus.Stopped)
                    {
                        MessageBox.Show(L("Service is already stopped.", "Le service est déjà arrêté.", "Der Dienst ist bereits gestoppt.", "Il servizio è già arrestato.", "El servicio ya está detenido."), L("Information", "Information", "Information", "Informazione", "Información"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    controller.Stop();
                }

                controller.WaitForStatus(desiredState, TimeSpan.FromSeconds(15));
                MessageBox.Show(
                    actionMessage + " " + L("completed successfully.", "terminé avec succès.", "erfolgreich abgeschlossen.", "completato con successo.", "completado con éxito."),
                    L("Success", "Succès", "Erfolg", "Successo", "Éxito"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    L("Unable to change service state.", "Impossible de modifier l'état du service.", "Dienststatus kann nicht geändert werden.", "Impossibile modificare lo stato del servizio.", "No se puede cambiar el estado del servicio.") + $"\n{ex.Message}",
                    L("Error", "Erreur", "Fehler", "Errore", "Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private ServiceControllerStatus? GetServiceStatus()
        {
            try
            {
                using var controller = new ServiceController(ServiceName);
                controller.Refresh();
                return controller.Status;
            }
            catch
            {
                return null;
            }
        }

        private static readonly Guid GUID_DEVCLASS_DISPLAY = new("4d36e968-e325-11ce-bfc1-08002be10318");

        private string? DetectEgpuDeviceInstanceId()
        {
            string[] searchPatterns = new[]
            {
                "PCI\\VEN_1002&DEV_1479",
                "PCI\\VEN_1002&DEV_"
            };

            foreach (string pattern in searchPatterns)
            {
                string? deviceId = FindDeviceInstanceIdByHardwarePattern(GUID_DEVCLASS_DISPLAY, pattern);
                if (!string.IsNullOrEmpty(deviceId))
                {
                    return deviceId;
                }
            }

            foreach (string pattern in searchPatterns)
            {
                string? deviceId = FindDeviceInstanceIdByHardwarePattern(Guid.Empty, pattern);
                if (!string.IsNullOrEmpty(deviceId))
                {
                    return deviceId;
                }
            }

            return null;
        }

        private string? FindDeviceInstanceIdByHardwarePattern(Guid classGuid, string hardwarePattern)
        {
            Guid guid = classGuid;
            IntPtr hDevInfo = SetupDiGetClassDevs(ref guid, null, IntPtr.Zero, DIGCF_PRESENT | (classGuid == Guid.Empty ? DIGCF_ALLCLASSES : 0));
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

        private async Task AttemptEnableWithRetry(string instanceId)
        {
            int maxRetries = Properties.Settings.Default.RetryCount > 0 ? Properties.Settings.Default.RetryCount : 6;
            int initialBackoffMs = Properties.Settings.Default.InitialBackoffMs > 0 ? Properties.Settings.Default.InitialBackoffMs : 1000;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                if (!string.IsNullOrWhiteSpace(instanceId) && DeviceInstanceExists(instanceId))
                {
                    Log($"Device found (by InstanceId) before attempt #{attempt + 1}. Enabling {instanceId}...");
                    SetDeviceState(instanceId, true);
                    return;
                }

                string? detected = DetectEgpuDeviceInstanceId();
                if (!string.IsNullOrEmpty(detected))
                {
                    // Update UI and settings on the UI thread
                    try
                    {
                        Invoke(new Action(() =>
                        {
                            txtInstanceId.Text = detected;
                            Properties.Settings.Default.DeviceInstanceId = detected;
                            Properties.Settings.Default.Save();
                            UpdateGpuInfoDisplay(detected);
                        }));
                    }
                    catch { }

                    Log($"Detected eGPU InstanceId on attempt #{attempt + 1}: {detected}. Enabling...");
                    SetDeviceState(detected, true);
                    return;
                }

                int delayMs = initialBackoffMs * (int)Math.Pow(2, attempt); // exponential backoff: base, base*2, base*4...
                Log($"Retry #{attempt + 1}: device not found yet. Waiting {delayMs}ms before next attempt...");
                await Task.Delay(delayMs);
            }

            Log("ERROR: eGPU not found after retries. Will not enable.");
        }

        private bool DeviceInstanceExists(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
                return false;

            Guid guid = Guid.Empty;
            IntPtr hDevInfo = SetupDiGetClassDevs(ref guid, null, IntPtr.Zero, DIGCF_ALLCLASSES | DIGCF_PRESENT);
            if (hDevInfo == (IntPtr)INVALID_HANDLE_VALUE)
                return false;

            try
            {
                SP_DEVINFO_DATA devInfoData = new SP_DEVINFO_DATA();
                devInfoData.cbSize = (uint)Marshal.SizeOf(devInfoData);

                for (uint i = 0; SetupDiEnumDeviceInfo(hDevInfo, i, ref devInfoData); i++)
                {
                    string? id = GetDeviceInstanceId(hDevInfo, ref devInfoData);
                    if (!string.IsNullOrEmpty(id) && string.Equals(id, instanceId, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(hDevInfo);
            }

            return false;
        }

        #region P/Invoke for Device Management
        // This section contains complex code to interact with low-level Windows APIs
        // for enabling and disabling hardware devices.

        private bool SetDeviceState(string instanceId, bool enable)
        {
            try
            {
                Log($"Attempting to {(enable ? "enable" : "disable")} device: {instanceId}");
                Guid guid = Guid.Empty; // An empty Guid searches all devices
                IntPtr hDevInfo = SetupDiGetClassDevs(ref guid, instanceId, IntPtr.Zero, DIGCF_ALLCLASSES | DIGCF_DEVICEINTERFACE);
                if (hDevInfo == (IntPtr)INVALID_HANDLE_VALUE)
                {
                    int err = Marshal.GetLastWin32Error();
                    Log($"ERROR: SetupDiGetClassDevs failed. Win32Error={err}");
                    throw new Win32Exception(err);
                }

                SP_DEVINFO_DATA devInfoData = new SP_DEVINFO_DATA();
                devInfoData.cbSize = (uint)Marshal.SizeOf(devInfoData);

                if (!SetupDiEnumDeviceInfo(hDevInfo, 0, ref devInfoData))
                {
                    int err = Marshal.GetLastWin32Error();
                    Log($"ERROR: SetupDiEnumDeviceInfo failed. Win32Error={err}");
                    throw new Win32Exception(err);
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
                    throw new Win32Exception(err);
                }

                bool called = SetupDiCallClassInstaller(DIF_PROPERTYCHANGE, hDevInfo, ref devInfoData);
                if (!called)
                {
                    int err = Marshal.GetLastWin32Error();
                    Log($"ERROR: SetupDiCallClassInstaller failed. Win32Error={err}");
                    throw new Win32Exception(err);
                }

                Log($"SUCCESS: Device has been {(enable ? "enabled" : "disabled")}.");
                return true;
            }
            catch (Exception ex)
            {
                Log($"ERROR while changing device state: {ex.Message}");
                if (ex is Win32Exception wex)
                {
                    Log($"Win32Exception.NativeErrorCode={wex.NativeErrorCode}");
                }
                return false;
            }
        }

        // P/Invoke constants and structures
        const int INVALID_HANDLE_VALUE = -1;
        const uint DIGCF_ALLCLASSES = 0x00000004;
        const uint DIGCF_PRESENT = 0x00000002;
        const uint DIGCF_DEVICEINTERFACE = 0x00000010;
        const uint SPDRP_DEVICEDESC = 0x00000000;
        const uint SPDRP_HARDWAREID = 0x00000001;
        const uint SPDRP_FRIENDLYNAME = 0x0000000C;
        const int CR_SUCCESS = 0;
        const int CR_ACCESS_DENIED = 5;
        const uint CM_REENUMERATE_SYNCHRONOUS = 0x00000001;
        const uint CM_REENUMERATE_RETRY_INSTALL = 0x00000002;
        const uint CM_REENUMERATE_ROOT = 0x00000004;
        const uint CM_LOCATE_DEVNODE_NORMAL = 0x00000000;
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
        [DllImport("cfgmgr32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int CM_Reenumerate_DevNode(uint dnDevNode, uint ulFlags);

        [DllImport("cfgmgr32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int CM_Get_Parent(out uint pdnDevInst, uint dnDevInst, uint ulFlags);

        [DllImport("cfgmgr32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int CM_Locate_DevNode(out uint pdnDevNode, string pDeviceID, uint ulFlags);

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

        #endregion

        private bool IsRunningAsAdministrator()
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }

        private bool RelaunchAsAdministrator()
        {
            try
            {
                var exePath = Process.GetCurrentProcess().MainModule!.FileName!;
                var psi = new ProcessStartInfo(exePath)
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    WorkingDirectory = Environment.CurrentDirectory
                };
                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                Log($"ERROR: Relaunch as admin failed: {ex.Message}");
                return false;
            }
        }
    }
}

