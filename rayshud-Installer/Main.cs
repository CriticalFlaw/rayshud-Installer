using Newtonsoft.Json;
using SharpRaven;
using SharpRaven.Data;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Drawing.Text;

namespace FlawHUD_Installer
{
    public partial class Main : Form
    {
        private RavenClient ravenClient = new RavenClient("https://e3d4daa2995342d1aaca5827b95fce2d:23548ce9177049f5a1601ceafeb8c609@sentry.io/1190168");
        private RootObject settings = new RootObject();
        public string TF2Directory;

        public Main()
        {
            InitializeComponent();
            lblCrosshair.Parent = pbPreview;
            lblXHairSize.Parent = pbPreview;
            txtXHairSize.Parent = pbPreview;
            lblCrosshair.BackColor = Color.Transparent;
            lblXHairSize.BackColor = Color.Transparent;
            txtXHairSize.BackColor = Color.Transparent;
            PrivateFontCollection PFC = new PrivateFontCollection();
            GetLiveVersion();
        }

        private void GetLiveVersion()
        {
            var client = new WebClient();
            var textFromURL = client.DownloadString("https://raw.githubusercontent.com/raysfire/rayshud/installer/README.md");
            string[] textFromURLArray = textFromURL.Split('\n');
            txtLiveVersion.Text = textFromURLArray[textFromURLArray.Length - 2];
            CheckTF2Directory();
        }

        private void CheckTF2Directory()
        {
            try
            {
                if (File.Exists($"{Application.StartupPath}\\TempHUD.zip"))
                    File.Delete($"{Application.StartupPath}\\TempHUD.zip");
                if (Directory.Exists("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Team Fortress 2\\tf\\custom"))
                    TF2Directory = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Team Fortress 2\\tf\\custom";
                else if (Directory.Exists("C:\\Program Files\\Steam\\steamapps\\common\\Team Fortress 2\\tf\\custom"))
                    TF2Directory = "C:\\Program Files\\Steam\\steamapps\\common\\Team Fortress 2\\tf\\custom";
                else if (Directory.Exists("D:\\Program Files (x86)\\Steam\\steamapps\\common\\Team Fortress 2\\tf\\custom"))
                    TF2Directory = "D:\\Program Files (x86)\\Steam\\steamapps\\common\\Team Fortress 2\\tf\\custom";
                else if (Directory.Exists("D:\\Program Files\\Steam\\steamapps\\common\\Team Fortress 2\\tf\\custom"))
                    TF2Directory = "D:\\Program Files\\Steam\\steamapps\\common\\Team Fortress 2\\tf\\custom";
                else
                {
                    var DirectoryBrowser = new FolderBrowserDialog();
                    DirectoryBrowser.Description = ("Please select your tf/custom folder. Example: \nC:/Program Files (x86)/Steam/steamapps/common/Team Fortress 2/tf/custom");
                    var validHUDDirectory = false;
                    while (validHUDDirectory == false)
                    {
                        if (DirectoryBrowser.ShowDialog() == DialogResult.OK)
                        {
                            if (DirectoryBrowser.SelectedPath.Contains("custom"))    //("Steam\\steamapps\\common\\Team Fortress 2\\tf\\custom"))
                            {
                                TF2Directory = DirectoryBrowser.SelectedPath;
                                validHUDDirectory = true;
                            }
                        }
                        else
                            break;
                    }
                }

                if (string.IsNullOrWhiteSpace(TF2Directory))
                    txtDirectory.Text = "tf/custom directory not set! Click the browse button to set it before using the installer";
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
                MessageBox.Show($"An error occurred while attempting to check tf/custom directory. \n{ex.Message}", "Error: Checking tf/custom directory!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CheckHUDDirectory()
        {
            try
            {
                if ((Directory.Exists($"{TF2Directory}\\rayshud-installer")) && (Directory.Exists($"{TF2Directory}\\rayshud")))
                    Directory.Delete($"{TF2Directory}\\rayshud", true);

                if (Directory.Exists($"{TF2Directory}\\rayshud-installer"))
                    Directory.Move($"{TF2Directory}\\rayshud-installer", $"{TF2Directory}\\rayshud");

                if (Directory.Exists($"{TF2Directory}\\rayshud"))
                {
                    btnUninstall.Enabled = true;
                    btnSaveChanges.Enabled = true;
                    txtDirectory.Text = $"{TF2Directory}\\rayshud";
                    txtInstalledVersion.Text = File.ReadLines($"{TF2Directory}\\rayshud\\README.md").Last().ToString();
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
                    ReadSettingsFile();
                    DisplayHUDSettings();
                }
                else
                {
                    btnInstall.Text = "Install";
                    txtStatus.Text = "Not Installed";
                    btnUninstall.Enabled = false;
                    btnSaveChanges.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"An error occurred while attempting to find rayshud version numbers \n{ex.Message}", "Error: Checking rayshud Version Numbers!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DisplayHUDSettings()
        {
            try
            {
                btnUberBarColor.BackColor = Color.FromArgb(235, 226, 202);
                btnUberFullColor.BackColor = Color.FromArgb(255, 50, 255);
                btnUberFlashColor1.BackColor = Color.FromArgb(255, 165, 0);
                btnUberFlashColor2.BackColor = Color.FromArgb(255, 69, 0);

                // CROSSHAIR
                btnXHairColor.BackColor = Color.FromArgb(242, 242, 242);
                lblCrosshair.ForeColor = btnXHairColor.BackColor;
                btnXHairPulseColor.BackColor = Color.FromArgb(255, 0, 0);

                // HEALTH and AMMO
                btnHealingDone.BackColor = Color.FromArgb(48, 255, 48);
                btnHealthNormal.BackColor = Color.FromArgb(235, 226, 202);
                btnHealthBuff.BackColor = Color.FromArgb(48, 255, 48);
                btnHealthLow.BackColor = Color.FromArgb(255, 153, 0);
                btnAmmoClip.BackColor = Color.FromArgb(48, 255, 48);
                btnAmmoReserve.BackColor = Color.FromArgb(72, 255, 255);
                btnAmmoClipLow.BackColor = Color.FromArgb(255, 42, 130);
                btnAmmoReserveLow.BackColor = Color.FromArgb(255, 128, 28);

                txtLastModified.Text = settings.LastModified.ToString();

                // GENERAL
                if (settings.HUDVersion)
                    cbHUDVersion.SelectedIndex = 1;
                else
                    cbHUDVersion.SelectedIndex = 0;

                if (settings.Scoreboard)
                    cbScoreboard.SelectedIndex = 1;
                else
                    cbScoreboard.SelectedIndex = 0;

                if (settings.ChatBox)
                    rbChatBoxTop.Checked = true;
                else
                    rbChatBoxBottom.Checked = true;

                if (settings.TeamSelect)
                    rbTeamSelectCenter.Checked = true;
                else
                    rbTeamSelectLeft.Checked = true;

                cbDisguiseImage.Checked = settings.DisguiseImage;

                cbDefaultMenuBG.Checked = settings.DefaultMenuBG;

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
                //lbXHairStyles.SelectedIndex = settings.XHairStyle - 1;

                cbXHairEnabled.Checked = settings.XHairEnabled;

                cbXHairOutline.Checked = settings.XHairOutline;

                cbXHairPulse.Checked = settings.XHairPulse;

                txtXHairSize.Text = settings.XHairSize;

                split = settings.XHairColor.Split(null);
                btnXHairColor.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));
                lblCrosshair.ForeColor = btnXHairColor.BackColor;

                split = settings.XHairPulseColor.Split(null);
                btnXHairPulseColor.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                // HEALTH and AMMO
                //lbHealthStyle.SelectedIndex = settings.HealthStyle - 1;

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
                MessageBox.Show($"An error occured when loading settings \n{ex.Message}", "Error: Loading Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string[] GetHUDSettingsList(string directory)
        {
            string[] lines = File.ReadAllLines(directory);
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].Replace("\"", string.Empty);
                lines[i] = lines[i].Replace("\t", string.Empty);
                lines[i] = lines[i].Replace("ypos", string.Empty);
                lines[i] = lines[i].Trim('"');
            }
            return lines;
        }

        public void UpdateSettingsFile()
        {
            WriteToSettings("HUDVersion", settings.HUDVersion.ToString());
            WriteToSettings("Scoreboard", settings.Scoreboard.ToString());
            WriteToSettings("ChatBox", settings.ChatBox.ToString());
            WriteToSettings("TeamSelect", settings.TeamSelect.ToString());
            WriteToSettings("DisguiseImage", settings.DisguiseImage.ToString());
            WriteToSettings("DefaultMenuBG", settings.DefaultMenuBG.ToString());
            WriteToSettings("UberAnimation", settings.UberAnimation.ToString());
            WriteToSettings("UberBarColor", settings.UberBarColor.ToString());
            WriteToSettings("UberFullColor", settings.UberFullColor.ToString());
            WriteToSettings("UberFlashColor1", settings.UberFlashColor1.ToString());
            WriteToSettings("UberFlashColor2", settings.UberFlashColor2.ToString());
            WriteToSettings("XHairEnabled", settings.XHairEnabled.ToString());
            WriteToSettings("XHairStyle", settings.XHairStyle.ToString());
            WriteToSettings("XHairOutline", settings.XHairOutline.ToString());
            WriteToSettings("XHairPulse", settings.XHairPulse.ToString());
            //WriteToSettings("XHairSize", settings.XHairSize.ToString());
            WriteToSettings("XHairColor", settings.XHairColor.ToString());
            WriteToSettings("XHairPulseColor", settings.XHairPulseColor.ToString());
            WriteToSettings("HealingDone", settings.HealingDone.ToString());
            WriteToSettings("HealthStyle", settings.HealthStyle.ToString());
            WriteToSettings("HealthNormal", settings.HealthNormal.ToString());
            WriteToSettings("HealthBuff", settings.HealthBuff.ToString());
            WriteToSettings("HealthLow", settings.HealthLow.ToString());
            WriteToSettings("AmmoClip", settings.AmmoClip.ToString());
            WriteToSettings("AmmoReserve", settings.AmmoReserve.ToString());
            WriteToSettings("AmmoClipLow", settings.AmmoClipLow.ToString());
            WriteToSettings("AmmoReserveLow", settings.AmmoReserveLow.ToString());
            WriteToSettings("LastModified", DateTime.Now.ToString());
            txtLastModified.Text = DateTime.Now.ToString();
        }

        private void WriteToSettings(string setting, string value)
        {
            string json = File.ReadAllText($"{TF2Directory}\\rayshud\\customizations\\settings.json");
            dynamic jsonObj = JsonConvert.DeserializeObject(json);
            jsonObj[setting] = value;
            string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
            File.WriteAllText($"{TF2Directory}\\rayshud\\customizations\\settings.json", output);
        }

        public void ReadSettingsFile()
        {
            using (StreamReader reader = new StreamReader($"{TF2Directory}\\rayshud\\customizations\\settings.json"))
            {
                string json = reader.ReadToEnd();
                settings = JsonConvert.DeserializeObject<RootObject>(json);
            }
        }

        private void btnInstall_Click(object sender, EventArgs e)
        {
            try
            {
                // Remove the downloaded file from the temporary location
                if (File.Exists($"{Application.StartupPath}\\TempHUD.zip"))
                    File.Delete($"{Application.StartupPath}\\TempHUD.zip");
                if (File.Exists($"{TF2Directory}\\rayshud\\customizations\\settings.json"))
                    File.Copy($"{TF2Directory}\\rayshud\\customizations\\settings.json", $"{Application.StartupPath}\\settings.json", true);

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
                if (File.Exists($"{Application.StartupPath}\\settings.json"))
                {
                    File.Copy($"{Application.StartupPath}\\settings.json", $"{TF2Directory}\\rayshud\\customizations\\settings.json", true);
                    File.Delete($"{Application.StartupPath}\\settings.json");
                }
                CheckHUDDirectory();
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"An error occurred while attempting to download rayshud \n{ex.Message}", $"Error: Downloading rayshud!", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    MessageBox.Show($"Finished Uninstalling rayshud...", "rayshud Uninstalled!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtDirectory.Text = TF2Directory;
                    txtInstalledVersion.Text = "...";
                    txtLastModified.Text = "...";
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
            UpdateSettingsFile();

            var colorScheme = $"{TF2Directory}\\rayshud\\resource\\scheme\\clientscheme_colors.res";
            var animations = $"{TF2Directory}\\rayshud\\scripts\\hudanimations_custom.txt";
            var chat = $"{TF2Directory}\\rayshud\\resource\\ui\\basechat.res";
            var layout = $"{TF2Directory}\\rayshud\\scripts\\hudlayout.res";

            if (settings.HUDVersion)
            {
                if (Directory.Exists($"{TF2Directory}\\rayshud\\materials\\console_off"))
                {
                    File.Copy($"{TF2Directory}\\rayshud\\customizations\\Main Menu\\Classic\\materials\\console\\background_upward.vtf", $"{TF2Directory}\\rayshud\\materials\\console_off\\background_upward.vtf", true);
                    File.Copy($"{TF2Directory}\\rayshud\\customizations\\Main Menu\\Classic\\materials\\console\\background_upward_widescreen.vtf", $"{TF2Directory}\\rayshud\\materials\\console_off\\background_upward_widescreen.vtf", true);
                }
                else
                {
                    File.Copy($"{TF2Directory}\\rayshud\\customizations\\Main Menu\\Classic\\materials\\console\\background_upward.vtf", $"{TF2Directory}\\rayshud\\materials\\console\\background_upward.vtf", true);
                    File.Copy($"{TF2Directory}\\rayshud\\customizations\\Main Menu\\Classic\\materials\\console\\background_upward_widescreen.vtf", $"{TF2Directory}\\rayshud\\materials\\console\\background_upward_widescreen.vtf", true);
                }
                File.Copy($"{TF2Directory}\\rayshud\\customizations\\Main Menu\\Classic\\resource\\ui\\mainmenuoverride.res", $"{TF2Directory}\\rayshud\\resource\\ui\\mainmenuoverride.res", true);
                File.Copy($"{TF2Directory}\\rayshud\\customizations\\Main Menu\\Classic\\resource\\gamemenu.res", $"{TF2Directory}\\rayshud\\resource\\gamemenu.res", true);
            }
            else
            {
                if (Directory.Exists($"{TF2Directory}\\rayshud\\materials\\console_off"))
                {
                    File.Copy($"{TF2Directory}\\rayshud\\customizations\\Main Menu\\Modern\\materials\\console\\background_upward.vtf", $"{TF2Directory}\\rayshud\\materials\\console_off\\background_upward.vtf", true);
                    File.Copy($"{TF2Directory}\\rayshud\\customizations\\Main Menu\\Modern\\materials\\console\\background_upward_widescreen.vtf", $"{TF2Directory}\\rayshud\\materials\\console_off\\background_upward_widescreen.vtf", true);
                }
                else
                {
                    File.Copy($"{TF2Directory}\\rayshud\\customizations\\Main Menu\\Modern\\materials\\console\\background_upward.vtf", $"{TF2Directory}\\rayshud\\materials\\console\\background_upward.vtf", true);
                    File.Copy($"{TF2Directory}\\rayshud\\customizations\\Main Menu\\Modern\\materials\\console\\background_upward_widescreen.vtf", $"{TF2Directory}\\rayshud\\materials\\console\\background_upward_widescreen.vtf", true);
                }
                File.Copy($"{TF2Directory}\\rayshud\\customizations\\Main Menu\\Modern\\resource\\ui\\mainmenuoverride.res", $"{TF2Directory}\\rayshud\\resource\\ui\\mainmenuoverride.res", true);
                File.Copy($"{TF2Directory}\\rayshud\\customizations\\Main Menu\\Modern\\resource\\gamemenu.res", $"{TF2Directory}\\rayshud\\resource\\gamemenu.res", true);
            }

            if (settings.Scoreboard)
                File.Copy($"{TF2Directory}\\rayshud\\customizations\\Scoreboard\\scoreboard-minimal.res", $"{TF2Directory}\\rayshud\\resource\\ui\\scoreboard.res", true);
            else
                File.Copy($"{TF2Directory}\\rayshud\\customizations\\Scoreboard\\scoreboard-default.res", $"{TF2Directory}\\rayshud\\resource\\ui\\scoreboard.res", true);

            if (settings.DefaultMenuBG)
            {
                if (Directory.Exists($"{TF2Directory}\\rayshud\\materials\\console"))
                {
                    Directory.Move($"{TF2Directory}\\rayshud\\materials\\console", $"{TF2Directory}\\rayshud\\materials\\console_off");
                    File.Move($"{TF2Directory}\\rayshud\\scripts\\chapterbackgrounds.txt", $"{TF2Directory}\\rayshud\\scripts\\chapterbackgrounds_off.txt");
                }
            }
            else
            {
                if ((Directory.Exists($"{TF2Directory}\\rayshud\\materials\\console_off")) && (File.Exists($"{TF2Directory}\\rayshud\\scripts\\chapterbackgrounds_off.txt")))
                {
                    Directory.Move($"{TF2Directory}\\rayshud\\materials\\console_off", $"{TF2Directory}\\rayshud\\materials\\console");
                    File.Move($"{TF2Directory}\\rayshud\\scripts\\chapterbackgrounds_off.txt", $"{TF2Directory}\\rayshud\\scripts\\chapterbackgrounds.txt");
                }
            }

            if (settings.TeamSelect)
            {
                File.Copy($"{TF2Directory}\\rayshud\\customizations\\Team Menu\\Teammenu-center.res", $"{TF2Directory}\\rayshud\\resource\\ui\\Teammenu.res", true);
                File.Copy($"{TF2Directory}\\rayshud\\customizations\\Team Menu\\ClassSelection-center.res", $"{TF2Directory}\\rayshud\\resource\\ui\\ClassSelection.res", true);
            }
            else
            {
                File.Copy($"{TF2Directory}\\rayshud\\customizations\\Team Menu\\Teammenu-left.res", $"{TF2Directory}\\rayshud\\resource\\ui\\Teammenu.res", true);
                File.Copy($"{TF2Directory}\\rayshud\\customizations\\Team Menu\\ClassSelection-left.res", $"{TF2Directory}\\rayshud\\resource\\ui\\ClassSelection.res", true);
            }

            if (settings.HealthStyle == 1)
                File.Copy($"{TF2Directory}\\rayshud\\customizations\\Player Health\\HudPlayerHealth-default.res", $"{TF2Directory}\\rayshud\\resource\\ui\\HudPlayerHealth.res", true);
            else if (settings.HealthStyle == 2)
                File.Copy($"{TF2Directory}\\rayshud\\customizations\\Player Health\\HudPlayerHealth-teambar.res", $"{TF2Directory}\\rayshud\\resource\\ui\\HudPlayerHealth.res", true);
            else if (settings.HealthStyle == 3)
                File.Copy($"{TF2Directory}\\rayshud\\customizations\\Player Health\\HudPlayerHealth-cross.res", $"{TF2Directory}\\rayshud\\resource\\ui\\HudPlayerHealth.res", true);
            else if (settings.HealthStyle == 4)
                File.Copy($"{TF2Directory}\\rayshud\\customizations\\Player Health\\HudPlayerHealth-broesel.res", $"{TF2Directory}\\rayshud\\resource\\ui\\HudPlayerHealth.res", true);
            //-------------------------------------------------------------------------

            string[] lines = File.ReadAllLines(animations);
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
            File.WriteAllLines(animations, lines);

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

            if (settings.UberAnimation == 1)
            {
                lines[104 - 1] = $"\t{lines[104 - 1].Replace("//", string.Empty).Trim()}";
                lines[105 - 1] = $"\t//{lines[105 - 1].Trim()}";
                lines[106 - 1] = $"\t//{lines[106 - 1].Trim()}";
            }
            else if (settings.UberAnimation == 2)
            {
                lines[104 - 1] = $"\t//{lines[104 - 1].Trim()}";
                lines[105 - 1] = $"\t{lines[105 - 1].Replace("//", string.Empty).Trim()}";
                lines[106 - 1] = $"\t//{lines[106 - 1].Trim()}";
            }
            else if (settings.UberAnimation == 3)
            {
                lines[104 - 1] = $"\t//{lines[104 - 1].Trim()}";
                lines[105 - 1] = $"\t//{lines[105 - 1].Trim()}";
                lines[106 - 1] = $"\t{lines[106 - 1].Replace("//", string.Empty).Trim()}";
            }

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
            //-------------------------------------------------------------------------

            lines = File.ReadAllLines(chat);
            if (settings.ChatBox)
                lines[10 - 1] = $"\t\t\"ypos\"\t\t\t\t\"20\"";
            else
                lines[10 - 1] = $"\t\t\"ypos\"\t\t\t\t\"360\"";
            File.WriteAllLines(chat, lines);

            lines = File.ReadAllLines(layout);
            switch (settings.XHairStyle)
            {
                case 1: //xHairCircle
                    SetCrosshairSettings(33);
                    break;

                case 2: //ScatterSpread
                    SetCrosshairSettings(50);
                    break;

                case 3: //BasicCross
                    SetCrosshairSettings(68);
                    break;

                case 4: //BasicCrossSmall
                    SetCrosshairSettings(85);
                    break;

                case 5: //BasicCrossLarge
                    SetCrosshairSettings(102);
                    break;

                case 6: //BasicDot
                    SetCrosshairSettings(119);
                    break;

                case 7: //CircleDot
                    SetCrosshairSettings(136);
                    break;

                case 8: //ThinCircle
                    SetCrosshairSettings(153);
                    break;

                case 9: //WingsPlus
                    SetCrosshairSettings(170);
                    break;

                case 10:    //Wings
                    SetCrosshairSettings(187);
                    break;

                case 11:    //WingsSmallDot
                    SetCrosshairSettings(204);
                    break;

                case 12:    //WingsSmall
                    SetCrosshairSettings(221);
                    break;

                case 13:    //OpenCross
                    SetCrosshairSettings(238);
                    break;

                case 14:    //OpenCrossDot
                    SetCrosshairSettings(255);
                    break;

                case 15:    //ThinCross
                    SetCrosshairSettings(272);
                    break;

                case 16:    //KonrWings
                    SetCrosshairSettings(289);
                    break;
            }
            // Control: cbXHairOutline
            // Location: scripts\hudlayout.res
            // Task: Add or remove Outline to the font name
            File.WriteAllLines(layout, lines);
            //-------------------------------------------------------------------------

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

            MessageBox.Show($"rayshud changes applied", "Changes Applied!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SetCrosshairSettings(int index)
        {
            var lines = File.ReadAllLines($"{TF2Directory}\\rayshud\\scripts\\hudlayout.res");
            if (settings.XHairEnabled)
            {
                lines[(index) - 1] = $"\t\t\"visible\"   \"1\"";
                lines[(index + 1) - 1] = $"\t\t\"enabled\"   \"1\"";
            }
            else
            {
                lines[(index) - 1] = $"\t\t\"visible\"   \"0\"";
                lines[(index) - 1] = $"\t\t\"enabled\"   \"0\"";
            }
            lines[(index + 7) - 1] = $"\t\t\"font\"   \"Crosshairs{settings.XHairSize}\"";
        }

        private void btnPlayTF2_Click(object sender, EventArgs e)
        {
            Process.Start("steam://rungameid/440");
            Application.Exit();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            // Prompt the user to provide the tf/custom directory themselves
            var IsDirectoryValid = false;
            var DirectoryBrowser = new FolderBrowserDialog();
            DirectoryBrowser.Description = ("Please select your tf/custom folder. Example: \nC:/Program Files (x86)/Steam/steamapps/common/Team Fortress 2/tf/custom");
            while (IsDirectoryValid == false)
            {
                // Until the correct path is provided or the user clicks 'Cancel' - keep prompting for a valid tf/custom directory.
                if (DirectoryBrowser.ShowDialog() == DialogResult.OK)
                {
                    if (DirectoryBrowser.SelectedPath.Contains("custom"))   //if (DirectoryBrowser.SelectedPath.Contains("Steam\\steamapps\\common\\Team Fortress 2\\tf\\custom"))
                    {
                        TF2Directory = DirectoryBrowser.SelectedPath;
                        txtDirectory.Text = TF2Directory;
                        btnInstall.Enabled = true;
                        btnPlayTF2.Enabled = true;
                        CheckHUDDirectory();
                        IsDirectoryValid = true;
                    }
                }
                else
                    break;
            }
        }

        private void btnColorPicker_Click(object sender, EventArgs e)
        {
            // Bring up the color picker dialog
            ColorDialog colorPicker = new ColorDialog();
            if (colorPicker.ShowDialog() == DialogResult.OK)
            {
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
        }

        private void cbHUDVersion_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbHUDVersion.SelectedIndex > 0)
                settings.HUDVersion = true;
            else
                settings.HUDVersion = false;
        }

        private void cbScoreboard_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbScoreboard.SelectedIndex > 0)
                settings.Scoreboard = true;
            else
                settings.Scoreboard = false;
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

        private void txtXHairSize_TextChanged(object sender, EventArgs e)
        {
            settings.XHairSize = txtXHairSize.Text;
        }

        private void lbPlayerHealth_SelectedIndexChanged(object sender, EventArgs e)
        {
            settings.HealthStyle = lbHealthStyle.SelectedIndex + 1;
        }

        private void lbXHairStyles_SelectedIndexChanged(object sender, EventArgs e)
        {
            settings.XHairStyle = lbXHairStyles.SelectedIndex + 1;
            switch (settings.XHairStyle)
            {
                case 1:
                    lblCrosshair.Text = "2";
                    txtXHairSize.Text = "26";
                    break;

                case 2:
                    lblCrosshair.Text = "2";
                    txtXHairSize.Text = "32";
                    break;

                case 3:
                    lblCrosshair.Text = "2";
                    txtXHairSize.Text = "18";
                    break;

                case 4:
                    lblCrosshair.Text = "3";
                    txtXHairSize.Text = "24";
                    break;

                case 5:
                    lblCrosshair.Text = "8";
                    txtXHairSize.Text = "34";
                    break;

                case 6:
                    lblCrosshair.Text = "i";
                    txtXHairSize.Text = "24";
                    break;

                case 7:
                    lblCrosshair.Text = "i";
                    txtXHairSize.Text = "24";
                    break;

                case 8:
                    lblCrosshair.Text = "h";
                    txtXHairSize.Text = "24";
                    break;

                case 9:
                    lblCrosshair.Text = "0";
                    txtXHairSize.Text = "32";
                    break;

                case 10:
                    lblCrosshair.Text = "9";
                    txtXHairSize.Text = "34";
                    break;

                case 11:
                    lblCrosshair.Text = "+";
                    txtXHairSize.Text = "24";
                    break;

                case 12:
                    lblCrosshair.Text = "d";
                    txtXHairSize.Text = "34";
                    break;

                case 13:
                    lblCrosshair.Text = "c";
                    txtXHairSize.Text = "34";
                    break;

                case 14:
                    lblCrosshair.Text = "g";
                    txtXHairSize.Text = "34";
                    break;

                case 15:
                    lblCrosshair.Text = "o";
                    txtXHairSize.Text = "34";
                    break;
            }
            settings.XHairSize = txtXHairSize.Text;
            //lblCrosshair.Font = new Font(lblCrosshair.Font.FontFamily, Convert.ToUInt32(cbXHairSize.SelectedValue);
        }

        private void btnOpenDirectory_Click(object sender, EventArgs e)
        {
            if (Directory.Exists($"{TF2Directory}\\rayshud"))
                Process.Start("explorer.exe", $"{TF2Directory}\\rayshud");
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
        public string XHairSize { get; set; }
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
