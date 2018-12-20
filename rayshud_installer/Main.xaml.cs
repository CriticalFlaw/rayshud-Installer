using ColorPickerWPF;
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
        private BrushConverter bc = new BrushConverter();

        public Main()
        {
            InitializeComponent();
            SetHUDDirectory();
            CleanUpDirectory();
            LoadHUDSettings();
        }

        /// <summary>
        /// Set the tf/custom HUD directory if it's not already set
        /// </summary>
        private void SetHUDDirectory()
        {
            if (string.IsNullOrWhiteSpace(settings.app_directory_base) || !settings.app_directory_base.Contains("tf\\custom"))
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (Directory.Exists($"{drive.Name}\\Program Files\\{settings.app_directory_steam}"))
                    {
                        settings.app_directory_base = $"{drive.Name}\\Program Files\\{settings.app_directory_steam}";
                        break;
                    }
                    else if (Directory.Exists($"{drive.Name}\\Program Files (x86)\\{settings.app_directory_steam}"))
                    {
                        settings.app_directory_base = $"{drive.Name}\\Program Files (x86)\\{settings.app_directory_steam}";
                        break;
                    }
                }
                if (string.IsNullOrWhiteSpace(settings.app_directory_base))
                    DisplayFolderBrowser();

                if (string.IsNullOrWhiteSpace(settings.app_directory_base))
                {
                    System.Windows.Forms.MessageBox.Show("The tf/custom directory needs to be set in order to use the installer", "Directory Not Set", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    System.Windows.Forms.Application.Exit();
                }
                settings.Save();
            }
            UpdateGUIButtons();
        }

        private void UpdateGUIButtons()
        {
            btn_Start.IsEnabled = false;
            btn_Save.IsEnabled = false;
            btn_Install.IsEnabled = false;
            btn_Uninstall.IsEnabled = false;
            lbl_Status.Content = "Directory is not set...";

            if (Directory.Exists(settings.app_directory_base))
            {
                btn_Start.IsEnabled = true;
                btn_Install.IsEnabled = true;
                settings.app_directory = settings.app_directory_base;
                lbl_Status.Content = "rayshud is not installed...";
                btn_Install.Content = "Install";
            }

            if (Directory.Exists($"{settings.app_directory_base}\\rayshud"))
            {
                btn_Save.IsEnabled = true;
                btn_Uninstall.IsEnabled = true;
                settings.app_directory = $"{settings.app_directory_base}\\rayshud";
                lbl_Status.Content = "rayshud is installed...";
                btn_Install.Content = "Refresh";
                CheckHUDVersion();
            }
            settings.Save();
        }

        /// <summary>
        /// Check the rayshud version number
        /// </summary>
        private void CheckHUDVersion()
        {
            try
            {
                if (!settings.app_directory.Contains("tf\\custom\\rayshud")) return;
                var client = new WebClient();
                var readme_text = client.DownloadString(settings.app_hud_readme).Split('\n');
                settings.app_hud_version_current = readme_text[readme_text.Length - 2];
                settings.app_hud_version_local = File.ReadLines($"{settings.app_directory}\\README.md").Last().Trim();
                if (settings.app_hud_version_local != settings.app_hud_version_current)
                {
                    btn_Install.Content = "Update";
                    lbl_News.Content = "Update Available!";
                }
                settings.Save();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"{settings.error_app_version}\n{ex.Message}", "Error: Version Check", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Apply HUD settings to rayshud files
        /// </summary>
        private void ApplyHUDSettings()
        {
            var writer = new HUDFileWriter();
            writer.mainMenuStyle();
            writer.scoreboardStyle();
            writer.defaultBackgrounds();
            writer.teamSelect();
            writer.healthStyle();
            writer.disguiseImage();
            writer.uberAnimation();
            writer.crosshairPulse();
            writer.classImage();
            writer.chatboxPos();
            writer.crosshair(int.Parse(cb_XHairSize.SelectedItem.ToString()));
            writer.colors();
            writer.damagePos();
        }

        /// <summary>
        /// Save GUI settings to file
        /// </summary>
        private void SaveHUDSettings()
        {
            try
            {
                settings.v_ClassicHUD = chk_ClassicHUD.IsChecked ?? false;
                settings.v_Scoreboard = chk_Scoreboard.IsChecked ?? false;
                settings.v_DisguiseImage = chk_DisguiseImage.IsChecked ?? false;
                settings.v_DefaultBG = chk_DefaultBG.IsChecked ?? false;
                settings.v_ClassImage = chk_ClassImage.IsChecked ?? false;
                settings.v_DamagePos = chk_DamagePos.IsChecked ?? false;
                if (chk_UberFlash.IsChecked == true)
                    settings.v_UberAnimation = 0;
                else if (chk_UberSolid.IsChecked == true)
                    settings.v_UberAnimation = 1;
                else
                    settings.v_UberAnimation = 2;
                settings.v_UberBarColor = btn_UberBarColor.Background.ToString();
                settings.v_UberFullColor = btn_UberFullColor.Background.ToString();
                settings.v_UberFlash1 = btn_UberFlash1.Background.ToString();
                settings.v_UberFlash2 = btn_UberFlash2.Background.ToString();
                settings.v_XHairStyle = cb_XHairStyle.SelectedIndex;
                settings.v_XHairSize = cb_XHairSize.SelectedIndex;
                settings.v_XHairBaseColor = btn_XHairColor.Background.ToString();
                settings.v_XHairPulseColor = btn_XHairPulse.Background.ToString();
                settings.v_XHairEnable = chk_XHairEnable.IsChecked ?? false;
                settings.v_XHairOutline = chk_XHairOutline.IsChecked ?? false;
                settings.v_XHairPulse = chk_XHairPulse.IsChecked ?? false;
                settings.v_HealthStyle = lb_HealthStyle.SelectedIndex;
                settings.v_HealthNormal = btn_HealthNormal.Background.ToString();
                settings.v_HealingDone = btn_HealingDone.Background.ToString();
                settings.v_HealthBuff = btn_HealthBuff.Background.ToString();
                settings.v_HealthLow = btn_HealthLow.Background.ToString();
                settings.v_AmmoClip = btn_AmmoClip.Background.ToString();
                settings.v_AmmoReserve = btn_AmmoReserve.Background.ToString();
                settings.v_AmmoClipLow = btn_AmmoClipLow.Background.ToString();
                settings.v_AmmoReserveLow = btn_AmmoReserveLow.Background.ToString();
                settings.v_TeamCenter = chk_TeamCenter.IsChecked ?? false;
                settings.v_ChatBottom = chk_ChatBottom.IsChecked ?? false;
                settings.app_hud_mod_date = DateTime.Now;
                settings.Save();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"{settings.error_app_save}\n{ex.Message}", "Error: Saving Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Load GUI settings from file
        /// </summary>
        private void LoadHUDSettings()
        {
            try
            {
                chk_ClassicHUD.IsChecked = settings.v_ClassicHUD;
                chk_Scoreboard.IsChecked = settings.v_Scoreboard;
                chk_DisguiseImage.IsChecked = settings.v_DisguiseImage;
                chk_DefaultBG.IsChecked = settings.v_DefaultBG;
                chk_ClassImage.IsChecked = settings.v_ClassImage;
                chk_DamagePos.IsChecked = settings.v_DamagePos;
                switch (settings.v_UberAnimation)
                {
                    case 1:
                        chk_UberFlash.IsChecked = true;
                        break;

                    case 2:
                        chk_UberRainbow.IsChecked = true;
                        break;

                    default:
                        chk_UberSolid.IsChecked = true;
                        break;
                }
                btn_UberBarColor.Background = (Brush)bc.ConvertFrom(settings.v_UberBarColor);
                btn_UberFullColor.Background = (Brush)bc.ConvertFrom(settings.v_UberFullColor);
                btn_UberFlash1.Background = (Brush)bc.ConvertFrom(settings.v_UberFlash1);
                btn_UberFlash2.Background = (Brush)bc.ConvertFrom(settings.v_UberFlash2);
                cb_XHairStyle.SelectedIndex = settings.v_XHairStyle;
                for (var i = 8; i <= 40; i += 2)
                    cb_XHairSize.Items.Add(i);
                cb_XHairSize.SelectedIndex = settings.v_XHairSize;
                btn_XHairColor.Background = (Brush)bc.ConvertFrom(settings.v_XHairBaseColor);
                btn_XHairPulse.Background = (Brush)bc.ConvertFrom(settings.v_XHairPulseColor);
                chk_XHairEnable.IsChecked = settings.v_XHairEnable;
                chk_XHairOutline.IsChecked = settings.v_XHairOutline;
                chk_XHairPulse.IsChecked = settings.v_XHairPulse;
                lb_HealthStyle.SelectedIndex = settings.v_HealthStyle;
                btn_HealthNormal.Background = (Brush)bc.ConvertFrom(settings.v_HealthNormal);
                btn_HealingDone.Background = (Brush)bc.ConvertFrom(settings.v_HealingDone);
                btn_HealthBuff.Background = (Brush)bc.ConvertFrom(settings.v_HealthBuff);
                btn_HealthLow.Background = (Brush)bc.ConvertFrom(settings.v_HealthLow);
                btn_AmmoClip.Background = (Brush)bc.ConvertFrom(settings.v_AmmoClip);
                btn_AmmoReserve.Background = (Brush)bc.ConvertFrom(settings.v_AmmoReserve);
                btn_AmmoClipLow.Background = (Brush)bc.ConvertFrom(settings.v_AmmoClipLow);
                btn_AmmoReserveLow.Background = (Brush)bc.ConvertFrom(settings.v_AmmoReserveLow);
                chk_TeamCenter.IsChecked = settings.v_TeamCenter;
                chk_ChatBottom.IsChecked = settings.v_ChatBottom;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"{settings.error_app_load}\n{ex.Message}", "Error: Loading Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Revert GUI settings to default
        /// </summary>
        private void ResetHUDSettings()
        {
            try
            {
                chk_ClassicHUD.IsChecked = false;
                chk_Scoreboard.IsChecked = false;
                chk_DisguiseImage.IsChecked = false;
                chk_DefaultBG.IsChecked = false;
                chk_ClassImage.IsChecked = false;
                chk_DamagePos.IsChecked = false;
                chk_UberFlash.IsChecked = true;
                btn_UberBarColor.Background = (Brush)bc.ConvertFrom("#EBE2CA");
                btn_UberFullColor.Background = (Brush)bc.ConvertFrom("#FF3219");
                btn_UberFlash1.Background = (Brush)bc.ConvertFrom("#FFA500");
                btn_UberFlash2.Background = (Brush)bc.ConvertFrom("#FF4500");
                cb_XHairStyle.SelectedIndex = 0;
                cb_XHairSize.SelectedIndex = 0;
                btn_XHairColor.Background = (Brush)bc.ConvertFrom("#F2F2F2");
                btn_XHairPulse.Background = (Brush)bc.ConvertFrom("#FF0000");
                chk_XHairEnable.IsChecked = false;
                chk_XHairOutline.IsChecked = false;
                chk_XHairPulse.IsChecked = false;
                lb_HealthStyle.SelectedIndex = 0;
                btn_HealthNormal.Background = (Brush)bc.ConvertFrom("#EBE2CA");
                btn_HealingDone.Background = (Brush)bc.ConvertFrom("#30FF30");
                btn_HealthBuff.Background = (Brush)bc.ConvertFrom("#30FF30");
                btn_HealthLow.Background = (Brush)bc.ConvertFrom("#FF9900");
                btn_AmmoClip.Background = (Brush)bc.ConvertFrom("#30FF30");
                btn_AmmoReserve.Background = (Brush)bc.ConvertFrom("#48FFFF");
                btn_AmmoClipLow.Background = (Brush)bc.ConvertFrom("#FF2A82");
                btn_AmmoReserveLow.Background = (Brush)bc.ConvertFrom("#FF801C");
                chk_TeamLeft.IsChecked = true;
                chk_ChatTop.IsChecked = true;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"{settings.error_app_reset}\n{ex.Message}", "Error: Resetting Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Display the folder browser dialog, asking the user to provide the tf/custom directory
        /// </summary>
        private void DisplayFolderBrowser()
        {
            var DirectoryBrowser = new FolderBrowserDialog { Description = $"Please select your tf\\custom folder. If the correct directory is not provided, the options to install and modify rayshud will not be available.", ShowNewFolderButton = true };
            if (DirectoryBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                if (DirectoryBrowser.SelectedPath.Contains("tf\\custom"))
                    settings.app_directory_base = DirectoryBrowser.SelectedPath;
        }

        /// <summary>
        /// Convert RGB color values to HEX
        /// </summary>
        private static string HexConverter(Color c)
        {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }

        private void CleanUpDirectory()
        {
            var app = System.Windows.Forms.Application.StartupPath;
            if (File.Exists($"{app}\\rayshud.zip"))
                File.Delete($"{app}\\rayshud.zip");
            if (Directory.Exists($"{settings.app_directory}\\rayshud-master"))
                Directory.Delete($"{settings.app_directory}\\rayshud-master", true);
        }

        #region CLICK EVENTS

        private void Btn_Install_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var client = new WebClient();
                client.DownloadFile("https://github.com/raysfire/rayshud/archive/master.zip", "rayshud.zip");
                ZipFile.ExtractToDirectory($"{System.Windows.Forms.Application.StartupPath}\\rayshud.zip", settings.app_directory_base);
                if (btn_Install.Content.ToString() != "Install")
                {
                    Directory.Delete(settings.app_directory, true);
                    Directory.Move($"{settings.app_directory_base}\\rayshud-master", settings.app_directory);
                }
                else
                    Directory.Move($"{settings.app_directory_base}\\rayshud-master", $"{settings.app_directory}\\rayshud");
                lbl_News.Content = $"Install Successful! {DateTime.Now}";
                if (!settings.app_directory.Contains("rayshud"))
                    settings.app_directory += "\\rayshud";
                settings.Save();
                CleanUpDirectory();
                SaveHUDSettings();
                ApplyHUDSettings();
                UpdateGUIButtons();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"{settings.error_app_install}\n{ex.Message}", "Error: Installing rayshud", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Btn_Uninstall_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Directory.Exists(settings.app_directory) && settings.app_directory.Contains("rayshud"))
                {
                    Directory.Delete(settings.app_directory, true);
                    settings.app_directory = settings.app_directory.Replace("rayshud", null);
                    lbl_News.Content = $"Uninstall Successful! {DateTime.Now}";
                    settings.Save();
                }
                SetHUDDirectory();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"{settings.error_app_uninstall}\n{ex.Message}", "Error: Uninstalling rayshud", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Btn_Save_Click(object sender, RoutedEventArgs e)
        {
            SaveHUDSettings();
            ApplyHUDSettings();
            lbl_News.Content = $"Settings Saved! {DateTime.Now}";
        }

        private void Btn_Reset_Click(object sender, RoutedEventArgs e)
        {
            ResetHUDSettings();
            lbl_News.Content = $"Settings Reset! {DateTime.Now}";
        }

        private void Btn_Start_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("steam://rungameid/440");
        }

        private void Btn_SteamGroup_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://steamcommunity.com/groups/rayshud");
        }

        private void Btn_ReportIssue_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/CriticalFlaw/rayshud-Installer/issues");
        }

        /// <summary>
        /// Display the color picker dialog, asking the user to pick the color for a given control
        /// </summary>
        private void ColorPicker_Click(object sender, RoutedEventArgs e)
        {
            if (ColorPickerWindow.ShowDialog(out var color) != true) return;
            ((System.Windows.Controls.Button)sender).Background = (Brush)bc.ConvertFrom(HexConverter(color));
        }

        #endregion CLICK EVENTS
    }
}