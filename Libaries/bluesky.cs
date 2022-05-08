using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BlueSky.Libaries
{
    internal class Lib
    {
        static System.Windows.Forms.ProgressBar pbarm;
        static int ExtMC;
        public static async Task<bool> InstallAppx(System.Windows.Forms.ProgressBar pbar, System.Windows.Forms.Label label, string PackageFamilyName)
        {
            int IsInstalled = await CheckInstalled(PackageFamilyName);
            if (IsInstalled == 0)
            {
                System.Windows.MessageBox.Show("Error: Target was already installed!", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }
            else
            {
                System.Windows.Forms.OpenFileDialog TargetAppx = new System.Windows.Forms.OpenFileDialog();
                TargetAppx.DefaultExt = ".appx";
                TargetAppx.Filter = "Appx File (*.appx)|*.appx";
                TargetAppx.Title = "Please select target Appx file";
                TargetAppx.ShowDialog();
                string AppxPath = TargetAppx.FileName;
                if (AppxPath.Length == 0)
                {
                    System.Windows.MessageBox.Show("Error: No file selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                else
                {
                    string AppxName = System.IO.Path.GetFileName(AppxPath);
                    MessageBoxResult result = (MessageBoxResult)MessageBox.Show("Do you want to install " + AppxName + "?", "Confirmination", MessageBoxButton.YesNo, MessageBoxImage.Asterisk);
                    if (result == MessageBoxResult.Yes)
                    {
                        pbarm = pbar;
                        pbar.Value = 0;
                        label.Text = "Installing " + AppxName + "...";
                        PowerShell ps = PowerShell.Create();
                        ps.AddCommand("Add-AppxPackage");
                        ps.AddParameter("Path", AppxPath);
                        ps.Streams.Error.DataAdded += InstallationErrorData;
                        ps.Streams.Progress.DataAdded += InstallationProgressData;
                        await Task.Run(delegate ()
                        {
                            ps.Invoke();
                        });
                        if (!ps.HadErrors)
                        {
                            label.Text = "Target installed successfully!";
                            System.Windows.MessageBox.Show("Target installed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                            return true;
                        }
                        else
                        {
                            label.Text = "Target failed to install.";
                            System.Windows.MessageBox.Show("Target failed to install.", "Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }   
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Error: Operation Cancelled!", "Notice", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                        return false;
                    }
                }
            }
        }

        public static async Task<bool> UninstallPackage(System.Windows.Forms.ProgressBar pbar, System.Windows.Forms.Label label, string PackageFamilyName)
        {
            int IsInstalled = await CheckInstalled(PackageFamilyName);
            if (IsInstalled == 1)
            {
                System.Windows.MessageBox.Show("Error: Target was not installed yet!", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }
            else
            {
                MessageBoxResult result = (MessageBoxResult)MessageBox.Show("Do you want to uninstall " + PackageFamilyName + "?", "Confirmination", MessageBoxButton.YesNo, MessageBoxImage.Asterisk);
                if (result == MessageBoxResult.Yes)
                {
                    pbarm = pbar;
                    pbar.Value = 0;
                    label.Text = "Uninstalling " + PackageFamilyName + "...";
                    string value = await GetName(PackageFamilyName);
                    PowerShell ps = PowerShell.Create();
                    ps.AddCommand("Remove-AppxPackage");
                    ps.AddParameter("-Package", value);
                    ps.Streams.Error.DataAdded += InstallationProgressData;
                    ps.Streams.Progress.DataAdded += InstallationErrorData;
                    await Task.Run(delegate ()
                    {
                        ps.Invoke();
                    });
                    if (!ps.HadErrors)
                    {
                        label.Text = "Target uninstalled successfully!";
                        System.Windows.MessageBox.Show("Target uninstalled successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                        return true;
                    }
                    else
                    {
                        label.Text = "Target failed to uninstall.";
                        System.Windows.MessageBox.Show("Target failed to nuinstall.", "Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("Error: Operation Cancelled!", "Notice", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    return false;
                }
            }
        }

        public static async Task<bool> LaunchMC(System.Windows.Forms.ProgressBar pbar, System.Windows.Forms.Label label, int Method=2, int Timer=40000)
        {
            int IsInstalled = await CheckInstalled("Microsoft.MinecraftUWP");
            if (IsInstalled == 0)
            {
                System.Windows.MessageBox.Show("Error: Minecraft was not installed yet!", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }
            else
            {
                pbar.Value = 20;
                label.Text = "Applying Windows telemetry and tracking patches...";
                await ApplyWindowsPatches();
                pbar.Value = 30;
                label.Text = "Preparing Minecraft...";
                string version = await GetVersion("Microsoft.MinecraftUWP");
                label.Text = "Preparing to launching Minecraft " + version + "...";
                if (Method == 1)
                {
                    if (Timer <= 0) Timer = 40000;
                    if (Timer > 240) Timer = 240000;
                    RegistryKey key;
                    key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Services\\ClipSVC\\Parameters");
                    key?.SetValue("ServiceDll", "%SystemRoot%\\System32\\ClipSVC_.dll", RegistryValueKind.String);
                    ServiceController serviceController = new ServiceController("ClipSVC");
                    if (serviceController.Status == ServiceControllerStatus.Running)
                    {
                        serviceController.Stop();
                    }
                    pbar.Value = 60;
                    label.Text = "Launching Minecraft...";
                    await Task.Run(delegate ()
                    {
                        Process.Start("minecraft://");
                    });
                    pbar.Value = 75;
                    label.Text = "Running timer...";
                    await Task.Delay(Timer);
                    pbar.Value = 95;
                    label.Text = "Killing...";
                    await Task.Run(delegate ()
                    {
                        Process[] processesByName = Process.GetProcessesByName("RuntimeBroker");
                        for (int i = 0; i < processesByName.Length; i++)
                        {
                            processesByName[i].Kill();
                        }
                    });
                    pbar.Value = 100;
                    label.Text = "Minecraft has been launched successfully!";
                    Process mc = Process.GetProcessesByName("Minecraft.Windows")[0];
                    mc.WaitForExit();
                    mc.Exited += ProcessEnded;
                    if (ExtMC != 0)
                    {
                        RegistryKey key1;
                        key1 = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Services\\ClipSVC\\Parameters");
                        key1?.SetValue("ServiceDll", "%SystemRoot%\\System32\\ClipSVC.dll", RegistryValueKind.String);
                        ServiceController serviceController1 = new ServiceController("ClipSVC");
                        if (serviceController1.Status == ServiceControllerStatus.Stopped)
                        {
                            serviceController1.Start();
                        }
                        await UndoApplyWindowsPatches();
                        label.Text = "System unpatched successfully!";
                        System.Windows.MessageBox.Show("Look like your Minecraft instance was just crashed or exited unexpectedly, but don't worry because BlueSky has unpatched your system automatically.", "Notice", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    }
                    else
                    {
                        RegistryKey key1;
                        key1 = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Services\\ClipSVC\\Parameters");
                        key1?.SetValue("ServiceDll", "%SystemRoot%\\System32\\ClipSVC.dll", RegistryValueKind.String);
                        ServiceController serviceController1 = new ServiceController("ClipSVC");
                        if (serviceController1.Status == ServiceControllerStatus.Stopped)
                        {
                            serviceController1.Start();
                        }
                        await UndoApplyWindowsPatches();
                        label.Text = "System unpatched successfully!";
                        System.Windows.MessageBox.Show("System has unpatched successfully!", "Notice", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    }
                }
                return true;
            }
        }
        private static void ProcessEnded(object sender, EventArgs e)
        {
            Process process = sender as Process;
            if (process != null)
            {
                int ExtMC = process.ExitCode;
            }
        }

        public static async Task ApplyWindowsPatches()
        {
            RegistryKey key;
            key = Registry.LocalMachine.CreateSubKey($"SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection");
            key?.SetValue("AllowTelemetry", 0x00000000, RegistryValueKind.DWord);
            RegistryKey key1;
            key1 = Registry.LocalMachine.CreateSubKey($"SOFTWARE\\Policies\\Microsoft\\Windows\\System");
            key1?.SetValue("PublishUserActivities", 0x00000000, RegistryValueKind.DWord);
            RegistryKey key3;
            key3 = Registry.CurrentUser.CreateSubKey($"Software\\Policies\\Microsoft\\Windows\\EdgeUI");
            key3?.SetValue("DisableMFUTracking", 0x00000001, RegistryValueKind.DWord);
            key3 = Registry.LocalMachine.CreateSubKey($"SOFTWARE\\Policies\\Microsoft\\Windows\\EdgeUI");
            key3?.SetValue("DisableMFUTracking", 0x00000001, RegistryValueKind.DWord);
            RegistryKey key4;
            key4 = Registry.LocalMachine.CreateSubKey($"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\DataCollection");
            key4?.SetValue("AllowTelemetry", 0x00000000, RegistryValueKind.DWord);
            RegistryKey key5;
            key5 = Registry.LocalMachine.CreateSubKey($"SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Policies\\DataCollection");
            key5?.SetValue("AllowTelemetry", 0x00000000, RegistryValueKind.DWord);
            RegistryKey key6;
            key6 = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Services\\DiagTrack\\Parameters");
            key6?.SetValue("ServiceDll", "%SystemRoot%\\system32\\diagtrack_.dll", RegistryValueKind.String);
            RegistryKey key7;
            key7 = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Services\\dmwappushservice\\Parameters");
            key7?.SetValue("ServiceDll", "%SystemRoot%\\system32\\dmwappushsvc_.dll", RegistryValueKind.String);
            ServiceController serviceController = new ServiceController("dmwappushservice");
            if (serviceController.Status == ServiceControllerStatus.Running)
            {
                serviceController.Stop();
            }
            ServiceController serviceController2 = new ServiceController("DiagTrack");
            if (serviceController2.Status == ServiceControllerStatus.Running)
            {
                serviceController2.Stop();
            }
        }
        public static async Task UndoApplyWindowsPatches()
        {
            RegistryKey key;
            key = Registry.LocalMachine.CreateSubKey($"SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection");
            key?.SetValue("AllowTelemetry", 0x00000001, RegistryValueKind.DWord);
            RegistryKey key1;
            key1 = Registry.LocalMachine.CreateSubKey($"SOFTWARE\\Policies\\Microsoft\\Windows\\System");
            key1?.SetValue("PublishUserActivities", 0x00000001, RegistryValueKind.DWord);
            RegistryKey key3;
            key3 = Registry.CurrentUser.CreateSubKey($"Software\\Policies\\Microsoft\\Windows\\EdgeUI");
            key3?.SetValue("DisableMFUTracking", 0x00000000, RegistryValueKind.DWord);
            key3 = Registry.LocalMachine.CreateSubKey($"SOFTWARE\\Policies\\Microsoft\\Windows\\EdgeUI");
            key3?.SetValue("DisableMFUTracking", 0x00000000, RegistryValueKind.DWord);
            RegistryKey key4;
            key4 = Registry.LocalMachine.CreateSubKey($"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\DataCollection");
            key4?.SetValue("AllowTelemetry", 0x00000001, RegistryValueKind.DWord);
            RegistryKey key5;
            key5 = Registry.LocalMachine.CreateSubKey($"SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Policies\\DataCollection");
            key5?.SetValue("AllowTelemetry", 0x00000001, RegistryValueKind.DWord);
            RegistryKey key6;
            key6 = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Services\\DiagTrack\\Parameters");
            key6?.SetValue("ServiceDll", "%SystemRoot%\\system32\\diagtrack.dll", RegistryValueKind.String);
            RegistryKey key7;
            key7 = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Services\\dmwappushservice\\Parameters");
            key7?.SetValue("ServiceDll", "%SystemRoot%\\system32\\dmwappushsvc.dll", RegistryValueKind.String);
            ServiceController serviceController = new ServiceController("dmwappushservice");
            if (serviceController.Status == ServiceControllerStatus.Stopped)
            {
                serviceController.Start();
            }
            ServiceController serviceController2 = new ServiceController("DiagTrack");
            if (serviceController2.Status == ServiceControllerStatus.Stopped)
            {
                serviceController2.Start();
            }
        }

        private static void InstallationProgressData(object sender, DataAddedEventArgs e)
        {
            PSDataCollection<ProgressRecord> psdataCollection = (PSDataCollection<ProgressRecord>)sender;
            int prog = psdataCollection[e.Index].PercentComplete;
            if (prog > 0)
            {
                G_progress = prog;
            }
        }
        static int G_progress
        {
            get
            {
                return 0;
            }
            set
            {
                set_progress(value);
            }

        }
        private delegate void progresser(int arg);
        static void set_progress(int arg)
        {
            if (pbarm.InvokeRequired)
            {
                var d = new progresser(set_progress);
                pbarm.Invoke(d, new object[] { arg });
            }
            else
            {
                pbarm.Value = arg;
            }
        }

        private static void InstallationErrorData(object sender, DataAddedEventArgs e)
        {
            PSDataCollection<ErrorRecord> psdataCollection = (PSDataCollection<ErrorRecord>)sender;
            string error = psdataCollection[e.Index].FullyQualifiedErrorId;
        }

        public static async Task<int> CheckInstalled(string PackageFamilyName)
        {
            PowerShell ps = PowerShell.Create();
            ps.AddCommand("Get-AppxPackage");
            ps.AddParameter("Name", PackageFamilyName);
            Collection<PSObject> results = null;
            await Task.Run(delegate
            {
                results = ps.Invoke();
            });
            if (results.Count == 0)
            {
                return 1;
            }
            return 0;
        }
        public static async Task<string> GetName(string package)
        {
            PowerShell ps = PowerShell.Create();
            ps.AddCommand("Get-AppxPackage");
            ps.AddParameter("Name", package);
            Collection<PSObject> results = null;
            await Task.Run(delegate
            {
                results = ps.Invoke();
            });
            if (results.Count == 0)
            {
                return null;
            }
            return (string)results[0].Members["PackageFullName"].Value;
        }
        public static async Task<string> GetVersion(string package)
        {
            PowerShell ps = PowerShell.Create();
            ps.AddCommand("Get-AppxPackage");
            ps.AddParameter("Name", package);
            Collection<PSObject> results = null;
            await Task.Run(delegate
            {
                results = ps.Invoke();
            });
            if (results.Count == 0)
            {
                return null;
            }
            return (string)results[0].Members["Version"].Value;
        }
    }
}
