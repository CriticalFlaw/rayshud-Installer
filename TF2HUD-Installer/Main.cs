using SharpRaven;
using SharpRaven.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Windows.Forms;

namespace FlawHUD_Installer
{
    public partial class frmMain : Form
    {
        // Initiate the error-tracker
        private RavenClient ravenClient = new RavenClient("https://e3d4daa2995342d1aaca5827b95fce2d:23548ce9177049f5a1601ceafeb8c609@sentry.io/1190168");

        public List<string> LatestHUDVersion = new List<string>();
        public string TF2Directory; //, hitEnable, hitVisible, hitXAxis, hitYAxis, hitStyle, hitOutline, hitSize, colorHealing, colorDamage, colorUber1, colorUber2, InstalledHUD, DownloadLink;

        public frmMain()
        {
            InitializeComponent();      // Start up the main components.
            GetLiveVersion();           // Check for the latest version
            CheckTF2Directory();        // Check if the default tf/custom directory exists
            CheckHUDDirectory();        // Check the tf directory for installed hud files
            ResetSettings();
        }

        private void CheckTF2Directory()
        {
            // Check for temporary HUD files, remove them if they exist.
            if (File.Exists($"{Application.StartupPath}\\TempHUD.zip"))
                File.Delete($"{Application.StartupPath}\\TempHUD.zip");

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

        private void CheckHUDDirectory()
        {
            try
            {
                // Loop through all the listed HUDS and check if any of them are installed
                if (Directory.Exists($"{TF2Directory}\\rayshud-master"))
                {
                    btnUninstall.Enabled = true;
                    txtInstalledVersion.Text = File.ReadLines($"{TF2Directory}\\rayshud-master\\README.md").Last().ToString();
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
                    LoadSettings();
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
                MessageBox.Show($"An error occurred while attempting to find rayshud version numbers \n {ex.Message}", "Error: Checking rayshud Version Numbers!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GetLiveVersion()
        {
            var WC = new WebClient();
            var textFromURL = WC.DownloadString("https://raw.githubusercontent.com/raysfire/rayshud/master/README.md");
            // Split the downloaded text into an array.
            string[] textFromURLArray = textFromURL.Split('\n');
            // Extract the live version number and add it to the list
            txtLiveVersion.Text = textFromURLArray[textFromURLArray.Length - 2];
        }

        private void LoadSettings()
        {
            try
            {
                var scripts = $"{TF2Directory}\\rayshud\\scripts";
                var resource = $"{TF2Directory}\\rayshud\\resource";
                var scheme = $"{TF2Directory}\\rayshud\\resource\\scheme";
                var ui = $"{TF2Directory}\\rayshud\\resource\\ui";

                ResetSettings();

                //var index = File.ReadLines($"{TF2Directory}\\rayshud\\scripts\\hudlayout.res").Skip(11).Take(1).First();       // Hitmarker Visible        LINE 12
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"An error occured when loading settings \n {ex.Message}", "Error: Loading Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResetSettings()
        {
            // GENERAL
            cbHUDVersion.SelectedIndex = 0;
            cbAspectRatio.SelectedIndex = 0;
            cbScoreboard.SelectedIndex = 0;

            rbUberAnimation1.Checked = false;
            rbUberAnimation2.Checked = true;
            rbUberAnimation3.Checked = false;
            btnUberBarColor.BackColor = System.Drawing.Color.White;
            btnUberFullColor.BackColor = System.Drawing.Color.White;
            btnUberFlashColor1.BackColor = System.Drawing.Color.White;
            btnUberFlashColor2.BackColor = System.Drawing.Color.White;

            rbChatBoxTop.Checked = false;
            rbChatBoxBottom.Checked = true;
            rbTeamSelectLeft.Checked = true;
            rbTeamSelectCenter.Checked = false;
            cbDisguiseImage.Checked = false;
            cbDefaultMenuBG.Checked = false;

            // CROSSHAIR
            cbCrosshair.Checked = false;
            cbXHairOutline.Checked = false;
            txtXHairHeight.Text = "0";
            txtXHairWidth.Text = "0";
            btnXHairColor.BackColor = System.Drawing.Color.White;
            cbXHairPulse.Checked = false;
            btnXHairPulse.BackColor = System.Drawing.Color.White;

            // HEALTH
            btnHealingDone.BackColor = System.Drawing.Color.White;
            btnNumColor.BackColor = System.Drawing.Color.White;
            btnBuffNumColor.BackColor = System.Drawing.Color.White;
            btnLowNumColor.BackColor = System.Drawing.Color.White;

            // AMMO
            btnAmmoClip.BackColor = System.Drawing.Color.White;
            btnAmmoReserve.BackColor = System.Drawing.Color.White;
            btnLowAmmoClip.BackColor = System.Drawing.Color.White;
            btnLowAmmoReserve.BackColor = System.Drawing.Color.White;
        }

        private void btnInstall_Click(object sender, EventArgs e)
        {
            try
            {
                // Remove the downloaded file from the temporary location
                if (File.Exists($"{Application.StartupPath}\\TempHUD.zip"))
                    File.Delete($"{Application.StartupPath}\\TempHUD.zip");

                WebClient WC = new WebClient(); // Download th HUD into a temporary location
                WC.DownloadFile("https://github.com/raysfire/rayshud/archive/master.zip", "TempHUD.zip");
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
                if (Directory.Exists($"{TF2Directory}\\rayshud-master"))
                {
                    Directory.Delete($"{TF2Directory}\\rayshud-master", true);
                    MessageBox.Show($"Finished Uninstalling rayshud...");
                    txtInstalledVersion.Text = "...";
                    CheckHUDDirectory();
                }
                else
                    MessageBox.Show($"File <i>rayshud-master</i> was not found in the tf/custom directory.", $"Error: rayshud Not Found!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"An error occurred while attempting to remove. \n {ex.Message}", $"Error: Uninstalling rayshud!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSaveChanges_Click(object sender, EventArgs e)
        {
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
                    break;

                case "btnUberFullColor":
                    btnUberFullColor.BackColor = colorPicker.Color;
                    break;

                case "btnUberFlashColor1":
                    btnUberFlashColor1.BackColor = colorPicker.Color;
                    break;

                case "btnUberFlashColor2":
                    btnUberFlashColor2.BackColor = colorPicker.Color;
                    break;

                case "btnXHairColor":
                    btnXHairColor.BackColor = colorPicker.Color;
                    break;

                case "btnXHairPulse":
                    btnXHairPulse.BackColor = colorPicker.Color;
                    break;

                case "btnHealingDone":
                    btnHealingDone.BackColor = colorPicker.Color;
                    break;

                case "btnNumColor":
                    btnNumColor.BackColor = colorPicker.Color;
                    break;

                case "btnBuffNumColor":
                    btnBuffNumColor.BackColor = colorPicker.Color;
                    break;

                case "btnLowNumColor":
                    btnLowNumColor.BackColor = colorPicker.Color;
                    break;

                case "btnAmmoClip":
                    btnAmmoClip.BackColor = colorPicker.Color;
                    break;

                case "btnAmmoReserve":
                    btnAmmoReserve.BackColor = colorPicker.Color;
                    break;

                case "btnLowAmmoClip":
                    btnLowAmmoClip.BackColor = colorPicker.Color;
                    break;

                case "btnLowAmmoReserve":
                    btnLowAmmoReserve.BackColor = colorPicker.Color;
                    break;
            }
        }

        private void lbXHairStyles_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Read scripts/hudlayout.res for the selected crosshair settings
            // Update the controls: cbCrosshair, cbXHairOutline, txtXHairAxisY, txtXHairAxisX, cbXHairSize, btnXHairColor, cbXHairPulse, btnXHairPulse
            // Update preview image
            switch (lbXHairStyles.SelectedItem)
            {
                case "BasicCross":
                    pbPreview2.ImageLocation = "https://i.imgur.com/9FMR0oN.jpg";
                    break;

                case "BasicCrossLarge":
                    pbPreview2.ImageLocation = "https://i.imgur.com/dUtMMpz.jpg";
                    break;

                case "BasicCrossSmall":
                    pbPreview2.ImageLocation = "https://i.imgur.com/x9M3tZA.jpg";
                    break;

                case "BasicDot":
                    pbPreview2.ImageLocation = "https://i.imgur.com/cM4B3Yq.jpg";
                    break;

                case "CircleDot":
                    pbPreview2.ImageLocation = "https://i.imgur.com/yUDWwOU.jpg";
                    break;

                case "KonrWings":
                    pbPreview2.ImageLocation = "https://i.imgur.com/ym1WUsP.jpg";
                    break;

                case "OpenCross":
                    pbPreview2.ImageLocation = "https://i.imgur.com/Yc5H81Q.jpg";
                    break;

                case "OpenCrossDot":
                    pbPreview2.ImageLocation = "https://i.imgur.com/YNLmuze.jpg";
                    break;

                case "ScatterSpread":
                    pbPreview2.ImageLocation = "https://i.imgur.com/S7yWqL8.jpg";
                    break;

                case "ThinCircle":
                    pbPreview2.ImageLocation = "https://i.imgur.com/T8ovez4.jpg";
                    break;

                case "ThinCross":
                    pbPreview2.ImageLocation = "https://i.imgur.com/SzZkbaB.jpg";
                    break;

                case "Wings":
                    pbPreview2.ImageLocation = "https://i.imgur.com/pOltRKf.jpg";
                    break;

                case "WingsPlus":
                    pbPreview2.ImageLocation = "https://i.imgur.com/uonkSki.jpg";
                    break;

                case "WingsSmall":
                    pbPreview2.ImageLocation = "https://i.imgur.com/eGqDvF0.jpg";
                    break;

                case "xHairCircle":
                    pbPreview2.ImageLocation = "https://i.imgur.com/vO6Q3KL.jpg";
                    break;
            }
        }

        public void Write()
        {
            var scripts = $"{TF2Directory}\\rayshud\\scripts";
            var resource = $"{TF2Directory}\\rayshud\\resource";
            var scheme = $"{TF2Directory}\\rayshud\\resource\\scheme";
            var ui = $"{TF2Directory}\\rayshud\\resource\\ui";

            // Control: cbHUDVersion
            // Location: customizations / Classic Main Menu
            // Task: Copy folder contents into root

            // Control: cbAspectRatio
            // Location: customizations / 4; 3 Ratio
            // Task: Copy folder contents into root

            // Control: cbScoreboard
            // Location: customizations / Scoreboard 6s or HL
            // Task: Copy folder contents into root

            // Control: cbDisguiseImage
            // Location: scripts / hudanimations_custom.txt
            // Task: Uncomment lines 87 - 89 and 94 - 96 if enabled

            // Control: cbDefaultMenuBG
            // Location: materials
            // Task: Rename folder to material_unused if enabled

            // Control: rbUberAnimation1 - 3
            // Location: scripts / hudanimations_custom.txt
            // Task: Uncomment lines 104 - 106 based on selection

            // Control: btnUberBarColor
            // Location: resource\scheme\clientscheme_colors.res
            // Task: Update line 32

            // Control: btnUberFullColor
            // Location: resource\scheme\clientscheme_colors.res
            // Task: Update line 35

            // Control: btnUberFlashColor1 - 2
            // Location: resource\scheme\clientscheme_colors.res
            // Task: Update line 37 - 38

            // Control: rbChatBoxTop - Bottom
            // Location: resource\ui\basechat.res
            // Task: Update line 10

            // Control: rbTeamSelectLeft - Center
            // Location: resource\ui
            // Task: Add or remove center to classselection and teammenu
            ////---------------------------------------------------------------------------------

            // Control: cbCrosshair
            // Location: scripts\hudlayout.res
            // Task: Update lines 3 - 4 in node

            // Control: cbXHairOutline
            // Location: scripts\hudlayout.res
            // Task: Add or remove Outline to the font name

            // Control: txtXHairHeight
            // Location: scripts\hudlayout.res
            // Task: Update line 8 in node

            // Control: txtXHairWidth
            // Location: scripts\hudlayout.res
            // Task: Update line 9 in node

            // Control: btnXHairColor
            // Location: resource\scheme\clientscheme_colors.res
            // Task: Update line 45

            // Control: cbXHairPulse
            // Location: scripts\hudanimations_custom.txt
            // Task: Comment out lines 80 - 81, based on selection

            // Control: btnXHairPulse
            // Location: resource\scheme\clientscheme_colors.res
            // Task: Update line 46
            ////---------------------------------------------------------------------------------

            // Control: cbPlayerHealth
            // Location: resource\ui
            // Task: Add or remove tags from HudPlayerHealth

            // Control: btnHealingDone
            // Location: resource\scheme\clientscheme_colors.res
            // Task: Update line 41

            // Control: btnNumColor
            // Location: resource\scheme\clientscheme_colors.res
            // Task: Update line 23

            // Control: btnBuffNumColor
            // Location: resource\scheme\clientscheme_colors.res
            // Task: Update line 24

            // Control: btnLowNumColor
            // Location: resource\scheme\clientscheme_colors.res
            // Task: Update line 25
            ////---------------------------------------------------------------------------------

            // Control: btnAmmoClip
            // Location: resource\scheme\clientscheme_colors.res
            // Task: Update line 7

            // Control: btnAmmoReserve
            // Location: resource\scheme\clientscheme_colors.res
            // Task: Update line 8

            // Control: btnLowAmmoClip
            // Location: resource\scheme\clientscheme_colors.res
            // Task: Update line 9

            // Control: btnLowAmmoReserve
            // Location: resource\scheme\clientscheme_colors.res
            // Task: Update line 10
        }
    }
}