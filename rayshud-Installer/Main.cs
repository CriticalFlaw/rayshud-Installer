using Newtonsoft.Json;
using SharpRaven;
using SharpRaven.Data;
using System;
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

        // Used to set the tf/custom directory that'll be used throughout
        private string TF2Directory;

        // Used for rendering the crosshair preview using a custom font
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, [System.Runtime.InteropServices.In] ref uint pcFonts);

        private readonly PrivateFontCollection fonts = new PrivateFontCollection();
        private Font myFont;

        public Main()
        {
            InitializeComponent();
            InitializeCustomFonts();
            CheckLiveVersion();
            CheckTF2Directory();
        }

        private void InitializeCustomFonts()
        {
            uint dummy = 0;
            var fontData = Properties.Resources.Crosshairs;
            //var fontData = Properties.Resources.Cerbetica;
            var fontPtr = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(fontData.Length);
            System.Runtime.InteropServices.Marshal.Copy(fontData, 0, fontPtr, fontData.Length);
            fonts.AddMemoryFont(fontPtr, fontData.Length);
            AddFontMemResourceEx(fontPtr, (uint)fontData.Length, IntPtr.Zero, ref dummy);
            System.Runtime.InteropServices.Marshal.FreeCoTaskMem(fontPtr);
            myFont = new Font(fonts.Families[0], 16.0F);
            lblCrosshair.Parent = pbPreview;
            lblCrosshair.BackColor = Color.Transparent;
            lblCrosshair.Font = new Font(myFont.FontFamily, 52, FontStyle.Regular);
            //this.Font = new Font(myFont.FontFamily, 8, FontStyle.Regular);
        }

        private void CheckLiveVersion()
        {
            try
            {
                var client = new WebClient();
                // Download the latest rayshud README
                var textFromURL = client.DownloadString(Properties.Settings.Default.GitVersion);
                var textFromURLArray = textFromURL.Split('\n');
                // Retrieve the latest version number from the README
                txtLiveVersion.Text = textFromURLArray[textFromURLArray.Length - 2];
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"{Properties.Settings.Default.ErrorVersionLive}\n{ex.Message}", "Error: Checking latest version", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CheckTF2Directory()
        {
            try
            {
                // Check default Steam installation directories for the tf/custom folder
                if (Directory.Exists(settings.TF2Directory) && settings.TF2Directory != "null")
                    TF2Directory = settings.TF2Directory;
                if (Directory.Exists($"C:\\Program Files (x86)\\{Properties.Settings.Default.TFDirectory}"))
                    TF2Directory = $"C:\\Program Files (x86)\\{Properties.Settings.Default.TFDirectory}";
                else if (Directory.Exists($"D:\\Program Files (x86)\\{Properties.Settings.Default.TFDirectory}"))
                    TF2Directory = $"D:\\Program Files (x86)\\{Properties.Settings.Default.TFDirectory}";
                else if (Directory.Exists($"C:\\Program Files\\{Properties.Settings.Default.TFDirectory}"))
                    TF2Directory = $"C:\\Program Files\\{Properties.Settings.Default.TFDirectory}";
                else if (Directory.Exists($"D:\\Program Files\\{Properties.Settings.Default.TFDirectory}"))
                    TF2Directory = $"D:\\Program Files\\{Properties.Settings.Default.TFDirectory}";
                else
                {
                    // If tf/custom is not found, ask the user to provide it
                    var validHUDDirectory = false;
                    var DirectoryBrowser = new FolderBrowserDialog();
                    DirectoryBrowser.Description = $"Please select your tf\\custom folder. Example:\n{Properties.Settings.Default.TFDirectory}";
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
                    settings.TF2Directory = txtDirectory.Text;
                    WriteToSettings("TF2Directory", settings.TF2Directory.Replace("\\", "/"));
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
                MessageBox.Show($"{Properties.Settings.Default.ErrorVersionLocal}\n{ex.Message}", "Error: Checking rayshud directory", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                // Main Menu Style - Modern (0) or Classic (1)
                if (settings.HUDVersion)
                    cbHUDVersion.SelectedIndex = 1;

                // Scoreboard Style - Normal (0) or Minimal (1)
                if (settings.Scoreboard)
                    cbScoreboard.SelectedIndex = 1;

                // Chatbox Position - Bottom-Left (false) or Top-Left (true)
                if (settings.ChatBox)
                    rbChatBoxTop.Checked = true;

                // Team/Class Select Position - Left (false) or Center (true)
                if (settings.TeamSelect)
                    rbTeamSelectCenter.Checked = true;

                // Spy Disguise Image - off (false) or on (true)
                cbDisguiseImage.Checked = settings.DisguiseImage;

                // Default Background Image - off (false) or on (true)
                cbDefaultMenuBG.Checked = settings.DefaultMenuBG;

                // Ubercharge Animation - Flash (1), Solid (2) or Rainbow (3)
                switch (settings.UberAnimation)
                {
                    default:
                        rbUberAnimation1.Checked = true;
                        break;

                    case 2:
                        rbUberAnimation2.Checked = true;
                        break;

                    case 3:
                        rbUberAnimation3.Checked = true;
                        break;
                }

                // Ubercharge Bar Color (RGB)
                var split = settings.UberBarColor.Split(null);
                btnUberBarColor.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                // Ubercharge Solid Color (RGB)
                split = settings.UberFullColor.Split(null);
                btnUberFullColor.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                // Ubercharge Flash Colors (RGB)
                split = settings.UberFlashColor1.Split(null);
                btnUberFlashColor1.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));
                split = settings.UberFlashColor2.Split(null);
                btnUberFlashColor2.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                // Crosshair Style
                lbXHairStyles.SelectedIndex = settings.XHairStyle - 1;

                // Crosshair Enable - off (false) or on (true)
                cbXHairEnabled.Checked = settings.XHairEnabled;

                // Crosshair Outline - off (false) or on (true)
                cbXHairOutline.Checked = settings.XHairOutline;

                // Crosshair Pulse - off (false) or on (true)
                cbXHairPulse.Checked = settings.XHairPulse;

                // Crosshair Sizes - based on crosshair style
                cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf(settings.XHairSize.ToString());

                // Crosshair Stock Color (RGB)
                split = settings.XHairColor.Split(null);
                btnXHairColor.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));
                lblCrosshair.ForeColor = btnXHairColor.BackColor;

                // Crosshair Pulse Color (RGB)
                split = settings.XHairPulseColor.Split(null);
                btnXHairPulseColor.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                // Player Health Style - Default (1), TeamBar (2), Cross (3) or Broesel (4)
                lbHealthStyle.SelectedIndex = settings.HealthStyle - 1;

                // Healing Done Color (RGB)
                split = settings.HealingDone.Split(null);
                btnHealingDone.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                // Health Stock Color (RGB)
                split = settings.HealthNormal.Split(null);
                btnHealthNormal.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                // Health Buff Color (RGB)
                split = settings.HealthBuff.Split(null);
                btnHealthBuff.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                // Health Low Color (RGB)
                split = settings.HealthLow.Split(null);
                btnHealthLow.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                // Ammo Clip Stock Color (RGB)
                split = settings.AmmoClip.Split(null);
                btnAmmoClip.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                // Ammo Reserve Stock Color (RGB)
                split = settings.AmmoReserve.Split(null);
                btnAmmoReserve.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                // Ammo Clip Low Color (RGB)
                split = settings.AmmoClipLow.Split(null);
                btnAmmoClipLow.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                // Ammo Reserve Low Color (RGB)
                split = settings.AmmoReserveLow.Split(null);
                btnAmmoReserveLow.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"{Properties.Settings.Default.ErrorLoad}\n{ex.Message}", "Error: Loading Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateSettingsFile()
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
            WriteToSettings("TF2Directory", settings.TF2Directory);
            WriteToSettings("LastModified", DateTime.Now.ToString(CultureInfo.CurrentCulture));
            txtLastModified.Text = DateTime.Now.ToString(CultureInfo.CurrentCulture);
        }

        private static void WriteToSettings(string setting, string value)
        {
            var json = File.ReadAllText($"{Application.StartupPath}\\{Properties.Settings.Default.SettingsName}");
            dynamic jsonObj = JsonConvert.DeserializeObject(json);
            jsonObj[setting] = value;
            var output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
            File.WriteAllText($"{Application.StartupPath}\\{Properties.Settings.Default.SettingsName}", output);
        }

        private void ReadFromSettings()
        {
            using (var reader = new StreamReader($"{Application.StartupPath}\\{Properties.Settings.Default.SettingsName}"))
            {
                var json = reader.ReadToEnd();
                settings = JsonConvert.DeserializeObject<RootObject>(json);
            }
        }

        private void btnInstall_Click(object sender, EventArgs e)
        {
            try
            {
                var client = new WebClient();
                // Remove the temporary downloaded rayshud files
                if (File.Exists($"{Application.StartupPath}\\{Properties.Settings.Default.TempName}"))
                    File.Delete($"{Application.StartupPath}\\{Properties.Settings.Default.TempName}");
                // Restore the configuration file if it has been removed
                if (!File.Exists($"{Application.StartupPath}\\settings.json"))
                    client.DownloadFile("https://raw.githubusercontent.com/CriticalFlaw/rayshud-Installer/master/rayshud-installer/settings.json", $"{Application.StartupPath}\\settings.json");
                else
                    UpdateSettingsFile();
                // Download the latest rayshud from GitHub and extract into the tf/custom directory
                client.DownloadFile("https://github.com/raysfire/rayshud/archive/installer.zip", "rayshud.zip");
                ZipFile.ExtractToDirectory($"{Application.StartupPath}\\{Properties.Settings.Default.TempName}", TF2Directory);
                // Either do a clean install or refresh/update of rayshud
                switch (btnInstall.Text)
                {
                    case "Update":
                    case "Refresh":
                        // Replace the installed rayshud with a fresh copy
                        Directory.Delete($"{TF2Directory}\\rayshud", true);
                        Directory.Move($"{TF2Directory}\\rayshud-{Properties.Settings.Default.GitBranch}", $"{TF2Directory}\\rayshud");
                        MessageBox.Show(Properties.Settings.Default.SuccessRefresh, "rayshud Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;

                    default:
                        Directory.Move($"{TF2Directory}\\rayshud-{Properties.Settings.Default.GitBranch}", $"{TF2Directory}\\rayshud");
                        MessageBox.Show(Properties.Settings.Default.SuccessInstall, "rayshud Installed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                }
                // Remove the temporary downloaded rayshud files
                if (File.Exists($"{Application.StartupPath}\\{Properties.Settings.Default.TempName}"))
                    File.Delete($"{Application.StartupPath}\\{Properties.Settings.Default.TempName}");
                CheckHUDDirectory();
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"{Properties.Settings.Default.ErrorInstall}\n{ex.Message}", "Error: Installing rayshud", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnUninstall_Click(object sender, EventArgs e)
        {
            try
            {
                if (Directory.Exists($"{TF2Directory}\\rayshud"))
                {
                    Directory.Delete($"{TF2Directory}\\rayshud", true);
                    MessageBox.Show(Properties.Settings.Default.SuccessRemove, "rayshud Uninstalled", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MessageBox.Show($"{Properties.Settings.Default.ErrorRemove}\n{ex.Message}", "Error: Uninstalling rayshud", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    File.Copy($"{scoreboard}\\scoreboard-minimal.res", $"{resource}\\scoreboard.res", true);
                else
                    File.Copy($"{scoreboard}\\scoreboard-default.res", $"{resource}\\scoreboard.res", true);

                // 3. Default Background - enable or disable the custom backgrounds files by renaming them
                if (settings.DefaultMenuBG)
                {
                    if (Directory.Exists(console) && File.Exists($"{scripts}\\chapterbackgrounds.txt"))
                    {
                        Directory.Move(console, $"{console}_off");
                        File.Move($"{scripts}\\chapterbackgrounds.txt", $"{scripts}\\chapterbackgrounds_off.txt");
                    }
                }
                else
                {
                    if (Directory.Exists($"{console}_off") && File.Exists($"{scripts}\\chapterbackgrounds_off.txt"))
                    {
                        Directory.Move($"{console}_off", console);
                        File.Move($"{scripts}\\chapterbackgrounds_off.txt", $"{scripts}\\chapterbackgrounds.txt");
                    }
                }

                // 4. Class/Team Select Style - either left or center, copy and replace existing files
                if (settings.TeamSelect)
                {
                    File.Copy($"{teammenu}\\TeamMenu-center.res", $"{resource}\\TeamMenu.res", true);
                    File.Copy($"{teammenu}\\ClassSelection-center.res", $"{resource}\\ClassSelection.res", true);
                }
                else
                {
                    File.Copy($"{teammenu}\\TeamMenu-left.res", $"{resource}\\TeamMenu.res", true);
                    File.Copy($"{teammenu}\\ClassSelection-left.res", $"{resource}\\ClassSelection.res", true);
                }

                // 5. Player Health Style - either default, cross, teambar or broesel, copy and replace existing files
                switch (settings.HealthStyle)
                {
                    default:
                        File.Copy($"{playerhealth}\\HudPlayerHealth-default.res", $"{resource}\\HudPlayerHealth.res", true);
                        break;

                    case 2:
                        File.Copy($"{playerhealth}\\HudPlayerHealth-teambar.res", $"{resource}\\HudPlayerHealth.res", true);
                        break;

                    case 3:
                        File.Copy($"{playerhealth}\\HudPlayerHealth-cross.res", $"{resource}\\HudPlayerHealth.res", true);
                        break;

                    case 4:
                        File.Copy($"{playerhealth}\\HudPlayerHealth-broesel.res", $"{resource}\\HudPlayerHealth.res", true);
                        break;
                }

                // Spy Disguise Image and Uber Animation - uncomment all
                var disguiseImageIndex = 87;
                var uberAnimationIndex = 104;
                var lines = File.ReadAllLines(animations);
                lines[(disguiseImageIndex + 0) - 1] = lines[(disguiseImageIndex + 0) - 1].Replace("//", string.Empty);
                lines[(disguiseImageIndex + 1) - 1] = lines[(disguiseImageIndex + 1) - 1].Replace("//", string.Empty);
                lines[(disguiseImageIndex + 2) - 1] = lines[(disguiseImageIndex + 2) - 1].Replace("//", string.Empty);
                lines[(disguiseImageIndex + 7) - 1] = lines[(disguiseImageIndex + 7) - 1].Replace("//", string.Empty);
                lines[(disguiseImageIndex + 8) - 1] = lines[(disguiseImageIndex + 8) - 1].Replace("//", string.Empty);
                lines[(disguiseImageIndex + 9) - 1] = lines[(disguiseImageIndex + 9) - 1].Replace("//", string.Empty);
                lines[(uberAnimationIndex + 0) - 1] = lines[(uberAnimationIndex + 0) - 1].Replace("//", string.Empty);
                lines[(uberAnimationIndex + 1) - 1] = lines[(uberAnimationIndex + 1) - 1].Replace("//", string.Empty);
                lines[(uberAnimationIndex + 2) - 1] = lines[(uberAnimationIndex + 2) - 1].Replace("//", string.Empty);
                File.WriteAllLines(animations, lines);

                // 6. Spy Disguise Image - enable or disable by commenting out the lines
                if (settings.DisguiseImage)
                {
                    lines[(disguiseImageIndex + 0) - 1] = $"\t{lines[(disguiseImageIndex + 0) - 1].Replace("//", string.Empty).Trim()}";
                    lines[(disguiseImageIndex + 1) - 1] = $"\t{lines[(disguiseImageIndex + 1) - 1].Replace("//", string.Empty).Trim()}";
                    lines[(disguiseImageIndex + 2) - 1] = $"\t{lines[(disguiseImageIndex + 2) - 1].Replace("//", string.Empty).Trim()}";
                    lines[(disguiseImageIndex + 7) - 1] = $"\t{lines[(disguiseImageIndex + 7) - 1].Replace("//", string.Empty).Trim()}";
                    lines[(disguiseImageIndex + 8) - 1] = $"\t{lines[(disguiseImageIndex + 8) - 1].Replace("//", string.Empty).Trim()}";
                    lines[(disguiseImageIndex + 9) - 1] = $"\t{lines[(disguiseImageIndex + 9) - 1].Replace("//", string.Empty).Trim()}";
                }
                else
                {
                    lines[(disguiseImageIndex + 0) - 1] = $"\t//{lines[(disguiseImageIndex + 0) - 1].Trim()}";
                    lines[(disguiseImageIndex + 1) - 1] = $"\t//{lines[(disguiseImageIndex + 1) - 1].Trim()}";
                    lines[(disguiseImageIndex + 2) - 1] = $"\t//{lines[(disguiseImageIndex + 2) - 1].Trim()}";
                    lines[(disguiseImageIndex + 7) - 1] = $"\t//{lines[(disguiseImageIndex + 7) - 1].Trim()}";
                    lines[(disguiseImageIndex + 8) - 1] = $"\t//{lines[(disguiseImageIndex + 8) - 1].Trim()}";
                    lines[(disguiseImageIndex + 9) - 1] = $"\t//{lines[(disguiseImageIndex + 9) - 1].Trim()}";
                }

                // 7. Uber Animation - enable or disable by commenting out the lines
                switch (settings.UberAnimation)
                {
                    default:
                        lines[(uberAnimationIndex + 0) - 1] = $"\t{lines[(uberAnimationIndex + 0) - 1].Replace("//", string.Empty).Trim()}";
                        lines[(uberAnimationIndex + 1) - 1] = $"\t//{lines[(uberAnimationIndex + 1) - 1].Trim()}";
                        lines[(uberAnimationIndex + 2) - 1] = $"\t//{lines[(uberAnimationIndex + 2) - 1].Trim()}";
                        break;

                    case 2:
                        lines[(uberAnimationIndex + 0) - 1] = $"\t//{lines[(uberAnimationIndex + 0) - 1].Trim()}";
                        lines[(uberAnimationIndex + 1) - 1] = $"\t{lines[(uberAnimationIndex + 1) - 1].Replace("//", string.Empty).Trim()}";
                        lines[(uberAnimationIndex + 2) - 1] = $"\t//{lines[(uberAnimationIndex + 2) - 1].Trim()}";
                        break;

                    case 3:
                        lines[(uberAnimationIndex + 0) - 1] = $"\t//{lines[(uberAnimationIndex + 0) - 1].Trim()}";
                        lines[(uberAnimationIndex + 1) - 1] = $"\t//{lines[(uberAnimationIndex + 1) - 1].Trim()}";
                        lines[(uberAnimationIndex + 2) - 1] = $"\t{lines[(uberAnimationIndex + 2) - 1].Replace("//", string.Empty).Trim()}";
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
                    else if (settings.XHairStyle == 16)
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

                    var crosshairStyleIndex = 16;
                    switch (settings.XHairStyle)
                    {
                        case 1: // BasicCross
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-102\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-99\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"2\"";
                            break;

                        case 2: // BasicCrossLarge
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-102\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-98\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"2\"";
                            break;

                        case 3: // BasicCrossSmall
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-101\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-99\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"2\"";
                            break;

                        case 4: // BasicDot
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-103\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-100\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"3\"";
                            break;

                        case 5: // CircleDot
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-103\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-96\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"8\"";
                            break;

                        case 6: // OpenCross
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-85\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-100\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"i\"";
                            break;

                        case 7: // OpenCrossDot
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-85\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-100\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"h\"";
                            break;

                        case 8: // ScatterSpread
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-99\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-99\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"0\"";
                            break;

                        case 9: // ThinCircle
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-100\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-96\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"9\"";
                            break;

                        case 10: // ThinCross
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-100\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-103\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"+\"";
                            break;

                        case 11: // Wings
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-100\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-97\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"d\"";
                            break;

                        case 12: // WingsPlus
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-100\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-97\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"c\"";
                            break;

                        case 13: // WingsSmall
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-100\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-97\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"g\"";
                            break;

                        case 14: // WingsSmallDot
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-100\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-97\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"f\"";
                            break;

                        case 15: // xHairCircle
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-100\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-102\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"0\"";
                            break;

                        default:
                            lines[crosshairStyleIndex - 1] = "\t\t\"labelText\"\t\t\"2\"";
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

                MessageBox.Show(Properties.Settings.Default.SuccessUpdate, "Changes Saved!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"{Properties.Settings.Default.ErrorUpdate}\n{ex.Message}", "Error: Updating rayshud", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            DirectoryBrowser.Description = $"Please select your tf\\custom folder. Example:\n{Properties.Settings.Default.TFDirectory}";
            DirectoryBrowser.ShowNewFolderButton = true;
            while (IsDirectoryValid == false)
            {
                // Until the correct path is provided or the user clicks 'Cancel' - keep prompting for a valid tf/custom directory.
                if (DirectoryBrowser.ShowDialog() == DialogResult.OK)
                {
                    if (!DirectoryBrowser.SelectedPath.Contains("tf\\custom")) continue;
                    TF2Directory = DirectoryBrowser.SelectedPath;
                    txtDirectory.Text = TF2Directory;
                    settings.TF2Directory = txtDirectory.Text;
                    WriteToSettings("TF2Directory", settings.TF2Directory.Replace("\\", "/"));
                    btnInstall.Enabled = true;
                    btnPlayTF2.Enabled = true;
                    CheckHUDDirectory();
                    IsDirectoryValid = true;
                }
                else
                    break;
            }
        }

        private void btnGitRepo_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/CriticalFlaw/rayshud-Installer");
        }

        private void btnGitIssue_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/CriticalFlaw/rayshud-Installer/issues");
        }

        private void btnSteamGroup_Click(object sender, EventArgs e)
        {
            Process.Start("https://steamcommunity.com/groups/rayshud");
        }

        private void btnAndKnuckles_Click(object sender, EventArgs e)
        {
            var bitmap = new Bitmap(Properties.Resources.KnucklesCrosses);
            var directory = $"{Application.StartupPath}\\KnuckleCrosses.jpg";
            if (File.Exists(directory))
                File.Delete(directory);
            bitmap.Save(directory);
            Process.Start(directory);
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

                default:
                    btnAndKnuckles.BackColor = colorPicker.Color;
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
                default:
                    for (int x = 8; x <= 40; x += 2)
                        cbXHairSizes.Items.Add(x.ToString());
                    break;

                case "KonrWings":
                    for (int x = 16; x <= 40; x += 8)
                        cbXHairSizes.Items.Add(x.ToString());
                    break;

                case "KnuckleCrosses":
                    for (int x = 10; x <= 50; x += 1)
                        cbXHairSizes.Items.Add(x.ToString());
                    break;
            }

            // Update the crosshair settings on the UI
            switch (settings.XHairStyle)
            {
                case 1: // BasicCross
                    lblCrosshair.Text = @"2";
                    lblCrosshair.Location = new Point(97, 33);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("26");
                    break;

                case 2: // BasicCrossLarge
                    lblCrosshair.Text = @"2";
                    lblCrosshair.Location = new Point(97, 33);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("32");
                    break;

                case 3: // BasicCrossSmall
                    lblCrosshair.Text = @"2";
                    lblCrosshair.Location = new Point(97, 33);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("18");
                    break;

                case 4: // BasicDot
                    lblCrosshair.Text = @"3";
                    lblCrosshair.Location = new Point(103, 31);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("24");
                    break;

                case 5: // CircleDot
                    lblCrosshair.Text = @"8";
                    lblCrosshair.Location = new Point(103, 31);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("34");
                    break;

                case 6: // OpenCross
                    lblCrosshair.Text = @"i";
                    lblCrosshair.Location = new Point(95, 28);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("24");
                    break;

                case 7: // OpenCrossDot
                    lblCrosshair.Text = @"h";
                    lblCrosshair.Location = new Point(95, 28);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("24");
                    break;

                case 8: // ScatterSpread
                    lblCrosshair.Text = @"0";
                    lblCrosshair.Location = new Point(104, 30);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("32");
                    break;

                case 9: // ThinCircle
                    lblCrosshair.Text = @"9";
                    lblCrosshair.Location = new Point(105, 32);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("34");
                    break;

                case 10: // ThinCross
                    lblCrosshair.Text = @"+";
                    lblCrosshair.Location = new Point(108, 30);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("24");
                    break;

                case 11: // Wings
                    lblCrosshair.Text = @"d";
                    lblCrosshair.Location = new Point(95, 32);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("34");
                    break;

                case 12: // WingsPlus
                    lblCrosshair.Text = @"c";
                    lblCrosshair.Location = new Point(95, 32);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("34");
                    break;

                case 13: // WingsSmall
                    lblCrosshair.Text = @"g";
                    lblCrosshair.Location = new Point(95, 32);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("34");
                    break;

                case 14: // WingsSmallDot
                    lblCrosshair.Text = @"f";
                    lblCrosshair.Location = new Point(95, 32);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("34");
                    break;

                case 15: // XHairCirle
                    lblCrosshair.Text = @"o";
                    lblCrosshair.Location = new Point(97, 32);
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
        public string TF2Directory { get; set; }
        public string LastModified { get; set; }
    }
}