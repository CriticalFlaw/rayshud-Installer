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
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace FlawHUD_Installer
{
    public partial class Main : Form
    {
        private RavenClient ravenClient = new RavenClient("https://e3d4daa2995342d1aaca5827b95fce2d:23548ce9177049f5a1601ceafeb8c609@sentry.io/1190168");
        private Settings settings = new Settings();
        private SettingsList settingsList = new SettingsList();
        private bool validTFDirectory, validHUDDirectory;
        public string TF2Directory;

        public Main()
        {
            InitializeComponent();
            GetLiveVersion();
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
                    validTFDirectory = true;
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
                if (Directory.Exists($"{TF2Directory}\\rayshud-installer"))
                    Directory.Move($"{TF2Directory}\\rayshud-installer", $"{TF2Directory}\\rayshud");
                if (Directory.Exists($"{TF2Directory}\\rayshud"))
                {
                    btnUninstall.Enabled = true;
                    btnSaveChanges.Enabled = true;
                    txtDirectory.Text = $"{TF2Directory}\rayshud";
                    txtInstalledVersion.Text = File.ReadLines($"{TF2Directory}\\rayshud\\README.md").Last().ToString();
                    if (txtInstalledVersion.ToString().Trim() == txtLiveVersion.ToString().Trim())
                        btnInstall.Text = "Refresh";
                    else
                        btnInstall.Text = "Update";
                    SetHUDSettings();
                    LoadHUDSettings();
                }
                else
                {
                    btnInstall.Text = "Install";
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

        private void SetHUDSettings()
        {
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
            btnUberBarColor.BackColor = Color.FromArgb(235, 226, 202);
            btnUberFullColor.BackColor = Color.FromArgb(255, 50, 255);
            btnUberFlashColor1.BackColor = Color.FromArgb(255, 165, 0);
            btnUberFlashColor2.BackColor = Color.FromArgb(255, 69, 0);

            // CROSSHAIR
            cbXHairEnabled.Checked = false;
            cbXHairOutline.Checked = false;
            cbXHairPulse.Checked = true;
            txtXHairHeight.Value = 200;
            txtXHairWidth.Value = 200;
            btnXHairColor.BackColor = Color.FromArgb(242, 242, 242);
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
        }

        private void LoadHUDSettings()
        {
            try
            {
                SetHUDSettings();

                var pattern = @"\d+";
                settings.HUDVersion = ReadFromSettings("HUDVersion") == "classic";   // modern
                settings.Scoreboard = ReadFromSettings("Scoreboard") == "minimal";   // normal

                //if (!File.Exists($"{TF2Directory}\\rayshud\\resource\\ui\\Teammenu-center.res"))
                //    settings.TeamSelect = true;
                //if (Directory.Exists($"{TF2Directory}\\rayshud\\materials\\console-default"))
                //    settings.DefaultMenuBG = true;
                //if (!File.Exists($"{TF2Directory}\\rayshud\\resource\\ui\\HudPlayerHealth-teambar.res"))
                //    settings.HealthStyle = 2;
                //else if (!File.Exists($"{TF2Directory}\\rayshud\\resource\\ui\\HudPlayerHealth-cross.res"))
                //    settings.HealthStyle = 3;
                //else if (!File.Exists($"{TF2Directory}\\rayshud\\resource\\ui\\HudPlayerHealth-broeselcross.res"))
                //    settings.HealthStyle = 4;
                //else
                //    settings.HealthStyle = 1;

                //string[] lines = GetHUDSettingsList($"{TF2Directory}\\rayshud\\resource\\ui\\basechat.res");
                //var matches = Regex.Matches(lines[10 - 1], pattern);
                //if (int.Parse(matches[0].Groups[0].Value) > 50)
                //    settings.ChatBox = true;

                //lines = GetHUDSettingsList($"{TF2Directory}\\rayshud\\scripts\\hudanimations_custom.txt");
                //if (lines[87 - 1].StartsWith("//"))
                //    settings.DisguiseImage = false;
                //if (lines[104 - 1].StartsWith("//") && lines[105 - 1].StartsWith("//"))
                //    settings.UberAnimation = 3;
                //else if (lines[104 - 1].StartsWith("//") && lines[106 - 1].StartsWith("//"))
                //    settings.UberAnimation = 2;
                //else
                //    settings.UberAnimation = 1;

                //lines = GetHUDSettingsList($"{TF2Directory}\\rayshud\\resource\\scheme\\clientscheme_colors.res");
                //matches = Regex.Matches(lines[7 - 1], pattern);
                //settings.AmmoClip = matches[1].Value;
                //matches = Regex.Matches(lines[8 - 1], pattern);
                //settings.AmmoReserve = matches[1].Value;
                //matches = Regex.Matches(lines[9 - 1], pattern);
                //settings.AmmoClipLow = matches[1].Value;
                //matches = Regex.Matches(lines[10 - 1], pattern);
                //settings.UberBarColor = matches[1].Value;
                //matches = Regex.Matches(lines[23 - 1], pattern);
                //settings.HealthNormal = matches[1].Value;
                //matches = Regex.Matches(lines[24 - 1], pattern);
                //settings.HealthBuff = matches[1].Value;
                //matches = Regex.Matches(lines[25 - 1], pattern);
                //settings.HealthLow = matches[1].Value;
                //matches = Regex.Matches(lines[32 - 1], pattern);
                //settings.AmmoReserveLow = matches[1].Value;
                //matches = Regex.Matches(lines[35 - 1], pattern);
                //settings.UberFullColor = matches[1].Value;
                //matches = Regex.Matches(lines[37 - 1], pattern);
                //settings.UberFlashColor1 = matches[1].Value;
                //matches = Regex.Matches(lines[38 - 1], pattern);
                //settings.UberFlashColor2 = matches[1].Value;
                //matches = Regex.Matches(lines[41 - 1], pattern);
                //settings.HealingDone = matches[1].Value;
                //matches = Regex.Matches(lines[45 - 1], pattern);
                //settings.XHairColor = matches[1].Value;
                //matches = Regex.Matches(lines[46 - 1], pattern);
                //settings.XHairPulseColor = matches[1].Value;

                DisplayHUDSettings();
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"An error occured when loading settings \n{ex.Message}", "Error: Loading Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DisplayHUDSettings()
        {
            try
            {
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
                if (settings.XHairEnabled)
                    cbXHairEnabled.Checked = true;
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
                lbHealthStyle.SelectedIndex = settings.HealthStyle;
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

        private void GetLiveVersion()
        {
            var client = new WebClient();
            var textFromURL = client.DownloadString("https://raw.githubusercontent.com/raysfire/rayshud/installer/README.md");
            string[] textFromURLArray = textFromURL.Split('\n');
            txtLiveVersion.Text = textFromURLArray[textFromURLArray.Length - 2];
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
            if (settings.HUDVersion)
                WriteToSettings("HUDVersion", "classic");
            else
                WriteToSettings("HUDVersion", "modern");

            if (settings.Scoreboard)
                WriteToSettings("Scoreboard", "minimal");
            else
                WriteToSettings("Scoreboard", "standard");
        }

        private void WriteToSettings(string setting, string value)
        {
            string json = File.ReadAllText($"{TF2Directory}\\rayshud\\customizations\\settings.json");
            dynamic jsonObj = JsonConvert.DeserializeObject(json);
            jsonObj[setting] = value;
            string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
            File.WriteAllText($"{TF2Directory}\\rayshud\\customizations\\settings.json", output);
        }

        public string ReadFromSettings(string query)
        {
            string json;
            using (var stream = new StreamReader(File.OpenRead($"{TF2Directory}\\rayshud\\customizations\\settings.json"), new UTF8Encoding(false)))
                json = stream.ReadToEnd();
            switch (query)
            {
                case "HUDVersion":
                    return JsonConvert.DeserializeObject<SettingsList>(json).HUDVersion;

                case "Scoreboard":
                    return JsonConvert.DeserializeObject<SettingsList>(json).Scoreboard;

                case "ChatBox":
                    return JsonConvert.DeserializeObject<SettingsList>(json).ChatBox;

                case "TeamSelect":
                    return JsonConvert.DeserializeObject<SettingsList>(json).TeamSelect;

                case "DisguiseImage":
                    return JsonConvert.DeserializeObject<SettingsList>(json).DisguiseImage;

                case "DefaultMenuBG":
                    return JsonConvert.DeserializeObject<SettingsList>(json).DefaultMenuBG;

                case "UberAnimation":
                    return JsonConvert.DeserializeObject<SettingsList>(json).UberAnimation;

                case "UberBarColor":
                    return JsonConvert.DeserializeObject<SettingsList>(json).UberBarColor;

                case "UberFullColor":
                    return JsonConvert.DeserializeObject<SettingsList>(json).UberFullColor;

                case "UberFlashColor1":
                    return JsonConvert.DeserializeObject<SettingsList>(json).UberFlashColor1;

                case "UberFlashColor2":
                    return JsonConvert.DeserializeObject<SettingsList>(json).UberFlashColor2;

                case "XHairStyle":
                    return JsonConvert.DeserializeObject<SettingsList>(json).XHairStyle;

                case "XHairEnabled":
                    return JsonConvert.DeserializeObject<SettingsList>(json).XHairEnabled;

                case "XHairOutline":
                    return JsonConvert.DeserializeObject<SettingsList>(json).XHairOutline;

                case "XHairPulse":
                    return JsonConvert.DeserializeObject<SettingsList>(json).XHairPulse;

                case "XHairHeight":
                    return JsonConvert.DeserializeObject<SettingsList>(json).XHairHeight;

                case "XHairWidth":
                    return JsonConvert.DeserializeObject<SettingsList>(json).XHairWidth;

                case "XHairColor":
                    return JsonConvert.DeserializeObject<SettingsList>(json).XHairColor;

                case "XHairPulseColor":
                    return JsonConvert.DeserializeObject<SettingsList>(json).XHairPulseColor;

                case "HealthStyle":
                    return JsonConvert.DeserializeObject<SettingsList>(json).HealthStyle;

                case "HealingDone":
                    return JsonConvert.DeserializeObject<SettingsList>(json).HealingDone;

                case "HealthNormal":
                    return JsonConvert.DeserializeObject<SettingsList>(json).HealthNormal;

                case "HealthBuff":
                    return JsonConvert.DeserializeObject<SettingsList>(json).HealthBuff;

                case "HealthLow":
                    return JsonConvert.DeserializeObject<SettingsList>(json).HealthLow;

                case "AmmoClip":
                    return JsonConvert.DeserializeObject<SettingsList>(json).AmmoClip;

                case "AmmoClipLow":
                    return JsonConvert.DeserializeObject<SettingsList>(json).AmmoClipLow;

                case "AmmoReserve":
                    return JsonConvert.DeserializeObject<SettingsList>(json).AmmoReserve;

                case "AmmoReserveLow":
                    return JsonConvert.DeserializeObject<SettingsList>(json).AmmoReserveLow;

                default:
                    return null;
            }
        }

        private void SetCrosshairSettings(int index)
        {
            var lines = File.ReadAllLines($"{TF2Directory}\\rayshud\\scripts\\hudlayout.res");
            if (settings.XHairEnabled)
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

        private void LoadCrosshairSettings(int index)
        {
            if (validHUDDirectory)
            {
                var pattern = "\\\"(.*?)\\\"";
                var lines = File.ReadAllLines($"{TF2Directory}\\rayshud\\scripts\\hudlayout.res");
                cbXHairEnabled.Checked = lines[(index) - 1] == "\"visible\"  \"0\"";
                cbXHairOutline.Checked = lines[(index + 1) - 1] == "\"enabled\"    \"0\"";
                var matches = Regex.Matches(lines[(index + 5) - 1], pattern);
                txtXHairWidth.Value = Convert.ToInt32(matches[1].Value);
                matches = Regex.Matches(lines[(index + 6) - 1], pattern);
                txtXHairHeight.Value = Convert.ToInt32(matches[1].Value);
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
            UpdateSettingsFile();

            var colorScheme = $"{TF2Directory}\\rayshud\\resource\\scheme\\clientscheme_colors.res";
            var animations = $"{TF2Directory}\\rayshud\\scripts\\hudanimations_custom.txt";
            var chat = $"{TF2Directory}\\rayshud\\resource\\ui\\basechat.res";
            var layout = $"{TF2Directory}\\rayshud\\scripts\\hudlayout.res";

            if (settings.HUDVersion)
            {
                File.Copy($"{TF2Directory}\\rayshud\\customizations\\Main Menu\\Classic\\materials\\console\\background_upward.vtf", $"{TF2Directory}\\rayshud\\materials\\console\\background_upward.vtf", true);
                File.Copy($"{TF2Directory}\\rayshud\\customizations\\Main Menu\\Classic\\materials\\console\\background_upward_widescreen.vtf", $"{TF2Directory}\\rayshud\\materials\\console\\background_upward_widescreen.vtf", true);
            }
            else
            {
                File.Copy($"{TF2Directory}\\rayshud\\customizations\\Main Menu\\Modern\\materials\\console\\background_upward.vtf", $"{TF2Directory}\\rayshud\\materials\\console\\background_upward.vtf", true);
                File.Copy($"{TF2Directory}\\rayshud\\customizations\\Main Menu\\Modern\\materials\\console\\background_upward_widescreen.vtf", $"{TF2Directory}\\rayshud\\materials\\console\\background_upward_widescreen.vtf", true);
            }

            if (settings.Scoreboard)
                File.Copy($"{TF2Directory}\\rayshud\\customizations\\Scoreboard\\scoreboard-minimal.res", $"{TF2Directory}\\rayshud\\resource\\ui\\scoreboard.res", true);
            else
                File.Copy($"{TF2Directory}\\rayshud\\customizations\\Scoreboard\\scoreboard-default.res", $"{TF2Directory}\\rayshud\\resource\\ui\\scoreboard.res", true);

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
            {
                File.Move($"{TF2Directory}\\rayshud\\customizations\\Team Menu\\Teammenu-center.res", $"{TF2Directory}\\rayshud\\resource\\ui\\Teammenu.res");
                File.Move($"{TF2Directory}\\rayshud\\customizations\\Team Menu\\ClassSelection-center.res", $"{TF2Directory}\\rayshud\\resource\\ui\\ClassSelection.res");
            }
            else
            {
                File.Move($"{TF2Directory}\\rayshud\\customizations\\Team Menu\\Teammenu-left.res", $"{TF2Directory}\\rayshud\\resource\\ui\\Teammenu.res");
                File.Move($"{TF2Directory}\\rayshud\\customizations\\Team Menu\\ClassSelection-left.res", $"{TF2Directory}\\rayshud\\resource\\ui\\Teammenu.res");
            }

            if (settings.HealthStyle == 1)
                File.Move($"{TF2Directory}\\rayshud\\customizations\\Player Health\\HudPlayerHealth-default.res", $"{TF2Directory}\\rayshud\\resource\\ui\\HudPlayerHealth.res");
            else if (settings.HealthStyle == 2)
                File.Move($"{TF2Directory}\\rayshud\\customizations\\Player Health\\HudPlayerHealth-teambar.res", $"{TF2Directory}\\rayshud\\resource\\ui\\HudPlayerHealth.res");
            else if (settings.HealthStyle == 3)
                File.Move($"{TF2Directory}\\rayshud\\customizations\\Player Health\\HudPlayerHealth-cross.res", $"{TF2Directory}\\rayshud\\resource\\ui\\HudPlayerHealth.res");
            else if (settings.HealthStyle == 4)
                File.Move($"{TF2Directory}\\rayshud\\customizations\\Player Health\\HudPlayerHealth-broesel.res", $"{TF2Directory}\\rayshud\\resource\\ui\\HudPlayerHealth.res");
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
            lines[9 - 1] = $"\"Ammo In Clip Low\"   \"{settings.AmmoClipLow}\"";
            lines[10 - 1] = $"\"Ammo In Reserve Low\"   \"{settings.AmmoReserveLow}\"";
            lines[23 - 1] = $"\"Health Normal\" \"{settings.HealthNormal}\"";
            lines[24 - 1] = $"\"Health Buff\"   \"{settings.HealthBuff}\"";
            lines[25 - 1] = $"\"Health Hurt\"   \"{settings.HealthLow}\"";
            lines[32 - 1] = $"\"Uber Bar Color\"    \"{settings.UberBarColor}\"";
            lines[35 - 1] = $"\"Solid Color Uber\"  \"{settings.UberFullColor}\"";
            lines[37 - 1] = $"\"Flashing Uber Color1\"  \"{settings.UberFlashColor1}\"";
            lines[38 - 1] = $"\"Flashing Uber Color2\"  \"{settings.UberFlashColor2}\"";
            lines[41 - 1] = $"\"Heal Numbers\"  \"{settings.HealingDone}\"";
            lines[45 - 1] = $"\"XHairEnabled\" \"{settings.XHairColor}\"";
            lines[46 - 1] = $"\"CrosshairDamage\"   \"{settings.XHairPulseColor}\"";
            File.WriteAllLines(colorScheme, lines);

            MessageBox.Show($"rayshud changes applied", "Changes Applied!", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                        validTFDirectory = true;
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
                        settings.AmmoClip = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                        break;

                    case "btnAmmoReserveLow":
                        btnAmmoReserveLow.BackColor = colorPicker.Color;
                        settings.AmmoReserve = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                        break;
                }
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

        private void cbXHairEnabled_CheckedChanged(object sender, EventArgs e)
        {
            settings.XHairEnabled = false;
            if (cbXHairEnabled.Checked)
                settings.XHairEnabled = true;
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
            settings.HealthStyle = lbHealthStyle.SelectedIndex + 1;
        }
    }

    public class Settings
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
        public bool XHairEnabled { get; set; }
        public bool XHairOutline { get; set; }      // false = disabled, true = enabled
        public bool XHairPulse { get; set; }        // false = disabled, true = enabled
        public int XHairHeight { get; set; }
        public int XHairWidth { get; set; }
        public string XHairColor { get; set; }      // [0-255] [0-255] [0-255] 255
        public string XHairPulseColor { get; set; } // [0-255] [0-255] [0-255] 255
        public int HealthStyle { get; set; }       // 1 = default, 2 = teambar, 3 = cross, 4 = broeselcross
        public string HealingDone { get; set; }     // [0-255] [0-255] [0-255] 255
        public string HealthNormal { get; set; }        // [0-255] [0-255] [0-255] 255
        public string HealthBuff { get; set; }    // [0-255] [0-255] [0-255] 255
        public string HealthLow { get; set; }     // [0-255] [0-255] [0-255] 255
        public string AmmoClip { get; set; }        // [0-255] [0-255] [0-255] 255
        public string AmmoReserve { get; set; }     // [0-255] [0-255] [0-255] 255
        public string AmmoClipLow { get; set; }     // [0-255] [0-255] [0-255] 255
        public string AmmoReserveLow { get; set; }  // [0-255] [0-255] [0-255] 255
    }

    public struct SettingsList
    {
        [JsonProperty("HUDVersion")] public string HUDVersion { get; private set; }
        [JsonProperty("Scoreboard")] public string Scoreboard { get; private set; }
        [JsonProperty("ChatBox")] public string ChatBox { get; private set; }
        [JsonProperty("TeamSelect")] public string TeamSelect { get; private set; }
        [JsonProperty("DisguiseImage")] public string DisguiseImage { get; private set; }
        [JsonProperty("DefaultMenuBG")] public string DefaultMenuBG { get; private set; }
        [JsonProperty("UberAnimation")] public string UberAnimation { get; private set; }
        [JsonProperty("UberBarColor")] public string UberBarColor { get; private set; }
        [JsonProperty("UberFullColor")] public string UberFullColor { get; private set; }
        [JsonProperty("UberFlashColor1")] public string UberFlashColor1 { get; private set; }
        [JsonProperty("UberFlashColor2")] public string UberFlashColor2 { get; private set; }
        [JsonProperty("XHairStyle")] public string XHairStyle { get; private set; }
        [JsonProperty("XHairEnabled")] public string XHairEnabled { get; private set; }
        [JsonProperty("XHairOutline")] public string XHairOutline { get; private set; }
        [JsonProperty("XHairPulse")] public string XHairPulse { get; private set; }
        [JsonProperty("XHairHeight")] public string XHairHeight { get; private set; }
        [JsonProperty("XHairWidth")] public string XHairWidth { get; private set; }
        [JsonProperty("XHairColor")] public string XHairColor { get; private set; }
        [JsonProperty("XHairPulseColor")] public string XHairPulseColor { get; private set; }
        [JsonProperty("HealthStyle")] public string HealthStyle { get; private set; }
        [JsonProperty("HealingDone")] public string HealingDone { get; private set; }
        [JsonProperty("HealthNormal")] public string HealthNormal { get; private set; }
        [JsonProperty("HealthBuff")] public string HealthBuff { get; private set; }
        [JsonProperty("HealthLow")] public string HealthLow { get; private set; }
        [JsonProperty("AmmoClip")] public string AmmoClip { get; private set; }
        [JsonProperty("AmmoReserve")] public string AmmoReserve { get; private set; }
        [JsonProperty("AmmoClipLow")] public string AmmoClipLow { get; private set; }
        [JsonProperty("AmmoReserveLow")] public string AmmoReserveLow { get; private set; }
    }
}