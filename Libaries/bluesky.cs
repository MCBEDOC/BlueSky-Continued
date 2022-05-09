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
using System.Runtime.InteropServices;
using System.IO;

namespace BlueSky.Libaries
{
    public static class Win32Processes
    {
        public static List<Process> GetProcessesLockingFile(string path)
        {
            uint handle;
            string key = Guid.NewGuid().ToString();
            int res = RmStartSession(out handle, 0, key);

            if (res != 0) throw new Exception("Could not begin restart session.  Unable to determine file locker.");

            try
            {
                const int MORE_DATA = 234;
                uint pnProcInfoNeeded, pnProcInfo = 0, lpdwRebootReasons = RmRebootReasonNone;

                string[] resources = { path };

                res = RmRegisterResources(handle, (uint)resources.Length, resources, 0, null, 0, null);

                if (res != 0) throw new Exception("Could not register resource.");
                res = RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, null, ref lpdwRebootReasons);

                if (res == MORE_DATA)
                {
                    return EnumerateProcesses(pnProcInfoNeeded, handle, lpdwRebootReasons);
                }
                else if (res != 0) throw new Exception("Could not list processes locking resource. Failed to get size of result.");
            }
            finally
            {
                RmEndSession(handle);
            }

            return new List<Process>();
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct RM_UNIQUE_PROCESS
        {
            public int dwProcessId;
            public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
        }

        const int RmRebootReasonNone = 0;
        const int CCH_RM_MAX_APP_NAME = 255;
        const int CCH_RM_MAX_SVC_NAME = 63;

        public enum RM_APP_TYPE
        {
            RmUnknownApp = 0,
            RmMainWindow = 1,
            RmOtherWindow = 2,
            RmService = 3,
            RmExplorer = 4,
            RmConsole = 5,
            RmCritical = 1000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct RM_PROCESS_INFO
        {
            public RM_UNIQUE_PROCESS Process;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_APP_NAME + 1)] public string strAppName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_SVC_NAME + 1)] public string strServiceShortName;

            public RM_APP_TYPE ApplicationType;
            public uint AppStatus;
            public uint TSSessionId;
            [MarshalAs(UnmanagedType.Bool)] public bool bRestartable;
        }

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        static extern int RmRegisterResources(uint pSessionHandle, uint nFiles, string[] rgsFilenames,
            uint nApplications, [In] RM_UNIQUE_PROCESS[] rgApplications, uint nServices,
            string[] rgsServiceNames);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Auto)]
        static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, string strSessionKey);

        [DllImport("rstrtmgr.dll")]
        static extern int RmEndSession(uint pSessionHandle);

        [DllImport("rstrtmgr.dll")]
        static extern int RmGetList(uint dwSessionHandle, out uint pnProcInfoNeeded,
            ref uint pnProcInfo, [In, Out] RM_PROCESS_INFO[] rgAffectedApps,
            ref uint lpdwRebootReasons);

        private static List<Process> EnumerateProcesses(uint pnProcInfoNeeded, uint handle, uint lpdwRebootReasons)
        {
            var processes = new List<Process>(10);
            var processInfo = new RM_PROCESS_INFO[pnProcInfoNeeded];
            var pnProcInfo = pnProcInfoNeeded;

            var res = RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, processInfo, ref lpdwRebootReasons);

            if (res != 0) throw new Exception("Could not list processes locking resource.");
            for (int i = 0; i < pnProcInfo; i++)
            {
                try
                {
                    processes.Add(Process.GetProcessById(processInfo[i].Process.dwProcessId));
                }
                catch (ArgumentException) { }
            }
            return processes;
        }
    }
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
                        return true;
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
                        return true;
                    }
                }
                else if (Method == 2)
                {
                    int locked = 0;
                    string x64DLL = @"C:\Windows\System32\Windows.ApplicationModel.Store.dll";
                    string x86DLL = @"C:\Windows\SysWOW64\Windows.ApplicationModel.Store.dll";
                    List<Process> x64Locked = Win32Processes.GetProcessesLockingFile(x64DLL);
                    List<Process> x86Locked = Win32Processes.GetProcessesLockingFile(x86DLL);
                    if (x64Locked.Any()) locked += 1;
                    if (x86Locked.Any()) locked += 1;
                    if (locked > 0)
                    {
                        MessageBoxResult result = (MessageBoxResult)MessageBox.Show("Warning: BlueSky has found process that will cause the launching process fail, do you want to end all of them and continue the launching process? \nIf you want to save your work, click Cancel to cancel the launching process and close all UWP applications. If you sure there is no UWP applications running in the background and this message is still show up, you can click OK to proceed with launching. \nBLUESKY WILL NOT RESPONSE FOR ANY FORM OF DATA LOSS OCCURS AS THE RESULT OF YOU CONTINUING TO PROCEED!", "Confirmination", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);
                        if (result == MessageBoxResult.Cancel)
                        {
                            System.Windows.MessageBox.Show("Operation cancelled!", "Notice", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                            return false;
                        }
                        else
                        {
                            try
                            {
                                if (x64Locked.Any())
                                {
                                    foreach (Process proc in x64Locked)
                                    {
                                        await Task.Run(delegate ()
                                        {
                                            proc.Kill();
                                        });
                                    }
                                }
                                if (x86Locked.Any())
                                {
                                    foreach (Process proc in x86Locked)
                                    {
                                        await Task.Run(delegate ()
                                        {
                                            proc.Kill();
                                        });
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Windows.MessageBox.Show("A fatal error has occured while BlueSky is trying to kill conflicting processes! \nError Information (screenshot if you want to ask for support). \n\nError: Kill Conflicting Process \nMethod: 2 (DLL) \nException: \n" + ex, "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                                return false;
                            }
                        }
                    }
                    pbar.Value = 65;
                    label.Text = "Taking permission...";
                    try
                    {
                        await TakePerm(x64DLL);
                        await TakePerm(x86DLL);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("A fatal error has occured while BlueSky is trying to take permission for patching system! \nError Information (screenshot if you want to ask for support). \n\nError: Take Permission \nMethod: 2 (DLL) \nException: \n" + ex, "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    pbar.Value = 70;
                    label.Text = "Patching system...";
                    if (Directory.Exists(@"C:\tempbackup")) Directory.Delete(@"C:\tempbackup");
                    if (!Directory.Exists(@"C:\tempbackup")) Directory.CreateDirectory(@"C:\tempbackup");
                    try
                    {
                        await Task.Run(() => File.Replace(x64DLL, AppDomain.CurrentDomain.BaseDirectory + "Assets/x64.dll", @"C:\tempbackup\x64.dll"));
                        await Task.Run(() => File.Replace(x86DLL, AppDomain.CurrentDomain.BaseDirectory + "Assets/x86.dll", @"C:\tempbackup\x64.dll"));
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("A fatal error has occured while BlueSky is trying to patching the system! \nError Information (screenshot if you want to ask for support). \n\nError: Patch System \nMethod: 2 (DLL) \nException: \n" + ex, "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    pbar.Value = 95;
                    label.Text = "Launching Minecraft...";
                    await Task.Run(delegate ()
                    {
                        Process.Start("minecraft://");
                    });
                    pbar.Value = 100;
                    label.Text = "Minecraft has been launched successfully!";
                    Process mc = Process.GetProcessesByName("Minecraft.Windows")[0];
                    mc.WaitForExit();
                    mc.Exited += ProcessEnded;
                    if (ExtMC != 0)
                    {
                        await Task.Run(() => File.Replace(x64DLL, @"C:\tempbackup\x64.dll", null));
                        await Task.Run(() => File.Replace(x86DLL, @"C:\tempbackup\x86.dll", null));
                        await UndoApplyWindowsPatches();
                        label.Text = "System unpatched successfully!";
                        System.Windows.MessageBox.Show("Look like your Minecraft instance was just crashed or exited unexpectedly, but don't worry because BlueSky has unpatched your system automatically.", "Notice", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                        return true;
                    }
                    else
                    {
                        await Task.Run(() => File.Replace(x64DLL, @"C:\tempbackup\x64.dll", null));
                        await Task.Run(() => File.Replace(x86DLL, @"C:\tempbackup\x86.dll", null));
                        await UndoApplyWindowsPatches();
                        label.Text = "System unpatched successfully!";
                        System.Windows.MessageBox.Show("System has unpatched successfully!", "Notice", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                        return true;
                    }
                }
                else if (Method == 3)
                {
                    return true;
                }
                else
                {
                    return false;
                }
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
            await Task.Run(delegate ()
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
            });
        }
        public static async Task UndoApplyWindowsPatches()
        {
            await Task.Run(delegate ()
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
            });
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
        public static async Task TakePerm(string path)
        {
            await Task.Run(delegate ()
            {
                ProcessStartInfo info = new ProcessStartInfo("cmd.exe");
                info.WindowStyle = ProcessWindowStyle.Hidden;
                info.Arguments = "/K /C takeown /f " + path + " && icacls " + path + " /grant \"" + Environment.UserName + "\":F";
                Process.Start(info);
            });
        }
    }
}
