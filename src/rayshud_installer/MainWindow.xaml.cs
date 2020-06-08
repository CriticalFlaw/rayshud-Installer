using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using AutoUpdaterDotNET;
using log4net;
using log4net.Config;
using Microsoft.Win32;
using rayshud_installer.Properties;
using Application = System.Windows.Forms.Application;
using MessageBox = System.Windows.Forms.MessageBox;

namespace rayshud_installer
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static readonly string[] RaysCrosshairs = {"2", "3", "8", "i", "h", "0", "9", "d", "c", "g", "f"};
        private readonly string _appPath = Application.StartupPath;

        public MainWindow()
        {
            var repository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(repository, new FileInfo("log4net.config"));
            Logger.Info("INITIALIZING...");
            InitializeComponent();
            SetupDirectory();
            ReloadHUDSettings();
            SetCrosshairControls();
            AutoUpdater.OpenDownloadPage = true;
            AutoUpdater.Start(Properties.Resources.app_update);
        }

        /// <summary>
        ///     Calls to download the latest version of rayshud
        /// </summary>
        private void DownloadHUD()
        {
            Logger.Info("Downloading the latest rayshud...");
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            var client = new WebClient();
            client.DownloadFile(Properties.Resources.app_download, "rayshud.zip");
            client.Dispose();
            Logger.Info("Downloading the latest rayshud...Done!");
            ExtractHUD();
            CleanDirectory();
        }

        /// <summary>
        ///     Calls to extract rayshud to the tf/custom directory
        /// </summary>
        /// <remarks>TODO: Refactor the update-refresh-install process</remarks>
        private void ExtractHUD(bool update = false)
        {
            var settings = Settings.Default;
            Logger.Info("Extracting downloaded rayshud to " + settings.hud_directory);
            ZipFile.ExtractToDirectory(_appPath + "\\rayshud.zip", settings.hud_directory);
            if (update)
                Directory.Delete(settings.hud_directory + "\\rayshud", true);
            if (Directory.Exists(settings.hud_directory + "\\rayshud"))
                Directory.Delete(settings.hud_directory + "\\rayshud", true);
            if (Directory.Exists(settings.hud_directory + "\\rayshud-master"))
                Directory.Move(settings.hud_directory + "\\rayshud-master", settings.hud_directory + "\\rayshud");
            //lblNews.Content = "Install finished at " + DateTime.Now;
            Logger.Info("Extracting downloaded rayshud...Done!");
        }

        /// <summary>
        ///     Set the tf/custom directory if not already set
        /// </summary>
        /// <remarks>TODO: Possible bug, consider refactoring</remarks>
        private void SetupDirectory(bool userSet = false)
        {
            if (!SearchRegistry() && !CheckUserPath() || userSet)
            {
                Logger.Info("Setting the tf/custom directory. Opening folder browser, asking the user.");
                using (var browser = new FolderBrowserDialog
                    {Description = Properties.Resources.info_folder_browser, ShowNewFolderButton = true})
                {
                    while (!browser.SelectedPath.Contains("tf\\custom"))
                        if (browser.ShowDialog() == System.Windows.Forms.DialogResult.OK &&
                            browser.SelectedPath.Contains("tf\\custom"))
                        {
                            var settings = Settings.Default;
                            settings.hud_directory = browser.SelectedPath;
                            settings.Save();
                            LblStatus.Content = settings.hud_directory;
                            Logger.Info("Directory has been set to " + LblStatus.Content);
                        }
                        else
                        {
                            break;
                        }
                }

                if (!CheckUserPath())
                {
                    Logger.Error("Unable to set the tf/custom directory. Exiting.");
                    MessageBox.Show(Properties.Resources.error_app_directory,
                        Properties.Resources.error_app_directory_title, MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    System.Windows.Application.Current.Shutdown();
                }
            }

            CleanDirectory();
            SetFormControls();
        }

        /// <summary>
        ///     Cleans up the tf/custom and installer directories
        /// </summary>
        private void CleanDirectory()
        {
            Logger.Info("Cleaning-up rayshud directories...");

            // Clean the application directory
            if (File.Exists(_appPath + "\\rayshud.zip"))
                File.Delete(_appPath + "\\rayshud.zip");

            // Clean the tf/custom directory
            var settings = Settings.Default;
            var hudDirectory = Directory.Exists(settings.hud_directory + "\\rayshud-master")
                ? settings.hud_directory + "\\rayshud-master"
                : string.Empty;

            if (!string.IsNullOrEmpty(hudDirectory))
            {
                // Remove the previous backup if found.
                if (File.Exists(settings.hud_directory + "\\rayshud-backup.zip"))
                    File.Delete(settings.hud_directory + "\\rayshud-backup.zip");

                Logger.Info("Found a rayshud installation. Creating a back-up.");
                ZipFile.CreateFromDirectory(hudDirectory, settings.hud_directory + "\\rayshud-backup.zip");
                Directory.Delete(hudDirectory, true);
                MessageBox.Show(Properties.Resources.info_create_backup, Properties.Resources.info_create_backup_title,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            Logger.Info("Cleaning-up rayshud directories...Done!");
        }

        private static bool SearchRegistry()
        {
            Logger.Info("Looking for the Team Fortress 2 directory...");
            var is64Bit = Environment.Is64BitProcess ? "Wow6432Node\\" : string.Empty;
            var directory = (string) Registry.GetValue($@"HKEY_LOCAL_MACHINE\Software\{is64Bit}Valve\Steam",
                "InstallPath", null);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                directory += "\\steamapps\\common\\Team Fortress 2\\tf\\custom";
                if (Directory.Exists(directory))
                {
                    var settings = Settings.Default;
                    settings.hud_directory = directory;
                    settings.Save();
                    Logger.Info("Directory found at " + settings.hud_directory);
                    return true;
                }
            }

            Logger.Info("Directory not found. Continuing...");
            return false;
        }

        /// <summary>
        ///     Display the error message box
        /// </summary>
        public static void ShowErrorMessage(string title, string message, string exception)
        {
            MessageBox.Show($@"{message} {exception}", string.Format(Properties.Resources.error_info, title),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            Logger.Error(exception);
        }

        /// <summary>
        ///     Check the rayshud version number
        /// </summary>
        public void CheckHUDVersion()
        {
            try
            {
                Logger.Info("Checking rayshud version...");
                var client = new WebClient();
                var readmeText = client.DownloadString(Properties.Resources.app_readme).Split('\n');
                client.Dispose();
                var current = readmeText[readmeText.Length - 2];
                var local = File.ReadLines(Settings.Default.hud_directory + "\\rayshud\\README.md").Last().Trim();
                if (!string.Equals(local, current))
                {
                    Logger.Info("Version Mismatch. New rayshud update available!");
                    BtnInstall.Content = "Update";
                    LblNews.Content = "Update Available!";
                }

                Logger.Info("Local version: " + local + "\t Live version: " + current);
                Logger.Info("Checking rayshud version...Done!");
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        /// <summary>
        ///     Check if rayshud is installed in the tf/custom directory
        /// </summary>
        public bool CheckHUDPath()
        {
            return Directory.Exists(Settings.Default.hud_directory + "\\rayshud");
        }

        /// <summary>
        ///     Check if user's directory setting is valid
        /// </summary>
        public bool CheckUserPath()
        {
            return !string.IsNullOrWhiteSpace(Settings.Default.hud_directory) &&
                   Settings.Default.hud_directory.Contains("tf\\custom");
        }

        /// <summary>
        ///     Update the installer controls like labels and buttons
        /// </summary>
        private void SetFormControls()
        {
            if (Directory.Exists(Settings.Default.hud_directory) && CheckUserPath())
            {
                var isInstalled = CheckHUDPath();
                if (isInstalled) CheckHUDVersion();
                BtnStart.IsEnabled = true;
                BtnInstall.IsEnabled = true;
                BtnInstall.Content = isInstalled ? "Refresh" : "Install";
                BtnSave.IsEnabled = isInstalled;
                BtnUninstall.IsEnabled = isInstalled;
                LblStatus.Content = $"rayshud is {(!isInstalled ? "not " : "")}installed...";
                Settings.Default.Save();
            }
            else
            {
                BtnStart.IsEnabled = false;
                BtnInstall.IsEnabled = false;
                BtnSave.IsEnabled = false;
                BtnUninstall.IsEnabled = false;
                LblStatus.Content = "Directory is not set...";
            }
        }

        private void SetCrosshairControls()
        {
            CbXHairHitmarker.IsEnabled = CbXHairEnable.IsChecked ?? false;
            IntXHairXPos.IsEnabled = CbXHairEnable.IsChecked ?? false;
            IntXHairYPos.IsEnabled = CbXHairEnable.IsChecked ?? false;
            CbXHairStyle.IsEnabled = CbXHairEnable.IsChecked ?? false;
            IntXHairSize.IsEnabled = CbXHairEnable.IsChecked ?? false;
            CpXHairColor.IsEnabled = CbXHairEnable.IsChecked ?? false;
            CpXHairPulse.IsEnabled = CbXHairEnable.IsChecked ?? false;
        }

        #region CLICK_EVENTS

        /// <summary>
        ///     Installs rayshud to the user's tf/custom folder
        /// </summary>
        private void BtnInstall_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Info("Installing rayshud...");
                var worker = new BackgroundWorker();
                worker.DoWork += (o, ea) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        DownloadHUD();
                        SaveHUDSettings();
                        ApplyHUDSettings();
                        SetFormControls();
                    });
                };
                worker.RunWorkerCompleted += (o, ea) =>
                {
                    BusyIndicator.IsBusy = false;
                    LblNews.Content = "Installation finished at " + DateTime.Now;
                    Logger.Info("Installing rayshud...Done!");
                    MessageBox.Show(Properties.Resources.info_install_complete_desc,
                        Properties.Resources.info_install_complete, MessageBoxButtons.OK, MessageBoxIcon.Information);
                };
                BusyIndicator.IsBusy = true;
                worker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Installing rayshud.", Properties.Resources.error_app_install, ex.Message);
            }
        }

        /// <summary>
        ///     Removes rayshud from the user's tf/custom folder
        /// </summary>
        private void BtnUninstall_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Info("Uninstalling rayshud...");
                if (!CheckHUDPath()) return;
                Directory.Delete(Settings.Default.hud_directory + "\\rayshud", true);
                LblNews.Content = "Uninstalled rayshud at " + DateTime.Now;
                SetupDirectory();
                Logger.Info("Uninstalling rayshud...Done!");
                MessageBox.Show(Properties.Resources.info_uninstall_complete_desc,
                    Properties.Resources.info_uninstall_complete, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Uninstalling rayshud.", Properties.Resources.error_app_uninstall, ex.Message);
            }
        }

        /// <summary>
        ///     Saves then applies the rayshud settings
        /// </summary>
        private void BtnSave_OnClick(object sender, RoutedEventArgs e)
        {
            var worker = new BackgroundWorker();
            worker.DoWork += (o, ea) =>
            {
                Dispatcher.Invoke(() =>
                {
                    SaveHUDSettings();
                    ApplyHUDSettings();
                });
            };
            worker.RunWorkerCompleted += (o, ea) => { BusyIndicator.IsBusy = false; };
            BusyIndicator.IsBusy = true;
            worker.RunWorkerAsync();
        }

        /// <summary>
        ///     Resets the rayshud settings to the default
        /// </summary>
        private void BtnReset_OnClick(object sender, RoutedEventArgs e)
        {
            ResetHUDSettings();
        }

        /// <summary>
        ///     Opens the directory browser to let the user to set their tf/custom directory
        /// </summary>
        private void BtnDirectory_OnClick(object sender, RoutedEventArgs e)
        {
            Logger.Info("Opening Directory Browser...");
            SetupDirectory(true);
        }

        /// <summary>
        ///     Launches Team Fortress 2 through Steam
        /// </summary>
        private void BtnStart_OnClick(object sender, RoutedEventArgs e)
        {
            Logger.Info("Launching Team Fortress 2...");
            Process.Start("steam://rungameid/440");
        }

        /// <summary>
        ///     Opens the GitHub issue tracker in a web browser
        /// </summary>
        private void ReportIssue_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("Opening Issue Tracker...");
            Process.Start("https://github.com/CriticalFlaw/rayshud-Installer/issues");
        }

        private void CbXHairEnable_OnClick(object sender, RoutedEventArgs e)
        {
            SetCrosshairControls();
        }

        private void CbXHairStyle_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RaysCrosshairs.Any(x => CbXHairStyle.SelectedValue.ToString().Equals(x)))
            {
                IntXHairXPos.Value = 103;
                IntXHairYPos.Value = 100;
                CbXHairStyle.FontFamily = new FontFamily(new Uri("pack://application:,,,/"),
                    "./Resources/style/Crosshairs.ttf #Crosshairs");
            }
            else
            {
                IntXHairXPos.Value = 25;
                IntXHairYPos.Value = 24;
                CbXHairStyle.FontFamily = new FontFamily(new Uri("pack://application:,,,/"),
                    "./Resources/style/KnucklesCrosses.ttf #KnucklesCrosses");
            }
        }

        #endregion CLICK_EVENTS

        #region SAVE_LOAD

        /// <summary>
        ///     Save user settings to the file
        /// </summary>
        private void SaveHUDSettings()
        {
            try
            {
                Logger.Info("Saving HUD Settings...");
                var settings = Settings.Default;
                settings.val_uber_animation = CbUberStyle.SelectedIndex;
                settings.color_uber_bar = CpUberBarColor.SelectedColor?.ToString();
                settings.color_uber_full = CpUberFullColor.SelectedColor?.ToString();
                settings.color_uber_flash1 = CpUberFlash1.SelectedColor?.ToString();
                settings.color_uber_flash2 = CpUberFlash2.SelectedColor?.ToString();

                settings.toggle_xhair_enable = CbXHairEnable.IsChecked ?? false;
                settings.toggle_xhair_pulse = CbXHairHitmarker.IsChecked ?? false;
                settings.toggle_xhair_outline = CbXHairOutline.IsChecked ?? false;
                settings.val_xhair_x = IntXHairXPos.Value ?? 103;
                settings.val_xhair_y = IntXHairYPos.Value ?? 100;
                settings.val_xhair_style = CbXHairStyle.SelectedIndex;
                settings.val_xhair_size = IntXHairSize.Value ?? 14;
                settings.color_xhair_normal = CpXHairColor.SelectedColor?.ToString();
                settings.color_xhair_pulse = CpXHairPulse.SelectedColor?.ToString();

                settings.color_ammo_clip = CpAmmoClip.SelectedColor?.ToString();
                settings.color_ammo_clip_low = CpAmmoClipLow.SelectedColor?.ToString();
                settings.color_ammo_reserve = CpAmmoReserve.SelectedColor?.ToString();
                settings.color_ammo_reserve_low = CpAmmoReserveLow.SelectedColor?.ToString();

                settings.color_health_normal = CpHealthNormal.SelectedColor?.ToString();
                settings.color_health_buffed = CpHealthBuff.SelectedColor?.ToString();
                settings.color_health_low = CpHealthLow.SelectedColor?.ToString();
                settings.val_health_style = CbHealthStyle.SelectedIndex;

                settings.toggle_classic_menu = CbClassicHud.IsChecked ?? false;
                settings.toggle_min_scoreboard = CbScoreboard.IsChecked ?? false;
                settings.toggle_disguise_image = CbDisguiseImage.IsChecked ?? false;
                settings.toggle_stock_backgrounds = CbDefaultBg.IsChecked ?? false;
                settings.toggle_menu_images = CbMenuImages.IsChecked ?? false;
                settings.toggle_damage_pos = CbDamagePos.IsChecked ?? false;
                settings.toggle_chat_bottom = CbChatBottom.IsChecked ?? false;
                settings.toggle_center_select = CbTeamCenter.IsChecked ?? false;
                settings.toggle_transparent_viewmodels = CbTransparentViewmodel.IsChecked ?? false;
                settings.toggle_alt_player_model = CbPlayerModel.IsChecked ?? false;

                settings.Save();
                Logger.Info("Saving HUD Settings...Done!");
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Saving HUD Settings.", Properties.Resources.error_app_save, ex.Message);
            }
        }

        /// <summary>
        ///     Load GUI with user settings from the file
        /// </summary>
        private void ReloadHUDSettings()
        {
            try
            {
                ResetHUDSettings();
                Logger.Info("Loading HUD Settings...");
                var settings = Settings.Default;
                var cc = new ColorConverter();

                CbUberStyle.SelectedIndex = settings.val_uber_animation;
                CpUberBarColor.SelectedColor = (Color) cc.ConvertFrom(settings.color_uber_bar);
                CpUberFullColor.SelectedColor = (Color) cc.ConvertFrom(settings.color_uber_full);
                CpUberFlash1.SelectedColor = (Color) cc.ConvertFrom(settings.color_uber_flash1);
                CpUberFlash2.SelectedColor = (Color) cc.ConvertFrom(settings.color_uber_flash2);

                CbXHairEnable.IsChecked = settings.toggle_xhair_enable;
                CbXHairHitmarker.IsChecked = settings.toggle_xhair_pulse;
                CbXHairOutline.IsChecked = settings.toggle_xhair_outline;
                IntXHairXPos.Value = settings.val_xhair_x;
                IntXHairYPos.Value = settings.val_xhair_y;
                CbXHairStyle.SelectedIndex = settings.val_xhair_style;
                IntXHairSize.Value = settings.val_xhair_size;
                CpXHairColor.SelectedColor = (Color) cc.ConvertFrom(settings.color_xhair_normal);
                CpXHairPulse.SelectedColor = (Color) cc.ConvertFrom(settings.color_xhair_pulse);

                CpAmmoClip.SelectedColor = (Color) cc.ConvertFrom(settings.color_ammo_clip);
                CpAmmoClipLow.SelectedColor = (Color) cc.ConvertFrom(settings.color_ammo_clip_low);
                CpAmmoReserve.SelectedColor = (Color) cc.ConvertFrom(settings.color_ammo_reserve);
                CpAmmoReserveLow.SelectedColor = (Color) cc.ConvertFrom(settings.color_ammo_reserve_low);

                CpHealthNormal.SelectedColor = (Color) cc.ConvertFrom(settings.color_health_normal);
                CpHealthBuff.SelectedColor = (Color) cc.ConvertFrom(settings.color_health_buffed);
                CpHealthLow.SelectedColor = (Color) cc.ConvertFrom(settings.color_health_low);
                CbHealthStyle.SelectedIndex = settings.val_health_style;

                settings.color_health_normal = CpHealthNormal.SelectedColor?.ToString();
                settings.color_health_buffed = CpHealthBuff.SelectedColor?.ToString();
                settings.color_health_low = CpHealthLow.SelectedColor?.ToString();
                settings.val_health_style = CbHealthStyle.SelectedIndex;

                CbClassicHud.IsChecked = settings.toggle_classic_menu;
                CbScoreboard.IsChecked = settings.toggle_min_scoreboard;
                CbDisguiseImage.IsChecked = settings.toggle_disguise_image;
                CbDefaultBg.IsChecked = settings.toggle_stock_backgrounds;
                CbMenuImages.IsChecked = settings.toggle_menu_images;
                CbDamagePos.IsChecked = settings.toggle_damage_pos;
                CbChatBottom.IsChecked = settings.toggle_chat_bottom;
                CbTeamCenter.IsChecked = settings.toggle_center_select;
                CbTransparentViewmodel.IsChecked = settings.toggle_transparent_viewmodels;
                CbPlayerModel.IsChecked = settings.toggle_alt_player_model;

                Logger.Info("Loading HUD Settings...Done!");
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Loading HUD Settings.", Properties.Resources.error_app_load, ex.Message);
            }
        }

        /// <summary>
        ///     Resets user settings to rayshud defaults
        /// </summary>
        private void ResetHUDSettings()
        {
            try
            {
                Logger.Info("Resetting HUD Settings...");
                var cc = new ColorConverter();
                CbUberStyle.SelectedIndex = 0;
                CpUberBarColor.SelectedColor = (Color) cc.ConvertFrom("#EBE2CA");
                CpUberFullColor.SelectedColor = (Color) cc.ConvertFrom("#FF3219");
                CpUberFlash1.SelectedColor = (Color) cc.ConvertFrom("#FFA500");
                CpUberFlash2.SelectedColor = (Color) cc.ConvertFrom("#FF4500");

                CbXHairEnable.IsChecked = false;
                CbXHairHitmarker.IsChecked = false;
                CbXHairOutline.IsChecked = false;
                IntXHairXPos.Value = 103;
                IntXHairYPos.Value = 100;
                CbXHairStyle.SelectedIndex = 6;
                IntXHairSize.Value = 14;
                CpXHairColor.SelectedColor = (Color) cc.ConvertFrom("#F2F2F2");
                CpXHairPulse.SelectedColor = (Color) cc.ConvertFrom("#FF0000");
                SetCrosshairControls();

                CpAmmoClip.SelectedColor = (Color) cc.ConvertFrom("#30FF30");
                CpAmmoClipLow.SelectedColor = (Color) cc.ConvertFrom("#FF2A82");
                CpAmmoReserve.SelectedColor = (Color) cc.ConvertFrom("#48FFFF");
                CpAmmoReserveLow.SelectedColor = (Color) cc.ConvertFrom("#FF801C");

                CpHealthNormal.SelectedColor = (Color) cc.ConvertFrom("#EBE2CA");
                CpHealthBuff.SelectedColor = (Color) cc.ConvertFrom("#30FF30");
                CpHealthLow.SelectedColor = (Color) cc.ConvertFrom("#FF9900");
                CbHealthStyle.SelectedIndex = 0;

                CbClassicHud.IsChecked = false;
                CbScoreboard.IsChecked = false;
                CbDisguiseImage.IsChecked = false;
                CbDefaultBg.IsChecked = false;
                CbMenuImages.IsChecked = false;
                CbDamagePos.IsChecked = false;
                CbChatBottom.IsChecked = false;
                CbTeamCenter.IsChecked = false;
                CbTransparentViewmodel.IsChecked = false;
                CbPlayerModel.IsChecked = false;

                LblNews.Content = "Settings Reset at " + DateTime.Now;
                Logger.Info("Resetting HUD Settings...Done!");
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Resetting HUD Settings.", Properties.Resources.error_app_reset, ex.Message);
            }
        }

        /// <summary>
        ///     Apply user settings to rayshud files
        /// </summary>
        private void ApplyHUDSettings()
        {
            Logger.Info("Applying HUD Settings...");
            var writer = new HUDController();
            writer.MainMenuStyle();
            writer.MainMenuBackground();
            writer.MainMenuClassImage();
            writer.ScoreboardStyle();
            writer.TeamSelect();
            writer.HealthStyle();
            writer.DisguiseImage();
            writer.UberchargeStyle();
            writer.CrosshairPulse();
            writer.ChatBoxPos();
            writer.Crosshair(CbXHairStyle.SelectedValue.ToString(), IntXHairSize.Value,
                !RaysCrosshairs.Any(x => CbXHairStyle.SelectedValue.ToString().Equals(x)));
            writer.Colors();
            writer.DamagePos();
            writer.TransparentViewmodels();
            writer.PlayerModelPos();
            LblNews.Content = "Settings Saved at " + DateTime.Now;
            Logger.Info("Resetting HUD Settings...Done!");
        }

        #endregion SAVE_LOAD
    }
}