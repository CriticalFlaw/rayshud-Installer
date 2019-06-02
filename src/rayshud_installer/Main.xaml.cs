﻿using log4net;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;

namespace rayshud_installer
{
    public partial class Main : Window
    {
        private Properties.Settings settings = new Properties.Settings();
        private string app = System.Windows.Forms.Application.StartupPath;
        public static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Main()
        {
            log4net.Config.XmlConfigurator.Configure();
            log.Info("Initializing");
            InitializeComponent();
            SetHUDDirectory();
            CleanUpDirectory();
            LoadHUDSettings();
        }

        /// <summary>
        /// Set the tf/custom directory if not already set
        /// </summary>
        private void SetHUDDirectory()
        {
            if (string.IsNullOrWhiteSpace(settings.app_hud_directory))
            {
                log.Info("Attempting to set the tf/custom directory...");
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (Directory.Exists(drive.Name + settings.app_tf_directory_32))
                    {
                        settings.app_hud_directory = drive.Name + settings.app_tf_directory_32;
                        log.Info("Setting tf/custom directory to " + settings.app_hud_directory);
                        break;
                    }
                    else if (Directory.Exists(drive.Name + settings.app_tf_directory_64))
                    {
                        settings.app_hud_directory = drive.Name + settings.app_tf_directory_64;
                        log.Info("Setting tf/custom directory to " + settings.app_hud_directory);
                        break;
                    }
                }
                log.Info("Unable to find the tf/custom directory. Asking user...");
                if (string.IsNullOrWhiteSpace(settings.app_hud_directory))
                    DisplayFolderBrowser();
                if (string.IsNullOrWhiteSpace(settings.app_hud_directory))
                {
                    log.Info("Unable to find the tf/custom directory. Exiting...");
                    System.Windows.Forms.MessageBox.Show(Properties.Resources.error_app_directory, "Directory Not Set", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    System.Windows.Application.Current.Shutdown();
                }
                settings.Save();
            }
            UpdateGUIButtons();
        }

        /// <summary>
        /// Update the installer controls like labels and buttons
        /// </summary>
        private void UpdateGUIButtons()
        {
            log.Info("Updating User Interface...");
            btn_Start.IsEnabled = false;
            btn_Install.IsEnabled = false;
            btn_Save.IsEnabled = false;
            btn_Uninstall.IsEnabled = false;
            lbl_Status.Content = "Directory is not set...";

            if (Directory.Exists(settings.app_hud_directory) && settings.app_hud_directory.Contains("tf\\custom"))
            {
                btn_Start.IsEnabled = true;
                btn_Install.IsEnabled = true;

                if (CheckHUDInstall())
                {
                    CheckHUDVersion();
                    btn_Install.Content = "Refresh";
                    btn_Save.IsEnabled = true;
                    btn_Uninstall.IsEnabled = true;
                    lbl_Status.Content = "rayshud is installed...";
                }
                else
                {
                    btn_Install.Content = "Install";
                    btn_Save.IsEnabled = false;
                    btn_Uninstall.IsEnabled = false;
                    lbl_Status.Content = "rayshud is not installed...";
                }
                //lbl_Status.Content = settings.app_hud_directory;
                settings.Save();
            }
        }

        /// <summary>
        /// Check if rayshud is installed
        /// </summary>
        public bool CheckHUDInstall()
        {
            if (Directory.Exists(settings.app_hud_directory + "\\rayshud"))
                return true;
            return false;
        }

        /// <summary>
        /// Check the rayshud version number
        /// </summary>
        private void CheckHUDVersion()
        {
            try
            {
                log.Info("Checking rayshud versions...");
                if (!CheckHUDInstall()) return;
                var client = new WebClient();
                var readme_text = client.DownloadString(settings.app_hud_readme).Split('\n');
                settings.app_hud_version_current = readme_text[readme_text.Length - 2];
                settings.app_hud_version_local = File.ReadLines(settings.app_hud_directory + "\\rayshud\\README.md").Last().Trim();
                log.Info("Local rayshud version: " + settings.app_hud_version_local);
                log.Info("Live rayshud version: " + settings.app_hud_version_current);
                if (settings.app_hud_version_local != settings.app_hud_version_current)
                {
                    btn_Install.Content = "Update";
                    lbl_News.Content = "Update Available!";
                }
                settings.Save();
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
            }
        }

        /// <summary>
        /// Apply HUD settings to rayshud files
        /// </summary>
        private void ApplyHUDSettings()
        {
            log.Info("        ======  Start Applying User Settings  ========");
            var writer = new HUDFileWriter();
            writer.defaultBackgrounds();
            writer.mainMenuStyle();
            writer.scoreboardStyle();
            writer.teamSelect();
            writer.healthStyle();
            writer.disguiseImage();
            writer.uberAnimation();
            writer.crosshairPulse();
            writer.classImage();
            writer.chatboxPos();
            writer.crosshair(cb_XHairSize.SelectedItem.ToString());
            writer.colors();
            writer.damagePos();
            log.Info("        ======  Done Applying User Settings  ========");
        }

        /// <summary>
        /// Save GUI settings to file
        /// </summary>
        private void SaveHUDSettings()
        {
            try
            {
                log.Info("        ======  Start Saving User Settings  =========");
                settings.hud_menu_classic = chk_ClassicHUD.IsChecked ?? false;
                settings.hud_scoreboard_minimal = chk_Scoreboard.IsChecked ?? false;
                settings.hud_disguise_image = chk_DisguiseImage.IsChecked ?? false;
                settings.hud_default_backgrounds = chk_DefaultBG.IsChecked ?? false;
                settings.hud_menu_class_image = chk_ClassImage.IsChecked ?? false;
                settings.hud_damage_above = chk_DamagePos.IsChecked ?? false;
                if (chk_UberFlash.IsChecked == true)
                    settings.hud_uber_animation = 0;
                else if (chk_UberSolid.IsChecked == true)
                    settings.hud_uber_animation = 1;
                else
                    settings.hud_uber_animation = 2;
                settings.hud_uber_color_bar = btn_UberBarColor.SelectedColor.Value.ToString();
                settings.hud_uber_color_full = btn_UberFullColor.SelectedColor.Value.ToString();
                settings.hud_uber_color_flash1 = btn_UberFlash1.SelectedColor.Value.ToString();
                settings.hud_uber_color_flash2 = btn_UberFlash2.SelectedColor.Value.ToString();
                settings.hud_xhair_style = cb_XHairStyle.SelectedIndex;
                settings.hud_xhair_size = cb_XHairSize.SelectedIndex;
                settings.hud_xhair_color_base = btn_XHairColor.SelectedColor.Value.ToString();
                settings.hud_xhair_color_pulse = btn_XHairPulse.SelectedColor.Value.ToString();
                settings.hud_xhair_enable = chk_XHairEnable.IsChecked ?? false;
                settings.hud_xhair_outline = chk_XHairOutline.IsChecked ?? false;
                settings.hud_xhair_pulse = chk_XHairPulse.IsChecked ?? false;
                settings.hud_health_style = lb_HealthStyle.SelectedIndex;
                settings.hud_health_normal = btn_HealthNormal.SelectedColor.Value.ToString();
                settings.hud_healing_done = btn_HealingDone.SelectedColor.Value.ToString();
                settings.hud_health_buff = btn_HealthBuff.SelectedColor.Value.ToString();
                settings.hud_health_low = btn_HealthLow.SelectedColor.Value.ToString();
                settings.hud_ammo_clip = btn_AmmoClip.SelectedColor.Value.ToString();
                settings.hud_ammo_reserve = btn_AmmoReserve.SelectedColor.Value.ToString();
                settings.hud_ammo_clip_low = btn_AmmoClipLow.SelectedColor.Value.ToString();
                settings.hud_ammo_reserve_low = btn_AmmoReserveLow.SelectedColor.Value.ToString();
                settings.hud_team_class_center = chk_TeamCenter.IsChecked ?? false;
                settings.hud_chat_bottom = chk_ChatBottom.IsChecked ?? false;
                settings.app_hud_mod_date = DateTime.Now;
                settings.Save();
                log.Info("        ======  Done Saving User Settings  =========");
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(Properties.Resources.error_app_save + "\n" + ex.Message, "Error: Saving Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
                log.Error(ex.Message);
            }
        }

        /// <summary>
        /// Load GUI settings from file
        /// </summary>
        private void LoadHUDSettings()
        {
            try
            {
                log.Info("        ======  Start Loading User Settings  ========");
                var cc = new ColorConverter();
                chk_ClassicHUD.IsChecked = settings.hud_menu_classic;
                chk_Scoreboard.IsChecked = settings.hud_scoreboard_minimal;
                chk_DisguiseImage.IsChecked = settings.hud_disguise_image;
                chk_DefaultBG.IsChecked = settings.hud_default_backgrounds;
                chk_ClassImage.IsChecked = settings.hud_menu_class_image;
                chk_DamagePos.IsChecked = settings.hud_damage_above;
                switch (settings.hud_uber_animation)
                {
                    default:
                        chk_UberSolid.IsChecked = true;
                        break;

                    case 1:
                        chk_UberFlash.IsChecked = true;
                        break;

                    case 2:
                        chk_UberRainbow.IsChecked = true;
                        break;
                }
                btn_UberBarColor.SelectedColor = (Color)cc.ConvertFrom(settings.hud_uber_color_bar);
                btn_UberFullColor.SelectedColor = (Color)cc.ConvertFrom(settings.hud_uber_color_full);
                btn_UberFlash1.SelectedColor = (Color)cc.ConvertFrom(settings.hud_uber_color_flash1);
                btn_UberFlash2.SelectedColor = (Color)cc.ConvertFrom(settings.hud_uber_color_flash2);
                cb_XHairStyle.SelectedIndex = settings.hud_xhair_style;
                for (var i = 8; i <= 40; i += 2)
                    cb_XHairSize.Items.Add(i);
                cb_XHairSize.SelectedIndex = settings.hud_xhair_size;
                btn_XHairColor.SelectedColor = (Color)cc.ConvertFrom(settings.hud_xhair_color_base);
                btn_XHairPulse.SelectedColor = (Color)cc.ConvertFrom(settings.hud_xhair_color_pulse);
                chk_XHairEnable.IsChecked = settings.hud_xhair_enable;
                chk_XHairOutline.IsChecked = settings.hud_xhair_outline;
                chk_XHairPulse.IsChecked = settings.hud_xhair_pulse;
                lb_HealthStyle.SelectedIndex = settings.hud_health_style;
                btn_HealthNormal.SelectedColor = (Color)cc.ConvertFrom(settings.hud_health_normal);
                btn_HealingDone.SelectedColor = (Color)cc.ConvertFrom(settings.hud_healing_done);
                btn_HealthBuff.SelectedColor = (Color)cc.ConvertFrom(settings.hud_health_buff);
                btn_HealthLow.SelectedColor = (Color)cc.ConvertFrom(settings.hud_health_low);
                btn_AmmoClip.SelectedColor = (Color)cc.ConvertFrom(settings.hud_ammo_clip);
                btn_AmmoReserve.SelectedColor = (Color)cc.ConvertFrom(settings.hud_ammo_reserve);
                btn_AmmoClipLow.SelectedColor = (Color)cc.ConvertFrom(settings.hud_ammo_clip_low);
                btn_AmmoReserveLow.SelectedColor = (Color)cc.ConvertFrom(settings.hud_ammo_reserve_low);
                chk_TeamCenter.IsChecked = settings.hud_team_class_center;
                chk_ChatBottom.IsChecked = settings.hud_chat_bottom;
                log.Info("        ======  Done Loading User Settings  ========");
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(Properties.Resources.error_app_load + "\n" + ex.Message, "Error: Loading Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
                log.Error(ex.Message);
            }
        }

        /// <summary>
        /// Revert GUI settings to default
        /// </summary>
        private void ResetHUDSettings()
        {
            try
            {
                log.Info("        ======  Start Resetting User Settings  =======");
                var cc = new ColorConverter();
                chk_ClassicHUD.IsChecked = false;
                chk_Scoreboard.IsChecked = false;
                chk_DisguiseImage.IsChecked = false;
                chk_DefaultBG.IsChecked = false;
                chk_ClassImage.IsChecked = false;
                chk_DamagePos.IsChecked = false;
                chk_UberFlash.IsChecked = true;
                btn_UberBarColor.SelectedColor = (Color)cc.ConvertFrom("#EBE2CA");
                btn_UberBarColor.SelectedColor = (Color)cc.ConvertFrom("#EBE2CA");
                btn_UberFullColor.SelectedColor = (Color)cc.ConvertFrom("#FF3219");
                btn_UberFlash1.SelectedColor = (Color)cc.ConvertFrom("#FFA500");
                btn_UberFlash2.SelectedColor = (Color)cc.ConvertFrom("#FF4500");
                cb_XHairStyle.SelectedIndex = 0;
                cb_XHairSize.SelectedIndex = 0;
                btn_XHairColor.SelectedColor = (Color)cc.ConvertFrom("#F2F2F2");
                btn_XHairPulse.SelectedColor = (Color)cc.ConvertFrom("#FF0000");
                chk_XHairEnable.IsChecked = false;
                chk_XHairOutline.IsChecked = false;
                chk_XHairPulse.IsChecked = false;
                lb_HealthStyle.SelectedIndex = 0;
                btn_HealthNormal.SelectedColor = (Color)cc.ConvertFrom("#EBE2CA");
                btn_HealingDone.SelectedColor = (Color)cc.ConvertFrom("#30FF30");
                btn_HealthBuff.SelectedColor = (Color)cc.ConvertFrom("#30FF30");
                btn_HealthLow.SelectedColor = (Color)cc.ConvertFrom("#FF9900");
                btn_AmmoClip.SelectedColor = (Color)cc.ConvertFrom("#30FF30");
                btn_AmmoReserve.SelectedColor = (Color)cc.ConvertFrom("#48FFFF");
                btn_AmmoClipLow.SelectedColor = (Color)cc.ConvertFrom("#FF2A82");
                btn_AmmoReserveLow.SelectedColor = (Color)cc.ConvertFrom("#FF801C");
                chk_TeamLeft.IsChecked = true;
                chk_ChatTop.IsChecked = true;
                log.Info("        ======  Done Resetting User Settings  =======");
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(Properties.Resources.error_app_reset + "\n" + ex.Message, "Error: Resetting Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
                log.Error(ex.Message);
            }
        }

        /// <summary>
        /// Display the folder browser dialog, asking the user to provide the tf/custom directory
        /// </summary>
        private void DisplayFolderBrowser()
        {
            log.Info("Opening Folder Browser");
            var directoryBrowser = new FolderBrowserDialog { Description = $"Please select your tf\\custom folder. If the correct directory is not provided, the options to install and modify rayshud will not be available.", ShowNewFolderButton = true };
            while (!directoryBrowser.SelectedPath.Contains("tf\\custom"))
            {
                if (directoryBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (directoryBrowser.SelectedPath.Contains("tf\\custom"))
                    {
                        settings.app_hud_directory = directoryBrowser.SelectedPath;
                        lbl_Status.Content = settings.app_hud_directory;
                    }
                }
                else
                    break;
            }
        }

        /// <summary>
        /// Convert color RGB values to HEX
        /// </summary>
        private static string HexConverter(Color c)
        {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }

        /// <summary>
        /// Cleans up the tf/custom and installer directories
        /// </summary>
        private void CleanUpDirectory()
        {
            log.Info("Cleaning up rayshud directories....");
            if (File.Exists(app + "\\rayshud.zip"))
                File.Delete(app + "\\rayshud.zip");
            if (Directory.Exists(settings.app_hud_directory + "\\rayshud-master"))
            {
                if (File.Exists(settings.app_hud_directory + "\\rayshud-backup.zip"))
                    File.Delete(settings.app_hud_directory + "\\rayshud-backup.zip");
                ZipFile.CreateFromDirectory(settings.app_hud_directory + "\\rayshud-master", settings.app_hud_directory + "\\rayshud-backup.zip");
                Directory.Delete(settings.app_hud_directory + "\\rayshud-master", true);
                System.Windows.Forms.MessageBox.Show("An existing rayshud-master folder has been found. To avoid conflicts, a backup of the file has been created in tf/custom/rayshud-backup.zip", "Backup Created", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        #region CLICK EVENTS

        /// <summary>
        /// Installs rayshud to the user's tf/custom folder
        /// </summary>
        private void Btn_Install_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                log.Info("        ======  Start Installing rayshud  ===========");
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                var client = new WebClient();
                log.Info("Downloading latest rayshud...");
                client.DownloadFile(settings.app_hud_download, "rayshud.zip");
                log.Info("Extracting downloaded rayshud to " + settings.app_hud_directory);
                ZipFile.ExtractToDirectory(app + "\\rayshud.zip", settings.app_hud_directory);
                if (btn_Install.Content.ToString() != "Install")
                    Directory.Delete(settings.app_hud_directory + "\\rayshud", true);
                log.Info("Normalizing the installed rayshud folder name");
                Directory.Move(settings.app_hud_directory + "\\rayshud-master", settings.app_hud_directory + "\\rayshud");
                lbl_News.Content = $"Install Successful! {DateTime.Now}";
                settings.Save();
                CleanUpDirectory();
                SaveHUDSettings();
                ApplyHUDSettings();
                UpdateGUIButtons();
                log.Info("        ======  Done Installing rayshud  ==========");
                System.Windows.Forms.MessageBox.Show("rayshud has been successfully installed", "Install Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(Properties.Resources.error_app_install, "Error: Installing rayshud", MessageBoxButtons.OK, MessageBoxIcon.Error);
                log.Error(ex.Message);
            }
        }

        /// <summary>
        /// Removes rayshud from the user's tf/custom folder
        /// </summary>
        private void Btn_Uninstall_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                log.Info("        ======  Start Uninstalling rayshud  ========");
                if (CheckHUDInstall())
                {
                    Directory.Delete(settings.app_hud_directory + "\\rayshud", true);
                    lbl_News.Content = $"Uninstall Successful! {DateTime.Now}";
                    settings.Save();
                }
                SetHUDDirectory();
                log.Info("        ======  Done Uninstalling rayshud  ========");
                System.Windows.Forms.MessageBox.Show("rayshud has been successfully uninstalled", "Uninstall Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(Properties.Resources.error_app_uninstall + "\n" + ex.Message, "Error: Uninstalling rayshud", MessageBoxButtons.OK, MessageBoxIcon.Error);
                log.Error(ex.Message);
            }
        }

        /// <summary>
        /// Calls to save and apply the rayshud settings
        /// </summary>
        private void Btn_Save_Click(object sender, RoutedEventArgs e)
        {
            SaveHUDSettings();
            ApplyHUDSettings();
            lbl_News.Content = $"Settings Saved! {DateTime.Now}";
        }

        /// <summary>
        /// Calls to reset the rayshud settings to default
        /// </summary>
        private void Btn_Reset_Click(object sender, RoutedEventArgs e)
        {
            ResetHUDSettings();
            lbl_News.Content = $"Settings Reset! {DateTime.Now}";
        }

        /// <summary>
        /// Starts Team Fortress 2 through Steam
        /// </summary>
        private void Btn_Start_Click(object sender, RoutedEventArgs e)
        {
            log.Info("Starting Team Fortress 2...");
            Process.Start("steam://rungameid/440");
        }

        /// <summary>
        /// Opens the directory browser to allow the user to change the tf/custom directory
        /// </summary>
        private void Btn_ChangeDirectory_Click(object sender, RoutedEventArgs e)
        {
            log.Info("Opening tf/custom directory...");
            DisplayFolderBrowser();
        }

        /// <summary>
        /// Opens the rayshud Steam Group in a web browser
        /// </summary>
        private void Btn_SteamGroup_Click(object sender, RoutedEventArgs e)
        {
            log.Info("Opening rayshud Steam Group...");
            Process.Start("https://steamcommunity.com/groups/rayshud");
        }

        /// <summary>
        /// Opens the GitHub issue tracker in a web browser
        /// </summary>
        private void Btn_ReportIssue_Click(object sender, RoutedEventArgs e)
        {
            log.Info("Opening GitHub issue tracker...");
            Process.Start("https://github.com/CriticalFlaw/rayshud-Installer/issues");
        }

        #endregion CLICK EVENTS
    }
}