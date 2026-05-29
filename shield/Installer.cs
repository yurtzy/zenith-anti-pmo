using System;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32;

[assembly: AssemblyTitle("Zenith Setup Installer")]
[assembly: AssemblyDescription("Zenith Self-Control Suite Setup Wizard")]
[assembly: AssemblyCompany("yurtzy")]
[assembly: AssemblyProduct("Zenith Focus Suite")]
[assembly: AssemblyCopyright("Copyright © 2026 yurtzy")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

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

        // Step 1b: Terms Controls
        private Label termsLabel;
        private TextBox termsTextBox;
        private CheckBox chkAgreeTerms;

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
        private TextBox finishDesc;

        // Step 6 & 7: Uninstall Controls
        private Button uninstallNavButton;
        private Label uninstallLabel;
        private Label uninstallDesc;
        private Label hwidLabel;
        private TextBox hwidTextBox;
        private Button copyHwidButton;
        private Label keyLabel;
        private TextBox keyTextBox;
        private Label uninstallStatusLabel;
        private bool uninstallFilesCleaned = true;

        private const string PrivateSalt = "zenith_focus_suite_private_salt_2026";

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool HideCaret(IntPtr hWnd);

        private string GetHWID()
        {
            try
            {
                object val = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography", "MachineGuid", "");
                string machineGuid = val != null ? val.ToString() : "FallbackGuid";
                using (System.Security.Cryptography.SHA256 sha = System.Security.Cryptography.SHA256.Create())
                {
                    byte[] hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(machineGuid));
                    string hex = BitConverter.ToString(hash).Replace("-", "");
                    return string.Format("ZEN-{0}-{1}", hex.Substring(0, 4), hex.Substring(4, 4));
                }
            }
            catch
            {
                return "ZEN-FAIL-HWID";
            }
        }

        private bool ValidateDeactivationKey(string hwid, string inputKey)
        {
            try
            {
                using (System.Security.Cryptography.SHA256 sha = System.Security.Cryptography.SHA256.Create())
                {
                    byte[] hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(hwid + PrivateSalt));
                    string hex = BitConverter.ToString(hash).Replace("-", "").Substring(0, 16);
                    string expected = string.Format("KEY-{0}-{1}-{2}-{3}", 
                        hex.Substring(0, 4), 
                        hex.Substring(4, 4), 
                        hex.Substring(8, 4), 
                        hex.Substring(12, 4)
                    );
                    return string.Equals(inputKey.Trim(), expected, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                return false;
            }
        }

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
            backButton.Click += (s, e) => {
                if (currentStep == 6)
                {
                    ShowStep(0);
                }
                else
                {
                    ShowStep(currentStep - 1);
                }
            };
            this.Controls.Add(backButton);

            // Uninstall Nav Button (visible on step 0)
            uninstallNavButton = new Button();
            uninstallNavButton.Text = "Uninstall";
            uninstallNavButton.Size = new Size(100, 36);
            uninstallNavButton.Location = new Point(30, this.Height - 65);
            uninstallNavButton.FlatStyle = FlatStyle.Flat;
            uninstallNavButton.BackColor = Color.FromArgb(20, 20, 24); // Solid Charcoal
            uninstallNavButton.ForeColor = Color.FromArgb(142, 142, 147);
            uninstallNavButton.Font = new Font("Arial", 9F, FontStyle.Bold);
            uninstallNavButton.FlatAppearance.BorderSize = 1;
            uninstallNavButton.FlatAppearance.BorderColor = Color.FromArgb(34, 34, 39);
            uninstallNavButton.Cursor = Cursors.Hand;
            uninstallNavButton.Click += (s, e) => ShowStep(6);
            this.Controls.Add(uninstallNavButton);

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

            if (uninstallNavButton != null)
            {
                uninstallNavButton.Visible = (currentStep == 0);
            }

            if (currentStep == 0)
            {
                // Step 0: Welcome screen
                backButton.Visible = false;
                nextButton.Text = "Next";
                nextButton.Enabled = true;

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
                welcomeDesc.Size = new Size(contentPanel.Width - 20, 100);
                welcomeDesc.Location = new Point(0, 55);
                contentPanel.Controls.Add(welcomeDesc);
            }
            else if (currentStep == 1)
            {
                // Step 1: License Agreement / EULA
                backButton.Visible = true;
                nextButton.Text = "Accept & Next";
                nextButton.Enabled = chkAgreeTerms != null && chkAgreeTerms.Checked;

                termsLabel = new Label();
                termsLabel.Text = "License Agreement & Terms";
                termsLabel.Font = new Font("Arial", 13F, FontStyle.Bold);
                termsLabel.ForeColor = Color.White;
                termsLabel.Size = new Size(contentPanel.Width, 25);
                termsLabel.Location = new Point(0, 5);
                contentPanel.Controls.Add(termsLabel);

                termsTextBox = new TextBox();
                termsTextBox.Multiline = true;
                termsTextBox.ScrollBars = ScrollBars.Vertical;
                termsTextBox.ReadOnly = true;
                termsTextBox.BackColor = Color.FromArgb(15, 15, 18);
                termsTextBox.ForeColor = Color.FromArgb(200, 200, 200);
                termsTextBox.BorderStyle = BorderStyle.FixedSingle;
                termsTextBox.Font = new Font("Segoe UI", 8.5F);
                termsTextBox.Size = new Size(contentPanel.Width, 115);
                termsTextBox.Location = new Point(0, 35);
                termsTextBox.Cursor = Cursors.Default;
                termsTextBox.MouseEnter += (s, e) => termsTextBox.Focus();
                termsTextBox.GotFocus += (s, e) => HideCaret(termsTextBox.Handle);
                termsTextBox.Text = 
                    "ZENITH SELF-CONTROL SUITE - END USER LICENSE AGREEMENT (EULA)\r\n\r\n" +
                    "Developed and Owned by: yurtzy\r\n\r\n" +
                    "1. ACCEPTANCE OF TERMS\r\n" +
                    "By installing and using the Zenith Suite, you agree to be bound by these terms. If you do not agree, cancel this installation.\r\n\r\n" +
                    "2. DESCRIPTION OF SERVICE\r\n" +
                    "Zenith is a productivity tool consisting of a Chrome extension and a native background service (zenith-shield.exe) to help prevent access to adult content and distracting websites/apps.\r\n\r\n" +
                    "3. BACKGROUND SHIELD & KEEP-ALIVE WATCHDOG\r\n" +
                    "The system-wide Guard operates in the background to monitor active window titles. If the 'Watchdog Keep-Alive' component is enabled, Zenith will run mutual-survival watchdog processes that prevent the background shield from being terminated via Task Manager. You acknowledge that this behavior is intentional to prevent digital compulsion.\r\n\r\n" +
                    "4. DISCLAIMER OF WARRANTY\r\n" +
                    "This software is provided 'as is' without warranty of any kind. The developer (yurtzy) is not liable for any system redirect errors, process terminations, or other issues resulting from the use of this software.\r\n\r\n" +
                    "5. PRIVACY POLICY\r\n" +
                    "All title scanning and blocking processes are performed entirely locally on your machine. No web traffic data, browsing history, or personal identifiers are uploaded or transmitted to any external servers.";
                contentPanel.Controls.Add(termsTextBox);

                if (chkAgreeTerms == null)
                {
                    chkAgreeTerms = new CheckBox();
                    chkAgreeTerms.Text = "I accept the Terms and Conditions of this Agreement";
                    chkAgreeTerms.ForeColor = Color.White;
                    chkAgreeTerms.FlatStyle = FlatStyle.Flat;
                    chkAgreeTerms.Font = new Font("Arial", 9F, FontStyle.Bold);
                    chkAgreeTerms.Size = new Size(contentPanel.Width, 22);
                    chkAgreeTerms.CheckedChanged += (s, e) => {
                        nextButton.Enabled = chkAgreeTerms.Checked;
                    };
                }
                chkAgreeTerms.Location = new Point(5, 160);
                contentPanel.Controls.Add(chkAgreeTerms);
            }
            else if (currentStep == 2)
            {
                // Step 2: Destination location selection (Editable + Browse Button)
                backButton.Visible = true;
                nextButton.Text = "Next";
                nextButton.Enabled = true;

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
            else if (currentStep == 3)
            {
                // Step 3: Feature Customization Options (Checkboxes with state retention)
                backButton.Visible = true;
                nextButton.Text = "Install";
                nextButton.Enabled = true;

                featureLabel = new Label();
                featureLabel.Text = "Customize Components";
                featureLabel.Font = new Font("Arial", 13F, FontStyle.Bold);
                featureLabel.ForeColor = Color.White;
                featureLabel.Size = new Size(contentPanel.Width, 25);
                featureLabel.Location = new Point(0, 5);
                contentPanel.Controls.Add(featureLabel);

                if (chkShield == null)
                {
                    chkShield = new CheckBox();
                    chkShield.Text = "Install Background Shield Guard (Anti-Bypass Protection)";
                    chkShield.ForeColor = Color.White;
                    chkShield.FlatStyle = FlatStyle.Flat;
                    chkShield.Font = new Font("Arial", 9F);
                    chkShield.Size = new Size(contentPanel.Width, 22);
                    chkShield.Checked = true;
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
                chkShield.Location = new Point(5, 45);
                contentPanel.Controls.Add(chkShield);

                if (chkApp == null)
                {
                    chkApp = new CheckBox();
                    chkApp.Text = "Install Standalone Desktop App Launcher shell";
                    chkApp.ForeColor = Color.White;
                    chkApp.FlatStyle = FlatStyle.Flat;
                    chkApp.Font = new Font("Arial", 9F);
                    chkApp.Size = new Size(contentPanel.Width, 22);
                    chkApp.Checked = true;
                }
                chkApp.Location = new Point(5, 75);
                contentPanel.Controls.Add(chkApp);

                if (chkDesktop == null)
                {
                    chkDesktop = new CheckBox();
                    chkDesktop.Text = "Create Desktop Application Shortcut link";
                    chkDesktop.ForeColor = Color.White;
                    chkDesktop.FlatStyle = FlatStyle.Flat;
                    chkDesktop.Font = new Font("Arial", 9F);
                    chkDesktop.Size = new Size(contentPanel.Width, 22);
                    chkDesktop.Checked = true;
                }
                chkDesktop.Location = new Point(5, 105);
                contentPanel.Controls.Add(chkDesktop);

                if (chkStartup == null)
                {
                    chkStartup = new CheckBox();
                    chkStartup.Text = "Automatically start background Guard on Windows boot";
                    chkStartup.ForeColor = Color.White;
                    chkStartup.FlatStyle = FlatStyle.Flat;
                    chkStartup.Font = new Font("Arial", 9F);
                    chkStartup.Size = new Size(contentPanel.Width, 22);
                    chkStartup.Checked = true;
                }
                chkStartup.Location = new Point(5, 135);
                chkStartup.Checked = chkShield.Checked && chkStartup.Checked;
                chkStartup.Enabled = chkShield.Checked;
                chkStartup.Location = new Point(5, 135);
                contentPanel.Controls.Add(chkStartup);

                if (chkWatchdog == null)
                {
                    chkWatchdog = new CheckBox();
                    chkWatchdog.Text = "Enable Watchdog Keep-Alive (Prevents ending in Task Manager)";
                    chkWatchdog.ForeColor = Color.White;
                    chkWatchdog.FlatStyle = FlatStyle.Flat;
                    chkWatchdog.Font = new Font("Arial", 9F);
                    chkWatchdog.Size = new Size(contentPanel.Width, 22);
                    chkWatchdog.Checked = true;
                }
                chkWatchdog.Checked = chkShield.Checked && chkWatchdog.Checked;
                chkWatchdog.Enabled = chkShield.Checked;
                chkWatchdog.Location = new Point(5, 165);
                contentPanel.Controls.Add(chkWatchdog);
            }
            else if (currentStep == 4)
            {
                // Step 4: Installation Progressive Meter Progress Bar
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
            else if (currentStep == 5)
            {
                // Step 5: Installation Completion screen
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

                finishDesc = new TextBox();
                finishDesc.Multiline = true;
                finishDesc.ScrollBars = ScrollBars.Vertical;
                finishDesc.ReadOnly = true;
                finishDesc.BackColor = Color.FromArgb(10, 10, 12);
                finishDesc.ForeColor = Color.FromArgb(142, 142, 147);
                finishDesc.BorderStyle = BorderStyle.None;
                finishDesc.Cursor = Cursors.Default;
                finishDesc.MouseEnter += (s, e) => finishDesc.Focus();
                finishDesc.GotFocus += (s, e) => HideCaret(finishDesc.Handle);
                
                string guardStatus = (chkShield != null && chkShield.Checked) ? "• System bypass guard is active in background.\r\n" : "";
                string startupStatus = (chkStartup != null && chkStartup.Checked) ? "• Guard will automatically start on Windows boot.\r\n" : "";
                string watchdogStatus = (chkWatchdog != null && chkWatchdog.Checked) ? "• Anti-kill watchdog protection enabled.\r\n" : "";
                string desktopStatus = (chkDesktop != null && chkDesktop.Checked) ? "• Standalone desktop shortcut generated.\r\n" : "";

                finishDesc.Text = guardStatus + startupStatus + watchdogStatus + desktopStatus +
                                  "• Folder security & dynamic deletion protection enabled.\r\n" +
                                  "• Extension folder path copied to your clipboard!\r\n\r\n" +
                                  "Required Actions to complete integration:\r\n" +
                                  "1. In Chrome, enable 'Developer Mode' (toggle in top-right).\r\n" +
                                  "2. Click 'Load unpacked' (button in top-left).\r\n" +
                                  "3. Press Ctrl+V to paste the path and click 'Select Folder'.\r\n" +
                                  "4. In extension Details, enable 'Allow in incognito'.";
                                  
                finishDesc.Font = new Font("Arial", 9F);
                finishDesc.Size = new Size(contentPanel.Width - 10, 150);
                finishDesc.Location = new Point(0, 40);
                contentPanel.Controls.Add(finishDesc);
            }
            else if (currentStep == 6)
            {
                // Step 6: Uninstall Screen
                backButton.Visible = true;
                nextButton.Text = "Validate & Remove";
                nextButton.Enabled = true;

                uninstallLabel = new Label();
                uninstallLabel.Text = "Deactivate & Uninstall Zenith";
                uninstallLabel.Font = new Font("Arial", 13F, FontStyle.Bold);
                uninstallLabel.ForeColor = Color.White;
                uninstallLabel.Size = new Size(contentPanel.Width, 25);
                uninstallLabel.Location = new Point(0, 5);
                contentPanel.Controls.Add(uninstallLabel);

                uninstallDesc = new Label();
                uninstallDesc.Text = "Enter your Deactivation Key to remove Zenith. To obtain a key, send your Hardware ID below to zenith.suite.help@outlook.com.";
                uninstallDesc.Font = new Font("Arial", 9F);
                uninstallDesc.ForeColor = Color.FromArgb(142, 142, 147);
                uninstallDesc.Size = new Size(contentPanel.Width - 10, 35);
                uninstallDesc.Location = new Point(0, 35);
                contentPanel.Controls.Add(uninstallDesc);

                // Hardware ID Section
                hwidLabel = new Label();
                hwidLabel.Text = "YOUR HARDWARE ID:";
                hwidLabel.Font = new Font("Arial", 8.5F, FontStyle.Bold);
                hwidLabel.ForeColor = Color.FromArgb(142, 142, 147);
                hwidLabel.Size = new Size(150, 20);
                hwidLabel.Location = new Point(0, 80);
                contentPanel.Controls.Add(hwidLabel);

                hwidTextBox = new TextBox();
                hwidTextBox.Text = GetHWID();
                hwidTextBox.ReadOnly = true;
                hwidTextBox.BackColor = Color.FromArgb(20, 20, 24);
                hwidTextBox.ForeColor = Color.White;
                hwidTextBox.BorderStyle = BorderStyle.FixedSingle;
                hwidTextBox.Font = new Font("Consolas", 10F, FontStyle.Bold);
                hwidTextBox.Size = new Size(180, 25);
                hwidTextBox.Location = new Point(140, 77);
                hwidTextBox.Cursor = Cursors.Default;
                hwidTextBox.MouseEnter += (s, e) => hwidTextBox.Focus();
                hwidTextBox.GotFocus += (s, e) => HideCaret(hwidTextBox.Handle);
                contentPanel.Controls.Add(hwidTextBox);

                copyHwidButton = new Button();
                copyHwidButton.Text = "Copy";
                copyHwidButton.Size = new Size(60, 23);
                copyHwidButton.Location = new Point(325, 77);
                copyHwidButton.FlatStyle = FlatStyle.Flat;
                copyHwidButton.BackColor = Color.FromArgb(26, 26, 32);
                copyHwidButton.ForeColor = Color.White;
                copyHwidButton.FlatAppearance.BorderColor = Color.FromArgb(45, 45, 52);
                copyHwidButton.Font = new Font("Arial", 8F, FontStyle.Bold);
                copyHwidButton.Cursor = Cursors.Hand;
                copyHwidButton.Click += (s, e) => {
                    try {
                        Clipboard.SetText(hwidTextBox.Text);
                        copyHwidButton.Text = "Copied!";
                        System.Windows.Forms.Timer resetTimer = new System.Windows.Forms.Timer();
                        resetTimer.Interval = 2000;
                        resetTimer.Tick += (sender_t, e_t) => {
                            copyHwidButton.Text = "Copy";
                            resetTimer.Stop();
                            resetTimer.Dispose();
                        };
                        resetTimer.Start();
                    } catch {}
                };
                contentPanel.Controls.Add(copyHwidButton);

                // Deactivation Key Section
                keyLabel = new Label();
                keyLabel.Text = "DEACTIVATION KEY:";
                keyLabel.Font = new Font("Arial", 8.5F, FontStyle.Bold);
                keyLabel.ForeColor = Color.FromArgb(142, 142, 147);
                keyLabel.Size = new Size(150, 20);
                keyLabel.Location = new Point(0, 115);
                contentPanel.Controls.Add(keyLabel);

                keyTextBox = new TextBox();
                keyTextBox.BackColor = Color.FromArgb(20, 20, 24);
                keyTextBox.ForeColor = Color.White;
                keyTextBox.BorderStyle = BorderStyle.FixedSingle;
                keyTextBox.Font = new Font("Consolas", 10F, FontStyle.Bold);
                keyTextBox.Size = new Size(245, 25);
                keyTextBox.Location = new Point(140, 112);
                contentPanel.Controls.Add(keyTextBox);

                // Status message
                uninstallStatusLabel = new Label();
                uninstallStatusLabel.Text = "";
                uninstallStatusLabel.Font = new Font("Arial", 9F, FontStyle.Bold);
                uninstallStatusLabel.ForeColor = Color.FromArgb(239, 68, 68); // Red
                uninstallStatusLabel.Size = new Size(contentPanel.Width - 10, 20);
                uninstallStatusLabel.Location = new Point(140, 145);
                contentPanel.Controls.Add(uninstallStatusLabel);
            }
            else if (currentStep == 7)
            {
                backButton.Visible = false;
                nextButton.Text = "Complete";
                nextButton.Width = 130;
                nextButton.Location = new Point(this.Width - 160, this.Height - 65);

                uninstallLabel = new Label();
                uninstallLabel.Text = "Zenith Suite Deactivated!";
                uninstallLabel.Font = new Font("Arial", 14F, FontStyle.Bold);
                uninstallLabel.ForeColor = Color.FromArgb(5, 150, 105); // Green
                uninstallLabel.Size = new Size(contentPanel.Width, 30);
                uninstallLabel.Location = new Point(0, 5);
                contentPanel.Controls.Add(uninstallLabel);

                Label successDesc = new Label();
                if (uninstallFilesCleaned)
                {
                    successDesc.Text = "All background guard shields have been terminated, and local files and shortcuts have been successfully deleted from your computer.\n\nClick 'Complete' to exit this setup wizard.";
                }
                else
                {
                    successDesc.Text = "Background guard shields have been successfully terminated.\n\nSome local files could not be deleted automatically (possibly locked by open browser profiles). You can safely delete them manually at your installation path:\n\n" + installPath + "\n\nClick 'Complete' to exit this setup wizard.";
                }
                successDesc.Font = new Font("Arial", 9.5F);
                successDesc.ForeColor = Color.FromArgb(142, 142, 147);
                successDesc.Size = new Size(contentPanel.Width - 10, 120);
                successDesc.Location = new Point(0, 45);
                contentPanel.Controls.Add(successDesc);
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
            if (currentStep == 6)
            {
                ExecuteUninstall();
            }
            else if (currentStep == 7)
            {
                this.Close();
            }
            else if (currentStep < 5)
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

        private void ExecuteUninstall()
        {
            string hwid = hwidTextBox.Text;
            string key = keyTextBox.Text;

            if (!ValidateDeactivationKey(hwid, key))
            {
                uninstallStatusLabel.ForeColor = Color.FromArgb(239, 68, 68); // Red
                uninstallStatusLabel.Text = "Invalid Deactivation Key. Please verify and try again.";
                return;
            }

            uninstallStatusLabel.ForeColor = Color.FromArgb(5, 150, 105); // Green
            uninstallStatusLabel.Text = "Key validated! Deactivating watchdog...";
            this.Update();

            try
            {
                // 1. Stop background processes (shield and watchdog) simultaneously
                ProcessStartInfo taskkillInfo = new ProcessStartInfo("taskkill", "/f /im zenith-shield.exe");
                taskkillInfo.CreateNoWindow = true;
                taskkillInfo.UseShellExecute = false;
                taskkillInfo.WindowStyle = ProcessWindowStyle.Hidden;
                
                using (Process p = Process.Start(taskkillInfo))
                {
                    p.WaitForExit(3000);
                }
                
                Thread.Sleep(800); // Wait for file locks to release
            }
            catch {}

            // 2. Perform cleanup of files and shortcuts
            uninstallFilesCleaned = true;
            UnprotectFolder(installPath);
            UnregisterExternalExtensions();
            try
            {
                // Delete Desktop Shortcut
                string desktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string desktopLnk = Path.Combine(desktopFolder, "Zenith.lnk");
                if (File.Exists(desktopLnk))
                {
                    File.Delete(desktopLnk);
                }

                // Delete Startup Shortcut
                string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                string startupLnk = Path.Combine(startupFolder, "ZenithShield.lnk");
                if (File.Exists(startupLnk))
                {
                    File.Delete(startupLnk);
                }

                // Delete directory files
                if (Directory.Exists(installPath))
                {
                    foreach (string file in Directory.GetFiles(installPath, "*.*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            uninstallFilesCleaned = false;
                        }
                    }
                    foreach (string dir in Directory.GetDirectories(installPath, "*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            Directory.Delete(dir, true);
                        }
                        catch {}
                    }
                    try
                    {
                        Directory.Delete(installPath, true);
                    }
                    catch {}
                }
            }
            catch
            {
                uninstallFilesCleaned = false;
            }

            // 3. Show Success Screen
            ShowStep(7);
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

                // Apply dynamic folder protection and register browser external extensions
                RegisterExternalExtensions(installPath);
                ProtectFolder(installPath);

                // 1. Activate background Shield process if selected
                if (chkShield != null && chkShield.Checked)
                {
                    StartBackgroundShield();
                }

                // 2. Perform Chrome configuration preparation
                CopyExtensionPathToClipboard();
                LaunchChromeExtensions();

                ShowStep(5);
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

                // 1. Copy core executables (only those requested)
                string[] coreExes = new string[] { "zenith-shield.exe", "zenith-app.exe" };
                foreach (string exe in coreExes)
                {
                    string srcPath = Path.Combine(sourceDir, exe);
                    if (File.Exists(srcPath))
                    {
                        if (exe == "zenith-shield.exe" && (chkShield == null || !chkShield.Checked)) continue;
                        if (exe == "zenith-app.exe" && (chkApp == null || !chkApp.Checked)) continue;
                        
                        string destPath = Path.Combine(installPath, exe);
                        File.Copy(srcPath, destPath, true);
                    }
                }

                // 2. Recursively copy ONLY the extension subfolder
                string srcExtensionDir = Path.Combine(sourceDir, "extension");
                if (Directory.Exists(srcExtensionDir))
                {
                    string destExtensionDir = Path.Combine(installPath, "extension");
                    CopyDirectoryRecursively(srcExtensionDir, destExtensionDir);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Warning copying files: " + ex.Message, "Zenith Setup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CopyDirectoryRecursively(string sourceDir, string destDir)
        {
            try
            {
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                // Copy all files in the current folder level
                foreach (string file in Directory.GetFiles(sourceDir))
                {
                    string destFile = Path.Combine(destDir, Path.GetFileName(file));
                    File.Copy(file, destFile, true);
                }

                // Recurse into subfolders
                foreach (string folder in Directory.GetDirectories(sourceDir))
                {
                    string folderName = Path.GetFileName(folder);
                    // Prevent infinite loops or copying git controls just in case
                    if (folderName.Equals(".git", StringComparison.OrdinalIgnoreCase)) continue;
                    
                    string destFolder = Path.Combine(destDir, folderName);
                    CopyDirectoryRecursively(folder, destFolder);
                }
            }
            catch {}
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
                // The extension matches the target installation path's extension subfolder
                Clipboard.SetText(Path.Combine(installPath, "extension"));
            }
            catch {}
        }

        private void LaunchChromeExtensions()
        {
            try
            {
                Process.Start("chrome.exe");
            }
            catch 
            {
                try
                {
                    Process.Start("msedge.exe");
                }
                catch {}
            }
        }

        private void ProtectFolder(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                DirectoryInfo dInfo = new DirectoryInfo(path);
                DirectorySecurity dSecurity = dInfo.GetAccessControl();

                SecurityIdentifier currentUser = WindowsIdentity.GetCurrent().User;

                // Create a deny rule for delete and delete subdirectories and files
                FileSystemAccessRule denyDelete = new FileSystemAccessRule(
                    currentUser,
                    FileSystemRights.Delete | FileSystemRights.DeleteSubdirectoriesAndFiles,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Deny
                );

                dSecurity.AddAccessRule(denyDelete);
                dInfo.SetAccessControl(dSecurity);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Folder protect error: " + ex.Message);
            }
        }

        private void UnprotectFolder(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    DirectoryInfo dInfo = new DirectoryInfo(path);
                    DirectorySecurity dSecurity = dInfo.GetAccessControl();
                    SecurityIdentifier currentUser = WindowsIdentity.GetCurrent().User;

                    // Remove all Deny Delete rules for current user
                    AuthorizationRuleCollection rules = dSecurity.GetAccessRules(true, false, typeof(SecurityIdentifier));
                    foreach (FileSystemAccessRule rule in rules)
                    {
                        if (rule.AccessControlType == AccessControlType.Deny && rule.IdentityReference == currentUser)
                        {
                            if ((rule.FileSystemRights & FileSystemRights.Delete) != 0 || 
                                (rule.FileSystemRights & FileSystemRights.DeleteSubdirectoriesAndFiles) != 0)
                            {
                                dSecurity.RemoveAccessRuleSpecific(rule);
                            }
                        }
                    }
                    dInfo.SetAccessControl(dSecurity);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Folder unprotect error: " + ex.Message);
            }
        }

        private void RegisterExternalExtensions(string path)
        {
            try
            {
                string extensionPath = Path.Combine(path, "extension");
                if (Directory.Exists(extensionPath))
                {
                    // Register for Chrome
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Google\Chrome\Extensions\bonebkgnmbaongbgjfalllepkbkahhda"))
                    {
                        if (key != null)
                        {
                            key.SetValue("path", extensionPath);
                            key.SetValue("version", "1.5.0");
                        }
                    }

                    // Register for Edge
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Edge\Extensions\bonebkgnmbaongbgjfalllepkbkahhda"))
                    {
                        if (key != null)
                        {
                            key.SetValue("path", extensionPath);
                            key.SetValue("version", "1.5.0");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Registry registration error: " + ex.Message);
            }
        }

        private void UnregisterExternalExtensions()
        {
            try
            {
                // Delete Chrome extension key
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Google\Chrome\Extensions\bonebkgnmbaongbgjfalllepkbkahhda", false);

                // Delete Edge extension key
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Edge\Extensions\bonebkgnmbaongbgjfalllepkbkahhda", false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Registry unregistration error: " + ex.Message);
            }
        }
    }
}
