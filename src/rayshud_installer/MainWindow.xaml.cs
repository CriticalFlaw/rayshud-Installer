using AutoUpdaterDotNET;
using log4net;
using Microsoft.Win32;
using rayshud_installer.Properties;
using System;
using System.Collections.Generic;
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
    public partial class MainWindow : Window
    {
        private readonly string appPath = System.Windows.Forms.Application.StartupPath;
        public static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public MainWindow()
        {
            log4net.Config.XmlConfigurator.Configure();
            logger.Info("Initializing");
            InitializeComponent();
            SetupDirectory();
            LoadHUDSettings();
            AutoUpdater.OpenDownloadPage = true;
            AutoUpdater.Start("https://raw.githubusercontent.com/CriticalFlaw/rayshud-Installer/master/Update.xml");
        }

        /// <summary>
        /// Set the tf/custom directory if not already set
        /// </summary>
        private void SetupDirectory(bool userOverride = false)
        {
            if (userOverride || (!SearchRegistry() && !CheckUserPath()))
            {
                logger.Info("> Asking user to provide the tf/custom directory.");
                ShowFolderBrowser();
                if (!CheckUserPath())
                {
                    logger.Info("> Unable to setup tf/custom directory. Exiting...");
                    System.Windows.Forms.MessageBox.Show(Properties.Resources.error_app_directory, "Directory Not Set", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    System.Windows.Application.Current.Shutdown();
                }
            }
            CleanDirectory();
            UpdateGUIButtons();
        }

        private bool SearchRegistry()
        {
            logger.Info("> Attempting to find Team Fortress 2 directory automatically.");
            var keyPath = (Environment.Is64BitProcess) ? @"HKEY_LOCAL_MACHINE\Software\Wow6432Node\Valve\Steam" : @"HKEY_LOCAL_MACHINE\Software\Valve\Steam";
            var directory = (string)Registry.GetValue(keyPath, "InstallPath", null);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                directory += "\\steamapps\\common\\Team Fortress 2\\tf\\custom";
                if (Directory.Exists(directory))
                {
                    rayshud.Default.hud_directory = directory;
                    rayshud.Default.Save();
                    logger.Info("> Found Team Fortress 2 directory in " + rayshud.Default.hud_directory);
                    return true;
                }
            }
            logger.Info("> Unable to find Team Fortress 2 directory automatically.");
            return false;
        }

        /// <summary>
        /// Cleans up the tf/custom and installer directories
        /// </summary>
        private void CleanDirectory()
        {
            logger.Info("> Cleaning up rayshud directories.");
            var settings = rayshud.Default;

            // Clean the application directory
            if (File.Exists(appPath + "\\rayshud.zip"))
            {
                logger.Info("> Found a zipped rayshud download. Removing...");
                File.Delete(appPath + "\\rayshud.zip");
            }

            // Clean the tf/custom directory
            if (Directory.Exists(settings.hud_directory + "\\rayshud-master"))
            {
                if (File.Exists(settings.hud_directory + "\\rayshud-backup.zip"))
                {
                    logger.Info("> Found an existing rayshud backup. Removing...");
                    File.Delete(settings.hud_directory + "\\rayshud-backup.zip");
                }
                logger.Info("> Found an existing rayshud install. Backing up...");
                ZipFile.CreateFromDirectory(settings.hud_directory + "\\rayshud-master", settings.hud_directory + "\\rayshud-backup.zip");
                Directory.Delete(settings.hud_directory + "\\rayshud-master", true);
                System.Windows.Forms.MessageBox.Show("An existing rayshud-master folder has been found. To avoid conflicts, a backup of the file has been created in tf/custom/rayshud-backup.zip", "Backup Created", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Calls to download and install rayshud
        /// </summary>
        private void InstallHUD()
        {
            try
            {
                logger.Info("> Start installing rayshud.");
                ShowBusyIndicator();
                DownloadHUD();
                ExtractHUD();
                CleanDirectory();
                SaveHUDSettings();
                ApplyHUDSettings();
                UpdateGUIButtons();
                logger.Info("> Done installing rayshud.");
                System.Windows.Forms.MessageBox.Show("rayshud has been successfully installed", "Install Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Installing rayshud", Properties.Resources.error_app_install, ex.Message);
            }
            finally
            {
                ShowBusyIndicator(false);
            }
        }

        /// <summary>
        /// Calls to download the latest version of rayshud
        /// </summary>
        private void DownloadHUD()
        {
            logger.Info("> Downloading latest rayshud...");
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            var client = new WebClient();
            client.DownloadFile(Properties.Resources.app_download, "rayshud.zip");
            client.Dispose();
            logger.Info("> Done downloading latest rayshud.");
        }

        /// <summary>
        /// Calls to extract rayshud to the tf/custom directory
        /// </summary>
        private void ExtractHUD(bool update = false)
        {
            var settings = rayshud.Default;
            logger.Info("> Extracting downloaded rayshud to " + settings.hud_directory);
            var updateMode = (Install.Content.ToString() == "Update") ? true : false;
            ZipFile.ExtractToDirectory(appPath + "\\rayshud.zip", settings.hud_directory);
            if (update) // TODO: Refactor the update-refresh-install process
                Directory.Delete(settings.hud_directory + "\\rayshud", true);
            logger.Info("> Normalizing the installed rayshud folder name");
            if (Directory.Exists(settings.hud_directory + "\\rayshud"))
                Directory.Delete(settings.hud_directory + "\\rayshud", true);
            if (Directory.Exists(settings.hud_directory + "\\rayshud-master"))
                Directory.Move(settings.hud_directory + "\\rayshud-master", settings.hud_directory + "\\rayshud");
            lblNews.Content = "Install finished at " + DateTime.Now;
            logger.Info("> Done downloading latest rayshud.");
        }

        /// <summary>
        /// Calls to uninstall rayshud
        /// </summary>
        private void UninstallHUD()
        {
            try
            {
                logger.Info("> Start uninstalling rayshud.");
                ShowBusyIndicator();
                var settings = rayshud.Default;
                if (!CheckHUDInstall()) return;
                Directory.Delete(settings.hud_directory + "\\rayshud", true);
                lblNews.Content = "Uninstalled rayshud at " + DateTime.Now;
                SetupDirectory();
                logger.Info("> Done uninstalling rayshud.");
                System.Windows.Forms.MessageBox.Show("rayshud has been successfully uninstalled", "rayshud Removed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Uninstalling rayshud", Properties.Resources.error_app_uninstall, ex.Message);
            }
            finally
            {
                ShowBusyIndicator(false);
            }
        }

        /// <summary>
        /// Update the installer controls like labels and buttons
        /// </summary>
        private void UpdateGUIButtons()
        {
            var settings = rayshud.Default;
            Start.Visibility = Visibility.Hidden;
            Install.Visibility = Visibility.Hidden;
            Save.Visibility = Visibility.Hidden;
            Uninstall.Visibility = Visibility.Hidden;
            lblStatus.Content = "Directory is not set...";

            if (Directory.Exists(settings.hud_directory) && CheckUserPath())
            {
                Start.Visibility = Visibility.Visible;
                Install.Visibility = Visibility.Visible;

                if (CheckHUDInstall())
                {
                    CheckHUDVersion();
                    Install.Content = "Refresh";
                    Save.Visibility = Visibility.Visible;
                    Uninstall.Visibility = Visibility.Visible;
                    lblStatus.Content = "rayshud is installed...";
                }
                else
                {
                    Install.Content = "Install";
                    Save.Visibility = Visibility.Hidden;
                    Uninstall.Visibility = Visibility.Hidden;
                    lblStatus.Content = "rayshud is not installed...";
                }
                settings.Save();
            }
        }

        /// <summary>
        /// Check if rayshud is installed in the tf/custom directory
        /// </summary>
        public bool CheckHUDInstall()
        {
            if (Directory.Exists(rayshud.Default.hud_directory + "\\rayshud"))
                return true;
            return false;
        }

        /// <summary>
        /// Check if user's directory setting is valid
        /// </summary>
        public bool CheckUserPath()
        {
            var settings = rayshud.Default;
            if (string.IsNullOrWhiteSpace(settings.hud_directory) || !settings.hud_directory.Contains("tf\\custom"))
                return false;
            return true;
        }

        /// <summary>
        /// Check the rayshud version number
        /// </summary>
        private void CheckHUDVersion()
        {
            try
            {
                logger.Info("> Checking rayshud versions.");
                if (!CheckHUDInstall()) return;
                var client = new WebClient();
                var readme_text = client.DownloadString(Properties.Resources.app_readme).Split('\n');
                client.Dispose();
                var current = readme_text[readme_text.Length - 2];
                var local = File.ReadLines(rayshud.Default.hud_directory + "\\rayshud\\README.md").Last().Trim();
                logger.Info("> Local version: " + local + "\t" + "> Live version: " + current);
                if (local != current)
                {
                    logger.Info("> Version mismatch. New update available.");
                    Install.Content = "Update";
                    lblNews.Content = "Update Available!";
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }
        }

        /// <summary>
        /// Apply user settings to rayshud files
        /// </summary>
        private void ApplyHUDSettings()
        {
            try
            {
                logger.Info("> Start applying user settings to rayshud");
                ShowBusyIndicator();
                var writer = new FileWriter();
                writer.MainMenuBackground();
                writer.MainMenuStyle();
                writer.ScoreboardStyle();
                writer.TeamSelect();
                writer.HealthStyle();
                writer.DisguiseImage();
                writer.UberchargeStyle();
                writer.CrosshairPulse();
                writer.MainMenuClassImage();
                writer.ChatBoxPos();
                writer.Crosshair(int.Parse(cbXHairSize.Text), tbXHairXPos.Value, tbXHairYPos.Value);
                writer.Colors();
                writer.DamagePos();
                writer.TransparentViewmodels();
                writer.PlayerModelPos();
                lblNews.Content = "Settings Saved at " + DateTime.Now;
                logger.Info("> Done applying user settings to rayshud");
            }
            finally
            {
                ShowBusyIndicator(false);
            }
        }

        #region SAVE, LOAD, RESET

        /// <summary>
        /// Save user settings to the file
        /// </summary>
        private void SaveHUDSettings()
        {
            try
            {
                logger.Info("> Start saving user settings.");
                ShowBusyIndicator();
                var settings = rayshud.Default;

                settings.toggle_classic_menu = chkClassicHUD.IsChecked ?? false;
                settings.toggle_min_scoreboard = chkScoreboard.IsChecked ?? false;
                settings.toggle_disguise_image = chkDisguiseImage.IsChecked ?? false;
                settings.toggle_stock_backgrounds = chkDefaultBG.IsChecked ?? false;
                settings.toggle_menu_images = chkMenuImages.IsChecked ?? false;
                settings.toggle_damage_pos = chkDamagePos.IsChecked ?? false;
                settings.toggle_center_select = chkTeamCenter.IsChecked ?? false;
                settings.toggle_chat_bottom = chkChatBottom.IsChecked ?? false;
                settings.toggle_xhair_outline = chkXHairOutline.IsChecked ?? false;
                settings.toggle_transparent_viewmodels = chkTransparentVM.IsChecked ?? false;
                settings.toggle_alt_player_model = chkPlayerModel.IsChecked ?? false;

                if (rbUberFlash.IsChecked == true)
                    settings.val_uber_animaton = (int)UberchargeStyles.Flash;
                else if (rbUberSolid.IsChecked == true)
                    settings.val_uber_animaton = (int)UberchargeStyles.Solid;
                else if (rbUberRainbow.IsChecked == true)
                    settings.val_uber_animaton = (int)UberchargeStyles.Rainbow;

                settings.color_uber_bar = cpUberBarColor.SelectedColor.Value.ToString();
                settings.color_uber_full = cpUberFullColor.SelectedColor.Value.ToString();
                settings.color_uber_flash1 = cpUberFlash1.SelectedColor.Value.ToString();
                settings.color_uber_flash2 = cpUberFlash2.SelectedColor.Value.ToString();

                settings.val_xhair_style = cbXHairStyle.SelectedIndex;
                settings.val_xhair_size = cbXHairSize.SelectedIndex;
                settings.color_xhair_normal = cpXHairColor.SelectedColor.Value.ToString();
                settings.color_xhair_pulse = cpXHairPulse.SelectedColor.Value.ToString();
                settings.toggle_xhair_enable = chkXHairEnable.IsChecked ?? false;
                settings.toggle_xhair_pulse = chkXHairPulse.IsChecked ?? false;

                settings.val_health_style = cbHealthStyle.SelectedIndex;
                settings.color_health_normal = cpHealthNormal.SelectedColor.Value.ToString();
                settings.color_health_healed = cpHealingDone.SelectedColor.Value.ToString();
                settings.color_health_buffed = cpHealthBuff.SelectedColor.Value.ToString();
                settings.color_health_low = cpHealthLow.SelectedColor.Value.ToString();

                settings.color_ammo_clip = cpAmmoClip.SelectedColor.Value.ToString();
                settings.color_ammo_clip_low = cpAmmoClipLow.SelectedColor.Value.ToString();
                settings.color_ammo_reserve = cpAmmoReserve.SelectedColor.Value.ToString();
                settings.color_ammo_reserve_low = cpAmmoReserveLow.SelectedColor.Value.ToString();

                settings.Save();
                logger.Info("> Done saving user settings.");
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Saving Settings", Properties.Resources.error_app_save, ex.Message);
            }
            finally
            {
                ShowBusyIndicator(false);
            }
        }

        /// <summary>
        /// Load GUI with user settings from the file
        /// </summary>
        private void LoadHUDSettings()
        {
            try
            {
                logger.Info("> Start loading user settings.");
                ShowBusyIndicator();
                var settings = rayshud.Default;
                var cc = new ColorConverter();

                chkClassicHUD.IsChecked = settings.toggle_classic_menu;
                chkScoreboard.IsChecked = settings.toggle_min_scoreboard;
                chkDisguiseImage.IsChecked = settings.toggle_disguise_image;
                chkDefaultBG.IsChecked = settings.toggle_stock_backgrounds;
                chkMenuImages.IsChecked = settings.toggle_menu_images;
                chkDamagePos.IsChecked = settings.toggle_damage_pos;
                chkChatBottom.IsChecked = settings.toggle_chat_bottom;
                chkTeamCenter.IsChecked = settings.toggle_center_select;
                chkXHairOutline.IsChecked = settings.toggle_xhair_outline;
                chkTransparentVM.IsChecked = settings.toggle_transparent_viewmodels;
                chkPlayerModel.IsChecked = settings.toggle_alt_player_model;

                switch (settings.val_uber_animaton)
                {
                    case (int)UberchargeStyles.Flash:
                        rbUberFlash.IsChecked = true;
                        break;

                    case (int)UberchargeStyles.Solid:
                        rbUberSolid.IsChecked = true;
                        break;

                    case (int)UberchargeStyles.Rainbow:
                        rbUberRainbow.IsChecked = true;
                        break;
                }
                cpUberBarColor.SelectedColor = (Color)cc.ConvertFrom(settings.color_uber_bar);
                cpUberFullColor.SelectedColor = (Color)cc.ConvertFrom(settings.color_uber_full);
                cpUberFlash1.SelectedColor = (Color)cc.ConvertFrom(settings.color_uber_flash1);
                cpUberFlash2.SelectedColor = (Color)cc.ConvertFrom(settings.color_uber_flash2);

                cbXHairStyle.SelectedIndex = settings.val_xhair_style;
                cbXHairSize.SelectedIndex = settings.val_xhair_size;
                cpXHairColor.SelectedColor = (Color)cc.ConvertFrom(settings.color_xhair_normal);
                cpXHairPulse.SelectedColor = (Color)cc.ConvertFrom(settings.color_xhair_pulse);
                chkXHairEnable.IsChecked = settings.toggle_xhair_enable;
                chkXHairPulse.IsChecked = settings.toggle_xhair_pulse;

                cbHealthStyle.SelectedIndex = settings.val_health_style;
                cpHealthNormal.SelectedColor = (Color)cc.ConvertFrom(settings.color_health_normal);
                cpHealingDone.SelectedColor = (Color)cc.ConvertFrom(settings.color_health_healed);
                cpHealthBuff.SelectedColor = (Color)cc.ConvertFrom(settings.color_health_buffed);
                cpHealthLow.SelectedColor = (Color)cc.ConvertFrom(settings.color_health_low);

                cpAmmoClip.SelectedColor = (Color)cc.ConvertFrom(settings.color_ammo_clip);
                cpAmmoClipLow.SelectedColor = (Color)cc.ConvertFrom(settings.color_ammo_clip_low);
                cpAmmoReserve.SelectedColor = (Color)cc.ConvertFrom(settings.color_ammo_reserve);
                cpAmmoReserveLow.SelectedColor = (Color)cc.ConvertFrom(settings.color_ammo_reserve_low);

                logger.Info("> Done loading user settings.");
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Loading Settings", Properties.Resources.error_app_load, ex.Message);
            }
            finally
            {
                ShowBusyIndicator(false);
            }
        }

        /// <summary>
        /// Reset user settings to their default values
        /// </summary>
        private void ResetHUDSettings()
        {
            try
            {
                logger.Info("> Start resetting user settings.");
                ShowBusyIndicator();
                var cc = new ColorConverter();

                chkClassicHUD.IsChecked = false;
                chkScoreboard.IsChecked = false;
                chkDisguiseImage.IsChecked = false;
                chkDefaultBG.IsChecked = false;
                chkMenuImages.IsChecked = false;
                chkDamagePos.IsChecked = false;
                rbUberFlash.IsChecked = true;
                chkChatBottom.IsChecked = false;
                chkTeamCenter.IsChecked = false;
                chkXHairOutline.IsChecked = false;
                chkTransparentVM.IsChecked = false;
                chkPlayerModel.IsChecked = false;

                cpUberBarColor.SelectedColor = (Color)cc.ConvertFrom("#EBE2CA");
                cpUberFullColor.SelectedColor = (Color)cc.ConvertFrom("#FF3219");
                cpUberFlash1.SelectedColor = (Color)cc.ConvertFrom("#FFA500");
                cpUberFlash2.SelectedColor = (Color)cc.ConvertFrom("#FF4500");

                cbXHairStyle.SelectedIndex = (int)CrosshairStyles.BasicCross;
                cbXHairSize.SelectedIndex = 0;
                cpXHairColor.SelectedColor = (Color)cc.ConvertFrom("#F2F2F2");
                cpXHairPulse.SelectedColor = (Color)cc.ConvertFrom("#FF0000");
                chkXHairEnable.IsChecked = false;
                chkXHairPulse.IsChecked = false;

                cbHealthStyle.SelectedIndex = (int)HealthStyles.Default;
                cpHealthNormal.SelectedColor = (Color)cc.ConvertFrom("#EBE2CA");
                cpHealingDone.SelectedColor = (Color)cc.ConvertFrom("#30FF30");
                cpHealthBuff.SelectedColor = (Color)cc.ConvertFrom("#30FF30");
                cpHealthLow.SelectedColor = (Color)cc.ConvertFrom("#FF9900");

                cpAmmoClip.SelectedColor = (Color)cc.ConvertFrom("#30FF30");
                cpAmmoClipLow.SelectedColor = (Color)cc.ConvertFrom("#FF2A82");
                cpAmmoReserve.SelectedColor = (Color)cc.ConvertFrom("#48FFFF");
                cpAmmoReserveLow.SelectedColor = (Color)cc.ConvertFrom("#FF801C");

                lblNews.Content = "Settings Reset at " + DateTime.Now;
                logger.Info("> Done resetting user settings.");
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Resetting Settings", Properties.Resources.error_app_reset, ex.Message);
            }
            finally
            {
                ShowBusyIndicator(false);
            }
        }

        #endregion SAVE, LOAD, RESET

        /// <summary>
        /// Show the folder browser, asks the user to provide the tf/custom directory
        /// </summary>
        private void ShowFolderBrowser()
        {
            // TODO: Possible bug, consider refactoring
            var settings = rayshud.Default;
            logger.Info("> Opening folder browser.");
            using (var directoryBrowser = new FolderBrowserDialog { Description = $"Please select your tf\\custom folder. If the correct directory is not provided, the options to install and modify rayshud will not be available.", ShowNewFolderButton = true })
            {
                while (!directoryBrowser.SelectedPath.Contains("tf\\custom"))
                {
                    if (directoryBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        if (directoryBrowser.SelectedPath.Contains("tf\\custom"))
                        {
                            settings.hud_directory = directoryBrowser.SelectedPath;
                            lblStatus.Content = settings.hud_directory;
                            logger.Info("> User set the directory to " + lblStatus.Content);
                            settings.Save();
                        }
                    }
                    else
                        break;
                }
            }
        }

        /// <summary>
        /// Display or hide the busy indicator
        /// </summary>
        private void ShowBusyIndicator(bool display = true)
        {
            busyIndicator.IsBusy = display;
        }

        /// <summary>
        /// Display the error message box
        /// </summary>
        public static void ShowErrorMessage(string title, string message, string exception)
        {
            System.Windows.Forms.MessageBox.Show(message + ". " + exception, "Error: " + title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            logger.Error("Error: " + exception);
        }

        #region CLICK EVENTS

        /// <summary>
        /// Installs rayshud to the user's tf/custom folder
        /// </summary>
        private void Install_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("User clicked to install rayshud.");
            InstallHUD();
        }

        /// <summary>
        /// Removes rayshud from the user's tf/custom folder
        /// </summary>
        private void Uninstall_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("User clicked to uninstall rayshud.");
            UninstallHUD();
        }

        /// <summary>
        /// Saves then applies the rayshud settings
        /// </summary>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("User clicked to save rayshud settings.");
            SaveHUDSettings();
            ApplyHUDSettings();
        }

        /// <summary>
        /// Resets the rayshud settings to the default
        /// </summary>
        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("User clicked to reset rayshud settings.");
            ResetHUDSettings();
        }

        /// <summary>
        /// Opens the directory browser to let the user to set their tf/custom directory
        /// </summary>
        private void ChangeDirectory_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("User clicked to open the directory browser.");
            SetupDirectory(true);
        }

        /// <summary>
        /// Opens the GitHub issue tracker in a web browser
        /// </summary>
        private void ReportIssue_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("User clicked to open the issue tracker.");
            Process.Start("https://github.com/CriticalFlaw/rayshud-Installer/issues");
        }

        /// <summary>
        /// Launches Team Fortress 2 through Steam
        /// </summary>
        private void Start_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("User clicked to launch Team Fortress 2.");
            Process.Start("steam://rungameid/440");
        }

        #endregion CLICK EVENTS

        #region CROSSHAIRS

        private static readonly Dictionary<CrosshairStyles, Tuple<int, int, string>> crosshairs = new Dictionary<CrosshairStyles, Tuple<int, int, string>>
        {
            { CrosshairStyles.BasicCross, new Tuple<int, int, string>(109,99,"2") },
            { CrosshairStyles.BasicDot, new Tuple<int, int, string>(103, 100, "3") },
            { CrosshairStyles.CircleDot, new Tuple<int, int, string>(100, 96, "8") },
            { CrosshairStyles.OpenCross, new Tuple<int, int, string>(85, 100, "i") },
            { CrosshairStyles.OpenCrossDot, new Tuple<int, int, string>(85, 100, "h") },
            { CrosshairStyles.ScatterSpread, new Tuple<int, int, string>(99, 99, "0") },
            { CrosshairStyles.ThinCircle, new Tuple<int, int, string>(100, 96, "9") },
            { CrosshairStyles.Wings, new Tuple<int, int, string>(100, 97, "d") },
            { CrosshairStyles.WingsPlus, new Tuple<int, int, string>(100, 97, "c") },
            { CrosshairStyles.WingsSmall, new Tuple<int, int, string>(100, 97, "g") },
            { CrosshairStyles.WingsSmallDot, new Tuple<int, int, string>(100, 97, "f") },
            { CrosshairStyles.xHairCircle, new Tuple<int, int, string>(100, 102, "0") },
            { CrosshairStyles.KonrWings, new Tuple<int, int, string>(108, 99, "i") }
        };

        private void XHairStyle_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var values = crosshairs[(CrosshairStyles)cbXHairStyle.SelectedIndex];
            tbXHairXPos.Text = values.Item1.ToString();
            tbXHairYPos.Text = values.Item2.ToString();
        }

        public static string GetCrosshairStyle(CrosshairStyles index)
        {
            return crosshairs[index].Item3;
        }

        #endregion CROSSHAIRS
    }
}