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
            cbResolution.SelectedIndex = 4;
            cbScoreboard.SelectedIndex = 0;
            cbMenuSounds.Checked = false;
            cbMinCTFDisplay.Checked = false;
            cbAlternateFont.Checked = false;
            cbDisguiseImage.Checked = false;
            cbDefaultMenuBG.Checked = false;

            // CROSSHAIR
            cbCrosshair.Checked = false;
            cbXHairOutline.Checked = false;
            txtXHairAxisY.Text = "10";
            txtXHairAxisX.Text = "10";
            cbXHairSize.SelectedIndex = 0;
            btnXHairColor.BackColor = System.Drawing.Color.White;
            cbXHairPulse.Checked = false;
            btnXHairPulse.BackColor = System.Drawing.Color.White;

            // HEALTH
            btnDamageDone.BackColor = System.Drawing.Color.White;
            btnHealingDone.BackColor = System.Drawing.Color.White;
            cbHealthCross.Checked = false;
            cbHealthPickup.Checked = false;
            btnNumColor.BackColor = System.Drawing.Color.White;
            btnBuffNumColor.BackColor = System.Drawing.Color.White;
            btnLowNumColor.BackColor = System.Drawing.Color.White;

            // AMMO
            rbLowAmmoEffect1.Checked = false;
            rbLowAmmoEffect2.Checked = false;
            rbLowAmmoEffect3.Checked = true;
            btnAmmoClip.BackColor = System.Drawing.Color.White;
            btnAmmoReserve.BackColor = System.Drawing.Color.White;
            btnLowAmmoClip.BackColor = System.Drawing.Color.White;
            btnLowAmmoReserve.BackColor = System.Drawing.Color.White;
            btnAmmoClipBlink.BackColor = System.Drawing.Color.White;
            btnAmmoReserveBlink.BackColor = System.Drawing.Color.White;

            // DAMAGE
            cbDamageOutline.Checked = false;
            rbDamageSizeSmall.Checked = true;
            rbDamageSizeMedium.Checked = false;
            rbDamageSizeLarge.Checked = false;
            btnDamageColor.BackColor = System.Drawing.Color.White;
            rbLastDamage1.Checked = false;
            rbLastDamage2.Checked = false;
            rbLastDamage3.Checked = true;
            btnLastDamageColor.BackColor = System.Drawing.Color.White;

            // MISC
            rbUberAnimation1.Checked = false;
            rbUberAnimation2.Checked = true;
            rbUberAnimation3.Checked = false;
            btnUberBarColor.BackColor = System.Drawing.Color.White;
            btnUberFullColor.BackColor = System.Drawing.Color.White;
            btnUberFlashColor.BackColor = System.Drawing.Color.White;
            rbChatBoxTop.Checked = false;
            rbChatBoxBottom.Checked = true;
            rbTeamSelectLeft.Checked = false;
            rbTeamSelectCenter.Checked = true;
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
                case "btnXHairColor":
                    btnXHairColor.BackColor = colorPicker.Color;
                    break;

                case "btnXHairPulse":
                    btnXHairPulse.BackColor = colorPicker.Color;
                    break;

                case "btnDamageDone":
                    btnDamageDone.BackColor = colorPicker.Color;
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

                case "btnAmmoClipBlink":
                    btnAmmoClipBlink.BackColor = colorPicker.Color;
                    break;

                case "btnAmmoReserveBlink":
                    btnAmmoReserveBlink.BackColor = colorPicker.Color;
                    break;

                case "btnDamageColor":
                    btnDamageColor.BackColor = colorPicker.Color;
                    break;

                case "btnLastDamageColor":
                    btnLastDamageColor.BackColor = colorPicker.Color;
                    break;

                case "btnUberBarColor":
                    btnUberBarColor.BackColor = colorPicker.Color;
                    break;

                case "btnUberFullColor":
                    btnUberFullColor.BackColor = colorPicker.Color;
                    break;

                case "btnUberFlashColor":
                    btnUberFlashColor.BackColor = colorPicker.Color;
                    break;
            }
        }

        private void cbAspectRatio_SelectedIndexChanged(object sender, EventArgs e)
        {
            cbResolution.Items.Clear();
            if (cbAspectRatio.SelectedIndex == 0)
            {
                cbResolution.Items.Add("1280x720");
                cbResolution.Items.Add("1360x768");
                cbResolution.Items.Add("1366x768");
                cbResolution.Items.Add("1600x900");
                cbResolution.Items.Add("1920x1080");
            }
            else
            {
                cbResolution.Items.Add("640x480");
                cbResolution.Items.Add("520x576");
                cbResolution.Items.Add("800x600");
                cbResolution.Items.Add("1024x768");
                cbResolution.Items.Add("1152x864");
                cbResolution.Items.Add("1024x768");
                cbResolution.Items.Add("1280x960");
                cbResolution.Items.Add("1280x1024");
            }
        }

        private void lbXHairStyles_SelectedIndexChanged(object sender, EventArgs e)
        {

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
    }
}