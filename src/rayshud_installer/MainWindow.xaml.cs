using AutoUpdaterDotNET;
using log4net;
using log4net.Config;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
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

namespace rayshud_installer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string appPath = System.Windows.Forms.Application.StartupPath;
        public static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public MainWindow()
        {
            var repository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(repository, new FileInfo("log4net.config"));
            logger.Info("INITIALIZING...");
            InitializeComponent();
            SetupDirectory();
            LoadHUDSettings();
            AutoUpdater.OpenDownloadPage = true;
            AutoUpdater.Start(Properties.Resources.app_update);
        }

        /// <summary>
        /// Calls to download the latest version of rayshud
        /// </summary>
        private void DownloadHUD()
        {
            logger.Info("Downloading the latest rayshud...");
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            var client = new WebClient();
            client.DownloadFile(Properties.Resources.app_download, "rayshud.zip");
            client.Dispose();
            logger.Info("Downloading the latest rayshud...Done!");
            ExtractHUD();
            CleanDirectory();
        }

        /// <summary>
        /// Calls to extract rayshud to the tf/custom directory
        /// </summary>
        /// <remarks>TODO: Refactor the update-refresh-install process</remarks>
        private void ExtractHUD(bool update = false)
        {
            var settings = Properties.Settings.Default;
            logger.Info("Extracting downloaded rayshud to " + settings.hud_directory);
            var updateMode = (Install.Content.ToString() == "Update") ? true : false;
            ZipFile.ExtractToDirectory(appPath + "\\rayshud.zip", settings.hud_directory);
            if (update)
                Directory.Delete(settings.hud_directory + "\\rayshud", true);
            if (Directory.Exists(settings.hud_directory + "\\rayshud"))
                Directory.Delete(settings.hud_directory + "\\rayshud", true);
            if (Directory.Exists(settings.hud_directory + "\\rayshud-master"))
                Directory.Move(settings.hud_directory + "\\rayshud-master", settings.hud_directory + "\\rayshud");
            //lblNews.Content = "Install finished at " + DateTime.Now;
            logger.Info("Extracting downloaded rayshud...Done!");
        }

        /// <summary>
        /// Set the tf/custom directory if not already set
        /// </summary>
        /// <remarks>TODO: Possible bug, consider refactoring</remarks>
        private void SetupDirectory(bool userSet = false)
        {
            if ((!SearchRegistry() && !CheckUserPath()) || userSet)
            {
                logger.Info("Setting the tf/custom directory. Opening folder browser, asking the user.");
                using (var browser = new FolderBrowserDialog { Description = Properties.Resources.info_folder_browser, ShowNewFolderButton = true })
                {
                    while (!browser.SelectedPath.Contains("tf\\custom"))
                    {
                        if (browser.ShowDialog() == System.Windows.Forms.DialogResult.OK && browser.SelectedPath.Contains("tf\\custom"))
                        {
                            var settings = Properties.Settings.Default;
                            settings.hud_directory = browser.SelectedPath;
                            settings.Save();
                            lblStatus.Content = settings.hud_directory;
                            logger.Info("Directory has been set to " + lblStatus.Content);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                if (!CheckUserPath())
                {
                    logger.Error("Unable to set the tf/custom directory. Exiting.");
                    System.Windows.Forms.MessageBox.Show(Properties.Resources.error_app_directory, "Directory Not Set", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    System.Windows.Application.Current.Shutdown();
                }
            }
            CleanDirectory();
            SetFormControls();
        }

        /// <summary>
        /// Cleans up the tf/custom and installer directories
        /// </summary>
        private void CleanDirectory()
        {
            logger.Info("Cleaning-up rayshud directories...");

            // Clean the application directory
            if (File.Exists(appPath + "\\rayshud.zip"))
                File.Delete(appPath + "\\rayshud.zip");

            // Clean the tf/custom directory
            var settings = Properties.Settings.Default;
            var hudDirectory = Directory.Exists(settings.hud_directory + "\\rayshud-master") ? settings.hud_directory + "\\rayshud-master" : string.Empty;
            hudDirectory = Directory.Exists(settings.hud_directory + "\\rayshud-stream") ? settings.hud_directory + "\\rayshud-stream" : hudDirectory;

            if (!string.IsNullOrEmpty(hudDirectory))
            {
                // Remove the previous backup if found.
                if (File.Exists(settings.hud_directory + "\\rayshud-backup.zip"))
                    File.Delete(settings.hud_directory + "\\rayshud-backup.zip");

                logger.Info("Found a rayshud installation. Creating a back-up.");
                ZipFile.CreateFromDirectory(hudDirectory, settings.hud_directory + "\\rayshud-backup.zip");
                Directory.Delete(hudDirectory, true);
                System.Windows.Forms.MessageBox.Show(Properties.Resources.info_create_backup, "Backup Created", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            logger.Info("Cleaning-up rayshud directories...Done!");
        }

        private bool SearchRegistry()
        {
            logger.Info("Looking for the Team Fortress 2 directory...");
            var is64Bit = (Environment.Is64BitProcess) ? "Wow6432Node\\" : string.Empty;
            var directory = (string)Registry.GetValue($@"HKEY_LOCAL_MACHINE\Software\{is64Bit}Valve\Steam", "InstallPath", null);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                directory += "\\steamapps\\common\\Team Fortress 2\\tf\\custom";
                if (Directory.Exists(directory))
                {
                    var settings = Properties.Settings.Default;
                    settings.hud_directory = directory;
                    settings.Save();
                    logger.Info("Directory found at " + settings.hud_directory);
                    return true;
                }
            }
            logger.Info("Directory not found. Continuing...");
            return false;
        }

        /// <summary>
        /// Display the error message box
        /// </summary>
        public static void ShowErrorMessage(string title, string message, string exception)
        {
            System.Windows.Forms.MessageBox.Show($"{message} {exception}", "Error: " + title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            logger.Error(exception);
        }

        /// <summary>
        /// Check the rayshud version number
        /// </summary>
        public void CheckHUDVersion()
        {
            try
            {
                logger.Info("Checking rayshud version...");
                //if (!CheckHUDPath()) return;
                var client = new WebClient();
                var readme_text = client.DownloadString(Properties.Resources.app_readme).Split('\n');
                client.Dispose();
                var current = readme_text[readme_text.Length - 2];
                var local = File.ReadLines(Properties.Settings.Default.hud_directory + "\\rayshud\\README.md").Last().Trim();
                if (!string.Equals(local, current))
                {
                    logger.Info("Version Mismatch. New rayshud update available!");
                    Install.Content = "Update";
                    lblNews.Content = "Update Available!";
                }
                logger.Info("Local version: " + local + "\t Live version: " + current);
                logger.Info("Checking rayshud version...Done!");
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }
        }

        /// <summary>
        /// Check if rayshud is installed in the tf/custom directory
        /// </summary>
        public bool CheckHUDPath()
        {
            return Directory.Exists(Properties.Settings.Default.hud_directory + "\\rayshud");
        }

        /// <summary>
        /// Check if user's directory setting is valid
        /// </summary>
        public bool CheckUserPath()
        {
            return !string.IsNullOrWhiteSpace(Properties.Settings.Default.hud_directory) && Properties.Settings.Default.hud_directory.Contains("tf\\custom");
        }

        /// <summary>
        /// Update the installer controls like labels and buttons
        /// </summary>
        private void SetFormControls()
        {
            if (Directory.Exists(Properties.Settings.Default.hud_directory) && CheckUserPath())
            {
                var isInstalled = CheckHUDPath();
                if (isInstalled) CheckHUDVersion();
                Start.IsEnabled = true;
                Install.IsEnabled = true;
                Install.Content = isInstalled ? "Refresh" : "Install";
                Save.IsEnabled = isInstalled ? true : false;
                Uninstall.IsEnabled = isInstalled ? true : false;
                lblStatus.Content = $"rayshud is {(!isInstalled ? "not " : "")}installed...";
                Properties.Settings.Default.Save();
            }
            else
            {
                Start.IsEnabled = false;
                Install.IsEnabled = false;
                Save.IsEnabled = false;
                Uninstall.IsEnabled = false;
                lblStatus.Content = "Directory is not set...";
            }
        }

        #region CLICK_EVENTS

        /// <summary>
        /// Installs rayshud to the user's tf/custom folder
        /// </summary>
        private void Install_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                logger.Info("Installing rayshud...");
                DownloadHUD();
                SaveHUDSettings();
                ApplyHUDSettings();
                SetFormControls();
                lblNews.Content = "Installation finished at " + DateTime.Now;
                logger.Info("Installing rayshud...Done!");
                System.Windows.Forms.MessageBox.Show("rayshud has been successfully installed", "Install Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Installing rayshud.", Properties.Resources.error_app_install, ex.Message);
            }
        }

        /// <summary>
        /// Removes rayshud from the user's tf/custom folder
        /// </summary>
        private void Uninstall_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                logger.Info("Uninstalling rayshud...");
                if (!CheckHUDPath()) return;
                Directory.Delete(Properties.Settings.Default.hud_directory + "\\rayshud", true);
                lblNews.Content = "Uninstalled rayshud at " + DateTime.Now;
                SetupDirectory();
                logger.Info("Uninstalling rayshud...Done!");
                System.Windows.Forms.MessageBox.Show("rayshud has been successfully uninstalled", "Uninstall Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Uninstalling rayshud.", Properties.Resources.error_app_uninstall, ex.Message);
            }
        }

        /// <summary>
        /// Saves then applies the rayshud settings
        /// </summary>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveHUDSettings();
            ApplyHUDSettings();
        }

        /// <summary>
        /// Resets the rayshud settings to the default
        /// </summary>
        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            ResetHUDSettings();
        }

        /// <summary>
        /// Opens the directory browser to let the user to set their tf/custom directory
        /// </summary>
        private void ChangeDirectory_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("Opening Directory Browser...");
            SetupDirectory(true);
        }

        /// <summary>
        /// Opens the GitHub issue tracker in a web browser
        /// </summary>
        private void ReportIssue_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("Opening Issue Tracker...");
            Process.Start("https://github.com/CriticalFlaw/rayshud-Installer/issues");
        }

        /// <summary>
        /// Launches Team Fortress 2 through Steam
        /// </summary>
        private void Start_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("Launching Team Fortress 2...");
            Process.Start("steam://rungameid/440");
        }

        #endregion CLICK_EVENTS

        #region SAVE_LOAD

        /// <summary>
        /// Save user settings to the file
        /// </summary>
        private void SaveHUDSettings()
        {
            try
            {
                logger.Info("Saving HUD Settings...");
                var settings = Properties.Settings.Default;
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
                settings.color_health_buffed = cpHealthBuff.SelectedColor.Value.ToString();
                settings.color_health_low = cpHealthLow.SelectedColor.Value.ToString();

                settings.color_ammo_clip = cpAmmoClip.SelectedColor.Value.ToString();
                settings.color_ammo_clip_low = cpAmmoClipLow.SelectedColor.Value.ToString();
                settings.color_ammo_reserve = cpAmmoReserve.SelectedColor.Value.ToString();
                settings.color_ammo_reserve_low = cpAmmoReserveLow.SelectedColor.Value.ToString();

                settings.Save();
                logger.Info("Saving HUD Settings...Done!");
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Saving HUD Settings.", Properties.Resources.error_app_save, ex.Message);
            }
        }

        /// <summary>
        /// Load GUI with user settings from the file
        /// </summary>
        private void LoadHUDSettings()
        {
            try
            {
                logger.Info("Loading HUD Settings...");
                var settings = Properties.Settings.Default;
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
                cpHealthBuff.SelectedColor = (Color)cc.ConvertFrom(settings.color_health_buffed);
                cpHealthLow.SelectedColor = (Color)cc.ConvertFrom(settings.color_health_low);

                cpAmmoClip.SelectedColor = (Color)cc.ConvertFrom(settings.color_ammo_clip);
                cpAmmoClipLow.SelectedColor = (Color)cc.ConvertFrom(settings.color_ammo_clip_low);
                cpAmmoReserve.SelectedColor = (Color)cc.ConvertFrom(settings.color_ammo_reserve);
                cpAmmoReserveLow.SelectedColor = (Color)cc.ConvertFrom(settings.color_ammo_reserve_low);

                logger.Info("Loading HUD Settings...Done!");
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Loading HUD Settings.", Properties.Resources.error_app_load, ex.Message);
            }
        }

        /// <summary>
        /// Reset user settings to their default values
        /// </summary>
        private void ResetHUDSettings()
        {
            try
            {
                logger.Info("Resetting HUD Settings...");
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
                cpHealthBuff.SelectedColor = (Color)cc.ConvertFrom("#30FF30");
                cpHealthLow.SelectedColor = (Color)cc.ConvertFrom("#FF9900");

                cpAmmoClip.SelectedColor = (Color)cc.ConvertFrom("#30FF30");
                cpAmmoClipLow.SelectedColor = (Color)cc.ConvertFrom("#FF2A82");
                cpAmmoReserve.SelectedColor = (Color)cc.ConvertFrom("#48FFFF");
                cpAmmoReserveLow.SelectedColor = (Color)cc.ConvertFrom("#FF801C");

                lblNews.Content = "Settings Reset at " + DateTime.Now;
                logger.Info("Resetting HUD Settings...Done!");
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Resetting HUD Settings.", Properties.Resources.error_app_reset, ex.Message);
            }
        }

        /// <summary>
        /// Apply user settings to rayshud files
        /// </summary>
        private void ApplyHUDSettings()
        {
            logger.Info("Applying HUD Settings...");
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
            writer.Crosshair();
            writer.CrosshairStyle(tbXHairXPos.Value, tbXHairYPos.Value);
            writer.Colors();
            writer.DamagePos();
            writer.TransparentViewmodels();
            writer.PlayerModelPos();
            lblNews.Content = "Settings Saved at " + DateTime.Now;
            logger.Info("Resetting HUD Settings...Done!");
        }

        #endregion SAVE_LOAD

        #region CROSSHAIRS

        public static readonly Dictionary<CrosshairStyles, Tuple<int, int, string>> crosshairs = new Dictionary<CrosshairStyles, Tuple<int, int, string>>
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
            var style = (CrosshairStyles)cbXHairStyle.SelectedIndex;
            tbXHairXPos.Text = crosshairs[style].Item1.ToString();
            tbXHairYPos.Text = crosshairs[style].Item2.ToString();
            SetCrosshairStyle(style);
        }

        private void cbXHairSize_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (XHairPreview == null) return;
            var selectedItem = (ComboBoxItem)cbXHairSize.SelectedValue;
            int.TryParse((string)selectedItem.Content, out var size);
            XHairPreview.FontSize = (size > 0) ? size * 3 : 72;
        }

        private void cpXHairColor_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (XHairPreview == null) return;
            XHairPreview_Color.Color = (Color)cpXHairColor.SelectedColor;
        }

        public void SetCrosshairStyle(CrosshairStyles index)
        {
            if (XHairPreview == null) return;
            switch (index)
            {
                case CrosshairStyles.BasicCross:
                    XHairPreview.Text = @"2";
                    //XHairPreview.Location = new Point(97, 33);
                    break;

                case CrosshairStyles.BasicDot:
                    XHairPreview.Text = @"3";
                    //XHairPreview.Location = new Point(103, 31);
                    break;

                case CrosshairStyles.CircleDot:
                    XHairPreview.Text = @"8";
                    //XHairPreview.Location = new Point(103, 31);
                    break;

                case CrosshairStyles.KonrWings:
                    XHairPreview.Text = @"i";
                    //XHairPreview.Location = new Point(97, 32);
                    break;

                case CrosshairStyles.OpenCross:
                    XHairPreview.Text = @"i";
                    //XHairPreview.Location = new Point(95, 28);
                    break;

                case CrosshairStyles.OpenCrossDot:
                    XHairPreview.Text = @"h";
                    //XHairPreview.Location = new Point(95, 28);
                    break;

                case CrosshairStyles.ScatterSpread:
                    XHairPreview.Text = @"0";
                    //XHairPreview.Location = new Point(104, 30);
                    break;

                case CrosshairStyles.ThinCircle:
                    XHairPreview.Text = @"9";
                    //XHairPreview.Location = new Point(105, 32);
                    break;

                case CrosshairStyles.Wings:
                    XHairPreview.Text = @"d";
                    //XHairPreview.Location = new Point(95, 32);
                    break;

                case CrosshairStyles.WingsPlus:
                    XHairPreview.Text = @"c";
                    //XHairPreview.Location = new Point(95, 32);
                    break;

                case CrosshairStyles.WingsSmall:
                    XHairPreview.Text = @"g";
                    //CrosshairFont.Location = new Point(95, 32);
                    break;

                case CrosshairStyles.WingsSmallDot:
                    XHairPreview.Text = @"f";
                    //XHairPreview.Location = new Point(95, 32);
                    break;

                case CrosshairStyles.xHairCircle:
                    XHairPreview.Text = @"o";
                    //XHairPreview.Location = new Point(97, 32);
                    break;

                default:
                    XHairPreview.Text = string.Empty;
                    break;
            }
        }

        #endregion CROSSHAIRS
    }
}