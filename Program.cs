using System.ServiceProcess;
using System.Security.Principal;

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
            bool adminMode = false;
            bool statusMode = false;
            bool installService = false;
            bool uninstallService = false;
            bool startService = false;
            bool stopService = false;
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
                else if (args[i] == "--admin" || args[i] == "-admin")
                {
                    adminMode = true;
                }
                else if (args[i] == "--status" || args[i] == "-status")
                {
                    statusMode = true;
                }
                else if (args[i] == "--install" || args[i] == "-i")
                {
                    installService = true;
                }
                else if (args[i] == "--uninstall" || args[i] == "--remove" || args[i] == "-u")
                {
                    uninstallService = true;
                }
                else if (args[i] == "--start")
                {
                    startService = true;
                }
                else if (args[i] == "--stop")
                {
                    stopService = true;
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

            if (adminMode && !IsRunningAsAdministrator())
            {
                RelaunchAsAdministrator(args);
                return;
            }

            if (installService)
            {
                AttachConsole(ATTACH_PARENT_PROCESS);
                InstallService(langCode);
                return;
            }

            if (uninstallService)
            {
                AttachConsole(ATTACH_PARENT_PROCESS);
                StopAndUninstallService(langCode);
                return;
            }

            if (startService)
            {
                AttachConsole(ATTACH_PARENT_PROCESS);
                ChangeServiceState(ServiceControllerStatus.Running, langCode);
                return;
            }

            if (stopService)
            {
                AttachConsole(ATTACH_PARENT_PROCESS);
                ChangeServiceState(ServiceControllerStatus.Stopped, langCode);
                return;
            }

            if (statusMode)
            {
                AttachConsole(ATTACH_PARENT_PROCESS);
                CheckServiceStatus(langCode);
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
                "fr" => "eGPU Manager - Aide\n\nUsage: eGPUManager.exe [options]\n\nOptions:\n  -p, --pciid <id>    Définir l'ID PCI de l'eGPU\n  -s, --service       Démarrer en mode service\n  --status, -status   Vérifier l'état du service\n  --install, -i       Installer le service Windows\n  --uninstall, --remove, -u   Désinstaller le service Windows\n  --start             Démarrer le service Windows\n  --stop              Arrêter le service Windows\n  --admin, -admin     Redémarrer avec privilèges administrateur\n  -e, --enable        Activer la gestion automatique au démarrage\n  -d, --disable       Désactiver la gestion automatique au démarrage\n  -h, --help          Afficher ce message d'aide\n\nExemples:\n  eGPUManager.exe --pciid \"PCI\\VEN_1002&DEV_1479...\" --enable\n  eGPUManager.exe --service\n  eGPUManager.exe --status\n  eGPUManager.exe --admin --install\n  eGPUManager.exe --admin --start\n  eGPUManager.exe --admin --stop\n",
                "de" => "eGPU Manager - Hilfe\n\nVerwendung: eGPUManager.exe [options]\n\nOptionen:\n  -p, --pciid <id>    eGPU-PCI-ID setzen\n  -s, --service       Im Service-Modus starten\n  --status, -status   Service-Status prüfen\n  --install, -i       Windows-Dienst installieren\n  --uninstall, --remove, -u   Windows-Dienst deinstallieren\n  --start             Windows-Dienst starten\n  --stop              Windows-Dienst stoppen\n  --admin, -admin     Mit Administratorrechten neu starten\n  -e, --enable        Automatische Verwaltung beim Start aktivieren\n  -d, --disable       Automatische Verwaltung beim Start deaktivieren\n  -h, --help          Diese Hilfemeldung anzeigen\n\nBeispiele:\n  eGPUManager.exe --pciid \"PCI\\VEN_1002&DEV_1479...\" --enable\n  eGPUManager.exe --service\n  eGPUManager.exe --status\n  eGPUManager.exe --admin --install\n  eGPUManager.exe --admin --start\n  eGPUManager.exe --admin --stop\n",
                "it" => "eGPU Manager - Aiuto\n\nUtilizzo: eGPUManager.exe [options]\n\nOpzioni:\n  -p, --pciid <id>    Impostare l'ID PCI eGPU\n  -s, --service       Avvia in modalità servizio\n  --status, -status   Verifica lo stato del servizio\n  --install, -i       Installa il servizio di Windows\n  --uninstall, --remove, -u   Disinstalla il servizio di Windows\n  --start             Avvia il servizio di Windows\n  --stop              Arresta il servizio di Windows\n  --admin, -admin     Riavvia con privilegi di amministratore\n  -e, --enable        Abilita la gestione automatica all'avvio\n  -d, --disable       Disabilita la gestione automatica all'avvio\n  -h, --help          Mostra questo messaggio di aiuto\n\nEsempi:\n  eGPUManager.exe --pciid \"PCI\\VEN_1002&DEV_1479...\" --enable\n  eGPUManager.exe --service\n  eGPUManager.exe --status\n  eGPUManager.exe --admin --install\n  eGPUManager.exe --admin --start\n  eGPUManager.exe --admin --stop\n",
                "es" => "eGPU Manager - Ayuda\n\nUso: eGPUManager.exe [opciones]\n\nOpciones:\n  -p, --pciid <id>    Establecer el ID de PCI eGPU\n  -s, --service       Iniciar en modo servicio\n  --status, -status   Verificar el estado del servicio\n  --install, -i       Instalar el servicio de Windows\n  --uninstall, --remove, -u   Desinstalar el servicio de Windows\n  --start             Iniciar el servicio de Windows\n  --stop              Detener el servicio de Windows\n  --admin, -admin     Reiniciar con privilegios de administrador\n  -e, --enable        Habilitar la gestión automática al inicio\n  -d, --disable       Deshabilitar la gestión automática al inicio\n  -h, --help          Mostrar este mensaje de ayuda\n\nEjemplos:\n  eGPUManager.exe --pciid \"PCI\\VEN_1002&DEV_1479...\" --enable\n  eGPUManager.exe --service\n  eGPUManager.exe --status\n  eGPUManager.exe --admin --install\n  eGPUManager.exe --admin --start\n  eGPUManager.exe --admin --stop\n",
                _ => "eGPU Manager - Help\n\nUsage: eGPUManager.exe [options]\n\nOptions:\n  -p, --pciid <id>    Set the eGPU PCI ID\n  -s, --service       Start in service mode\n  --status, -status   Check service status\n  --install, -i       Install the Windows service\n  --uninstall, --remove, -u   Uninstall the Windows service\n  --start             Start the Windows service\n  --stop              Stop the Windows service\n  --admin, -admin     Restart with administrator privileges\n  -e, --enable        Enable automatic management on startup\n  -d, --disable       Disable automatic management on startup\n  -h, --help          Display this help message\n\nExamples:\n  eGPUManager.exe --pciid \"PCI\\VEN_1002&DEV_1479...\" --enable\n  eGPUManager.exe --service\n  eGPUManager.exe --status\n  eGPUManager.exe --admin --install\n  eGPUManager.exe --admin --start\n  eGPUManager.exe --admin --stop\n"
            };
            System.Console.WriteLine(helpText);
        }

        static void CheckServiceStatus(string langCode)
        {
            try
            {
                ServiceController[] services = ServiceController.GetServices();
                ServiceController? eGpuService = services.FirstOrDefault(s => s.ServiceName == "eGPUManager");

                if (eGpuService == null)
                {
                    string notInstalledMsg = langCode switch
                    {
                        "fr" => "État du service eGPUManager: Non installé",
                        "de" => "eGPUManager-Dienststatus: Nicht installiert",
                        "it" => "Stato del servizio eGPUManager: Non installato",
                        "es" => "Estado del servicio eGPUManager: No instalado",
                        _ => "eGPUManager service status: Not installed"
                    };
                    System.Console.WriteLine(notInstalledMsg);
                    return;
                }

                eGpuService.Refresh();
                string statusStr = eGpuService.Status switch
                {
                    ServiceControllerStatus.Running => langCode switch
                    {
                        "fr" => "En cours d'exécution",
                        "de" => "Läuft",
                        "it" => "In esecuzione",
                        "es" => "En ejecución",
                        _ => "Running"
                    },
                    ServiceControllerStatus.Stopped => langCode switch
                    {
                        "fr" => "Arrêté",
                        "de" => "Gestoppt",
                        "it" => "Arrestato",
                        "es" => "Detenido",
                        _ => "Stopped"
                    },
                    ServiceControllerStatus.StartPending => langCode switch
                    {
                        "fr" => "Démarrage en cours",
                        "de" => "Start steht aus",
                        "it" => "Inizio in sospeso",
                        "es" => "Inicio pendiente",
                        _ => "Start Pending"
                    },
                    ServiceControllerStatus.StopPending => langCode switch
                    {
                        "fr" => "Arrêt en cours",
                        "de" => "Stopp steht aus",
                        "it" => "Arresto in sospeso",
                        "es" => "Parada pendiente",
                        _ => "Stop Pending"
                    },
                    ServiceControllerStatus.ContinuePending => langCode switch
                    {
                        "fr" => "Continuation en cours",
                        "de" => "Fortsetzung steht aus",
                        "it" => "Continuazione in sospeso",
                        "es" => "Continuación pendiente",
                        _ => "Continue Pending"
                    },
                    ServiceControllerStatus.PausePending => langCode switch
                    {
                        "fr" => "Pause en cours",
                        "de" => "Pause steht aus",
                        "it" => "Pausa in sospeso",
                        "es" => "Pausa pendiente",
                        _ => "Pause Pending"
                    },
                    ServiceControllerStatus.Paused => langCode switch
                    {
                        "fr" => "Suspendu",
                        "de" => "Angehalten",
                        "it" => "In pausa",
                        "es" => "En pausa",
                        _ => "Paused"
                    },
                    _ => langCode switch
                    {
                        "fr" => "Inconnu",
                        "de" => "Unbekannt",
                        "it" => "Sconosciuto",
                        "es" => "Desconocido",
                        _ => "Unknown"
                    }
                };

                string statusLabel = langCode switch
                {
                    "fr" => "État du service eGPUManager:",
                    "de" => "eGPUManager-Dienststatus:",
                    "it" => "Stato del servizio eGPUManager:",
                    "es" => "Estado del servicio eGPUManager:",
                    _ => "eGPUManager service status:"
                };

                System.Console.WriteLine($"{statusLabel} {statusStr}");
            }
            catch (Exception ex)
            {
                string errorMsg = langCode switch
                {
                    "fr" => $"Erreur lors de la vérification du service: {ex.Message}",
                    "de" => $"Fehler beim Überprüfen des Dienstes: {ex.Message}",
                    "it" => $"Errore durante il controllo del servizio: {ex.Message}",
                    "es" => $"Error al comprobar el servicio: {ex.Message}",
                    _ => $"Error checking service: {ex.Message}"
                };
                System.Console.WriteLine(errorMsg);
            }
        }

        static bool IsRunningAsAdministrator()
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }

        static string QuoteArgument(string arg)
        {
            if (string.IsNullOrEmpty(arg))
                return "\"\"";

            if (arg.Contains(' ') || arg.Contains('\t') || arg.Contains('"'))
                return "\"" + arg.Replace("\"", "\\\"") + "\"";

            return arg;
        }

        static void RelaunchAsAdministrator(string[] args)
        {
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? System.Windows.Forms.Application.ExecutablePath;
            string arguments = string.Join(" ", args.Where(a => a != "--admin" && a != "-admin").Select(QuoteArgument));

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas",
                Arguments = arguments
            };

            try
            {
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                AttachConsole(ATTACH_PARENT_PROCESS);
                System.Console.WriteLine($"Failed to relaunch as administrator: {ex.Message}");
            }
        }

        static void InstallService(string langCode)
        {
            if (!IsRunningAsAdministrator())
            {
                System.Console.WriteLine(langCode switch
                {
                    "fr" => "Administrateur requis pour installer le service.",
                    "de" => "Administratorrechte sind erforderlich, um den Dienst zu installieren.",
                    "it" => "Sono necessari privilegi di amministratore per installare il servizio.",
                    "es" => "Se requieren privilegios de administrador para instalar el servicio.",
                    _ => "Administrator privileges are required to install the service."
                });
                return;
            }

            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? System.Windows.Forms.Application.ExecutablePath;
            string innerBinPath = $"\"{exePath}\" --service";
            string args = $"create eGPUManager binPath= \"{innerBinPath}\" start= auto DisplayName= \"eGPU Manager\"";

            var psi = new System.Diagnostics.ProcessStartInfo("sc", args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try
            {
                using var proc = System.Diagnostics.Process.Start(psi);
                if (proc == null)
                {
                    System.Console.WriteLine(langCode switch
                    {
                        "fr" => "Impossible de démarrer sc.exe.",
                        "de" => "Kann sc.exe nicht starten.",
                        "it" => "Impossibile avviare sc.exe.",
                        "es" => "No se puede iniciar sc.exe.",
                        _ => "Failed to start sc.exe."
                    });
                    return;
                }

                string output = proc.StandardOutput.ReadToEnd();
                string error = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode == 0)
                {
                    System.Console.WriteLine(langCode switch
                    {
                        "fr" => "Service installé avec succès.",
                        "de" => "Dienst erfolgreich installiert.",
                        "it" => "Servizio installato con successo.",
                        "es" => "Servicio instalado correctamente.",
                        _ => "Service installed successfully."
                    });
                }
                else
                {
                    System.Console.WriteLine((langCode switch
                    {
                        "fr" => "Échec de l'installation du service.",
                        "de" => "Die Dienstinstallation ist fehlgeschlagen.",
                        "it" => "Impossibile installare il servizio.",
                        "es" => "No se pudo instalar el servicio.",
                        _ => "Failed to install the service."
                    }) + $"\n{error}\n{output}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine((langCode switch
                {
                    "fr" => "Échec de l'installation du service.",
                    "de" => "Die Dienstinstallation ist fehlgeschlagen.",
                    "it" => "Impossibile installare il servizio.",
                    "es" => "No se pudo instalar el servicio.",
                    _ => "Failed to install the service."
                }) + $"\n{ex.Message}");
            }
        }

        static void StopAndUninstallService(string langCode)
        {
            if (!IsRunningAsAdministrator())
            {
                System.Console.WriteLine(langCode switch
                {
                    "fr" => "Administrateur requis pour désinstaller le service.",
                    "de" => "Administratorrechte sind erforderlich, um den Dienst zu deinstallieren.",
                    "it" => "Sono necessari privilegi di amministratore per disinstallare il servizio.",
                    "es" => "Se requieren privilegios de administrador para desinstalar el servicio.",
                    _ => "Administrator privileges are required to uninstall the service."
                });
                return;
            }

            string args = "delete eGPUManager";
            var psi = new System.Diagnostics.ProcessStartInfo("sc", args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try
            {
                using var proc = System.Diagnostics.Process.Start(psi);
                if (proc == null)
                {
                    System.Console.WriteLine(langCode switch
                    {
                        "fr" => "Impossible de démarrer sc.exe.",
                        "de" => "Kann sc.exe nicht starten.",
                        "it" => "Impossibile avviare sc.exe.",
                        "es" => "No se puede iniciar sc.exe.",
                        _ => "Failed to start sc.exe."
                    });
                    return;
                }

                string output = proc.StandardOutput.ReadToEnd();
                string error = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode == 0)
                {
                    System.Console.WriteLine(langCode switch
                    {
                        "fr" => "Service désinstallé avec succès.",
                        "de" => "Dienst erfolgreich deinstalliert.",
                        "it" => "Servizio disinstallato correttamente.",
                        "es" => "Servicio desinstalado correctamente.",
                        _ => "Service uninstalled successfully."
                    });
                }
                else
                {
                    System.Console.WriteLine((langCode switch
                    {
                        "fr" => "Échec de la désinstallation du service.",
                        "de" => "Die Deinstallation des Dienstes ist fehlgeschlagen.",
                        "it" => "Impossibile disinstallare il servizio.",
                        "es" => "No se pudo desinstalar el servicio.",
                        _ => "Failed to uninstall the service."
                    }) + $"\n{error}\n{output}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine((langCode switch
                {
                    "fr" => "Échec de la désinstallation du service.",
                    "de" => "Die Deinstallation des Dienstes ist fehlgeschlagen.",
                    "it" => "Impossibile disinstallare il servizio.",
                    "es" => "No se pudo desinstalar el servicio.",
                    _ => "Failed to uninstall the service."
                }) + $"\n{ex.Message}");
            }
        }

        static void ChangeServiceState(ServiceControllerStatus desiredState, string langCode)
        {
            if (!IsRunningAsAdministrator())
            {
                System.Console.WriteLine(langCode switch
                {
                    "fr" => "Administrateur requis pour modifier l'état du service.",
                    "de" => "Administratorrechte sind erforderlich, um den Dienststatus zu ändern.",
                    "it" => "Sono necessari privilegi di amministratore per modificare lo stato del servizio.",
                    "es" => "Se requieren privilegios de administrador para cambiar el estado del servicio.",
                    _ => "Administrator privileges are required to change service state."
                });
                return;
            }

            try
            {
                using var controller = new ServiceController("eGPUManager");
                controller.Refresh();

                if (desiredState == ServiceControllerStatus.Running)
                {
                    if (controller.Status == ServiceControllerStatus.Running)
                    {
                        System.Console.WriteLine(langCode switch
                        {
                            "fr" => "Le service est déjà en cours d'exécution.",
                            "de" => "Der Dienst läuft bereits.",
                            "it" => "Il servizio è già in esecuzione.",
                            "es" => "El servicio ya está en ejecución.",
                            _ => "Service is already running."
                        });
                        return;
                    }
                    controller.Start();
                }
                else
                {
                    if (controller.Status == ServiceControllerStatus.Stopped)
                    {
                        System.Console.WriteLine(langCode switch
                        {
                            "fr" => "Le service est déjà arrêté.",
                            "de" => "Der Dienst ist bereits gestoppt.",
                            "it" => "Il servizio è già arrestato.",
                            "es" => "El servicio ya está detenido.",
                            _ => "Service is already stopped."
                        });
                        return;
                    }
                    controller.Stop();
                }

                controller.WaitForStatus(desiredState, TimeSpan.FromSeconds(15));
                System.Console.WriteLine(langCode switch
                {
                    "fr" => desiredState == ServiceControllerStatus.Running ? "Service démarré avec succès." : "Service arrêté avec succès.",
                    "de" => desiredState == ServiceControllerStatus.Running ? "Dienst erfolgreich gestartet." : "Dienst erfolgreich gestoppt.",
                    "it" => desiredState == ServiceControllerStatus.Running ? "Servizio avviato correttamente." : "Servizio arrestato correttamente.",
                    "es" => desiredState == ServiceControllerStatus.Running ? "Servicio iniciado correctamente." : "Servicio detenido correctamente.",
                    _ => desiredState == ServiceControllerStatus.Running ? "Service started successfully." : "Service stopped successfully."
                });
            }
            catch (Exception ex)
            {
                System.Console.WriteLine((langCode switch
                {
                    "fr" => "Impossible de modifier l'état du service.",
                    "de" => "Dienststatus kann nicht geändert werden.",
                    "it" => "Impossibile modificare lo stato del servizio.",
                    "es" => "No se puede cambiar el estado del servicio.",
                    _ => "Unable to change service state."
                }) + $"\n{ex.Message}");
            }
        }
    }
}
