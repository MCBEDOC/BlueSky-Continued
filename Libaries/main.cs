using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using System.Windows;
using System.Windows.Forms;
using System.Diagnostics;
using System.ServiceProcess;
using System.Runtime.InteropServices;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows.Documents;
using Windows.Management.Deployment;
using System.Collections.ObjectModel;
using System.Threading;
using System.Net;
using Microsoft.Win32;

namespace BlueSky.Libaries
{
    internal class BS
    {
        private const int PROCESS_CREATE_THREAD = 2;
        private const int PROCESS_QUERY_INFORMATION = 1024;
        private const int PROCESS_VM_OPERATION = 8;
        private const int PROCESS_VM_WRITE = 32;
        private const int PROCESS_VM_READ = 16;
        private const uint MEM_COMMIT = 4096u;
        private const uint MEM_RESERVE = 8192u;
        private const uint PAGE_READWRITE = 4u;

        static System.Windows.Forms.ProgressBar pbarm;
        static System.Windows.Forms.Label labelm;

        static int extMC;

        static string version;

        static string versions = "0.13";
        static string verc;
        public static void notice(string msg)
        {
            System.Windows.Forms.MessageBox.Show(msg);
        }

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        public static async void installmc(System.Windows.Forms.ProgressBar pbar, System.Windows.Forms.Label label)
        {
            int inst = checkmc();
            if (inst == 1)
            {
                pbarm = pbar;
                labelm = label;
                System.Windows.Forms.OpenFileDialog mcpck = new System.Windows.Forms.OpenFileDialog();
                mcpck.DefaultExt = ".appx";
                mcpck.Filter = "Appx File (*.appx)|*.appx|MSIX File (*.msix)|*.msix";
                mcpck.ShowDialog();
                string PCKPath = mcpck.FileName;
                if (PCKPath.Length != 0)
                {
                    pbar.Value = 0;
                    label.Text = "Installing package...";
                    PowerShell ps = PowerShell.Create();
                    ps.AddCommand("Add-AppxPackage");
                    ps.AddParameter("Path", PCKPath);
                    ps.Streams.Error.DataAdded += Error_DataAdded;
                    ps.Streams.Progress.DataAdded += Progress_DataAdded;
                    await Task.Run(delegate ()
                    {
                        ps.Invoke();
                    });
                    if (!ps.HadErrors)
                    {
                        label.Text = "Package installed successfully!";
                    }
                    else
                    {
                        label.Text = "Package failed to install! Error code PKG_INSTALL_FAILED";
                    }
                }
                else
                {
                    notice("Operation cancelled!");
                }
            }
            else
            {
                notice("Minecraft is installed! Error Code MC_INSTALLED");
            }
        }

        private static void Error_DataAdded(object sender, DataAddedEventArgs e)
        {
            PSDataCollection<ErrorRecord> psdataCollection = (PSDataCollection<ErrorRecord>)sender;
            string error = psdataCollection[e.Index].FullyQualifiedErrorId;
        }

        private static void Progress_DataAdded(object sender, DataAddedEventArgs e)
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

        public static async void installmcspec(string appx, System.Windows.Forms.ProgressBar pbar, System.Windows.Forms.Label label)
        {
            int inst = checkmc();
            if (inst == 2)
            {
                pbarm = pbar;
                labelm = label;
                string PCKPath = appx;
                if (PCKPath.Length != 0)
                {
                    pbar.Value = 0;
                    label.Text = "Installing package...";
                    PowerShell ps = PowerShell.Create();
                    ps.AddCommand("Add-AppxPackage");
                    ps.AddParameter("Path", PCKPath);
                    ps.Streams.Error.DataAdded += Error_DataAdded;
                    ps.Streams.Progress.DataAdded += Progress_DataAdded;
                    await Task.Run(delegate ()
                    {
                        ps.Invoke();
                    });
                    if (!ps.HadErrors)
                    {
                        label.Text = "Package installed successfully!";
                    }
                    else
                    {
                        label.Text = "Package failed to install! Error code PKG_INSTALL_FAILED";
                    }
                }
                else
                {
                    notice("Operation cancelled!");
                }
            }
            else
            {
                notice("Minecraft is installed! Error Code MC_INSTALLED");
            }
        }

        public static int notice_ask(string title, string msg)
        {
            MessageBoxResult result = (MessageBoxResult)System.Windows.Forms.MessageBox.Show(msg, title, (MessageBoxButtons)MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                return 1;
            }
            else
            {
                return 2;
            }
        }

        public static async void uninstallmc(System.Windows.Forms.ProgressBar pbar, System.Windows.Forms.Label label)
        {
            int inst = checkmc();
            if (inst == 2)
            {
                int ask = notice_ask("Warning", "Do you want to uninstall Minecraft? This will also delete all of your worlds, resources pack, behavior pack and stuff!");
                if (ask == 1)
                {
                    pbar.Value = 0;
                    label.Text = "Uninstalling...";
                    string value = await GetName("Microsoft.MinecraftUWP");
                    PowerShell ps = PowerShell.Create();
                    ps.AddCommand("Remove-AppxPackage");
                    ps.AddParameter("-Package", value);
                    ps.Streams.Error.DataAdded += Error_DataAdded;
                    ps.Streams.Progress.DataAdded += Progress_DataAdded;
                    await Task.Run(delegate ()
                    {
                        ps.Invoke();
                    });
                    if (!ps.HadErrors)
                    {
                        label.Text = "Package uninstalled successfully!";
                    }
                    else
                    {
                        label.Text = "Package failed to install! Error code PKG_UNINSTALL_FAILED";
                    }
                }
                else
                {
                    notice("Operation Cancelled!");
                }
            }
            else
            {
                notice("Minecraft is not installed! Error Code MC_NOT_INSTALLED");
            }
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
        public static async void launch(int winver, int method, int timeout, System.Windows.Forms.ProgressBar pbar, System.Windows.Forms.Label label)
        {
            int inst = checkmc();
            if (inst == 2)
            {
                pbar.Style = ProgressBarStyle.Marquee;
                pbar.MarqueeAnimationSpeed = 50;
                if (method == 0)
                {
                    //at1
                    string reganti2 = AppDomain.CurrentDomain.BaseDirectory + "Assets/1.reg";
                    Process regeditProcess2 = Process.Start("regedit.exe", "/s \"" + reganti2 + "\"");
                    regeditProcess2.WaitForExit();
                    //at2
                    string reganti3 = AppDomain.CurrentDomain.BaseDirectory + "Assets/2.reg";
                    Process regeditProcess3 = Process.Start("regedit.exe", "/s \"" + reganti3 + "\"");
                    regeditProcess3.WaitForExit();
                    //at3
                    string reganti4 = AppDomain.CurrentDomain.BaseDirectory + "Assets/3.reg";
                    Process regeditProcess4 = Process.Start("regedit.exe", "/s \"" + reganti4 + "\"");
                    regeditProcess4.WaitForExit();
                    //disable services
                    ServiceController serviceController2 = new ServiceController("dmwappushservice");
                    if (serviceController2.Status == ServiceControllerStatus.Running)
                    {
                        serviceController2.Stop();
                    }
                    ServiceController serviceController21 = new ServiceController("DiagTrack");
                    if (serviceController21.Status == ServiceControllerStatus.Running)
                    {
                        serviceController21.Stop();
                    }
                    await Prepare();
                    await GetRidService();
                    await LaunchProtocol();
                    await KillRB();
                    await Task.Delay(timeout);
                    pbar.Style = ProgressBarStyle.Blocks;
                    pbar.MarqueeAnimationSpeed = 0;
                    label.Text = "Done!";
                    Process mc = Process.GetProcessesByName("Minecraft.Windows")[0];
                    mc.WaitForExit();
                    mc.Exited += ProcessEnded;
                    ws();
                    string disable = AppDomain.CurrentDomain.BaseDirectory + "Assets/DISABLE.reg";
                    Process regeditProcess = Process.Start("regedit.exe", "/s \"" + disable + "\"");
                    regeditProcess.WaitForExit();
                    ServiceController serviceController = new ServiceController("ClipSVC");
                    if (serviceController.Status != ServiceControllerStatus.Running)
                    {
                        serviceController.Start();
                    }
                }
                else if (method == 2)
                {
                    label.Text = "Launching...";
                    //at1
                    string reganti2 = AppDomain.CurrentDomain.BaseDirectory + "Assets/1.reg";
                    Process regeditProcess2 = Process.Start("regedit.exe", "/s \"" + reganti2 + "\"");
                    regeditProcess2.WaitForExit();
                    //at2
                    string reganti3 = AppDomain.CurrentDomain.BaseDirectory + "Assets/2.reg";
                    Process regeditProcess3 = Process.Start("regedit.exe", "/s \"" + reganti3 + "\"");
                    regeditProcess3.WaitForExit();
                    //at3
                    string reganti4 = AppDomain.CurrentDomain.BaseDirectory + "Assets/3.reg";
                    Process regeditProcess4 = Process.Start("regedit.exe", "/s \"" + reganti4 + "\"");
                    regeditProcess4.WaitForExit();
                    //disable services
                    ServiceController serviceController2 = new ServiceController("dmwappushservice");
                    if (serviceController2.Status == ServiceControllerStatus.Running)
                    {
                        serviceController2.Stop();
                    }
                    ServiceController serviceController21 = new ServiceController("DiagTrack");
                    if (serviceController21.Status == ServiceControllerStatus.Running)
                    {
                        serviceController21.Stop();
                    }
                    //debloat uwp a bit
                    try
                    {
                        EnsureTask();
                        await KillRB();
                        await KillUWP();
                    }
                    catch (Exception)
                    {
                        notice("Game Launching Error Occured! The launcher cannot attempt to end process needed for game launching! Error Code CRIC_PROC_END_FAILED");
                    }
                    //backup
                    if (Directory.Exists("C:\\BlueSky\\Backup"))
                    {
                        Directory.Delete("C:\\BlueSky\\Backup", true);
                    }
                    if (!Directory.Exists("C:\\BlueSky"))
                    {
                        Directory.CreateDirectory("C:\\BlueSky");
                    }
                    Directory.CreateDirectory("C:\\BlueSky\\Backup");
                    takeperm("C:\\Windows\\System32\\Windows.ApplicationModel.Store.dll");
                    takeperm("C:\\Windows\\SysWOW64\\Windows.ApplicationModel.Store.dll");
                    File.Copy("C:\\Windows\\System32\\Windows.ApplicationModel.Store.dll", "C:\\BlueSky\\Backup\\BCKX64.dll", true);
                    File.Copy("C:\\Windows\\SysWOW64\\Windows.ApplicationModel.Store.dll", "C:\\BlueSky\\Backup\\BCKX86.dll", true);
                    //create a pernament backup in case anything goes wrong so badly
                    if (!File.Exists("C:\\BlueSky\\Backup\\DONOTTOUCH_X64.dll"))
                    {
                        File.Copy("C:\\Windows\\System32\\Windows.ApplicationModel.Store.dll", "C:\\BlueSky\\Backup\\DONOTTOUCH_X64.dll", true);
                    }
                    if (!File.Exists("C:\\BlueSky\\Backup\\DONOTTOUCH_X86.dll"))
                    {
                        File.Copy("C:\\Windows\\SysWOW64\\Windows.ApplicationModel.Store.dll", "C:\\BlueSky\\Backup\\DONOTTOUCH_X86.dll", true);
                    }
                    //ensure directory
                    if (Directory.Exists("C:\\BlueSky\\Temp"))
                    {
                        Directory.Delete("C:\\BlueSky\\Temp", true);
                    }
                    if (!Directory.Exists("C:\\BlueSky"))
                    {
                        Directory.CreateDirectory("C:\\BlueSky");
                    }
                    Directory.CreateDirectory("C:\\BlueSky\\Temp");
                    //ensure patched original file
                    //ensure files are deleted
                    if (File.Exists(@"C:\Windows\System32\Windows.ApplicationModel.Store.dll"))
                    {
                        try
                        {
                            await Task.Run(() => File.Delete(@"C:\Windows\System32\Windows.ApplicationModel.Store.dll"));
                        }
                        catch (Exception)
                        {
                            notice("Game Launching Error Occured! Launcher couldn't remove needed files. Error Code LAUNCH_REM_PREP_FAIL");
                        }
                    }
                    else
                    {
                        if(File.Exists(@"C:\Windows\SysWOW64\Windows.ApplicationModel.Store.dll"))
                        {
                            try
                            {
                                await Task.Run(() => File.Delete(@"C:\Windows\SysWOW64\Windows.ApplicationModel.Store.dll"));
                            }
                            catch (Exception)
                            {
                                notice("Game Launching Error Occured! Launcher couldn't remove needed files. Error Code LAUNCH_REM_PREP_FAIL");
                            }
                        }
                    }
                    //before patch check
                    if (File.Exists(@"C:\Windows\System32\Windows.ApplicationModel.Store.dll"))
                    {
                        notice("Warning: Game Launching might fail due to unclean system!");
                    }
                    if (File.Exists(@"C:\Windows\SysWOW64\Windows.ApplicationModel.Store.dll"))
                    {
                        notice("Warning: Game Launching might fail due to unclean system!");
                    }
                    //patch
                    await Task.Run(() => File.Copy(AppDomain.CurrentDomain.BaseDirectory + "Assets/LaunchingAsset/1164.dll", "C:\\BlueSky\\Temp\\tempx64.dll"));
                    //File.Copy(AppDomain.CurrentDomain.BaseDirectory + "Assets/LaunchingAsset/1186.dll", "C:\\BlueSky\\Temp\\tempx86.dll");
                    try
                    {
                        await Task.Run(() => File.Copy("C:\\BlueSky\\Temp\\tempx64.dll", "C:\\Windows\\System32\\Windows.ApplicationModel.Store.dll", true));
                        //File.Copy("C:\\BlueSky\\Temp\\tempx86.dll", "C:\\Windows\\SysWOW64\\Windows.ApplicationModel.Store.dll", true);
                    }
                    catch (Exception)
                    {
                        notice("Game Launching Error Occured! Launcher cannot access to File System. Error Code FS_ACCESS_FAILED");
                    }
                    Directory.Delete("C:\\BlueSky\\Temp", true);
                    Process.Start("minecraft://");
                    Process mc = Process.GetProcessesByName("Minecraft.Windows")[0];
                    mc.WaitForExit();
                    mc.Exited += ProcessEnded;
                    label.Text = "Done!";
                    if (extMC != 0)
                    {
                        try
                        {
                            EnsureTask();
                            await KillRB();
                            await KillUWP();
                        }
                        catch (Exception)
                        {
                            notice("Game Launching Error Occured! Launcher cannot prepare for recovering to system! Error Code RECOVER_PREP_FAILED");
                        }
                        label.Text = "Minecraft abnormal exit code delected! Rolling back... Error Code MC_ABN_EXT_CODE!";
                        try
                        {
                            await Task.Run(() => File.Copy("C:\\BlueSky\\Backup\\BCKX64.dll", "C:\\Windows\\System32\\Windows.ApplicationModel.Store.dll", true));
                            //File.Copy("C:\\BlueSky\\Backup\\BCKX86.dll", "C:\\Windows\\SysWOW64\\Windows.ApplicationModel.Store.dll", true);
                        }
                        catch (Exception)
                        {
                            notice("Game Launching Error Occured! Launcher cannot recover the File System! Error Code FS_RECOVER_FAILED");
                        }
                        label.Text = "Cleaning up...";
                        label.Text = "All task completed successfully.";
                        pbar.Style = ProgressBarStyle.Blocks;
                        pbar.MarqueeAnimationSpeed = 0;
                    }
                    else
                    {
                        try
                        {
                            EnsureTask();
                            await KillRB();
                            await KillUWP();
                        }
                        catch (Exception)
                        {
                            notice("Game Launching Error Occured! Launcher cannot prepare for recovering to system! Error Code RECOVER_PREP_FAILED");
                        }
                        label.Text = "Minecraft closed successfully, rolling back...";
                        try
                        {
                            await Task.Run(() => File.Copy("C:\\BlueSky\\Backup\\BCKX64.dll", "C:\\Windows\\System32\\Windows.ApplicationModel.Store.dll", true));
                            //File.Copy("C:\\BlueSky\\Backup\\BCKX86.dll", "C:\\Windows\\SysWOW64\\Windows.ApplicationModel.Store.dll", true);
                        }
                        catch (Exception)
                        {
                            notice("Game Launching Error Occured! Launcher cannot recover the File System! Error Code FS_RECOVER_FAILED");
                        }
                        label.Text = "Cleaning up...";
                        label.Text = "All task completed successfully.";
                        pbar.Style = ProgressBarStyle.Blocks;
                        pbar.MarqueeAnimationSpeed = 0;
                    }
                }
                else if (method == 3)
                {
                    //at1
                    string reganti2 = AppDomain.CurrentDomain.BaseDirectory + "Assets/1.reg";
                    Process regeditProcess2 = Process.Start("regedit.exe", "/s \"" + reganti2 + "\"");
                    regeditProcess2.WaitForExit();
                    //at2
                    string reganti3 = AppDomain.CurrentDomain.BaseDirectory + "Assets/2.reg";
                    Process regeditProcess3 = Process.Start("regedit.exe", "/s \"" + reganti3 + "\"");
                    regeditProcess3.WaitForExit();
                    //at3
                    string reganti4 = AppDomain.CurrentDomain.BaseDirectory + "Assets/3.reg";
                    Process regeditProcess4 = Process.Start("regedit.exe", "/s \"" + reganti4 + "\"");
                    regeditProcess4.WaitForExit();
                    //disable services
                    ServiceController serviceController2 = new ServiceController("dmwappushservice");
                    if (serviceController2.Status == ServiceControllerStatus.Running)
                    {
                        serviceController2.Stop();
                    }
                    ServiceController serviceController21 = new ServiceController("DiagTrack");
                    if (serviceController21.Status == ServiceControllerStatus.Running)
                    {
                        serviceController21.Stop();
                    }
                    Process.Start("minecraft://");
                    pbar.Style = ProgressBarStyle.Blocks;
                    pbar.MarqueeAnimationSpeed = 0;
                }
                else
                {
                    notice("Invaild method or timer! Error Code INVAILD_DATA");
                }
            }
            else
            {
                notice("Minecraft is not installed! Please install Minecraft trial from Microsoft Store or from BlueSky's Installer. Error Code MC_NOT_INSTALLED");
            }
        }

        private static void ProcessEnded(object sender, EventArgs e)
        {
            Process process = sender as Process;
            if (process != null)
            {
                int extMC = process.ExitCode;
            }
        }

        public static async Task Prepare()
        {
            await Task.Run(delegate ()
            {
                string enable = AppDomain.CurrentDomain.BaseDirectory + "Assets/ENABLE.reg";
                Process regeditProcess = Process.Start("regedit.exe", "/s \"" + enable + "\"");
                regeditProcess.WaitForExit();
            });
        }

        public static async Task GetRidService()
        {
            await Task.Run(delegate ()
            {
                ServiceController serviceController = new ServiceController("ClipSVC");
                if (serviceController.Status != ServiceControllerStatus.Stopped)
                {
                    serviceController.Stop();
                }
            });
        }

        public static async Task LaunchProtocol()
        {
            await Task.Run(delegate ()
            {
                Process.Start("minecraft://");
                for (int num = 20; num != 0; num--)
                {
                    Task.Delay(1000).Wait();
                }
            });
        }

        public static async Task KillRB()
        {
            await Task.Delay(5000);
            await Task.Run(delegate ()
            {
                Process[] processesByName = Process.GetProcessesByName("RuntimeBroker");
                for (int i = 0; i < processesByName.Length; i++)
                {
                    processesByName[i].Kill();
                }
            });
        }

        public static async Task KillUWP()
        {
            await Task.Delay(5000);
            await Task.Run(delegate ()
            {
                Process[] processesByName = Process.GetProcessesByName("ApplicationFrameHost");
                for (int i = 0; i < processesByName.Length; i++)
                {
                    processesByName[i].Kill();
                }
            });
        }

        public static bool EnsureTask()
        {
            try
            {
                Process.Start("RuntimeBroker.exe");
                Process.Start("ApplicationFrameHost.exe");
            }
            catch (Exception)
            {
                return false;     //not success
            }
            return true;
        }

        public static void injectdll(string DLL)
        {
            Task.Delay(1000);
            Process[] targetProcessIndex = Process.GetProcessesByName("Minecraft.Windows");
            if (targetProcessIndex.Length != 0)
            {
                applePerm(DLL);
                Process targetProcess = Process.GetProcessesByName("Minecraft.Windows")[0];
                IntPtr procHandle = OpenProcess(1082, bInheritHandle: false, targetProcess.Id);
                IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
                IntPtr allocMemAddress = VirtualAllocEx(procHandle, IntPtr.Zero, (uint)((DLL.Length + 1) * Marshal.SizeOf(typeof(char))), 12288u, 4u);
                WriteProcessMemory(procHandle, allocMemAddress, Encoding.Default.GetBytes(DLL), (uint)((DLL.Length + 1) * Marshal.SizeOf(typeof(char))), out var _);
                CreateRemoteThread(procHandle, IntPtr.Zero, 0u, loadLibraryAddr, allocMemAddress, 0u, IntPtr.Zero);
            }
        }

        public static void applePerm(string DLLPath)
        {
            FileInfo InfoFile = new FileInfo(DLLPath);
            FileSecurity fSecurity = InfoFile.GetAccessControl();
            fSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier("S-1-15-2-1"), FileSystemRights.FullControl, InheritanceFlags.None, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
            InfoFile.SetAccessControl(fSecurity);
        }

        public static void takeperm(string path)
        {
            ProcessStartInfo info = new ProcessStartInfo("cmd.exe");
            info.WindowStyle = ProcessWindowStyle.Hidden;
            info.Arguments = "/K /C takeown /f " + path + " && icacls " + path + " /grant \"" + Environment.UserName + "\":F";
            Process.Start(info);
        }

        public static async void ws()
        {
            await KillRB();
            await KillUWP();
        }

        public static void desvc()
        {
            ws();
            string disable = AppDomain.CurrentDomain.BaseDirectory + "Assets/DISABLE.reg";
            Process regeditProcess = Process.Start("regedit.exe", "/s \"" + disable + "\"");
            regeditProcess.WaitForExit();
            ServiceController serviceController = new ServiceController("ClipSVC");
            if (serviceController.Status != ServiceControllerStatus.Running)
            {
                serviceController.Start();
            }
        }

        public static int checkmc()
        {
            //check if Minecraft is installed
            Windows.Management.Deployment.PackageManager P = new Windows.Management.Deployment.PackageManager();
            var pkgManager = new PackageManager();
            var pkg = P.FindPackagesForUser(string.Empty, "Microsoft.MinecraftUWP_8wekyb3d8bbwe").FirstOrDefault();
            try
            {
                bool aval = pkg.Status.NotAvailable;
            }
            catch (Exception)
            {
                return 1;       //not installed
            }
            return 2;            //installed
        }

        public static void Logger(string log)
        {
            string time = DateTime.Now.ToString("HH:mm:ss tt");
            Console.WriteLine("[" + time + "]" + " " + log);
        }

        public static void task_init()
        {
            pbarm.Value = 0;
        }

        public static async Task<string> mcver()
        {
            try
            {
                PowerShell ps = PowerShell.Create();
                ps.AddCommand("Get-AppxPackage");
                ps.AddParameter("Name", "Microsoft.MinecraftUWP");
                Collection<PSObject> results = null;
                await Task.Run(delegate
                {
                    results = ps.Invoke();
                });
                if (results.Count == 0)
                {
                    return null;
                }
                version = results[0].Members["Version"].Value?.ToString();
                return version;
            }
            catch (Exception)
            {
                notice("Launcher cannot retrive game version! Error Code GAME_VER_REV_FAILED");
            }
            return null;
        }

        public static void FetchUpdate()
        {
            if (!Directory.Exists("C:\\BlueSky"))
            {
                Directory.CreateDirectory("C:\\BlueSky\\");
            }
            try
            {
                WebClient Client = new WebClient();
                Client.DownloadFile("https://raw.githubusercontent.com/ClickNinYT/idk/main/idk.ver", @"C:\BlueSky\data.ver");
            }
            catch (Exception)
            {
                System.Windows.Forms.MessageBox.Show("Failed to check for update! Please check your internet connection and try again later.");
            }
            verc = File.ReadLines(@"C:\BlueSky\data.ver").Take(1).First();
            Compare(verc);
        }

        public static void Compare(string ver1)
        {
            if (ver1 != versions)
            {
                NewVersion();
                File.Delete("C:\\BlueSky\\data.ver");
            }
            else
            {
                LatestAlready();
            }
        }

        public static void NewVersion()
        {
            int pro = notice_ask("Update?", "New Version Available! \n\nYour Current Version: " + versions + "\nLatest Version: " + verc + "\n\nDo you want to update now?");
            if (pro == 1)
            {
                Process.Start(AppDomain.CurrentDomain.BaseDirectory + "BlueSky.Updater.exe");
                Process.GetCurrentProcess().Kill();
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Update cancelled! Please note that update will give you new bug fixes, features, and more. Please update if possible.");
            }
        }

        public static void LatestAlready()
        {
            System.Windows.Forms.MessageBox.Show("This version of BlueSky is the latest version.");
        }

        public static void GetMCID()
        {
            Process.Start("minecraft://");
            Thread.Sleep(5000);
            Process[] s = Process.GetProcessesByName("Minecraft.Windows");
            int sid = s.Length;
            int id = s[sid - 1].Id;
            sid -= 1;
            notice(id.ToString());
        }

        public static string GetWindowsBuild()
        {
            RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            var UBR = registryKey.GetValue("UBR").ToString();
            var CurrentBuild = registryKey.GetValue("CurrentBuild").ToString();
            string version = CurrentBuild + "." + UBR;
            return version;
        }

        public static void DumpDLL()
        {
            string build = GetWindowsBuild();
            string dumpfolder = @"C:\Dump_" + build;
            if (!Directory.Exists(dumpfolder))
            {
                Directory.CreateDirectory(dumpfolder);
            }
            else
            {
                Directory.Delete(dumpfolder, true);
                Directory.CreateDirectory(dumpfolder);
            }
            File.Copy(@"C:\Windows\System32\Windows.ApplicationModel.Store.dll", dumpfolder + @"\" + build + "x64.dll");
            File.Copy(@"C:\Windows\SysWOW64\Windows.ApplicationModel.Store.dll", dumpfolder + @"\" + build + "x86.dll");
            notice("Your system DLL is dumped to: " + dumpfolder);
        }
    }
}
