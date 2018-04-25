using Newtonsoft.Json;
using SharpRaven;
using SharpRaven.Data;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Windows.Forms;

namespace rayshud_Installer
{
    public partial class Main : Form
    {
        // Create the error-tracking object
        private readonly RavenClient ravenClient = new RavenClient(Properties.Settings.Default.SentryIO);
        // Create the HUD configuration object
        private RootObject settings = new RootObject();
        public string TF2Directory;

        #region DEBUG
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, [System.Runtime.InteropServices.In] ref uint pcFonts);
        private PrivateFontCollection fonts = new PrivateFontCollection();
        private Font myFont;
        #endregion DEBUG

        public Main()
        {
            InitializeComponent();
            InitializeCustomFonts();
            CheckLiveVersion();
            CheckTF2Directory();
        }

        #region DEBUG
        private void InitializeCustomFonts()
        {
            var fontData = Properties.Resources.Crosshairs;
            var fontPtr = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(fontData.Length);
            System.Runtime.InteropServices.Marshal.Copy(fontData, 0, fontPtr, fontData.Length);
            uint dummy = 0;
            fonts.AddMemoryFont(fontPtr, fontData.Length);
            AddFontMemResourceEx(fontPtr, (uint)fontData.Length, IntPtr.Zero, ref dummy);
            System.Runtime.InteropServices.Marshal.FreeCoTaskMem(fontPtr);
            myFont = new Font(fonts.Families[0], 16.0F);

            lblCrosshair.Parent = pbPreview;
            lblCrosshair.BackColor = Color.Transparent;
            lblCrosshair.Font = new Font(myFont.FontFamily, 52, FontStyle.Regular);
            //lblCrosshair.Font = myFont;
            //this.Font = myFont;

            // This event will be raised on the worker thread when the worker starts
            backgroundWorker1.DoWork += backgroundWorker1_DoWork;
            // This event will be raised when we call ReportProgress
            backgroundWorker1.ProgressChanged += backgroundWorker1_ProgressChanged;
        }

        // On worker thread so do our thing!
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // Report progress to 'UI' thread
                backgroundWorker1.ReportProgress(25);
                // Remove the temporary downloaded rayshud files
                if (File.Exists($"{Application.StartupPath}\\{Properties.Settings.Default.TempFileName}.zip"))
                    File.Delete($"{Application.StartupPath}\\{Properties.Settings.Default.TempFileName}.zip");
                // Download the latest rayshud from GitHub and extract into the tf/custom directory
                // Report progress to 'UI' thread
                backgroundWorker1.ReportProgress(50);
                var client = new WebClient();
                // Back-up the installer configuration file
                if (!File.Exists($"{Application.StartupPath}\\settings.json"))
                    client.DownloadFile($"https://raw.githubusercontent.com/CriticalFlaw/rayshud-Installer/master/rayshud-installer/settings.json", $"{Application.StartupPath}\\settings.json");
                client.DownloadFile($"https://github.com/raysfire/rayshud/archive/{Properties.Settings.Default.GitBranch}.zip", $"{Properties.Settings.Default.TempFileName}.zip");    //DEBUG
                ZipFile.ExtractToDirectory($"{Application.StartupPath}\\{Properties.Settings.Default.TempFileName}.zip", TF2Directory);
                // Either do a clean install or refresh/update of rayshud
                switch (btnInstall.Text)
                {
                    case "Install":
                        Directory.Move($"{TF2Directory}\\rayshud-{Properties.Settings.Default.GitBranch}", $"{TF2Directory}\\rayshud");
                        MessageBox.Show(Properties.Settings.Default.SuccessInstallMessage, "rayshud Installed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;

                    case "Update":
                    case "Refresh":
                        // Replace the installed rayshud with a fresh copy
                        Directory.Delete($"{TF2Directory}\\rayshud", true);
                        Directory.Move($"{TF2Directory}\\rayshud-{Properties.Settings.Default.GitBranch}", $"{TF2Directory}\\rayshud");
                        MessageBox.Show(Properties.Settings.Default.SuccessUpdateMessage, "rayshud Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                }
                // Report progress to 'UI' thread
                backgroundWorker1.ReportProgress(75);
                // Remove the temporary downloaded rayshud files
                if (File.Exists($"{Application.StartupPath}\\{Properties.Settings.Default.TempFileName}.zip"))
                    File.Delete($"{Application.StartupPath}\\{Properties.Settings.Default.TempFileName}.zip");
                // Report progress to 'UI' thread
                backgroundWorker1.ReportProgress(100);
                // Simulate long task
                System.Threading.Thread.Sleep(100);
                //CheckHUDDirectory();
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"{Properties.Settings.Default.ErrorInstallMessage}\n{ex.Message}", "Error: Installing rayshud", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // Back on the 'UI' thread so we can update the progress bar
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // The progress percentage is a property of e
            progressBar1.Value = e.ProgressPercentage;
        }
        #endregion

        private void CheckLiveVersion()
        {
            try
            {
                var client = new WebClient();
                // Download the latest rayshud README
                var textFromURL = client.DownloadString(Properties.Settings.Default.GetLiveVersion);
                var textFromURLArray = textFromURL.Split('\n');
                // Retrieve the latest version number from the README
                txtLiveVersion.Text = textFromURLArray[textFromURLArray.Length - 2];
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"{Properties.Settings.Default.ErrorLiveVersion}\n{ex.Message}", "Error: Checking latest version", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CheckTF2Directory()
        {
            try
            {
                // Check default Steam installation directories for the tf/custom folder
                if (Directory.Exists($"C:\\Program Files (x86)\\{Properties.Settings.Default.TFDirectory}"))
                    TF2Directory = $"C:\\Program Files (x86)\\{Properties.Settings.Default.TFDirectory}";
                else if (Directory.Exists($"D:\\Program Files (x86)\\{Properties.Settings.Default.TFDirectory}"))
                    TF2Directory = $"D:\\Program Files (x86)\\{Properties.Settings.Default.TFDirectory}";
                else if (Directory.Exists($"C:\\Program Files\\{Properties.Settings.Default.TFDirectory}"))
                    TF2Directory = $"C:\\Program Files\\{Properties.Settings.Default.TFDirectory}";
                else if (Directory.Exists($"D:\\Program Files\\{Properties.Settings.Default.TFDirectory}"))
                    TF2Directory = $"D:\\Program Files\\{Properties.Settings.Default.TFDirectory}";
                else if (Directory.Exists("C:\\Users\\igor.nikitin\\Downloads\\tf\\custom"))
                    TF2Directory = "C:\\Users\\igor.nikitin\\Downloads\\tf\\custom";    // DEBUG
                else
                {
                    // If tf/custom is not found, ask the user to provide it
                    var validHUDDirectory = false;
                    var DirectoryBrowser = new FolderBrowserDialog();
                    DirectoryBrowser.Description = $"{Properties.Settings.Default.UserShowDirectory}\n{Properties.Settings.Default.TFDirectory}";
                    DirectoryBrowser.ShowNewFolderButton = true;
                    while (validHUDDirectory == false)
                    {
                        // Loop until the user clicks Cancel or provides a directory that contains tf/custom
                        if (DirectoryBrowser.ShowDialog() == DialogResult.OK)
                        {
                            if (!DirectoryBrowser.SelectedPath.Contains("tf\\custom")) continue;
                            TF2Directory = DirectoryBrowser.SelectedPath;
                            validHUDDirectory = true;
                        }
                        else
                            break;
                    }
                }

                if (string.IsNullOrWhiteSpace(TF2Directory))
                    txtDirectory.Text = Properties.Settings.Default.TFDirectoryNotSet;
                else
                {
                    txtDirectory.Text = TF2Directory;
                    btnInstall.Enabled = true;
                    btnPlayTF2.Enabled = true;
                    CheckHUDDirectory();
                }
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"{Properties.Settings.Default.TFDirectoryNotFound}\n{ex.Message}", "Error: Checking tf/custom directory", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CheckHUDDirectory()
        {
            try
            {
                if (Directory.Exists($"{TF2Directory}\\rayshud"))
                {
                    btnUninstall.Enabled = true;
                    btnSaveChanges.Enabled = true;
                    btnOpenDirectory.Enabled = true;
                    btnSetDefault.Enabled = true;
                    txtDirectory.Text = $"{TF2Directory}\\rayshud";
                    // Get the version number from the installed rayshud README
                    txtInstalledVersion.Text = File.ReadLines($"{TF2Directory}\\rayshud\\README.md").Last();
                    // Compare the live and installed version numbers to determine if rayshud is updated
                    if (txtInstalledVersion.ToString().Trim() == txtLiveVersion.ToString().Trim())
                    {
                        btnInstall.Text = "Refresh";
                        txtStatus.Text = "Installed, Updated";
                    }
                    else
                    {
                        btnInstall.Text = "Update";
                        txtStatus.Text = "Installed, Outdated";
                    }
                }
                else
                {
                    btnInstall.Text = "Install";
                    txtStatus.Text = "Not Installed";
                    btnUninstall.Enabled = false;
                    btnSaveChanges.Enabled = false;
                    btnOpenDirectory.Enabled = false;
                    btnSetDefault.Enabled = false;
                }
                ReadFromSettings();
                DisplayHUDSettings();
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"{Properties.Settings.Default.ErrorTFDirectory}\n{ex.Message}", "Error: Checking rayshud directory", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DisplayHUDSettings()
        {
            try
            {
                // Set installer controls to default rayshud values
                cbHUDVersion.SelectedIndex = 0;
                cbScoreboard.SelectedIndex = 0;
                rbChatBoxBottom.Checked = true;
                rbTeamSelectLeft.Checked = true;
                btnUberBarColor.BackColor = Color.FromArgb(235, 226, 202);
                btnUberFullColor.BackColor = Color.FromArgb(255, 50, 255);
                btnUberFlashColor1.BackColor = Color.FromArgb(255, 165, 0);
                btnUberFlashColor2.BackColor = Color.FromArgb(255, 69, 0);
                btnXHairColor.BackColor = Color.FromArgb(242, 242, 242);
                lblCrosshair.ForeColor = btnXHairColor.BackColor;
                btnXHairPulseColor.BackColor = Color.FromArgb(255, 0, 0);
                btnHealingDone.BackColor = Color.FromArgb(48, 255, 48);
                btnHealthNormal.BackColor = Color.FromArgb(235, 226, 202);
                btnHealthBuff.BackColor = Color.FromArgb(48, 255, 48);
                btnHealthLow.BackColor = Color.FromArgb(255, 153, 0);
                btnAmmoClip.BackColor = Color.FromArgb(48, 255, 48);
                btnAmmoReserve.BackColor = Color.FromArgb(72, 255, 255);
                btnAmmoClipLow.BackColor = Color.FromArgb(255, 42, 130);
                btnAmmoReserveLow.BackColor = Color.FromArgb(255, 128, 28);
                txtLastModified.Text = settings.LastModified;

                // Set installer controls to configuration values
                if (settings.HUDVersion)
                    cbHUDVersion.SelectedIndex = 1;

                if (settings.Scoreboard)
                    cbScoreboard.SelectedIndex = 1;

                if (settings.ChatBox)
                    rbChatBoxTop.Checked = true;

                if (settings.TeamSelect)
                    rbTeamSelectCenter.Checked = true;

                cbDisguiseImage.Checked = settings.DisguiseImage;

                cbDefaultMenuBG.Checked = settings.DefaultMenuBG;

                switch (settings.UberAnimation)
                {
                    case 1:
                        rbUberAnimation1.Checked = true;
                        break;

                    case 2:
                        rbUberAnimation2.Checked = true;
                        break;

                    case 3:
                        rbUberAnimation3.Checked = true;
                        break;
                }

                var split = settings.UberBarColor.Split(null);
                btnUberBarColor.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                split = settings.UberFullColor.Split(null);
                btnUberFullColor.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                split = settings.UberFlashColor1.Split(null);
                btnUberFlashColor1.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                split = settings.UberFlashColor2.Split(null);
                btnUberFlashColor2.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                lbXHairStyles.SelectedIndex = settings.XHairStyle - 1;

                cbXHairEnabled.Checked = settings.XHairEnabled;

                cbXHairOutline.Checked = settings.XHairOutline;

                cbXHairPulse.Checked = settings.XHairPulse;
                
                cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf(settings.XHairSize.ToString());

                split = settings.XHairColor.Split(null);
                btnXHairColor.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));
                lblCrosshair.ForeColor = btnXHairColor.BackColor;

                split = settings.XHairPulseColor.Split(null);
                btnXHairPulseColor.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                lbHealthStyle.SelectedIndex = settings.HealthStyle - 1;

                split = settings.HealingDone.Split(null);
                btnHealingDone.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                split = settings.HealthNormal.Split(null);
                btnHealthNormal.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                split = settings.HealthBuff.Split(null);
                btnHealthBuff.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                split = settings.HealthLow.Split(null);
                btnHealthLow.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                split = settings.AmmoClip.Split(null);
                btnAmmoClip.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                split = settings.AmmoReserve.Split(null);
                btnAmmoReserve.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                split = settings.AmmoClipLow.Split(null);
                btnAmmoClipLow.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                split = settings.AmmoReserveLow.Split(null);
                btnAmmoReserveLow.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"{Properties.Settings.Default.ErrorLoadConfiguration}\n{ex.Message}", "Error: Loading Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void UpdateSettingsFile()
        {
            // Update the installer-configuration file with the current settings
            WriteToSettings("HUDVersion", settings.HUDVersion.ToString());
            WriteToSettings("Scoreboard", settings.Scoreboard.ToString());
            WriteToSettings("ChatBox", settings.ChatBox.ToString());
            WriteToSettings("TeamSelect", settings.TeamSelect.ToString());
            WriteToSettings("DisguiseImage", settings.DisguiseImage.ToString());
            WriteToSettings("DefaultMenuBG", settings.DefaultMenuBG.ToString());
            WriteToSettings("UberAnimation", settings.UberAnimation.ToString());
            WriteToSettings("UberBarColor", settings.UberBarColor);
            WriteToSettings("UberFullColor", settings.UberFullColor);
            WriteToSettings("UberFlashColor1", settings.UberFlashColor1);
            WriteToSettings("UberFlashColor2", settings.UberFlashColor2);
            WriteToSettings("XHairEnabled", settings.XHairEnabled.ToString());
            WriteToSettings("XHairStyle", settings.XHairStyle.ToString());
            WriteToSettings("XHairOutline", settings.XHairOutline.ToString());
            WriteToSettings("XHairPulse", settings.XHairPulse.ToString());
            WriteToSettings("XHairSize", settings.XHairSize.ToString());
            WriteToSettings("XHairColor", settings.XHairColor);
            WriteToSettings("XHairPulseColor", settings.XHairPulseColor);
            WriteToSettings("HealingDone", settings.HealingDone);
            WriteToSettings("HealthStyle", settings.HealthStyle.ToString());
            WriteToSettings("HealthNormal", settings.HealthNormal);
            WriteToSettings("HealthBuff", settings.HealthBuff);
            WriteToSettings("HealthLow", settings.HealthLow);
            WriteToSettings("AmmoClip", settings.AmmoClip);
            WriteToSettings("AmmoReserve", settings.AmmoReserve);
            WriteToSettings("AmmoClipLow", settings.AmmoClipLow);
            WriteToSettings("AmmoReserveLow", settings.AmmoReserveLow);
            WriteToSettings("LastModified", DateTime.Now.ToString(CultureInfo.CurrentCulture));
            txtLastModified.Text = DateTime.Now.ToString(CultureInfo.CurrentCulture);
        }

        private void WriteToSettings(string setting, string value)
        {
            string json = File.ReadAllText($"{Application.StartupPath}\\settings.json");
            dynamic jsonObj = JsonConvert.DeserializeObject(json);
            jsonObj[setting] = value;
            string output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
            File.WriteAllText($"{Application.StartupPath}\\settings.json", output);
        }

        public void ReadFromSettings()
        {
            using (var reader = new StreamReader($"{Application.StartupPath}\\settings.json"))
            {
                string json = reader.ReadToEnd();
                settings = JsonConvert.DeserializeObject<RootObject>(json);
            }
        }

        private void btnInstall_Click(object sender, EventArgs e)
        {
            // Start the background worker
            //backgroundWorker1.RunWorkerAsync();
            try
            {
                var client = new WebClient();
                // Remove the temporary downloaded rayshud files
                if (File.Exists($"{Application.StartupPath}\\{Properties.Settings.Default.TempFileName}.zip"))
                    File.Delete($"{Application.StartupPath}\\{Properties.Settings.Default.TempFileName}.zip");
                // Restore the configuration file if it has been removed
                if (!File.Exists($"{Application.StartupPath}\\settings.json"))
                    client.DownloadFile($"https://raw.githubusercontent.com/CriticalFlaw/rayshud-Installer/master/rayshud-installer/settings.json", $"{Application.StartupPath}\\settings.json");
                else
                    UpdateSettingsFile();
                // Download the latest rayshud from GitHub and extract into the tf/custom directory
                client.DownloadFile($"https://github.com/raysfire/rayshud/archive/{Properties.Settings.Default.GitBranch}.zip", $"{Properties.Settings.Default.TempFileName}.zip");    //DEBUG
                ZipFile.ExtractToDirectory($"{Application.StartupPath}\\{Properties.Settings.Default.TempFileName}.zip", TF2Directory);
                // Either do a clean install or refresh/update of rayshud
                switch (btnInstall.Text)
                {
                    case "Install":
                        Directory.Move($"{TF2Directory}\\rayshud-{Properties.Settings.Default.GitBranch}", $"{TF2Directory}\\rayshud");
                        MessageBox.Show(Properties.Settings.Default.SuccessInstallMessage, "rayshud Installed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;

                    case "Update":
                    case "Refresh":
                        // Replace the installed rayshud with a fresh copy
                        Directory.Delete($"{TF2Directory}\\rayshud", true);
                        Directory.Move($"{TF2Directory}\\rayshud-{Properties.Settings.Default.GitBranch}", $"{TF2Directory}\\rayshud");
                        MessageBox.Show(Properties.Settings.Default.SuccessUpdateMessage, "rayshud Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                }
                // Remove the temporary downloaded rayshud files
                if (File.Exists($"{Application.StartupPath}\\{Properties.Settings.Default.TempFileName}.zip"))
                    File.Delete($"{Application.StartupPath}\\{Properties.Settings.Default.TempFileName}.zip");
                CheckHUDDirectory();
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"{Properties.Settings.Default.ErrorInstallMessage}\n{ex.Message}", "Error: Installing rayshud", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnUninstall_Click(object sender, EventArgs e)
        {
            try
            {
                if (Directory.Exists($"{TF2Directory}\\rayshud"))
                {
                    Directory.Delete($"{TF2Directory}\\rayshud", true);
                    MessageBox.Show(Properties.Settings.Default.SuccessUninstallMessage, "rayshud Uninstalled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtDirectory.Text = TF2Directory;
                    txtInstalledVersion.Text = "...";
                    txtLastModified.Text = "...";
                    CheckHUDDirectory();
                }
                else
                    btnUninstall.Enabled = false;
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"{Properties.Settings.Default.ErrorUninstallMessage}\n{ex.Message}", "Error: Uninstalling rayshud", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSaveChanges_Click(object sender, EventArgs e)
        {
            try
            {
                // Update the installer configuration file
                UpdateSettingsFile();
                // Set the directories to be used during the file reads/writes
                var console = $"{TF2Directory}\\rayshud\\materials\\console";
                var resource = $"{TF2Directory}\\rayshud\\resource\\ui";
                var scripts = $"{TF2Directory}\\rayshud\\scripts";
                var colorScheme = $"{TF2Directory}\\rayshud\\resource\\scheme\\clientscheme_colors.res";
                var animations = $"{TF2Directory}\\rayshud\\scripts\\hudanimations_custom.txt";
                var chat = $"{TF2Directory}\\rayshud\\resource\\ui\\basechat.res";
                var layout = $"{TF2Directory}\\rayshud\\scripts\\hudlayout.res";
                var classicMaterials = $"{TF2Directory}\\rayshud\\customizations\\Main Menu\\Classic\\materials\\console";
                var classicResources = $"{TF2Directory}\\rayshud\\customizations\\Main Menu\\Classic\\resource\\";
                var modernMaterials = $"{TF2Directory}\\rayshud\\customizations\\Main Menu\\Modern\\materials\\console";
                var modernResources = $"{TF2Directory}\\rayshud\\customizations\\Main Menu\\Modern\\resource\\";
                var scoreboard = $"{TF2Directory}\\rayshud\\customizations\\Scoreboard";
                var teammenu = $"{TF2Directory}\\rayshud\\customizations\\Team Menu";
                var playerhealth = $"{TF2Directory}\\rayshud\\customizations\\Player Health";

                // 1. Main Menu Style - either classic or modern, copy and replace existing files
                if (settings.HUDVersion)
                {
                    if (Directory.Exists($"{console}_off"))
                    {
                        File.Copy($"{classicMaterials}\\background_upward.vtf", $"{console}_off\\background_upward.vtf", true);
                        File.Copy($"{classicMaterials}\\background_upward_widescreen.vtf", $"{console}_off\\background_upward_widescreen.vtf", true);
                    }
                    else
                    {
                        File.Copy($"{classicMaterials}\\background_upward.vtf", $"{console}\\background_upward.vtf", true);
                        File.Copy($"{classicMaterials}\\background_upward_widescreen.vtf", $"{console}\\background_upward_widescreen.vtf", true);
                    }

                    File.Copy($"{classicResources}\\ui\\mainmenuoverride.res", $"{resource}\\mainmenuoverride.res", true);
                    File.Copy($"{classicResources}\\gamemenu.res", $"{TF2Directory}\\rayshud\\resource\\gamemenu.res", true);
                }
                else
                {
                    if (Directory.Exists($"{console}_off"))
                    {
                        File.Copy($"{modernMaterials}\\background_upward.vtf", $"{console}_off\\background_upward.vtf", true);
                        File.Copy($"{modernMaterials}\\background_upward_widescreen.vtf", $"{console}_off\\background_upward_widescreen.vtf", true);
                    }
                    else
                    {
                        File.Copy($"{modernMaterials}\\background_upward.vtf", $"{console}\\background_upward.vtf", true);
                        File.Copy($"{modernMaterials}\\background_upward_widescreen.vtf", $"{console}\\background_upward_widescreen.vtf", true);
                    }

                    File.Copy($"{modernResources}\\ui\\mainmenuoverride.res", $"{resource}\\mainmenuoverride.res", true);
                    File.Copy($"{modernResources}\\gamemenu.res", $"{TF2Directory}\\rayshud\\resource\\gamemenu.res", true);
                }

                // 2. Scoreboard Style - either normal or minimal (6v6), copy and replace existing files
                if (settings.Scoreboard)
                    File.Copy($"{scoreboard}\\{Properties.Settings.Default.FileScoreboard}-minimal.res", $"{resource}\\{Properties.Settings.Default.FileScoreboard}.res", true);
                else
                    File.Copy($"{scoreboard}\\{Properties.Settings.Default.FileScoreboard}-default.res", $"{resource}\\{Properties.Settings.Default.FileScoreboard}.res", true);

                // 3. Default Background - enable or disable the custom backgrounds files by renaming them
                if (settings.DefaultMenuBG)
                {
                    if (Directory.Exists(console))
                    {
                        Directory.Move(console, $"{console}_off");
                        File.Move($"{scripts}\\{Properties.Settings.Default.FileChapterBackgrounds}.txt", $"{scripts}\\{Properties.Settings.Default.FileChapterBackgrounds}_off.txt");
                    }
                }
                else
                {
                    if (Directory.Exists($"{console}_off") && File.Exists($"{scripts}\\{Properties.Settings.Default.FileChapterBackgrounds}_off.txt"))
                    {
                        Directory.Move($"{console}_off", console);
                        File.Move($"{scripts}\\{Properties.Settings.Default.FileChapterBackgrounds}_off.txt", $"{scripts}\\{Properties.Settings.Default.FileChapterBackgrounds}.txt");
                    }
                }

                // 4. Class/Team Select Style - either left or center, copy and replace existing files
                if (settings.TeamSelect)
                {
                    File.Copy($"{teammenu}\\{Properties.Settings.Default.FileTeamMenu}-center.res", $"{resource}\\{Properties.Settings.Default.FileTeamMenu}.res", true);
                    File.Copy($"{teammenu}\\{Properties.Settings.Default.FileClassSelection}-center.res", $"{resource}\\{Properties.Settings.Default.FileClassSelection}.res", true);
                }
                else
                {
                    File.Copy($"{teammenu}\\{Properties.Settings.Default.FileTeamMenu}-left.res", $"{resource}\\{Properties.Settings.Default.FileTeamMenu}.res", true);
                    File.Copy($"{teammenu}\\{Properties.Settings.Default.FileClassSelection}-left.res", $"{resource}\\{Properties.Settings.Default.FileClassSelection}.res", true);
                }

                // 5. Player Health Style - either default, cross, teambar or broesel, copy and replace existing files
                switch (settings.HealthStyle)
                {
                    case 1:
                        File.Copy($"{playerhealth}\\{Properties.Settings.Default.FilePlayerHealth}-default.res", $"{resource}\\{Properties.Settings.Default.FilePlayerHealth}.res",
                            true);
                        break;

                    case 2:
                        File.Copy($"{playerhealth}\\{Properties.Settings.Default.FilePlayerHealth}-teambar.res", $"{resource}\\{Properties.Settings.Default.FilePlayerHealth}.res",
                            true);
                        break;

                    case 3:
                        File.Copy($"{playerhealth}\\{Properties.Settings.Default.FilePlayerHealth}-cross.res", $"{resource}\\{Properties.Settings.Default.FilePlayerHealth}.res",
                            true);
                        break;

                    case 4:
                        File.Copy($"{playerhealth}\\{Properties.Settings.Default.FilePlayerHealth}-broesel.res", $"{resource}\\{Properties.Settings.Default.FilePlayerHealth}.res",
                            true);
                        break;
                }

                // Spy Disguise Image and Uber Animation - uncomment all
                var lines = File.ReadAllLines(animations);
                lines[87 - 1] = lines[87 - 1].Replace("//", string.Empty);
                lines[88 - 1] = lines[88 - 1].Replace("//", string.Empty);
                lines[89 - 1] = lines[89 - 1].Replace("//", string.Empty);
                lines[94 - 1] = lines[94 - 1].Replace("//", string.Empty);
                lines[95 - 1] = lines[95 - 1].Replace("//", string.Empty);
                lines[96 - 1] = lines[96 - 1].Replace("//", string.Empty);
                lines[104 - 1] = lines[104 - 1].Replace("//", string.Empty);
                lines[105 - 1] = lines[105 - 1].Replace("//", string.Empty);
                lines[106 - 1] = lines[106 - 1].Replace("//", string.Empty);
                File.WriteAllLines(animations, lines);

                // 6. Spy Disguise Image - enable or disable by commenting out the lines
                if (settings.DisguiseImage)
                {
                    lines[87 - 1] = $"\t{lines[87 - 1].Replace("//", string.Empty).Trim()}";
                    lines[88 - 1] = $"\t{lines[88 - 1].Replace("//", string.Empty).Trim()}";
                    lines[89 - 1] = $"\t{lines[89 - 1].Replace("//", string.Empty).Trim()}";
                    lines[94 - 1] = $"\t{lines[94 - 1].Replace("//", string.Empty).Trim()}";
                    lines[95 - 1] = $"\t{lines[95 - 1].Replace("//", string.Empty).Trim()}";
                    lines[96 - 1] = $"\t{lines[96 - 1].Replace("//", string.Empty).Trim()}";
                }
                else
                {
                    lines[87 - 1] = $"\t//{lines[87 - 1].Trim()}";
                    lines[88 - 1] = $"\t//{lines[88 - 1].Trim()}";
                    lines[89 - 1] = $"\t//{lines[89 - 1].Trim()}";
                    lines[94 - 1] = $"\t//{lines[94 - 1].Trim()}";
                    lines[95 - 1] = $"\t//{lines[95 - 1].Trim()}";
                    lines[96 - 1] = $"\t//{lines[96 - 1].Trim()}";
                }

                // 7. Uber Animation - enable or disable by commenting out the lines
                switch (settings.UberAnimation)
                {
                    case 1:
                        lines[104 - 1] = $"\t{lines[104 - 1].Replace("//", string.Empty).Trim()}";
                        lines[105 - 1] = $"\t//{lines[105 - 1].Trim()}";
                        lines[106 - 1] = $"\t//{lines[106 - 1].Trim()}";
                        break;

                    case 2:
                        lines[104 - 1] = $"\t//{lines[104 - 1].Trim()}";
                        lines[105 - 1] = $"\t{lines[105 - 1].Replace("//", string.Empty).Trim()}";
                        lines[106 - 1] = $"\t//{lines[106 - 1].Trim()}";
                        break;

                    case 3:
                        lines[104 - 1] = $"\t//{lines[104 - 1].Trim()}";
                        lines[105 - 1] = $"\t//{lines[105 - 1].Trim()}";
                        lines[106 - 1] = $"\t{lines[106 - 1].Replace("//", string.Empty).Trim()}";
                        break;
                }

                // 8. Crosshair Pulse - enable or disable by commenting out the lines
                if (settings.XHairPulse)
                {
                    lines[80 - 1] = lines[80 - 1].Replace("//", string.Empty);
                    lines[81 - 1] = lines[81 - 1].Replace("//", string.Empty);
                }
                else
                {
                    lines[80 - 1] = $"//{lines[80 - 1]}";
                    lines[81 - 1] = $"//{lines[81 - 1]}";
                }

                File.WriteAllLines(animations, lines);

                // 9. Chat box position - either top or bottom, change the ypos value of basechat.res
                lines = File.ReadAllLines(chat);
                if (settings.ChatBox)
                    lines[10 - 1] = "\t\t\"ypos\"\t\t\t\t\"30\"";
                else
                    lines[10 - 1] = "\t\t\"ypos\"\t\t\t\t\"360\"";
                File.WriteAllLines(chat, lines);

                // Crosshairs - disable all and remove outlining
                lines = File.ReadAllLines(layout);
                for (int x = 13; x <= 51; x += 19)
                {
                    lines[x - 1] = "\t\t\"visible\"\t\t\"0\"";
                    lines[x + 1 - 1] = "\t\t\"enabled\"\t\t\"0\"";
                    lines[x + 7 - 1] = lines[x + 7 - 1].Replace("Outline", string.Empty);
                    File.WriteAllLines(layout, lines);
                }

                // 10. Crosshairs - either enabled or disabled with or without outlines, change the visible, enabled and font values of hudlayout.res
                if (settings.XHairEnabled)
                {
                    if (settings.XHairStyle >= 1 && settings.XHairStyle <= 15)
                    {
                        lines[13 - 1] = "\t\t\"visible\"\t\t\"1\"";
                        lines[14 - 1] = "\t\t\"enabled\"\t\t\"1\"";
                        if (settings.XHairOutline)
                            lines[20 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{cbXHairSizes.Text}Outline\"";
                        else
                            lines[20 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{cbXHairSizes.Text}\"";
                    }
                    else if(settings.XHairStyle == 16)
                    {
                        lines[32 - 1] = "\t\t\"visible\"\t\t\"1\"";
                        lines[33 - 1] = "\t\t\"enabled\"\t\t\"1\"";
                        if (settings.XHairOutline)
                            lines[39 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{cbXHairSizes.Text}Outline\"";
                        else
                            lines[39 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{cbXHairSizes.Text}\"";
                    }
                    else if (settings.XHairStyle >= 17 && settings.XHairStyle <= 84)
                    {
                        lines[51 - 1] = "\t\t\"visible\"\t\t\"1\"";
                        lines[52 - 1] = "\t\t\"enabled\"\t\t\"1\"";
                        if (settings.XHairOutline)
                            lines[58 - 1] = $"\t\t\"font\"\t\t\t\"size:{cbXHairSizes.Text},outline:on\"";
                        else
                            lines[58 - 1] = $"\t\t\"font\"\t\t\t\"size:{cbXHairSizes.Text},outline:off\"";
                    }

                    switch (settings.XHairStyle)
                    {
                        case 1: // BasicCross
                        case 2: // BasicCrossLarge
                        case 3: // BasicCrossSmall
                            lines[21 - 1] = "\t\t\"labelText\"\t\t\"2\"";
                            break;

                        case 4: // BasicDot
                            lines[21 - 1] = "\t\t\"labelText\"\t\t\"3\"";
                            break;

                        case 5: // CircleDot
                            lines[21 - 1] = "\t\t\"labelText\"\t\t\"8\"";
                            break;

                        case 6: // OpenCross
                            lines[21 - 1] = "\t\t\"labelText\"\t\t\"i\"";
                            break;

                        case 7: // OpenCrossDot
                            lines[21 - 1] = "\t\t\"labelText\"\t\t\"h\"";
                            break;

                        case 8: // ScatterSpread
                            lines[21 - 1] = "\t\t\"labelText\"\t\t\"0\"";
                            break;

                        case 9: // ThinCircle
                            lines[21 - 1] = "\t\t\"labelText\"\t\t\"9\"";
                            break;

                        case 10: // ThinCross
                            lines[21 - 1] = "\t\t\"labelText\"\t\t\"+\"";
                            break;

                        case 11: // Wings
                            lines[21 - 1] = "\t\t\"labelText\"\t\t\"d\"";
                            break;

                        case 12: // WingsPlus
                            lines[21 - 1] = "\t\t\"labelText\"\t\t\"c\"";
                            break;

                        case 13: // WingsSmall
                            lines[21 - 1] = "\t\t\"labelText\"\t\t\"g\"";
                            break;

                        case 14: // WingsSmallDot
                            lines[21 - 1] = "\t\t\"labelText\"\t\t\"f\"";
                            break;

                        case 15: // xHairCircle
                            lines[21 - 1] = "\t\t\"labelText\"\t\t\"o\"";
                            break;

                        case 16: // KonrWings
                            lines[289 - 1] = "\t\t\"visible\"\t\t\"1\"";
                            lines[290 - 1] = "\t\t\"enabled\"\t\t\"1\"";
                            if (settings.XHairOutline)
                                lines[296 - 1] = $"\t\t\"font\"\t\t\t\"KonrWings{cbXHairSizes.Text}Outline\"";
                            else
                                lines[296 - 1] = $"\t\t\"font\"\t\t\t\"KonrWings{cbXHairSizes.Text}\"";
                            break;
                    }

                    File.WriteAllLines(layout, lines);
                }

                // 11. Color Values - replace the color RGB values in clientscheme_colors.res file
                lines = File.ReadAllLines(colorScheme);
                lines[7 - 1] = $"\t\t\"Ammo In Clip\"\t\t\t\t\t\"{settings.AmmoClip}\"";
                lines[8 - 1] = $"\t\t\"Ammo In Reserve\"\t\t\t\t\"{settings.AmmoReserve}\"";
                lines[9 - 1] = $"\t\t\"Ammo In Clip Low\"\t\t\t\t\"{settings.AmmoClipLow}\"";
                lines[10 - 1] = $"\t\t\"Ammo In Reserve Low\"\t\t\t\"{settings.AmmoReserveLow}\"";
                lines[23 - 1] = $"\t\t\"Health Normal\"\t\t\t\t\t\"{settings.HealthNormal}\"";
                lines[24 - 1] = $"\t\t\"Health Buff\"\t\t\t\t\t\"{settings.HealthBuff}\"";
                lines[25 - 1] = $"\t\t\"Health Hurt\"\t\t\t\t\t\"{settings.HealthLow}\"";
                lines[32 - 1] = $"\t\t\"Uber Bar Color\"\t\t\t\t\"{settings.UberBarColor}\"";
                lines[35 - 1] = $"\t\t\"Solid Color Uber\"\t\t\t\t\"{settings.UberFullColor}\"";
                lines[37 - 1] = $"\t\t\"Flashing Uber Color1\"\t\t\t\"{settings.UberFlashColor1}\"";
                lines[38 - 1] = $"\t\t\"Flashing Uber Color2\"\t\t\t\"{settings.UberFlashColor2}\"";
                lines[41 - 1] = $"\t\t\"Heal Numbers\"\t\t\t\t\t\"{settings.HealingDone}\"";
                lines[45 - 1] = $"\t\t\"Crosshair\"\t\t\t\t\t\t\"{settings.XHairColor}\"";
                lines[46 - 1] = $"\t\t\"CrosshairDamage\"\t\t\t\t\"{settings.XHairPulseColor}\"";
                File.WriteAllLines(colorScheme, lines);

                MessageBox.Show(Properties.Settings.Default.SuccessApplyChanges, "Changes Saved!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"{Properties.Settings.Default.ErrorApplyChanges}\n{ex.Message}", "Error: Updating rayshud", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnPlayTF2_Click(object sender, EventArgs e)
        {
            // Start Team Fortress 2 through Steam
            Process.Start("steam://rungameid/440");
            Application.Exit();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            var IsDirectoryValid = false;
            var DirectoryBrowser = new FolderBrowserDialog();
            DirectoryBrowser.Description = $"{Properties.Settings.Default.UserShowDirectory}\n{Properties.Settings.Default.TFDirectory}";
            DirectoryBrowser.ShowNewFolderButton = true;
            while (IsDirectoryValid == false)
            {
                // Until the correct path is provided or the user clicks 'Cancel' - keep prompting for a valid tf/custom directory.
                if (DirectoryBrowser.ShowDialog() == DialogResult.OK)
                {
                    if (!DirectoryBrowser.SelectedPath.Contains("custom")) continue;   //Properties.Settings.Default.TFDirectoryValidation  // DEBUG
                    TF2Directory = DirectoryBrowser.SelectedPath;
                    txtDirectory.Text = TF2Directory;
                    btnInstall.Enabled = true;
                    btnPlayTF2.Enabled = true;
                    CheckHUDDirectory();
                    IsDirectoryValid = true;
                }
                else
                    break;
            }
        }

        private void btnColorPicker_Click(object sender, EventArgs e)
        {
            // Create the color picker dialog object
            var colorPicker = new ColorDialog();
            if (colorPicker.ShowDialog() != DialogResult.OK) return;
            switch (((Button)sender).Name)
            {
                case "btnUberBarColor":
                    // Update the button color based on selection
                    btnUberBarColor.BackColor = colorPicker.Color;
                    // Update the configuration color-value
                    settings.UberBarColor = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnUberFullColor":
                    btnUberFullColor.BackColor = colorPicker.Color;
                    settings.UberFullColor = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnUberFlashColor1":
                    btnUberFlashColor1.BackColor = colorPicker.Color;
                    settings.UberFlashColor1 = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnUberFlashColor2":
                    btnUberFlashColor2.BackColor = colorPicker.Color;
                    settings.UberFlashColor2 = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnXHairColor":
                    btnXHairColor.BackColor = colorPicker.Color;
                    settings.XHairColor = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    // Update the crosshair preview color
                    lblCrosshair.ForeColor = btnXHairColor.BackColor;
                    break;

                case "btnXHairPulseColor":
                    btnXHairPulseColor.BackColor = colorPicker.Color;
                    settings.XHairPulseColor = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnHealingDone":
                    btnHealingDone.BackColor = colorPicker.Color;
                    settings.HealingDone = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnHealthNormal":
                    btnHealthNormal.BackColor = colorPicker.Color;
                    settings.HealthNormal = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnHealthBuff":
                    btnHealthBuff.BackColor = colorPicker.Color;
                    settings.HealthBuff = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnHealthLow":
                    btnHealthLow.BackColor = colorPicker.Color;
                    settings.HealthLow = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnAmmoClip":
                    btnAmmoClip.BackColor = colorPicker.Color;
                    settings.AmmoClip = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnAmmoReserve":
                    btnAmmoReserve.BackColor = colorPicker.Color;
                    settings.AmmoReserve = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnAmmoClipLow":
                    btnAmmoClipLow.BackColor = colorPicker.Color;
                    settings.AmmoClipLow = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnAmmoReserveLow":
                    btnAmmoReserveLow.BackColor = colorPicker.Color;
                    settings.AmmoReserveLow = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;
            }
        }

        private void cbHUDVersion_SelectedIndexChanged(object sender, EventArgs e)
        {
            settings.HUDVersion = cbHUDVersion.SelectedIndex > 0;
        }

        private void cbScoreboard_SelectedIndexChanged(object sender, EventArgs e)
        {
            settings.Scoreboard = cbScoreboard.SelectedIndex > 0;
        }

        private void cbDisguiseImage_CheckedChanged(object sender, EventArgs e)
        {
            settings.DisguiseImage = cbDisguiseImage.Checked;
        }

        private void cbDefaultMenuBG_CheckedChanged(object sender, EventArgs e)
        {
            settings.DefaultMenuBG = cbDefaultMenuBG.Checked;
        }

        private void rbChatBox_CheckedChanged(object sender, EventArgs e)
        {
            settings.ChatBox = rbChatBoxTop.Checked;
        }

        private void rbTeamSelect_CheckedChanged(object sender, EventArgs e)
        {
            settings.TeamSelect = rbTeamSelectCenter.Checked;
        }

        private void rbUberAnimation_CheckedChanged(object sender, EventArgs e)
        {
            if (rbUberAnimation1.Checked)
                settings.UberAnimation = 1;
            else if (rbUberAnimation2.Checked)
                settings.UberAnimation = 2;
            else if (rbUberAnimation3.Checked)
                settings.UberAnimation = 3;
        }

        private void cbXHairEnabled_CheckedChanged(object sender, EventArgs e)
        {
            settings.XHairEnabled = cbXHairEnabled.Checked;
        }

        private void cbXHairOutline_CheckedChanged(object sender, EventArgs e)
        {
            settings.XHairOutline = cbXHairOutline.Checked;
        }

        private void cbXHairPulse_CheckedChanged(object sender, EventArgs e)
        {
            settings.XHairPulse = cbXHairPulse.Checked;
        }

        private void cbXHairSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            settings.XHairSize = Convert.ToInt32(cbXHairSizes.Items[cbXHairSizes.SelectedIndex].ToString());
        }

        private void lbPlayerHealth_SelectedIndexChanged(object sender, EventArgs e)
        {
            settings.HealthStyle = lbHealthStyle.SelectedIndex + 1;
        }

        private void lbXHairStyles_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Save the crosshair style to the installer-config file
            settings.XHairStyle = lbXHairStyles.SelectedIndex + 1;
            // Reinitialize the crosshair size options
            cbXHairSizes.Items.Clear();
            switch (lbXHairStyles.SelectedItem.ToString())
            {
                case "KonrWings":
                    for (int x = 16; x <= 40; x += 8)
                        cbXHairSizes.Items.Add(x.ToString());
                    break;
                case "KnuckleCrosses":
                    for (int x = 10; x <= 50; x += 1)
                        cbXHairSizes.Items.Add(x.ToString());
                    break;
                default:
                    for (int x = 8; x <= 40; x += 2)
                        cbXHairSizes.Items.Add(x.ToString());
                    break;
            }

            // Update the crosshair settings on the UI
            switch (settings.XHairStyle)
            {
                case 1:
                    lblCrosshair.Text = @"2";
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("26");
                    break;

                case 2:
                    lblCrosshair.Text = @"2";
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("32");
                    break;

                case 3:
                    lblCrosshair.Text = @"2";
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("18");
                    break;

                case 4:
                    lblCrosshair.Text = @"3";
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("24");
                    break;

                case 5:
                    lblCrosshair.Text = @"8";
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("34");
                    break;

                case 6:
                    lblCrosshair.Text = @"i";
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("24");
                    break;

                case 7:
                    lblCrosshair.Text = @"h";
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("24");
                    break;

                case 9:
                    lblCrosshair.Text = @"0";
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("32");
                    break;

                case 10:
                    lblCrosshair.Text = @"9";
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("34");
                    break;

                case 11:
                    lblCrosshair.Text = @"+";
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("24");
                    break;

                case 12:
                    lblCrosshair.Text = @"d";
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("34");
                    break;

                case 13:
                    lblCrosshair.Text = @"c";
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("34");
                    break;

                case 14:
                    lblCrosshair.Text = @"g";
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("34");
                    break;

                case 15:
                    lblCrosshair.Text = @"o";
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("34");
                    break;

                default:
                    lblCrosshair.Text = string.Empty;
                    cbXHairSizes.SelectedIndex = 0;
                    break;
            }
        }

        private void btnOpenDirectory_Click(object sender, EventArgs e)
        {
            if (Directory.Exists($"{TF2Directory}\\rayshud"))
                Process.Start("explorer.exe", $"{TF2Directory}\\rayshud");
        }

        private void btnSetDefault_Click(object sender, EventArgs e)
        {
            settings.HUDVersion = false;
            settings.Scoreboard = false;
            settings.ChatBox = false;
            settings.TeamSelect = false;
            settings.DisguiseImage = false;
            settings.DefaultMenuBG = false;
            settings.UberAnimation = 1;
            settings.UberBarColor = "235 226 202 255";
            settings.UberFullColor = "255 50 255 255";
            settings.UberFlashColor1 = "255 165 0 255";
            settings.UberFlashColor2 = "255 69 0 255";
            settings.XHairEnabled = false;
            settings.XHairStyle = 1;
            settings.XHairOutline = false;
            settings.XHairPulse = true;
            settings.XHairSize = 20;
            settings.XHairColor = "242 242 242 255";
            settings.XHairPulseColor = "255 0 0 255";
            settings.HealingDone = "48 255 48 255";
            settings.HealthStyle = 1;
            settings.HealthNormal = "235 226 202 255";
            settings.HealthBuff = "48 255 48 255";
            settings.HealthLow = "255 153 0 255";
            settings.AmmoClip = "48 255 48 255";
            settings.AmmoReserve = "72 255 255 255";
            settings.AmmoClipLow = "255 42 130 255";
            settings.AmmoReserveLow = "255 128 28 255";
            settings.LastModified = DateTime.Now.ToString(CultureInfo.CurrentCulture);
            UpdateSettingsFile();
            DisplayHUDSettings();
        }

        private void btnAndKnuckles_Click(object sender, EventArgs e)
        {
            var directory = $"{Application.StartupPath}\\KnuckleCrosses.jpg";
            if (File.Exists(directory))
                File.Delete(directory);
            var bitmap = new Bitmap(Properties.Resources.KnucklesCrosses1);
            bitmap.Save(directory);
            Process.Start(directory);
        }
    }

    public class RootObject
    {
        public bool HUDVersion { get; set; }
        public bool Scoreboard { get; set; }
        public bool ChatBox { get; set; }
        public bool TeamSelect { get; set; }
        public bool DisguiseImage { get; set; }
        public bool DefaultMenuBG { get; set; }
        public int UberAnimation { get; set; }
        public string UberBarColor { get; set; }
        public string UberFullColor { get; set; }
        public string UberFlashColor1 { get; set; }
        public string UberFlashColor2 { get; set; }
        public bool XHairEnabled { get; set; }
        public int XHairStyle { get; set; }
        public bool XHairOutline { get; set; }
        public bool XHairPulse { get; set; }
        public int XHairSize { get; set; }
        public string XHairColor { get; set; }
        public string XHairPulseColor { get; set; }
        public string HealingDone { get; set; }
        public int HealthStyle { get; set; }
        public string HealthNormal { get; set; }
        public string HealthBuff { get; set; }
        public string HealthLow { get; set; }
        public string AmmoClip { get; set; }
        public string AmmoClipLow { get; set; }
        public string AmmoReserve { get; set; }
        public string AmmoReserveLow { get; set; }
        public string LastModified { get; set; }
    }
}