using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace NextOSSetup
{
    public class SetupForm : Form
    {
        private Label lblTitle;
        private ListBox lstDrives;
        private Button btnRefresh;
        private CheckBox chkFormat;
        private Button btnInstall;
        private TextBox txtStatus;

        public SetupForm()
        {
            InitializeComponent();
            RefreshDrives();
        }

        private void InitializeComponent()
        {
            this.Text = "NextOS Installer";
            this.Width = 600;
            this.Height = 400;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            lblTitle = new Label();
            lblTitle.Text = "NextOS Setup";
            lblTitle.Font = new System.Drawing.Font("Arial", 16);
            lblTitle.AutoSize = true;
            lblTitle.Top = 20;
            lblTitle.Left = 20;
            this.Controls.Add(lblTitle);

            lstDrives = new ListBox();
            lstDrives.Top = 60;
            lstDrives.Left = 20;
            lstDrives.Width = 200;
            lstDrives.Height = 200;
            this.Controls.Add(lstDrives);

            btnRefresh = new Button();
            btnRefresh.Text = "Refresh";
            btnRefresh.Top = 270;
            btnRefresh.Left = 20;
            btnRefresh.Click += BtnRefresh_Click;
            this.Controls.Add(btnRefresh);

            chkFormat = new CheckBox();
            chkFormat.Text = "Format partition";
            chkFormat.Top = 310;
            chkFormat.Left = 20;
            this.Controls.Add(chkFormat);

            btnInstall = new Button();
            btnInstall.Text = "Install";
            btnInstall.Top = 350;
            btnInstall.Left = 20;
            btnInstall.Width = 200;
            btnInstall.Click += BtnInstall_Click;
            this.Controls.Add(btnInstall);

            txtStatus = new TextBox();
            txtStatus.Top = 60;
            txtStatus.Left = 250;
            txtStatus.Width = 300;
            txtStatus.Height = 260;
            txtStatus.Multiline = true;
            txtStatus.ScrollBars = ScrollBars.Vertical;
            txtStatus.ReadOnly = true;
            this.Controls.Add(txtStatus);
        }

        private void RefreshDrives()
        {
            lstDrives.Items.Clear();
            try
            {
                DriveInfo[] drives = DriveInfo.GetDrives();
                foreach (DriveInfo drive in drives)
                {
                    if (drive.DriveType == DriveType.Fixed)
                    {
                        string label = drive.IsReady ? drive.VolumeLabel : "";
                        lstDrives.Items.Add($"{drive.Name}  {drive.DriveFormat}  {drive.TotalSize / (1024 * 1024 * 1024)} GB  {label}");
                    }
                }
            }
            catch (Exception ex)
            {
                AppendStatus($"Error listing drives: {ex.Message}");
            }
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            RefreshDrives();
        }

        private void BtnInstall_Click(object sender, EventArgs e)
        {
            if (lstDrives.SelectedItem == null)
            {
                MessageBox.Show("Please select a target partition.", "Error");
                return;
            }

            string entry = lstDrives.SelectedItem.ToString();
            string driveLetter = entry.Substring(0, 2); // e.g. "C:\"

            if (chkFormat.Checked)
            {
                AppendStatus($"Formatting {driveLetter}...");
                RunCommand("cmd.exe", $"/c format {driveLetter} /FS:NTFS /Q /Y");
            }

            string wimPath = @"D:\sources\install.wim";
            AppendStatus("Applying image. This may take several minutes...");
            RunCommand("dism.exe", $"/Apply-Image /ImageFile:{wimPath} /Index:1 /ApplyDir:{driveLetter}");

            AppendStatus("Installing bootloader...");
            RunCommand("bcdboot.exe", $"{driveLetter}\Windows /s {driveLetter} /f UEFI");

            AppendStatus("Installation complete. Please reboot.");
            MessageBox.Show("Installation complete. Please reboot.", "NextOS Installer");
        }

        private void RunCommand(string file, string args)
        {
            try
            {
                var p = new Process();
                p.StartInfo.FileName = file;
                p.StartInfo.Arguments = args;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.UseShellExecute = false;
                p.Start();
                p.WaitForExit();
                AppendStatus($"{file} {args} completed with exit code {p.ExitCode}");
            }
            catch (Exception ex)
            {
                AppendStatus($"Error running {file}: {ex.Message}");
            }
        }

        private void AppendStatus(string text)
        {
            txtStatus.AppendText(text + Environment.NewLine);
        }
    }
}
