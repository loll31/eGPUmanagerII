using System.ServiceProcess;

namespace eGPUManager
{
    internal static class Program
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // Detect language
            string langCode = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

            // Parse command-line arguments
            bool showHelp = false;
            bool serviceMode = false;
            string? pciId = null;
            bool enableMode = false;
            
            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i] == "--help" || args[i] == "-h" || args[i] == "/?" || args[i] == "/help"))
                {
                    showHelp = true;
                }
                else if (args[i] == "--service" || args[i] == "-s")
                {
                    serviceMode = true;
                }
                else if ((args[i] == "--pciid" || args[i] == "-p") && i + 1 < args.Length)
                {
                    pciId = args[i + 1];
                    i++;
                }
                else if (args[i] == "--enable" || args[i] == "-e")
                {
                    enableMode = true;
                }
                else if (args[i] == "--disable" || args[i] == "-d")
                {
                    enableMode = false;
                }
            }

            if (showHelp)
            {
                AttachConsole(ATTACH_PARENT_PROCESS);
                DisplayHelp(langCode);
                return;
            }

            if (serviceMode)
            {
                ServiceBase.Run(new GpuManagerService(pciId, enableMode));
                return;
            }
            
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1(pciId, enableMode));
        }

        static void DisplayHelp(string langCode)
        {
            string helpText = langCode switch
            {
                "fr" => "eGPU Manager - Aide\n\nUsage: eGPUManager.exe [options]\n\nOptions:\n  -p, --pciid <id>    Définir l'ID PCI de l'eGPU\n  -s, --service       Démarrer en mode service\n  -e, --enable        Activer la gestion automatique au démarrage\n  -d, --disable       Désactiver la gestion automatique au démarrage\n  -h, --help          Afficher ce message d'aide\n\nExemples:\n  eGPUManager.exe --pciid \"PCI\\VEN_1002&DEV_1479...\" --enable\n  eGPUManager.exe --service\n",
                "de" => "eGPU Manager - Hilfe\n\nVerwendung: eGPUManager.exe [options]\n\nOptionen:\n  -p, --pciid <id>    eGPU-PCI-ID setzen\n  -s, --service       Im Service-Modus starten\n  -e, --enable        Automatische Verwaltung beim Start aktivieren\n  -d, --disable       Automatische Verwaltung beim Start deaktivieren\n  -h, --help          Diese Hilfemeldung anzeigen\n\nBeispiele:\n  eGPUManager.exe --pciid \"PCI\\VEN_1002&DEV_1479...\" --enable\n  eGPUManager.exe --service\n",
                "it" => "eGPU Manager - Aiuto\n\nUtilizzo: eGPUManager.exe [options]\n\nOpzioni:\n  -p, --pciid <id>    Impostare l'ID PCI eGPU\n  -s, --service       Avvia in modalità servizio\n  -e, --enable        Abilita la gestione automatica all'avvio\n  -d, --disable       Disabilita la gestione automatica all'avvio\n  -h, --help          Mostra questo messaggio di aiuto\n\nEsempi:\n  eGPUManager.exe --pciid \"PCI\\VEN_1002&DEV_1479...\" --enable\n  eGPUManager.exe --service\n",
                "es" => "eGPU Manager - Ayuda\n\nUso: eGPUManager.exe [opciones]\n\nOpciones:\n  -p, --pciid <id>    Establecer el ID de PCI eGPU\n  -s, --service       Iniciar en modo servicio\n  -e, --enable        Habilitar la gestión automática al inicio\n  -d, --disable       Deshabilitar la gestión automática al inicio\n  -h, --help          Mostrar este mensaje de ayuda\n\nEjemplos:\n  eGPUManager.exe --pciid \"PCI\\VEN_1002&DEV_1479...\" --enable\n  eGPUManager.exe --service\n",
                _ => "eGPU Manager - Help\n\nUsage: eGPUManager.exe [options]\n\nOptions:\n  -p, --pciid <id>    Set the eGPU PCI ID\n  -s, --service       Start in service mode\n  -e, --enable        Enable automatic management on startup\n  -d, --disable       Disable automatic management on startup\n  -h, --help          Display this help message\n\nExamples:\n  eGPUManager.exe --pciid \"PCI\\VEN_1002&DEV_1479...\" --enable\n  eGPUManager.exe --service\n"
            };
            System.Console.WriteLine(helpText);
        }
    }
}