using SharpRaven;
using SharpRaven.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace FlawHUD_Installer
{
    public partial class frmMain : Form
    {
        // Initiate the error-tracker
        private RavenClient ravenClient = new RavenClient("https://e3d4daa2995342d1aaca5827b95fce2d:23548ce9177049f5a1601ceafeb8c609@sentry.io/1190168");

        public HUDSettings settings = new HUDSettings();
        public List<string> LatestHUDVersion = new List<string>();
        public string TF2Directory; //, hitEnable, hitVisible, hitXAxis, hitYAxis, hitStyle, hitOutline, hitSize, colorHealing, colorDamage, colorUber1, colorUber2, InstalledHUD, DownloadLink;

        public frmMain()
        {
            InitializeComponent();      // Start up the main components.
            GetLiveVersion();           // Check for the latest version
            CheckTF2Directory();        // Check if the default tf/custom directory exists
            CheckHUDDirectory();        // Check the tf directory for installed hud files
        }

        private void GetLiveVersion()
        {
            var WC = new WebClient();
            var textFromURL = WC.DownloadString("https://raw.githubusercontent.com/raysfire/rayshud/installer/README.md");
            // Split the downloaded text into an array.
            string[] textFromURLArray = textFromURL.Split('\n');
            // Extract the live version number and add it to the list
            txtLiveVersion.Text = textFromURLArray[textFromURLArray.Length - 2];
        }

        private void CheckTF2Directory()
        {
            // Check for temporary HUD files, remove them if they exist.
            if (File.Exists($"{Application.StartupPath}\\TempHUD.zip"))
                File.Delete($"{Application.StartupPath}\\TempHUD.zip");

            try
            {
                // Check if the tf/custom folder exists in any of the preset directory paths
                if (Directory.Exists("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Team Fortress 2\\tf\\custom"))
                {
                    TF2Directory = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Team Fortress 2\\tf\\custom";
                    txtDirectory.Text = TF2Directory;
                    btnPlayTF2.Enabled = true;
                }
                else if (Directory.Exists("D:\\Program Files (x86)\\Steam\\steamapps\\common\\Team Fortress 2\\tf\\custom"))
                {
                    TF2Directory = "D:\\Program Files (x86)\\Steam\\steamapps\\common\\Team Fortress 2\\tf\\custom";
                    txtDirectory.Text = TF2Directory;
                    btnPlayTF2.Enabled = true;
                }
                else
                {
                    // Prompt the user to provide the tf/custom directory themselves
                    var IsTF2PathValid = false;
                    var TF2DirBrowser = new FolderBrowserDialog();
                    TF2DirBrowser.Description = ("Please select your tf/custom folder. Example: \n C:/Program Files (x86)/Steam/steamapps/common/Team Fortress 2/tf/custom");
                    while (IsTF2PathValid == false)
                    {
                        // Until the correct path is provided or the user clicks 'Cancel' - keep prompting for a valid tf/custom directory.
                        if (TF2DirBrowser.ShowDialog() == DialogResult.OK && TF2DirBrowser.SelectedPath.Contains("Steam\\steamapps\\common\\Team Fortress 2\\tf\\custom"))
                        {
                            TF2Directory = TF2DirBrowser.SelectedPath;
                            txtDirectory.Text = $"{TF2Directory}\\rayshud";
                            btnPlayTF2.Enabled = true;
                            IsTF2PathValid = true;
                        }
                        else if (TF2DirBrowser.ShowDialog() == DialogResult.Cancel)
                            Application.Exit();
                    }
                }
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"An error occurred while attempting to check tf/custom directory. \n{ex.Message}", "Error: Checking tf/custom directory!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CheckHUDDirectory()
        {
            try
            {
                if (Directory.Exists($"{TF2Directory}\\rayshud-master"))
                    Directory.Move($"{TF2Directory}\\rayshud-master", $"{TF2Directory}\\rayshud");
                // Loop through all the listed HUDS and check if any of them are installed
                if (Directory.Exists($"{TF2Directory}\\rayshud"))
                {
                    btnUninstall.Enabled = true;
                    txtInstalledVersion.Text = File.ReadLines($"{TF2Directory}\\rayshud\\README.md").Last().ToString();
                    // If the local version number matches the live version, only let the user to reinstall a fresh copy of the HUD
                    if (txtInstalledVersion.ToString().Trim() == txtLiveVersion.ToString().Trim())
                    {
                        btnInstall.Text = "Refresh";
                        lblStatus.Text = "Installed, Updated";
                    }
                    else
                    {
                        // If the local version number does not match the live version, notify the user that an update is available
                        btnInstall.Text = "Update";
                        lblStatus.Text = "Installed, Outdated";
                        MessageBox.Show($"New version of rayshud available! Click 'Update' to download the latest version.", "rayshud Update Available!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    LoadHUDSettings();
                }
                else
                {
                    btnInstall.Text = "Install";
                    btnUninstall.Enabled = false;
                    lblStatus.Text = "Not Installed...";
                }
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"An error occurred while attempting to find rayshud version numbers \n{ex.Message}", "Error: Checking rayshud Version Numbers!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void LoadHUDSettings()
        {
            try
            {
                #region DEFAULT

                // GENERAL
                cbHUDVersion.SelectedIndex = 0;
                cbScoreboard.SelectedIndex = 0;

                rbChatBoxTop.Checked = true;
                rbChatBoxBottom.Checked = false;
                rbTeamSelectLeft.Checked = true;
                rbTeamSelectCenter.Checked = false;
                cbDisguiseImage.Checked = false;
                cbDefaultMenuBG.Checked = false;

                rbUberAnimation1.Checked = true;
                rbUberAnimation2.Checked = false;
                rbUberAnimation3.Checked = false;
                btnUberBarColor.BackColor = Color.White;
                btnUberFullColor.BackColor = Color.White;
                btnUberFlashColor1.BackColor = Color.White;
                btnUberFlashColor2.BackColor = Color.White;

                // CROSSHAIR
                cbCrosshair.Checked = false;
                cbXHairOutline.Checked = false;
                cbXHairPulse.Checked = true;
                txtXHairHeight.Value = 200;
                txtXHairWidth.Value = 200;
                btnXHairColor.BackColor = Color.White;
                btnXHairPulseColor.BackColor = Color.White;

                // HEALTH and AMMO
                btnHealingDone.BackColor = Color.White;
                btnNumColor.BackColor = Color.White;
                btnBuffNumColor.BackColor = Color.White;
                btnLowNumColor.BackColor = Color.White;
                btnAmmoClip.BackColor = Color.White;
                btnAmmoReserve.BackColor = Color.White;
                btnLowAmmoClip.BackColor = Color.White;
                btnLowAmmoReserve.BackColor = Color.White;

                #endregion DEFAULT

                #region READ

                var custom = $"{TF2Directory}\\rayshud\\customizations\\config.txt";
                var colorScheme = $"{TF2Directory}\\rayshud\\resource\\scheme\\clientscheme_colors.res";
                var animations = $"{TF2Directory}\\rayshud\\scripts\\hudanimations_custom.txt";
                var chat = $"{TF2Directory}\\rayshud\\resource\\ui\\basechat.res";
                var pattern = @"\""(.*?)\""";

                string[] lines = File.ReadAllLines(custom);
                settings.HUDVersion = lines[1 - 1] == "true";
                settings.Scoreboard = lines[2 - 1] == "true";
                lines = File.ReadAllLines(chat);
                var matches = Regex.Matches(lines[10 - 1], pattern);
                if (int.Parse(matches[1].Value) > 50)
                    settings.ChatBox = true;
                if (!File.Exists($"{TF2Directory}\\rayshud\\resource\\ui\\Teammenu-center.res"))
                    settings.TeamSelect = true;
                if (Directory.Exists($"{TF2Directory}\\rayshud\\materials\\console_default"))
                    settings.DefaultMenuBG = true;
                lines = File.ReadAllLines(animations);
                if (lines[87 - 1].StartsWith("//"))
                    settings.DisguiseImage = false;
                if (lines[104 - 1].StartsWith("//") && lines[105 - 1].StartsWith("//"))
                    settings.UberAnimation = 3;
                else if (lines[104 - 1].StartsWith("//") && lines[106 - 1].StartsWith("//"))
                    settings.UberAnimation = 2;
                else
                    settings.UberAnimation = 1;
                if (!File.Exists($"{TF2Directory}\\rayshud\\resource\\ui\\HudPlayerHealth-teambar.res"))
                    settings.PlayerHealth = 2;
                else if (!File.Exists($"{TF2Directory}\\rayshud\\resource\\ui\\HudPlayerHealth-cross.res"))
                    settings.PlayerHealth = 3;
                else if (!File.Exists($"{TF2Directory}\\rayshud\\resource\\ui\\HudPlayerHealth-broeselcross.res"))
                    settings.PlayerHealth = 4;
                else
                    settings.PlayerHealth = 1;

                lines = File.ReadAllLines(colorScheme);
                matches = Regex.Matches(lines[7 - 1], pattern);
                settings.AmmoClip = matches[1].Value;
                matches = Regex.Matches(lines[8 - 1], pattern);
                settings.AmmoReserve = matches[1].Value;
                matches = Regex.Matches(lines[9 - 1], pattern);
                settings.LowAmmoClip = matches[1].Value;
                matches = Regex.Matches(lines[10 - 1], pattern);
                settings.UberBarColor = matches[1].Value;
                matches = Regex.Matches(lines[23 - 1], pattern);
                settings.NumColor = matches[1].Value;
                matches = Regex.Matches(lines[24 - 1], pattern);
                settings.BuffNumColor = matches[1].Value;
                matches = Regex.Matches(lines[25 - 1], pattern);
                settings.LowNumColor = matches[1].Value;
                matches = Regex.Matches(lines[32 - 1], pattern);
                settings.LowAmmoReserve = matches[1].Value;
                matches = Regex.Matches(lines[35 - 1], pattern);
                settings.UberFullColor = matches[1].Value;
                matches = Regex.Matches(lines[37 - 1], pattern);
                settings.UberFlashColor1 = matches[1].Value;
                matches = Regex.Matches(lines[38 - 1], pattern);
                settings.UberFlashColor2 = matches[1].Value;
                matches = Regex.Matches(lines[41 - 1], pattern);
                settings.HealingDone = matches[1].Value;
                matches = Regex.Matches(lines[45 - 1], pattern);
                settings.XHairColor = matches[1].Value;
                matches = Regex.Matches(lines[46 - 1], pattern);
                settings.XHairPulseColor = matches[1].Value;

                #endregion READ

                #region WRITE

                // GENERAL
                if (settings.HUDVersion)
                    cbHUDVersion.SelectedIndex = 1;
                if (settings.Scoreboard)
                    cbScoreboard.SelectedIndex = 1;
                if (settings.ChatBox)
                    rbChatBoxTop.Checked = true;
                else
                    rbChatBoxBottom.Checked = true;
                if (settings.TeamSelect)
                    rbTeamSelectCenter.Checked = true;
                else
                    rbTeamSelectLeft.Checked = true;
                if (settings.DisguiseImage)
                    cbDisguiseImage.Checked = true;
                if (settings.DefaultMenuBG)
                    cbDefaultMenuBG.Checked = true;
                if (settings.UberAnimation == 1)
                    rbUberAnimation1.Checked = true;
                else if (settings.UberAnimation == 2)
                    rbUberAnimation2.Checked = true;
                else if (settings.UberAnimation == 3)
                    rbUberAnimation3.Checked = true;
                string[] split = settings.UberBarColor.Split(null);
                btnUberBarColor.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));
                split = settings.UberFullColor.Split(null);
                btnUberFullColor.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));
                split = settings.UberFlashColor1.Split(null);
                btnUberFlashColor1.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));
                split = settings.UberFlashColor2.Split(null);
                btnUberFlashColor2.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                // CROSSHAIR
                lbXHairStyles.SelectedIndex = lbXHairStyles.FindStringExact(settings.XHairStyle);
                if (settings.Crosshair)
                    cbCrosshair.Checked = true;
                if (settings.XHairOutline)
                    cbXHairOutline.Checked = true;
                if (settings.XHairPulse)
                    cbXHairPulse.Checked = true;
                txtXHairHeight.Value = settings.XHairHeight;
                txtXHairWidth.Value = settings.XHairWidth;
                split = settings.XHairColor.Split(null);
                btnXHairColor.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));
                split = settings.XHairPulseColor.Split(null);
                btnXHairPulseColor.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                // HEALTH and AMMO
                lbPlayerHealth.SelectedIndex = settings.PlayerHealth;
                split = settings.HealingDone.Split(null);
                btnHealingDone.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));
                split = settings.NumColor.Split(null);
                btnNumColor.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));
                split = settings.BuffNumColor.Split(null);
                btnBuffNumColor.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));
                split = settings.LowNumColor.Split(null);
                btnLowNumColor.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));
                split = settings.AmmoClip.Split(null);
                btnAmmoClip.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));
                split = settings.AmmoReserve.Split(null);
                btnAmmoReserve.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));
                split = settings.LowAmmoClip.Split(null);
                btnLowAmmoClip.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));
                split = settings.LowAmmoReserve.Split(null);
                btnLowAmmoReserve.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                #endregion WRITE
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"An error occured when loading settings \n{ex.Message}", "Error: Loading Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnInstall_Click(object sender, EventArgs e)
        {
            try
            {
                // Remove the downloaded file from the temporary location
                if (File.Exists($"{Application.StartupPath}\\TempHUD.zip"))
                    File.Delete($"{Application.StartupPath}\\TempHUD.zip");

                WebClient WC = new WebClient(); // Download th HUD into a temporary location
                WC.DownloadFile("https://github.com/raysfire/rayshud/archive/installer.zip", "TempHUD.zip");
                // Extract the downloaded HUD into the tf/custom folder
                ZipFile.ExtractToDirectory($"{Application.StartupPath}\\TempHUD.zip", TF2Directory);
                switch (btnInstall.Text)
                {
                    case "Install":
                        MessageBox.Show($"Finished Installing rayshud...", "rayshud Installed!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;

                    case "Update":
                        MessageBox.Show($"Finished Updating rayshud...", "rayshud Updated!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;

                    case "Refresh":
                        MessageBox.Show($"Finished Refreshing rayshud...", "rayshud Refreshed!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                }
                CheckHUDDirectory();
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"An error occurred while attempting to download rayshud \n {ex.Message}", $"Error: Downloading rayshud!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnUninstall_Click(object sender, EventArgs e)
        {
            try
            {
                // Verify that the selected HUD that's being uninstalled actually exists in the tf/custom folder
                if (Directory.Exists($"{TF2Directory}\\rayshud"))
                {
                    Directory.Delete($"{TF2Directory}\\rayshud", true);
                    MessageBox.Show($"Finished Uninstalling rayshud...");
                    txtInstalledVersion.Text = "...";
                    CheckHUDDirectory();
                }
                else
                    MessageBox.Show($"rayshud< was not found in the tf/custom directory.", $"Error: rayshud Not Found!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"An error occurred while attempting to remove. \n{ex.Message}", $"Error: Uninstalling rayshud!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSaveChanges_Click(object sender, EventArgs e)
        {
            var custom = $"{TF2Directory}\\rayshud\\customizations\\config.txt";
            var colorScheme = $"{TF2Directory}\\rayshud\\resource\\scheme\\clientscheme_colors.res";
            var animations = $"{TF2Directory}\\rayshud\\scripts\\hudanimations_custom.txt";
            var chat = $"{TF2Directory}\\rayshud\\resource\\ui\\basechat.res";
            var layout = $"{TF2Directory}\\rayshud\\scripts\\hudlayout.res";

            if (settings.HUDVersion)
            {
                FileInfo Sourcefile = new FileInfo($"{TF2Directory}\\rayshud\\customizations\\Main Menu Classic");
                Sourcefile.CopyTo($"{TF2Directory}\\rayshud", true);
            }
            else
            {
                FileInfo Sourcefile = new FileInfo($"{TF2Directory}\\rayshud\\customizations\\Main Menu Default");
                Sourcefile.CopyTo($"{TF2Directory}\\rayshud", true);
            }

            if (settings.Scoreboard)
            {
                FileInfo Sourcefile = new FileInfo($"{TF2Directory}\\rayshud\\customizations\\Scoreboard Minimal");
                Sourcefile.CopyTo($"{TF2Directory}\\rayshud", true);
            }
            else
            {
                FileInfo Sourcefile = new FileInfo($"{TF2Directory}\\rayshud\\customizations\\Scoreboard Default");
                Sourcefile.CopyTo($"{TF2Directory}\\rayshud", true);
            }

            if (settings.DefaultMenuBG)
            {
                Directory.Move($"{TF2Directory}\\rayshud\\material\\console", $"{TF2Directory}\\rayshud\\material\\console_off");
                File.Move($"{TF2Directory}\\rayshud\\material\\chapterbackgrounds.txt", $"{TF2Directory}\\rayshud\\material\\chapterbackgrounds_off.txt");
            }
            else
            {
                Directory.Move($"{TF2Directory}\\rayshud\\material\\console_off", $"{TF2Directory}\\rayshud\\material\\console");
                File.Move($"{TF2Directory}\\rayshud\\material\\chapterbackgrounds_off.txt", $"{TF2Directory}\\rayshud\\material\\chapterbackgrounds.txt");
            }

            if (settings.TeamSelect)
                File.Move($"{TF2Directory}\\rayshud\\customizations\\Team Select\\resource\\ui\\Teammenu-center.res", $"{TF2Directory}\\rayshud\\resource\\ui\\Teammenu.res");
            else
                File.Move($"{TF2Directory}\\rayshud\\customizations\\Team Select\\resource\\ui\\Teammenu-default.res", $"{TF2Directory}\\rayshud\\resource\\ui\\Teammenu.res");

            if (settings.PlayerHealth == 1)
                File.Move($"{TF2Directory}\\rayshud\\customizations\\Player Health\\resource\\ui\\HudPlayerHealth-default.res", $"{TF2Directory}\\rayshud\\resource\\ui\\HudPlayerHealth.res");
            else if (settings.PlayerHealth == 2)
                File.Move($"{TF2Directory}\\rayshud\\customizations\\Player Health\\resource\\ui\\HudPlayerHealth-teambar.res", $"{TF2Directory}\\rayshud\\resource\\ui\\HudPlayerHealth.res");
            else if (settings.PlayerHealth == 3)
                File.Move($"{TF2Directory}\\rayshud\\customizations\\Player Health\\resource\\ui\\HudPlayerHealth-cross.res", $"{TF2Directory}\\rayshud\\resource\\ui\\HudPlayerHealth.res");
            else if (settings.PlayerHealth == 4)
                File.Move($"{TF2Directory}\\rayshud\\customizations\\Player Health\\resource\\ui\\HudPlayerHealth-broesel.res", $"{TF2Directory}\\rayshud\\resource\\ui\\HudPlayerHealth.res");
            //-------------------------------------------------------------------------

            string[] lines = File.ReadAllLines(animations);
            if (settings.DisguiseImage)
            {
                lines[87 - 1] = $"//{lines[87 - 1]}";
                lines[88 - 1] = $"//{lines[88 - 1]}";
                lines[89 - 1] = $"//{lines[89 - 1]}";
                lines[94 - 1] = $"//{lines[94 - 1]}";
                lines[95 - 1] = $"//{lines[95 - 1]}";
                lines[96 - 1] = $"//{lines[96 - 1]}";
            }
            else
            {
                lines[87 - 1] = lines[87 - 1].Replace("//", string.Empty);
                lines[88 - 1] = lines[88 - 1].Replace("//", string.Empty);
                lines[89 - 1] = lines[89 - 1].Replace("//", string.Empty);
                lines[94 - 1] = lines[94 - 1].Replace("//", string.Empty);
                lines[95 - 1] = lines[95 - 1].Replace("//", string.Empty);
                lines[96 - 1] = lines[96 - 1].Replace("//", string.Empty);
            }

            if (settings.UberAnimation == 1)
            {
                lines[104 - 1] = lines[104 - 1].Replace("//", string.Empty);
                lines[105 - 1] = $"//{lines[105 - 1]}";
                lines[106 - 1] = $"//{lines[106 - 1]}";
            }
            else if (settings.UberAnimation == 2)
            {
                lines[104 - 1] = $"//{lines[104 - 1]}";
                lines[105 - 1] = lines[105 - 1].Replace("//", string.Empty);
                lines[106 - 1] = $"//{lines[106 - 1]}";
            }
            else if (settings.UberAnimation == 3)
            {
                lines[104 - 1] = $"//{lines[104 - 1]}";
                lines[105 - 1] = $"//{lines[105 - 1]}";
                lines[106 - 1] = lines[106 - 1].Replace("//", string.Empty);
            }

            if (settings.XHairPulse)
            {
                lines[80 - 1] = lines[87 - 1].Replace("//", string.Empty);
                lines[81 - 1] = lines[88 - 1].Replace("//", string.Empty);
            }
            else
            {
                lines[80 - 1] = $"//{lines[87 - 1]}";
                lines[81 - 1] = $"//{lines[88 - 1]}";
            }
            File.WriteAllLines(animations, lines);
            //-------------------------------------------------------------------------

            lines = File.ReadAllLines(chat);
            if (settings.ChatBox)
                lines[10 - 1] = $"\"ypos\"   \"150\"";
            else
                lines[10 - 1] = $"\"ypos\"   \"30\"";
            File.WriteAllLines(chat, lines);

            lines = File.ReadAllLines(layout);
            switch (settings.XHairStyle)
            {
                case "xHairCircle":
                    SetCrosshairSettings(33);
                    break;

                case "ScatterSpread":
                    SetCrosshairSettings(50);
                    break;

                case "BasicCross":
                    SetCrosshairSettings(68);
                    break;

                case "BasicCrossSmall":
                    SetCrosshairSettings(85);
                    break;

                case "BasicCrossLarge":
                    SetCrosshairSettings(102);
                    break;

                case "BasicDot":
                    SetCrosshairSettings(119);
                    break;

                case "CircleDot":
                    SetCrosshairSettings(136);
                    break;

                case "ThinCircle":
                    SetCrosshairSettings(153);
                    break;

                case "WingsPlus":
                    SetCrosshairSettings(170);
                    break;

                case "Wings":
                    SetCrosshairSettings(187);
                    break;

                case "WingsSmallDot":
                    SetCrosshairSettings(204);
                    break;

                case "WingsSmall":
                    SetCrosshairSettings(221);
                    break;

                case "OpenCross":
                    SetCrosshairSettings(238);
                    break;

                case "OpenCrossDot":
                    SetCrosshairSettings(255);
                    break;

                case "ThinCross":
                    SetCrosshairSettings(272);
                    break;

                case "KonrWings":
                    SetCrosshairSettings(289);
                    break;
            }
            // Control: cbXHairOutline
            // Location: scripts\hudlayout.res
            // Task: Add or remove Outline to the font name
            File.WriteAllLines(layout, lines);
            //-------------------------------------------------------------------------

            lines = File.ReadAllLines(colorScheme);
            lines[7 - 1] = $"\"Ammo In Clip\"   \"{settings.AmmoClip}\"";
            lines[8 - 1] = $"\"Ammo In Reserve\"    \"{settings.AmmoReserve}\"";
            lines[9 - 1] = $"\"Ammo In Clip Low\"   \"{settings.LowAmmoClip}\"";
            lines[10 - 1] = $"\"Ammo In Reserve Low\"   \"{settings.LowAmmoReserve}\"";
            lines[23 - 1] = $"\"Health Normal\" \"{settings.NumColor}\"";
            lines[24 - 1] = $"\"Health Buff\"   \"{settings.BuffNumColor}\"";
            lines[25 - 1] = $"\"Health Hurt\"   \"{settings.LowNumColor}\"";
            lines[32 - 1] = $"\"Uber Bar Color\"    \"{settings.UberBarColor}\"";
            lines[35 - 1] = $"\"Solid Color Uber\"  \"{settings.UberFullColor}\"";
            lines[37 - 1] = $"\"Flashing Uber Color1\"  \"{settings.UberFlashColor1}\"";
            lines[38 - 1] = $"\"Flashing Uber Color2\"  \"{settings.UberFlashColor2}\"";
            lines[41 - 1] = $"\"Heal Numbers\"  \"{settings.HealingDone}\"";
            lines[45 - 1] = $"\"Crosshair\" \"{settings.XHairColor}\"";
            lines[46 - 1] = $"\"CrosshairDamage\"   \"{settings.XHairPulseColor}\"";
            File.WriteAllLines(colorScheme, lines);
        }

        private void btnPlayTF2_Click(object sender, EventArgs e)
        {
            Process.Start("steam://rungameid/440");
            Application.Exit();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            // Prompt the user to provide the tf/custom directory themselves
            var IsTF2PathValid = false;
            var TF2DirBrowser = new FolderBrowserDialog();
            TF2DirBrowser.Description = ("Please select your tf/custom folder. Example: \n C:/Program Files (x86)/Steam/steamapps/common/Team Fortress 2/tf/custom");
            while (IsTF2PathValid == false)
            {
                // Until the correct path is provided or the user clicks 'Cancel' - keep prompting for a valid tf/custom directory.
                if (TF2DirBrowser.ShowDialog() == DialogResult.OK && TF2DirBrowser.SelectedPath.Contains("Steam\\steamapps\\common\\Team Fortress 2\\tf\\custom"))
                {
                    TF2Directory = TF2DirBrowser.SelectedPath;
                    btnPlayTF2.Enabled = true;
                    IsTF2PathValid = true;
                }
                else if (TF2DirBrowser.ShowDialog() == DialogResult.Cancel)
                    break;
            }
        }

        private void btnColorPicker_Click(object sender, EventArgs e)
        {
            // Bring up the color picker dialog
            ColorDialog colorPicker = new ColorDialog();
            colorPicker.ShowDialog();
            switch (((Button)sender).Name)
            {
                case "btnUberBarColor":
                    btnUberBarColor.BackColor = colorPicker.Color;
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
                    break;

                case "btnXHairPulse":
                    btnXHairPulseColor.BackColor = colorPicker.Color;
                    settings.XHairPulseColor = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnHealingDone":
                    btnHealingDone.BackColor = colorPicker.Color;
                    settings.HealingDone = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnNumColor":
                    btnNumColor.BackColor = colorPicker.Color;
                    settings.NumColor = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnBuffNumColor":
                    btnBuffNumColor.BackColor = colorPicker.Color;
                    settings.BuffNumColor = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnLowNumColor":
                    btnLowNumColor.BackColor = colorPicker.Color;
                    settings.LowNumColor = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnAmmoClip":
                    btnAmmoClip.BackColor = colorPicker.Color;
                    settings.AmmoClip = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnAmmoReserve":
                    btnAmmoReserve.BackColor = colorPicker.Color;
                    settings.AmmoReserve = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnLowAmmoClip":
                    btnLowAmmoClip.BackColor = colorPicker.Color;
                    settings.AmmoClip = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnLowAmmoReserve":
                    btnLowAmmoReserve.BackColor = colorPicker.Color;
                    settings.AmmoReserve = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;
            }
        }

        private void cbHUDVersion_SelectedIndexChanged(object sender, EventArgs e)
        {
            settings.HUDVersion = false;
            if (cbHUDVersion.SelectedIndex > 0)
                settings.HUDVersion = true;
        }

        private void cbScoreboard_SelectedIndexChanged(object sender, EventArgs e)
        {
            settings.Scoreboard = false;
            if (cbScoreboard.SelectedIndex > 0)
                settings.Scoreboard = true;
        }

        private void cbDisguiseImage_CheckedChanged(object sender, EventArgs e)
        {
            settings.DisguiseImage = false;
            if (cbDisguiseImage.Checked)
                settings.DisguiseImage = true;
        }

        private void cbDefaultMenuBG_CheckedChanged(object sender, EventArgs e)
        {
            settings.DefaultMenuBG = false;
            if (cbDefaultMenuBG.Checked)
                settings.DefaultMenuBG = true;
        }

        private void rbChatBox_CheckedChanged(object sender, EventArgs e)
        {
            settings.ChatBox = false;
            if (rbChatBoxTop.Checked)
                settings.ChatBox = true;
        }

        private void rbTeamSelect_CheckedChanged(object sender, EventArgs e)
        {
            settings.TeamSelect = false;
            if (rbTeamSelectCenter.Checked)
                settings.TeamSelect = true;
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

        private void cbCrosshair_CheckedChanged(object sender, EventArgs e)
        {
            settings.Crosshair = false;
            if (cbCrosshair.Checked)
                settings.Crosshair = true;
        }

        private void cbXHairOutline_CheckedChanged(object sender, EventArgs e)
        {
            settings.XHairOutline = false;
            if (cbXHairOutline.Checked)
                settings.XHairOutline = true;
        }

        private void cbXHairPulse_CheckedChanged(object sender, EventArgs e)
        {
            settings.XHairPulse = false;
            if (cbXHairPulse.Checked)
                settings.XHairPulse = true;
        }

        private void txtXHairSize_TextChanged(object sender, EventArgs e)
        {
            if (txtXHairHeight.Value < 0 && txtXHairHeight.Value > 500)
                txtXHairHeight.Value = 200;
            else
                settings.XHairHeight = Convert.ToInt32(txtXHairHeight.Value);

            if (txtXHairWidth.Value < 0 && txtXHairWidth.Value > 500)
                txtXHairWidth.Value = 200;
            else
                settings.XHairWidth = Convert.ToInt32(txtXHairWidth.Value);
        }

        private void lbPlayerHealth_SelectedIndexChanged(object sender, EventArgs e)
        {
            settings.PlayerHealth = lbPlayerHealth.SelectedIndex + 1;
        }

        private void lbXHairStyles_SelectedIndexChanged(object sender, EventArgs e)
        {
            settings.XHairStyle = lbXHairStyles.SelectedItem.ToString();
            switch (settings.XHairStyle)
            {
                case "xHairCircle":
                    pbPreview2.ImageLocation = "https://i.imgur.com/vO6Q3KL.jpg";
                    LoadCrosshairSettings(33);
                    break;

                case "ScatterSpread":
                    pbPreview2.ImageLocation = "https://i.imgur.com/S7yWqL8.jpg";
                    LoadCrosshairSettings(50);
                    break;

                case "BasicCross":
                    pbPreview2.ImageLocation = "https://i.imgur.com/9FMR0oN.jpg";
                    LoadCrosshairSettings(68);
                    break;

                case "BasicCrossSmall":
                    pbPreview2.ImageLocation = "https://i.imgur.com/x9M3tZA.jpg";
                    LoadCrosshairSettings(85);
                    break;

                case "BasicCrossLarge":
                    pbPreview2.ImageLocation = "https://i.imgur.com/dUtMMpz.jpg";
                    LoadCrosshairSettings(102);
                    break;

                case "BasicDot":
                    pbPreview2.ImageLocation = "https://i.imgur.com/cM4B3Yq.jpg";
                    LoadCrosshairSettings(119);
                    break;

                case "CircleDot":
                    pbPreview2.ImageLocation = "https://i.imgur.com/yUDWwOU.jpg";
                    LoadCrosshairSettings(136);
                    break;

                case "ThinCircle":
                    pbPreview2.ImageLocation = "https://i.imgur.com/T8ovez4.jpg";
                    LoadCrosshairSettings(153);
                    break;

                case "WingsPlus":
                    pbPreview2.ImageLocation = "https://i.imgur.com/uonkSki.jpg";
                    LoadCrosshairSettings(170);
                    break;

                case "Wings":
                    pbPreview2.ImageLocation = "https://i.imgur.com/pOltRKf.jpg";
                    LoadCrosshairSettings(187);
                    break;

                case "WingsSmallDot":
                    pbPreview2.ImageLocation = "https://i.imgur.com/eGqDvF0.jpg";
                    LoadCrosshairSettings(204);
                    break;

                case "WingsSmall":
                    pbPreview2.ImageLocation = "https://i.imgur.com/eGqDvF0.jpg";
                    LoadCrosshairSettings(221);
                    break;

                case "OpenCross":
                    pbPreview2.ImageLocation = "https://i.imgur.com/Yc5H81Q.jpg";
                    LoadCrosshairSettings(238);
                    break;

                case "OpenCrossDot":
                    pbPreview2.ImageLocation = "https://i.imgur.com/YNLmuze.jpg";
                    LoadCrosshairSettings(255);
                    break;

                case "ThinCross":
                    pbPreview2.ImageLocation = "https://i.imgur.com/SzZkbaB.jpg";
                    LoadCrosshairSettings(272);
                    break;

                case "KonrWings":
                    pbPreview2.ImageLocation = "https://i.imgur.com/ym1WUsP.jpg";
                    LoadCrosshairSettings(289);
                    break;
            }
        }

        public void SetCrosshairSettings(int index)
        {
            var lines = File.ReadAllLines($"{TF2Directory}\\rayshud\\scripts\\hudlayout.res");
            if (settings.Crosshair)
            {
                lines[(index) - 1] = $"\"visible\"   \"1\"";
                lines[(index + 1) - 1] = $"\"enabled\"   \"1\"";
            }
            else
            {
                lines[(index) - 1] = $"\"visible\"   \"0\"";
                lines[(index) - 1] = $"\"enabled\"   \"0\"";
            }
            lines[(index + 5) - 1] = $"\"wide\"   \"{settings.XHairWidth}\"";
            lines[(index + 6) - 1] = $"\"tall\"   \"{settings.XHairHeight}\"";
        }

        public void LoadCrosshairSettings(int index)
        {
            var pattern = "\\\"(.*?)\\\"";
            var lines = File.ReadAllLines($"{TF2Directory}\\rayshud\\scripts\\hudlayout.res");
            cbCrosshair.Checked = lines[(index) - 1] == "\"visible\"  \"0\"";
            cbXHairOutline.Checked = lines[(index + 1) - 1] == "\"enabled\"    \"0\"";
            var matches = Regex.Matches(lines[(index + 5) - 1], pattern);
            txtXHairWidth.Value = Convert.ToInt32(matches[1].Value);
            matches = Regex.Matches(lines[(index + 6) - 1], pattern);
            txtXHairHeight.Value = Convert.ToInt32(matches[1].Value);
        }
    }

    public class HUDSettings
    {
        public bool HUDVersion { get; set; }        // false = modern, true = classic
        public bool Scoreboard { get; set; }        // false = normal, true = minimal
        public bool ChatBox { get; set; }           // false = top, true = bottom
        public bool TeamSelect { get; set; }        // false = left, true = center
        public bool DisguiseImage { get; set; }     // false = disabled, true = enabled
        public bool DefaultMenuBG { get; set; }     // false = disabled, true = enabled
        public int UberAnimation { get; set; }      // 1 = flash, 2 = solid, 3 = rainbow
        public string UberBarColor { get; set; }    // [0-255] [0-255] [0-255] 255
        public string UberFullColor { get; set; }   // [0-255] [0-255] [0-255] 255
        public string UberFlashColor1 { get; set; } // [0-255] [0-255] [0-255] 255
        public string UberFlashColor2 { get; set; } // [0-255] [0-255] [0-255] 255
        public string XHairStyle { get; set; }
        public bool Crosshair { get; set; }
        public bool XHairOutline { get; set; }      // false = disabled, true = enabled
        public bool XHairPulse { get; set; }        // false = disabled, true = enabled
        public int XHairHeight { get; set; }
        public int XHairWidth { get; set; }
        public string XHairColor { get; set; }      // [0-255] [0-255] [0-255] 255
        public string XHairPulseColor { get; set; } // [0-255] [0-255] [0-255] 255
        public int PlayerHealth { get; set; }       // 1 = default, 2 = teambar, 3 = cross, 4 = broeselcross
        public string HealingDone { get; set; }     // [0-255] [0-255] [0-255] 255
        public string NumColor { get; set; }        // [0-255] [0-255] [0-255] 255
        public string BuffNumColor { get; set; }    // [0-255] [0-255] [0-255] 255
        public string LowNumColor { get; set; }     // [0-255] [0-255] [0-255] 255
        public string AmmoClip { get; set; }        // [0-255] [0-255] [0-255] 255
        public string AmmoReserve { get; set; }     // [0-255] [0-255] [0-255] 255
        public string LowAmmoClip { get; set; }     // [0-255] [0-255] [0-255] 255
        public string LowAmmoReserve { get; set; }  // [0-255] [0-255] [0-255] 255
    }
}