using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using System.IO;
using System.Threading;

namespace BlueSky.Updater
{
    public partial class Form1 : Form
    {
        int Cancel = 1;
        int CP = 1;
        public Form1()
        {
            InitializeComponent();
            Updater(1);
        }

        async Task Init()
        {
            if (!Directory.Exists("C:\\BlueSkyUpdater"))
            {
                Directory.CreateDirectory("C:\\BlueSkyUpdater");
            }
            else
            {
                if (File.Exists("C:\\BlueSkyUpdater\\update.rar"))
                {
                    File.Delete("C:\\BlueSkyUpdater\\update.rar");
                }
                if (File.Exists("C:\\BlueSkyUpdater\\data.ver"))
                {
                    File.Delete("C:\\BlueSkyUpdater\\data.ver");
                }
            }

        }

        async void Updater(int Phase)
        {
            if (Phase == 1)
            {
                CP = 2;
                await Init();
                label1.Text = "Preparing for update...";
                WebClient Client = new WebClient();
                Client.DownloadFile("https://raw.githubusercontent.com/ClickNinYT/idk/main/idk.ver", "C:\\BlueSkyUpdater\\data.ver");
                NextStage();
            }
            else if (Phase == 2)
            {
                label1.Text = "Parsing update data...";
                progressBar1.Style = ProgressBarStyle.Marquee;
                progressBar1.MarqueeAnimationSpeed = 50;
                string verc = File.ReadLines("C:\\BlueSkyUpdater\\data.ver").Take(1).First();
                string link = File.ReadLines("C:\\BlueSkyUpdater\\data.ver").Skip(1).Take(2).First();
                label1.Text = "Preparing for update...";
                progressBar1.Style = ProgressBarStyle.Blocks;
                progressBar1.MarqueeAnimationSpeed = 0;
                progressBar1.Value = 0;
                label1.Text = "Downloading BlueSky...";
                CP = 3;
                Thread thread = new Thread(() => {
                    WebClient client = new WebClient();
                    client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                    client.DownloadFileCompleted += new AsyncCompletedEventHandler(client_DownloadFileCompleted);
                    client.DownloadFileAsync(new Uri(link), "C:\\BlueSkyUpdater\\update.rar");
                });
                thread.Start();
            }
            else if (Phase == 3)
            {
                Cancel = 0;
                string root = AppDomain.CurrentDomain.BaseDirectory;
                progressBar1.Style = ProgressBarStyle.Marquee;
                progressBar1.MarqueeAnimationSpeed = 50;
                label1.Text = "Deleting old version of BlueSky...";
                string pth = AppDomain.CurrentDomain.BaseDirectory;
                string extractFrom = "C:\\BlueSkyUpdater\\update.rar";
                string extractTo = "C:\\BlueSkyUpdater\\ext";
                File.Delete(AppDomain.CurrentDomain.BaseDirectory + "BlueSky.exe");
                File.Delete(AppDomain.CurrentDomain.BaseDirectory + "BlueSky.exe.config");
                File.Delete(AppDomain.CurrentDomain.BaseDirectory + "BlueSky.pdb");
                File.Delete(AppDomain.CurrentDomain.BaseDirectory + "System.Management.Automation.dll");
                File.Delete(AppDomain.CurrentDomain.BaseDirectory + "Windows.winmd");
                Directory.Delete(AppDomain.CurrentDomain.BaseDirectory + "\\Assets", true);
                await Task.Delay(5000);
                label1.Text = "Extracting update files...";
                Directory.CreateDirectory("C:\\BlueSkyUpdater\\ext");
                await Task.Run(delegate ()
                {
                    string zPath = AppDomain.CurrentDomain.BaseDirectory + "7zip/7z.exe";
                    ProcessStartInfo pro = new ProcessStartInfo();
                    pro.WindowStyle = ProcessWindowStyle.Hidden;
                    pro.FileName = zPath;
                    pro.Arguments = "x \"" + extractFrom + "\" -o" + extractTo;
                    Process x = Process.Start(pro);
                    x.WaitForExit();
                });
                label1.Text = "Installing update...";
                File.Copy("C:\\BlueSkyUpdater\\ext\\BlueSky.exe", root + "\\BlueSky.exe", true);
                File.Copy("C:\\BlueSkyUpdater\\ext\\BlueSky.exe.config", root + "\\BlueSky.exe.config", true);
                File.Copy("C:\\BlueSkyUpdater\\ext\\BlueSky.pdb", root + "\\BlueSky.pdb", true);
                File.Copy("C:\\BlueSkyUpdater\\ext\\System.Management.Automation.dll", root + "\\System.Management.Automation.dll", true);
                File.Copy("C:\\BlueSkyUpdater\\ext\\Windows.winmd", root + "\\Windows.winmd", true);
                Directory.CreateDirectory(root + "\\Assets");
                DirectoryCopy("C:\\BlueSkyUpdater\\ext\\Assets", root + "\\Assets", true);
                label1.Text = "Cleaning up...";
                Process[] processesByName1 = Process.GetProcessesByName("7zG");
                for (int i = 0; i < processesByName1.Length; i++)
                {
                    processesByName1[i].Kill();
                }
                Directory.Delete("C:\\BlueSkyUpdater\\ext", true);
                Finishes();
            }
        }

        void NextStage()
        {
            Updater(2);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(Cancel == 1)
            {
                Environment.Exit(0);
            }
            else
            {
                MessageBox.Show("You cannot exit BlueSky Updater this time!");
            }
        }

        private void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            progressBar1.Style = ProgressBarStyle.Marquee;
            progressBar1.MarqueeAnimationSpeed = 50;
            if (CP == 3)
            {
                Updater(3);
            }
            else if (CP == 2)
            {
                Updater(2);
            }
            else if (CP == 4)
            {
                Finishes();
            }
        }

        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            this.BeginInvoke((MethodInvoker)delegate {
                double bytesIn = double.Parse(e.BytesReceived.ToString());
                double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
                double percentage = bytesIn / totalBytes * 100;
                label1.Text = "Downloading BlueSky... \nDownloaded " + e.BytesReceived + " / " + e.TotalBytesToReceive;
                progressBar1.Value = int.Parse(Math.Truncate(percentage).ToString());
            });
        }

        void Finishes()
        {
            if (File.Exists("C:\\BlueSkyUpdater\\update.rar"))
            {
                File.Delete("C:\\BlueSkyUpdater\\update.rar");
            }
            if (File.Exists("C:\\BlueSkyUpdater\\data.ver"))
            {
                File.Delete("C:\\BlueSkyUpdater\\data.ver");
            }
            progressBar1.Style = ProgressBarStyle.Blocks;
            progressBar1.MarqueeAnimationSpeed = 0;
            label1.Text = "Update Complete!";
            progressBar1.Value = 100;
            button1.Enabled = false;
            MessageBox.Show("Update Complete! Click OK to close this program and start the new updated installation of BlueSky! Enjoy.");
            Process.Start(AppDomain.CurrentDomain.BaseDirectory + "BlueSky.exe");
            Process.GetCurrentProcess().Kill();
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            FileInfo[] files = dir.GetFiles();

            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }
            if (copySubDirs)
            {

                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}
