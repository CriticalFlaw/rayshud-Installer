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
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace rayshud_Installer
{
    public partial class Main : Form
    {
        // Create the error-tracking object
        private readonly RavenClient ravenClient = new RavenClient("https://e3d4daa2995342d1aaca5827b95fce2d:23548ce9177049f5a1601ceafeb8c609@sentry.io/1190168");
        // Create the HUD configuration object
        private RootObject settings = new RootObject();
        public string TF2Directory;

        public Main()
        {
            InitializeComponent();
            InitializeCustomFonts();
            GetLiveVersion();
        }

        private void InitializeCustomFonts()
        {
            // Create a private font collection object.
            var fonts = new PrivateFontCollection();
            // Select your font from the resources.
            int fontLength = Properties.Resources.Crosshairs.Length;
            // Create a buffer to read in to
            var fontdata = Properties.Resources.Crosshairs;
            // Create an unsafe memory block for the font data
            var data = Marshal.AllocCoTaskMem(fontLength);
            // Copy the bytes to the unsafe memory block
            Marshal.Copy(fontdata, 0, data, fontLength);
            // Pass the font to the font collection
            fonts.AddMemoryFont(data, fontLength);
            lblCrosshair.Font = new Font(fonts.Families[0], lblCrosshair.Font.Size);
            // Set label background to be transparent over a picture-box
            lblCrosshair.Parent = pbPreview;
            txtXHairSize.Parent = pbPreview;
            lblCrosshair.BackColor = Color.Transparent;
            txtXHairSize.BackColor = Color.Transparent;
        }

        private void GetLiveVersion()
        {
            try
            {
                var client = new WebClient();
                // Download the latest rayshud README
                var textFromURL = client.DownloadString("https://raw.githubusercontent.com/raysfire/rayshud/installer/README.md");
                var textFromURLArray = textFromURL.Split('\n');
                // Retrieve the latest version number from the README
                txtLiveVersion.Text = textFromURLArray[textFromURLArray.Length - 2];
                CheckTF2Directory();
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"Unable to find the latest version number. \n{ex.Message}", @"Error: Finding latest version number", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CheckTF2Directory()
        {
            try
            {
                // Check for temporary files, remove them if found
                if (File.Exists($"{Application.StartupPath}\\TempHUD.zip"))
                    File.Delete($"{Application.StartupPath}\\TempHUD.zip");
                // Check default Steam installation directories for the tf/custom folder
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
                    // If tf/custom is not found, ask the user to provide it
                    var DirectoryBrowser = new FolderBrowserDialog
                    {
                        Description = "Please select your tf/custom folder. Example: \nC:/Program Files (x86)/Steam/steamapps/common/Team Fortress 2/tf/custom"
                    };
                    var validHUDDirectory = false;
                    while (validHUDDirectory == false)
                    {
                        // Loop until the user clicks Cancel or provides a directory that contains tf/custom
                        if (DirectoryBrowser.ShowDialog() == DialogResult.OK)
                        {
                            if (!DirectoryBrowser.SelectedPath.Contains("custom")) continue;        //"Steam\\steamapps\\common\\Team Fortress 2\\tf\\custom"   //DEBUG
                            TF2Directory = DirectoryBrowser.SelectedPath;
                            validHUDDirectory = true;
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
                MessageBox.Show($"Unable to find the tf/custom directory. \n{ex.Message}", @"Error: Finding tf/custom directory", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    ReadFromSettings();
                    DisplayHUDSettings();
                }
                else
                {
                    btnInstall.Text = "Install";
                    txtStatus.Text = "Not Installed";
                    btnUninstall.Enabled = false;
                    btnSaveChanges.Enabled = false;
                    btnOpenDirectory.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"An error occurred while verifying rayshud version numbers \n{ex.Message}", @"Error: Verifying rayshud version", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                txtXHairSize.Text = settings.XHairSize;

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
                MessageBox.Show($"An error occured while loading configuration settings. \n{ex.Message}", @"Error: Loading Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            WriteToSettings("XHairSize", settings.XHairSize);
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
            string json = File.ReadAllText($"{TF2Directory}\\rayshud\\customizations\\settings.json");
            dynamic jsonObj = JsonConvert.DeserializeObject(json);
            jsonObj[setting] = value;
            string output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
            File.WriteAllText($"{TF2Directory}\\rayshud\\customizations\\settings.json", output);
        }

        public void ReadFromSettings()
        {
            using (var reader = new StreamReader($"{TF2Directory}\\rayshud\\customizations\\settings.json"))
            {
                string json = reader.ReadToEnd();
                settings = JsonConvert.DeserializeObject<RootObject>(json);
            }
        }

        private void btnInstall_Click(object sender, EventArgs e)
        {
            try
            {
                // Remove the temporary downloaded rayshud files
                if (File.Exists($"{Application.StartupPath}\\TempHUD.zip"))
                    File.Delete($"{Application.StartupPath}\\TempHUD.zip");
                // Back-up the installer configuration file
                if (File.Exists($"{TF2Directory}\\rayshud\\customizations\\settings.json"))
                    File.Copy($"{TF2Directory}\\rayshud\\customizations\\settings.json", $"{Application.StartupPath}\\settings.json", true);
                // Download the latest rayshud from GitHub and extract into the tf/custom directory
                var client = new WebClient();
                client.DownloadFile("https://github.com/raysfire/rayshud/archive/installer.zip", "TempHUD.zip");    //DEBUG
                ZipFile.ExtractToDirectory($"{Application.StartupPath}\\TempHUD.zip", TF2Directory);
                // Either do a clean install or refresh/update of rayshud
                switch (btnInstall.Text)
                {
                    case "Install":
                        Directory.Move($"{TF2Directory}\\rayshud-installer", $"{TF2Directory}\\rayshud");
                        MessageBox.Show("Done installing rayshud", @"rayshud Installed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;

                    case "Update":
                    case "Refresh":
                        // Replace the installed rayshud with a fresh copy
                        Directory.Delete($"{TF2Directory}\\rayshud", true);
                        Directory.Move($"{TF2Directory}\\rayshud-installer", $"{TF2Directory}\\rayshud");
                        // Restore the installation configuration file
                        if (File.Exists($"{Application.StartupPath}\\settings.json"))
                        {
                            File.Copy($"{Application.StartupPath}\\settings.json", $"{TF2Directory}\\rayshud\\customizations\\settings.json", true);
                            File.Delete($"{Application.StartupPath}\\settings.json");
                        }
                        MessageBox.Show("Done updating rayshud", @"rayshud Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                }
                // Remove the temporary downloaded rayshud files
                if (File.Exists($"{Application.StartupPath}\\TempHUD.zip"))
                    File.Delete($"{Application.StartupPath}\\TempHUD.zip");
                CheckHUDDirectory();
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"An error occurred while attempting to download rayshud \n{ex.Message}", @"Error: Downloading rayshud", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnUninstall_Click(object sender, EventArgs e)
        {
            try
            {
                if (Directory.Exists($"{TF2Directory}\\rayshud"))
                {
                    Directory.Delete($"{TF2Directory}\\rayshud", true);
                    MessageBox.Show("Done uninstalling rayshud", @"rayshud Uninstalled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtDirectory.Text = TF2Directory;
                    txtInstalledVersion.Text = "...";
                    txtLastModified.Text = "...";
                    CheckHUDDirectory();
                }
                else
                    MessageBox.Show("rayshud was not found in the tf/custom directory", @"Error: rayshud Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"An error occurred while attempting to remove rayshud \n{ex.Message}", @"Error: Uninstalling rayshud", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSaveChanges_Click(object sender, EventArgs e)
        {
            // DEBUG
            try
            {

            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"An error occurred while applying installer changes to rayshud \n{ex.Message}", @"Error: Updating rayshud", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Update the installer configuration file
            UpdateSettingsFile();
            // Set the directories to be used during the file reads/writes
            var console = $"{TF2Directory}\\rayshud\\materials\\console";
            var resource = $"{TF2Directory}\\rayshud\\resource\\ui";
            var scripts = $"{TF2Directory}\\rayshud\\scripts";
            var mainmenu = $"{TF2Directory}\\rayshud\\customizations\\Main Menu";
            var scoreboard = $"{TF2Directory}\\rayshud\\customizations\\Scoreboard";
            var teammenu = $"{TF2Directory}\\rayshud\\customizations\\Team Menu";
            var playerhealth = $"{TF2Directory}\\rayshud\\customizations\\Player Health";
            var colorScheme = $"{TF2Directory}\\rayshud\\resource\\scheme\\clientscheme_colors.res";
            var animations = $"{TF2Directory}\\rayshud\\scripts\\hudanimations_custom.txt";
            var chat = $"{TF2Directory}\\rayshud\\resource\\ui\\basechat.res";
            var layout = $"{TF2Directory}\\rayshud\\scripts\\hudlayout.res";

            // 1. Main Menu Style - either classic or modern, copy and replace existing files
            if (settings.HUDVersion)
            {
                if (Directory.Exists($"{console}_off"))
                {
                    File.Copy($"{mainmenu}\\Classic\\materials\\console\\background_upward.vtf", $"{console}_off\\background_upward.vtf", true);
                    File.Copy($"{mainmenu}\\Classic\\materials\\console\\background_upward_widescreen.vtf", $"{console}_off\\background_upward_widescreen.vtf", true);
                }
                else
                {
                    File.Copy($"{mainmenu}\\Classic\\materials\\console\\background_upward.vtf", $"{console}\\background_upward.vtf", true);
                    File.Copy($"{mainmenu}\\Classic\\materials\\console\\background_upward_widescreen.vtf", $"{console}\\background_upward_widescreen.vtf", true);
                }
                File.Copy($"{mainmenu}\\Classic\\resource\\ui\\mainmenuoverride.res", $"{resource}\\mainmenuoverride.res", true);
                File.Copy($"{mainmenu}\\Classic\\resource\\gamemenu.res", $"{TF2Directory}\\rayshud\\resource\\gamemenu.res", true);
            }
            else
            {
                if (Directory.Exists($"{console}_off"))
                {
                    File.Copy($"{mainmenu}\\Modern\\materials\\console\\background_upward.vtf", $"{console}_off\\background_upward.vtf", true);
                    File.Copy($"{mainmenu}\\Modern\\materials\\console\\background_upward_widescreen.vtf", $"{console}_off\\background_upward_widescreen.vtf", true);
                }
                else
                {
                    File.Copy($"{mainmenu}\\Modern\\materials\\console\\background_upward.vtf", $"{console}\\background_upward.vtf", true);
                    File.Copy($"{mainmenu}\\Modern\\materials\\console\\background_upward_widescreen.vtf", $"{console}\\background_upward_widescreen.vtf", true);
                }
                File.Copy($"{mainmenu}\\Modern\\resource\\ui\\mainmenuoverride.res", $"{resource}\\mainmenuoverride.res", true);
                File.Copy($"{mainmenu}\\Modern\\resource\\gamemenu.res", $"{TF2Directory}\\rayshud\\resource\\gamemenu.res", true);
            }

            // 2. Scoreboard Style - either normal or minimal (6v6), copy and replace existing files
            if (settings.Scoreboard)
                File.Copy($"{scoreboard}\\scoreboard-minimal.res", $"{resource}\\scoreboard.res", true);
            else
                File.Copy($"{scoreboard}\\scoreboard-default.res", $"{resource}\\scoreboard.res", true);

            // 3. Default Background - enable or disable the custom backgrounds files by renaming them
            if (settings.DefaultMenuBG)
            {
                if (Directory.Exists(console))
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
                File.Copy($"{teammenu}\\Teammenu-center.res", $"{resource}\\Teammenu.res", true);
                File.Copy($"{teammenu}\\ClassSelection-center.res", $"{resource}\\ClassSelection.res", true);
            }
            else
            {
                File.Copy($"{teammenu}\\Teammenu-left.res", $"{resource}\\Teammenu.res", true);
                File.Copy($"{teammenu}\\ClassSelection-left.res", $"{resource}\\ClassSelection.res", true);
            }

            switch (settings.HealthStyle)
            {
                // 5. Player Health Style - either default, cross, teambar or broesel, copy and replace existing files
                case 1:
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

            switch (settings.UberAnimation)
            {
                // 7. Uber Animation - enable or disable by commenting out the lines
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
            for (int x = 34; x <= 290; x += 17)
            {
                lines[x - 1] = "\t\t\"visible\"\t\t\"0\"";
                lines[x + 1 - 1] = "\t\t\"enabled\"\t\t\"0\"";
                lines[x + 7 - 1] = lines[x + 7 - 1].Replace("Outline", string.Empty);
                File.WriteAllLines(layout, lines);
            }

            // 10. Crosshairs - either enabled or disabled with or without outlines, change the visible, enabled and font values of hudlayout.res
            if (settings.XHairEnabled)
            {
                switch (settings.XHairStyle)
                {
                    case 1:     // BasicCross
                        lines[34 - 1] = "\t\t\"visible\"\t\t\"1\"";
                        lines[35 - 1] = "\t\t\"enabled\"\t\t\"1\"";
                        if (settings.XHairOutline)
                            lines[41 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}Outline\"";
                        else
                            lines[41 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}\"";
                        break;

                    case 2:     // BasicCrossLarge
                        lines[51 - 1] = "\t\t\"visible\"\t\t\"1\"";
                        lines[52 - 1] = "\t\t\"enabled\"\t\t\"1\"";
                        if (settings.XHairOutline)
                            lines[58 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}Outline\"";
                        else
                            lines[58 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}\"";
                        break;

                    case 3:     // BasicCrossSmall
                        lines[68 - 1] = "\t\t\"visible\"\t\t\"1\"";
                        lines[69 - 1] = "\t\t\"enabled\"\t\t\"1\"";
                        if (settings.XHairOutline)
                            lines[75 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}Outline\"";
                        else
                            lines[75 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}\"";
                        break;

                    case 4:     // BasicDot
                        lines[85 - 1] = "\t\t\"visible\"\t\t\"1\"";
                        lines[86 - 1] = "\t\t\"enabled\"\t\t\"1\"";
                        if (settings.XHairOutline)
                            lines[92 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}Outline\"";
                        else
                            lines[92 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}\"";
                        break;

                    case 5:     // CircleDot
                        lines[102 - 1] = "\t\t\"visible\"\t\t\"1\"";
                        lines[103 - 1] = "\t\t\"enabled\"\t\t\"1\"";
                        if (settings.XHairOutline)
                            lines[109 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}Outline\"";
                        else
                            lines[109 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}\"";
                        break;

                    case 6:     // KonrWings
                        lines[119 - 1] = "\t\t\"visible\"\t\t\"1\"";
                        lines[120 - 1] = "\t\t\"enabled\"\t\t\"1\"";
                        if (settings.XHairOutline)
                            lines[126 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}Outline\"";
                        else
                            lines[126 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}\"";
                        break;

                    case 7:     // OpenCross
                        lines[136 - 1] = "\t\t\"visible\"\t\t\"1\"";
                        lines[137 - 1] = "\t\t\"enabled\"\t\t\"1\"";
                        if (settings.XHairOutline)
                            lines[143 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}Outline\"";
                        else
                            lines[143 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}\"";
                        break;

                    case 8:     // OpenCrossDot
                        lines[153 - 1] = "\t\t\"visible\"\t\t\"1\"";
                        lines[154 - 1] = "\t\t\"enabled\"\t\t\"1\"";
                        if (settings.XHairOutline)
                            lines[160 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}Outline\"";
                        else
                            lines[160 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}\"";
                        break;

                    case 9:     // ScatterSpread
                        lines[170 - 1] = "\t\t\"visible\"\t\t\"1\"";
                        lines[171 - 1] = "\t\t\"enabled\"\t\t\"1\"";
                        if (settings.XHairOutline)
                            lines[177 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}Outline\"";
                        else
                            lines[177 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}\"";
                        break;

                    case 10:    // ThinCircle
                        lines[187 - 1] = "\t\t\"visible\"\t\t\"1\"";
                        lines[188 - 1] = "\t\t\"enabled\"\t\t\"1\"";
                        if (settings.XHairOutline)
                            lines[194 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}Outline\"";
                        else
                            lines[194 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}\"";
                        break;

                    case 11:    // ThinCross
                        lines[204 - 1] = "\t\t\"visible\"\t\t\"1\"";
                        lines[205 - 1] = "\t\t\"enabled\"\t\t\"1\"";
                        if (settings.XHairOutline)
                            lines[211 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}Outline\"";
                        else
                            lines[211 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}\"";
                        break;

                    case 12:    // Wings
                        lines[221 - 1] = "\t\t\"visible\"\t\t\"1\"";
                        lines[222 - 1] = "\t\t\"enabled\"\t\t\"1\"";
                        if (settings.XHairOutline)
                            lines[228 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}Outline\"";
                        else
                            lines[228 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}\"";
                        break;

                    case 13:    // WingsPlus
                        lines[238 - 1] = "\t\t\"visible\"\t\t\"1\"";
                        lines[239 - 1] = "\t\t\"enabled\"\t\t\"1\"";
                        if (settings.XHairOutline)
                            lines[245 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}Outline\"";
                        else
                            lines[245 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}\"";
                        break;

                    case 14:    // WingsSmall
                        lines[255 - 1] = "\t\t\"visible\"\t\t\"1\"";
                        lines[256 - 1] = "\t\t\"enabled\"\t\t\"1\"";
                        if (settings.XHairOutline)
                            lines[262 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}Outline\"";
                        else
                            lines[262 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}\"";
                        break;

                    case 15:    // WingsSmallDot
                        lines[272 - 1] = "\t\t\"visible\"\t\t\"1\"";
                        lines[273 - 1] = "\t\t\"enabled\"\t\t\"1\"";
                        if (settings.XHairOutline)
                            lines[279 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}Outline\"";
                        else
                            lines[279 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}\"";
                        break;

                    case 16:    // xHairCircle
                        lines[289 - 1] = "\t\t\"visible\"\t\t\"1\"";
                        lines[290 - 1] = "\t\t\"enabled\"\t\t\"1\"";
                        if (settings.XHairOutline)
                            lines[296 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}Outline\"";
                        else
                            lines[296 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{txtXHairSize.Text}\"";
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

            MessageBox.Show("rayshud settings saved and applied.", "Changes Saved!", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            var DirectoryBrowser = new FolderBrowserDialog
            {
                Description = "Please select your tf/custom folder. Example: \nC:/Program Files (x86)/Steam/steamapps/common/Team Fortress 2/tf/custom"
            };
            while (IsDirectoryValid == false)
            {
                // Until the correct path is provided or the user clicks 'Cancel' - keep prompting for a valid tf/custom directory.
                if (DirectoryBrowser.ShowDialog() == DialogResult.OK)
                {
                    if (!DirectoryBrowser.SelectedPath.Contains("custom")) continue;   //"Steam\\steamapps\\common\\Team Fortress 2\\tf\\custom"  // DEBUG
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
                    lblCrosshair.Text = @"2";
                    txtXHairSize.Text = @"26";
                    break;

                case 2:
                    lblCrosshair.Text = @"2";
                    txtXHairSize.Text = @"32";
                    break;

                case 3:
                    lblCrosshair.Text = @"2";
                    txtXHairSize.Text = @"18";
                    break;

                case 4:
                    lblCrosshair.Text = @"3";
                    txtXHairSize.Text = @"24";
                    break;

                case 5:
                    lblCrosshair.Text = @"8";
                    txtXHairSize.Text = @"34";
                    break;

                case 6:
                    lblCrosshair.Text = @"i";
                    txtXHairSize.Text = @"24";
                    break;

                case 7:
                    lblCrosshair.Text = @"i";
                    txtXHairSize.Text = @"24";
                    break;

                case 8:
                    lblCrosshair.Text = @"h";
                    txtXHairSize.Text = @"24";
                    break;

                case 9:
                    lblCrosshair.Text = @"0";
                    txtXHairSize.Text = @"32";
                    break;

                case 10:
                    lblCrosshair.Text = @"9";
                    txtXHairSize.Text = @"34";
                    break;

                case 11:
                    lblCrosshair.Text = @"+";
                    txtXHairSize.Text = @"24";
                    break;

                case 12:
                    lblCrosshair.Text = @"d";
                    txtXHairSize.Text = @"34";
                    break;

                case 13:
                    lblCrosshair.Text = @"c";
                    txtXHairSize.Text = @"34";
                    break;

                case 14:
                    lblCrosshair.Text = @"g";
                    txtXHairSize.Text = @"34";
                    break;

                case 15:
                    lblCrosshair.Text = @"o";
                    txtXHairSize.Text = @"34";
                    break;

                default:
                    lblCrosshair.Text = string.Empty;
                    txtXHairSize.Text = string.Empty;
                    break;
            }
            settings.XHairSize = txtXHairSize.Text;
        }

        private void btnOpenDirectory_Click(object sender, EventArgs e)
        {
            if (Directory.Exists($"{TF2Directory}\\rayshud"))
                Process.Start("explorer.exe", $"{TF2Directory}\\rayshud");
        }

        private void btnSetDefault_Click(object sender, EventArgs e)
        {
            WriteToSettings("HUDVersion", "False");
            WriteToSettings("Scoreboard", "False");
            WriteToSettings("ChatBox", "False");
            WriteToSettings("TeamSelect", "False");
            WriteToSettings("DisguiseImage", "False");
            WriteToSettings("DefaultMenuBG", "False");
            WriteToSettings("UberAnimation", "1");
            WriteToSettings("UberBarColor", "235 226 202 255");
            WriteToSettings("UberFullColor", "255 50 255 255");
            WriteToSettings("UberFlashColor1", "255 165 0 255");
            WriteToSettings("UberFlashColor2", "255 69 0 255");
            WriteToSettings("XHairEnabled", "False");
            WriteToSettings("XHairStyle", "1");
            WriteToSettings("XHairOutline", "False");
            WriteToSettings("XHairPulse", "True");
            WriteToSettings("XHairSize", "20");
            WriteToSettings("XHairColor", "242 242 242 255");
            WriteToSettings("XHairPulseColor", "255 0 0 255");
            WriteToSettings("HealingDone", "48 255 48 255");
            WriteToSettings("HealthStyle", "1");
            WriteToSettings("HealthNormal", "235 226 202 255");
            WriteToSettings("HealthBuff", "48 255 48 255");
            WriteToSettings("HealthLow", "255 153 0 255");
            WriteToSettings("AmmoClip", "48 255 48 255");
            WriteToSettings("AmmoReserve", "72 255 255 255");
            WriteToSettings("AmmoClipLow", "255 42 130 255");
            WriteToSettings("AmmoReserveLow", "255 128 28 255");
            WriteToSettings("LastModified", DateTime.Now.ToString(CultureInfo.CurrentCulture));
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