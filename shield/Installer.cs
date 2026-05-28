using System;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;

namespace ZenithInstaller
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new InstallerForm());
        }
    }

    class InstallerForm : Form
    {
        private int currentStep = 0;
        private string installPath;

        // Custom Titlebar Controls
        private Panel headerPanel;
        private Label titleLabel;
        private Button closeButton;
        
        // Main Navigation Controls
        private Panel contentPanel;
        private Button nextButton;
        private Button backButton;

        // Step 1: Welcome Controls
        private Label welcomeLabel;
        private Label welcomeDesc;

        // Step 2: Path Controls
        private Label pathLabel;
        private TextBox pathTextBox;
        private Button browseButton;

        // Step 3: Feature Checkbox Controls
        private Label featureLabel;
        private CheckBox chkShield;
        private CheckBox chkApp;
        private CheckBox chkDesktop;
        private CheckBox chkStartup;
        private CheckBox chkWatchdog;

        // Step 4: Progress Controls
        private Label progressLabel;
        private Panel progressBarBg;
        private Panel progressBarFill;
        private System.Windows.Forms.Timer installTimer;
        private int progressValue = 0;

        // Step 5: Finished Controls
        private Label finishLabel;
        private Label finishDesc;

        public InstallerForm()
        {
            // Install Destination path (Default AppData/Local/Zenith)
            installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Zenith");

            // Window Setup (Borderless minimalist design)
            this.Size = new Size(520, 360);
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(10, 10, 12); // Matte Black
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Zenith Anti-PMO Setup";

            InitializeStyles();
            ShowStep(0);
        }

        private void InitializeStyles()
        {
            // Title Bar Panel
            headerPanel = new Panel();
            headerPanel.Size = new Size(this.Width, 40);
            headerPanel.Location = new Point(0, 0);
            headerPanel.BackColor = Color.FromArgb(15, 15, 18);
            headerPanel.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    this.Capture = false;
                    Message msg = Message.Create(this.Handle, 0XA1, new IntPtr(2), IntPtr.Zero);
                    this.WndProc(ref msg);
                }
            };

            // Title Label
            titleLabel = new Label();
            titleLabel.Text = "ZENITH SUITE INSTALLER";
            titleLabel.ForeColor = Color.White;
            titleLabel.Font = new Font("Arial", 9F, FontStyle.Bold);
            titleLabel.Location = new Point(15, 12);
            titleLabel.AutoSize = true;
            headerPanel.Controls.Add(titleLabel);

            // Close Button
            closeButton = new Button();
            closeButton.Text = "X";
            closeButton.Size = new Size(30, 24);
            closeButton.Location = new Point(this.Width - 40, 8);
            closeButton.FlatStyle = FlatStyle.Flat;
            closeButton.ForeColor = Color.FromArgb(142, 142, 147);
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.BackColor = Color.Transparent;
            closeButton.Click += (s, e) => this.Close();
            closeButton.MouseEnter += (s, e) => closeButton.ForeColor = Color.White;
            closeButton.MouseLeave += (s, e) => closeButton.ForeColor = Color.FromArgb(142, 142, 147);
            headerPanel.Controls.Add(closeButton);

            this.Controls.Add(headerPanel);

            // Content Panel (Pages)
            contentPanel = new Panel();
            contentPanel.Size = new Size(this.Width - 60, 200);
            contentPanel.Location = new Point(30, 60);
            this.Controls.Add(contentPanel);

            // Next / Install Button
            nextButton = new Button();
            nextButton.Size = new Size(130, 36);
            nextButton.Location = new Point(this.Width - 160, this.Height - 65);
            nextButton.FlatStyle = FlatStyle.Flat;
            nextButton.BackColor = Color.FromArgb(37, 99, 235); // Solid Blue Accent
            nextButton.ForeColor = Color.White;
            nextButton.Font = new Font("Arial", 9F, FontStyle.Bold);
            nextButton.FlatAppearance.BorderSize = 0;
            nextButton.Cursor = Cursors.Hand;
            nextButton.Click += NextButton_Click;
            this.Controls.Add(nextButton);

            // Back Button
            backButton = new Button();
            backButton.Text = "Back";
            backButton.Size = new Size(100, 36);
            backButton.Location = new Point(this.Width - 270, this.Height - 65);
            backButton.FlatStyle = FlatStyle.Flat;
            backButton.BackColor = Color.FromArgb(20, 20, 24); // Solid Charcoal
            backButton.ForeColor = Color.FromArgb(142, 142, 147);
            backButton.Font = new Font("Arial", 9F, FontStyle.Bold);
            backButton.FlatAppearance.BorderSize = 1;
            backButton.FlatAppearance.BorderColor = Color.FromArgb(34, 34, 39);
            backButton.Cursor = Cursors.Hand;
            backButton.Click += (s, e) => ShowStep(currentStep - 1);
            this.Controls.Add(backButton);

            // Bottom Border Decoration
            Panel borderLine = new Panel();
            borderLine.Size = new Size(this.Width, 1);
            borderLine.Location = new Point(0, this.Height - 85);
            borderLine.BackColor = Color.FromArgb(34, 34, 39);
            this.Controls.Add(borderLine);
        }

        private void ShowStep(int step)
        {
            currentStep = step;
            contentPanel.Controls.Clear();
            nextButton.Visible = true;
            nextButton.Width = 130;
            nextButton.Location = new Point(this.Width - 160, this.Height - 65);

            if (currentStep == 0)
            {
                // Step 0: Welcome screen
                backButton.Visible = false;
                nextButton.Text = "Next";

                welcomeLabel = new Label();
                welcomeLabel.Text = "Break the Compulsion Cycle.";
                welcomeLabel.Font = new Font("Arial", 16F, FontStyle.Bold);
                welcomeLabel.ForeColor = Color.White;
                welcomeLabel.Size = new Size(contentPanel.Width, 35);
                welcomeLabel.Location = new Point(0, 10);
                contentPanel.Controls.Add(welcomeLabel);

                welcomeDesc = new Label();
                welcomeDesc.Text = "Welcome to Zenith Anti-PMO Setup.\n\nThis wizard will install your standalone Desktop App, Extension files, and system-wide background Shield.\n\nAll components are extremely lightweight (under 90 KB total) and operate silently using flat, distraction-free matte styling.";
                welcomeDesc.Font = new Font("Arial", 9.5F);
                welcomeDesc.ForeColor = Color.FromArgb(142, 142, 147);
                welcomeDesc.Size = new Size(contentPanel.Width - 20, 120);
                welcomeDesc.Location = new Point(0, 55);
                contentPanel.Controls.Add(welcomeDesc);
            }
            else if (currentStep == 1)
            {
                // Step 1: Destination location selection (Editable + Browse Button)
                backButton.Visible = true;
                nextButton.Text = "Next";

                pathLabel = new Label();
                pathLabel.Text = "Choose Install Location";
                pathLabel.Font = new Font("Arial", 13F, FontStyle.Bold);
                pathLabel.ForeColor = Color.White;
                pathLabel.Size = new Size(contentPanel.Width, 30);
                pathLabel.Location = new Point(0, 10);
                contentPanel.Controls.Add(pathLabel);

                Label pathDesc = new Label();
                pathDesc.Text = "Select the folder where you want to install Zenith. Click 'Browse' to choose a custom directory.";
                pathDesc.Font = new Font("Arial", 9.5F);
                pathDesc.ForeColor = Color.FromArgb(142, 142, 147);
                pathDesc.Size = new Size(contentPanel.Width - 20, 45);
                pathDesc.Location = new Point(0, 45);
                contentPanel.Controls.Add(pathDesc);

                pathTextBox = new TextBox();
                pathTextBox.Text = installPath;
                pathTextBox.BackColor = Color.FromArgb(20, 20, 24);
                pathTextBox.ForeColor = Color.White;
                pathTextBox.BorderStyle = BorderStyle.FixedSingle;
                pathTextBox.Font = new Font("Consolas", 10F);
                pathTextBox.Size = new Size(contentPanel.Width - 110, 30);
                pathTextBox.Location = new Point(0, 110);
                pathTextBox.ReadOnly = false;
                pathTextBox.TextChanged += (s, e) => installPath = pathTextBox.Text;
                contentPanel.Controls.Add(pathTextBox);

                // Browse Button
                browseButton = new Button();
                browseButton.Text = "Browse...";
                browseButton.Size = new Size(95, 25);
                browseButton.Location = new Point(contentPanel.Width - 95, 109);
                browseButton.FlatStyle = FlatStyle.Flat;
                browseButton.BackColor = Color.FromArgb(26, 26, 32);
                browseButton.ForeColor = Color.White;
                browseButton.FlatAppearance.BorderColor = Color.FromArgb(45, 45, 52);
                browseButton.Font = new Font("Arial", 8.5F, FontStyle.Bold);
                browseButton.Cursor = Cursors.Hand;
                browseButton.Click += BrowseButton_Click;
                contentPanel.Controls.Add(browseButton);
            }
            else if (currentStep == 2)
            {
                // Step 2: Feature Customization Options (Checkboxes)
                backButton.Visible = true;
                nextButton.Text = "Install";

                featureLabel = new Label();
                featureLabel.Text = "Customize Components";
                featureLabel.Font = new Font("Arial", 13F, FontStyle.Bold);
                featureLabel.ForeColor = Color.White;
                featureLabel.Size = new Size(contentPanel.Width, 25);
                featureLabel.Location = new Point(0, 5);
                contentPanel.Controls.Add(featureLabel);

                // Checkbox 1: System Shield
                chkShield = new CheckBox();
                chkShield.Text = "Install Background Shield Guard (Anti-Bypass Protection)";
                chkShield.ForeColor = Color.White;
                chkShield.FlatStyle = FlatStyle.Flat;
                chkShield.Font = new Font("Arial", 9F);
                chkShield.Size = new Size(contentPanel.Width, 22);
                chkShield.Location = new Point(5, 45);
                chkShield.Checked = true;
                contentPanel.Controls.Add(chkShield);

                // Checkbox 2: Standalone Desktop App
                chkApp = new CheckBox();
                chkApp.Text = "Install Standalone Desktop App Launcher shell";
                chkApp.ForeColor = Color.White;
                chkApp.FlatStyle = FlatStyle.Flat;
                chkApp.Font = new Font("Arial", 9F);
                chkApp.Size = new Size(contentPanel.Width, 22);
                chkApp.Location = new Point(5, 75);
                chkApp.Checked = true;
                contentPanel.Controls.Add(chkApp);

                // Checkbox 3: Desktop Shortcut
                chkDesktop = new CheckBox();
                chkDesktop.Text = "Create Desktop Application Shortcut link";
                chkDesktop.ForeColor = Color.White;
                chkDesktop.FlatStyle = FlatStyle.Flat;
                chkDesktop.Font = new Font("Arial", 9F);
                chkDesktop.Size = new Size(contentPanel.Width, 22);
                chkDesktop.Location = new Point(5, 105);
                chkDesktop.Checked = true;
                contentPanel.Controls.Add(chkDesktop);

                // Checkbox 4: Windows Startup
                chkStartup = new CheckBox();
                chkStartup.Text = "Automatically start background Guard on Windows boot";
                chkStartup.ForeColor = Color.White;
                chkStartup.FlatStyle = FlatStyle.Flat;
                chkStartup.Font = new Font("Arial", 9F);
                chkStartup.Size = new Size(contentPanel.Width, 22);
                chkStartup.Location = new Point(5, 135);
                chkStartup.Checked = true;
                contentPanel.Controls.Add(chkStartup);

                // Checkbox 5: Watchdog Protection
                chkWatchdog = new CheckBox();
                chkWatchdog.Text = "Enable Watchdog Keep-Alive (Prevents ending in Task Manager)";
                chkWatchdog.ForeColor = Color.White;
                chkWatchdog.FlatStyle = FlatStyle.Flat;
                chkWatchdog.Font = new Font("Arial", 9F);
                chkWatchdog.Size = new Size(contentPanel.Width, 22);
                chkWatchdog.Location = new Point(5, 165);
                chkWatchdog.Checked = true;
                contentPanel.Controls.Add(chkWatchdog);

                // Bind Startup & Watchdog activation option state to Shield option state
                chkShield.CheckedChanged += (s, e) => {
                    if (!chkShield.Checked)
                    {
                        chkStartup.Checked = false;
                        chkStartup.Enabled = false;
                        chkWatchdog.Checked = false;
                        chkWatchdog.Enabled = false;
                    }
                    else
                    {
                        chkStartup.Enabled = true;
                        chkStartup.Checked = true;
                        chkWatchdog.Enabled = true;
                        chkWatchdog.Checked = true;
                    }
                };
            }
            else if (currentStep == 3)
            {
                // Step 3: Installation Progressive Meter Progress Bar
                backButton.Visible = false;
                nextButton.Visible = false;

                progressLabel = new Label();
                progressLabel.Text = "Initializing files...";
                progressLabel.Font = new Font("Arial", 12F, FontStyle.Bold);
                progressLabel.ForeColor = Color.White;
                progressLabel.Size = new Size(contentPanel.Width, 30);
                progressLabel.Location = new Point(0, 35);
                contentPanel.Controls.Add(progressLabel);

                progressBarBg = new Panel();
                progressBarBg.Size = new Size(contentPanel.Width, 12);
                progressBarBg.Location = new Point(0, 85);
                progressBarBg.BackColor = Color.FromArgb(31, 31, 36);
                contentPanel.Controls.Add(progressBarBg);

                progressBarFill = new Panel();
                progressBarFill.Size = new Size(0, 12);
                progressBarFill.Location = new Point(0, 0);
                progressBarFill.BackColor = Color.FromArgb(37, 99, 235); // Solid Blue
                progressBarBg.Controls.Add(progressBarFill);

                progressValue = 0;
                installTimer = new System.Windows.Forms.Timer();
                installTimer.Interval = 50;
                installTimer.Tick += InstallTimer_Tick;
                installTimer.Start();
            }
            else if (currentStep == 4)
            {
                // Step 4: Installation Completion screen
                backButton.Visible = false;
                nextButton.Visible = true;
                
                // If Standalone App was installed, let them launch it, otherwise just exit setup
                if (chkApp != null && chkApp.Checked)
                {
                    nextButton.Text = "Launch Standalone App";
                    nextButton.Width = 180;
                }
                else
                {
                    nextButton.Text = "Complete Setup";
                    nextButton.Width = 130;
                }
                nextButton.Location = new Point(this.Width - (nextButton.Width + 30), this.Height - 65);

                finishLabel = new Label();
                finishLabel.Text = "Zenith Suite Configured!";
                finishLabel.Font = new Font("Arial", 14F, FontStyle.Bold);
                finishLabel.ForeColor = Color.FromArgb(5, 150, 105); // Solid Green Accent
                finishLabel.Size = new Size(contentPanel.Width, 30);
                finishLabel.Location = new Point(0, 5);
                contentPanel.Controls.Add(finishLabel);

                finishDesc = new Label();
                
                string guardStatus = (chkShield != null && chkShield.Checked) ? "• System bypass guard is active in background.\n" : "";
                string startupStatus = (chkStartup != null && chkStartup.Checked) ? "• Guard will automatically start on Windows boot.\n" : "";
                string watchdogStatus = (chkWatchdog != null && chkWatchdog.Checked) ? "• Anti-kill watchdog protection enabled.\n" : "";
                string desktopStatus = (chkDesktop != null && chkDesktop.Checked) ? "• Standalone desktop shortcut generated.\n" : "";

                finishDesc.Text = guardStatus + startupStatus + watchdogStatus + desktopStatus +
                                  "• Extension directory path copied to your clipboard.\n" +
                                  "• Google Chrome opened automatically.\n\n" +
                                  "Required Actions to complete integration:\n" +
                                  "1. In Chrome, click 'Load unpacked' (Developer Mode enabled).\n" +
                                  "2. Press Ctrl+V to paste the path, and click Select Folder.\n" +
                                  "3. Enable 'Allow in incognito' in extensions Details.";
                                  
                finishDesc.Font = new Font("Arial", 9F);
                finishDesc.ForeColor = Color.FromArgb(142, 142, 147);
                finishDesc.Size = new Size(contentPanel.Width, 140);
                finishDesc.Location = new Point(0, 40);
                contentPanel.Controls.Add(finishDesc);
            }
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select target folder to install Zenith Anti-PMO:";
                fbd.SelectedPath = installPath;
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    installPath = fbd.SelectedPath;
                    pathTextBox.Text = installPath;
                }
            }
        }

        private void NextButton_Click(object sender, EventArgs e)
        {
            if (currentStep < 4)
            {
                ShowStep(currentStep + 1);
            }
            else
            {
                // Action on Exit Setup
                if (chkApp != null && chkApp.Checked)
                {
                    try
                    {
                        string appOutput = Path.Combine(installPath, "zenith-app.exe");
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = appOutput,
                            WorkingDirectory = installPath
                        });
                    }
                    catch {}
                }
                this.Close();
            }
        }

        private void InstallTimer_Tick(object sender, EventArgs e)
        {
            progressValue += 4;
            if (progressValue <= 100)
            {
                progressBarFill.Width = (contentPanel.Width * progressValue) / 100;
                
                if (progressValue == 20)
                {
                    progressLabel.Text = "Creating target directories...";
                    CreateDirectories();
                }
                else if (progressValue == 60)
                {
                    progressLabel.Text = "Copying suite components...";
                    CopySuiteFiles();
                }
                else if (progressValue == 85)
                {
                    progressLabel.Text = "Setting up shortcut triggers...";
                    
                    if (chkDesktop != null && chkDesktop.Checked)
                    {
                        CreateDesktopShortcut();
                    }
                    
                    if (chkStartup != null && chkStartup.Checked)
                    {
                        RegisterBackgroundStartup();
                    }

                    if (chkWatchdog != null)
                    {
                        string disableFilePath = Path.Combine(installPath, "watchdog.disabled");
                        if (!chkWatchdog.Checked)
                        {
                            try
                            {
                                File.WriteAllText(disableFilePath, "disabled");
                            }
                            catch {}
                        }
                        else
                        {
                            try
                            {
                                if (File.Exists(disableFilePath))
                                {
                                    File.Delete(disableFilePath);
                                }
                            }
                            catch {}
                        }
                    }
                }
            }
            else
            {
                installTimer.Stop();

                // 1. Activate background Shield process if selected
                if (chkShield != null && chkShield.Checked)
                {
                    StartBackgroundShield();
                }

                // 2. Perform Chrome configuration preparation
                CopyExtensionPathToClipboard();
                LaunchChromeExtensions();

                ShowStep(4);
            }
        }

        private void CreateDirectories()
        {
            try
            {
                // Ensure target folder exists without deleting user's custom parent folders!
                if (!Directory.Exists(installPath))
                {
                    Directory.CreateDirectory(installPath);
                }
            }
            catch {}
        }

        private void CopySuiteFiles()
        {
            try
            {
                string sourceDir = AppDomain.CurrentDomain.BaseDirectory;
                
                // Copy directory structure recursively
                foreach (string dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
                {
                    string relativePath = dirPath.Substring(sourceDir.Length).TrimStart('\\', '/');
                    
                    // Skip source files, git controls, or compilation helper folders
                    if (relativePath.StartsWith("shield") || relativePath.StartsWith(".git"))
                    {
                        continue;
                    }
                    Directory.CreateDirectory(Path.Combine(installPath, relativePath));
                }

                // Copy all selected files
                foreach (string filePath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
                {
                    string relativePath = filePath.Substring(sourceDir.Length).TrimStart('\\', '/');
                    string fileName = Path.GetFileName(filePath).ToLower();

                    // Filter shield source directories and setup installer files
                    if (relativePath.StartsWith("shield") || relativePath.StartsWith(".git") || 
                        fileName == "create_shortcut.vbs" || fileName == "zenith-setup.exe")
                    {
                        continue;
                    }

                    // Filter components based on user's feature choices
                    if (fileName == "zenith-shield.exe" && (chkShield == null || !chkShield.Checked))
                    {
                        continue;
                    }
                    if (fileName == "zenith-app.exe" && (chkApp == null || !chkApp.Checked))
                    {
                        continue;
                    }

                    string destPath = Path.Combine(installPath, relativePath);
                    File.Copy(filePath, destPath, true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Warning copying files: " + ex.Message, "Zenith Setup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CreateDesktopShortcut()
        {
            try
            {
                string desktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string shortcutPath = Path.Combine(desktopFolder, "Zenith.lnk");
                
                // Target zenith-app.exe if installed, fallback to target extension folder otherwise
                string targetPath = (chkApp != null && chkApp.Checked) ? 
                    Path.Combine(installPath, "zenith-app.exe") : installPath;

                CreateWScriptShortcut(shortcutPath, targetPath, installPath, "Zenith Anti-PMO Desktop Control Center");
            }
            catch {}
        }

        private void RegisterBackgroundStartup()
        {
            try
            {
                string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                string shortcutPath = Path.Combine(startupFolder, "ZenithShield.lnk");
                string targetExe = Path.Combine(installPath, "zenith-shield.exe");

                CreateWScriptShortcut(shortcutPath, targetExe, installPath, "Zenith Background Guard");
            }
            catch {}
        }

        private void CreateWScriptShortcut(string path, string target, string workingDir, string description)
        {
            try
            {
                string scriptPath = Path.Combine(installPath, "lnk_create.vbs");
                string vbsContent = string.Format(
                    "Set oWS = WScript.CreateObject(\"WScript.Shell\")\n" +
                    "Set oLink = oWS.CreateShortcut(\"{0}\")\n" +
                    "oLink.TargetPath = \"{1}\"\n" +
                    "oLink.WorkingDirectory = \"{2}\"\n" +
                    "oLink.Description = \"{3}\"\n" +
                    "oLink.Save()",
                    path.Replace("\\", "\\\\"),
                    target.Replace("\\", "\\\\"),
                    workingDir.Replace("\\", "\\\\"),
                    description
                );
                File.WriteAllText(scriptPath, vbsContent);

                ProcessStartInfo info = new ProcessStartInfo("wscript.exe", "\"" + scriptPath + "\"");
                info.WindowStyle = ProcessWindowStyle.Hidden;
                Process.Start(info).WaitForExit();

                if (File.Exists(scriptPath))
                {
                    File.Delete(scriptPath);
                }
            }
            catch {}
        }

        private void StartBackgroundShield()
        {
            try
            {
                // Terminate any existing running guard process
                Process[] activeShields = Process.GetProcessesByName("zenith-shield");
                foreach (Process p in activeShields)
                {
                    p.Kill();
                }
                Thread.Sleep(500);
            }
            catch {}

            try
            {
                string shieldExe = Path.Combine(installPath, "zenith-shield.exe");
                if (File.Exists(shieldExe))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = shieldExe,
                        WorkingDirectory = installPath,
                        WindowStyle = ProcessWindowStyle.Hidden
                    });
                }
            }
            catch {}
        }

        private void CopyExtensionPathToClipboard()
        {
            try
            {
                // The extension matches the target installation path folder
                Clipboard.SetText(installPath);
            }
            catch {}
        }

        private void LaunchChromeExtensions()
        {
            try
            {
                Process.Start("chrome.exe", "chrome://extensions");
            }
            catch {}
        }
    }
}
