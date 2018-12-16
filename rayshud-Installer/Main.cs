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
using System.Windows.Forms;

namespace rayshud_Installer
{
    public partial class Main : Form
    {
        // Create the error-tracking object
        private readonly RavenClient ravenClient = new RavenClient(Properties.Settings.Default.sentry_io_key);

        // Used to set the tf/custom directory that'll be used throughout
        private string TF2Directory;

        // Used to read and write HUD options to the settings file
        private Properties.Settings settings = new Properties.Settings();

        // Used for rendering the crosshair preview using a custom font
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, [System.Runtime.InteropServices.In] ref uint pcFonts);

        private readonly PrivateFontCollection fonts = new PrivateFontCollection();
        private Font myFont;

        public Main()
        {
            InitializeComponent();
            InitializeCustomFonts();
            CheckLiveVersion();
            CheckTF2Directory();
        }

        private void InitializeCustomFonts()
        {
            uint dummy = 0;
            var fontData = Properties.Resources.Crosshairs;
            //var fontData = Properties.Resources.Cerbetica;
            var fontPtr = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(fontData.Length);
            System.Runtime.InteropServices.Marshal.Copy(fontData, 0, fontPtr, fontData.Length);
            fonts.AddMemoryFont(fontPtr, fontData.Length);
            AddFontMemResourceEx(fontPtr, (uint)fontData.Length, IntPtr.Zero, ref dummy);
            System.Runtime.InteropServices.Marshal.FreeCoTaskMem(fontPtr);
            myFont = new Font(fonts.Families[0], 16.0F);
            lblCrosshair.Parent = pbPreview;
            lblCrosshair.BackColor = Color.Transparent;
            lblCrosshair.Font = new Font(myFont.FontFamily, 52, FontStyle.Regular);
            //this.Font = new Font(myFont.FontFamily, 8, FontStyle.Regular);
        }

        private void CheckLiveVersion()
        {
            try
            {
                var client = new WebClient();
                // Download the latest rayshud README
                var readme_text = client.DownloadString(settings.git_hud_readme).Split('\n');
                // Retrieve the latest version number from the README
                StatusBarVersion.Text = readme_text[readme_text.Length - 2];
                // Retrieve the latest assembly version
                readme_text = client.DownloadString(settings.git_installer_version).Split('\n');
                // Retrieve the installed assembly version
                var versionInfo = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
                // Compare the installer version numbers
                if (string.Equals(readme_text[readme_text.Length - 2], $"[assembly: AssemblyVersion(\"{versionInfo.FileVersion}\")]"))
                    MessageBox.Show(settings.msg_update_installer, "New Installer Version Available!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"{settings.error_version_live}\n{ex.Message}", "Error: Checking latest version", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CheckTF2Directory()
        {
            try
            {
                // Check if a valid directory already exists in the settings file
                if (Directory.Exists(settings.v_TF2Directory) && settings.v_TF2Directory != null)
                    TF2Directory = settings.v_TF2Directory;
                else
                {
                    // Otherwise go through all the hard-drives and attempt to find the directory
                    foreach (var drive in DriveInfo.GetDrives())
                    {
                        if (Directory.Exists($"{drive.Name}\\Program Files\\{settings.directory_base}"))
                        {
                            TF2Directory = $"{drive.Name}\\Program Files)\\{settings.directory_base}";
                            break;
                        }
                        else if (Directory.Exists($"{drive.Name}\\Program Files (x86)\\{settings.directory_base}"))
                        {
                            TF2Directory = $"{drive.Name}\\Program Files (x86)\\{settings.directory_base}";
                            break;
                        }
                    }
                }
                // If a directory is still not set, ask the user to provide it manually
                if (string.IsNullOrWhiteSpace(TF2Directory))
                {
                    // Create the directory browser object
                    var DirectoryBrowser = new FolderBrowserDialog()
                    {
                        Description = $"Please select your tf\\custom folder. Example:\n{settings.directory_base}",
                        ShowNewFolderButton = true
                    };
                    var validHUDDirectory = false;
                    while (validHUDDirectory == false)
                    {
                        // Loop until the user clicks Cancel or provides a directory that contains tf/custom
                        if (DirectoryBrowser.ShowDialog() == DialogResult.OK)
                        {
                            if (!DirectoryBrowser.SelectedPath.Contains("tf\\custom")) continue;
                            TF2Directory = DirectoryBrowser.SelectedPath;
                            validHUDDirectory = true;
                        }
                        else if (DirectoryBrowser.ShowDialog() == DialogResult.Cancel)
                            break;  // User clicks Cancel
                        else
                            break;  // User clicks anything else
                    }
                }

                // If the directory is STILL not set, disable installation
                if (string.IsNullOrWhiteSpace(TF2Directory))
                {
                    txtDirectory.Text = settings.directory_not_set;
                    btnInstall.Enabled = false;
                    btnPlayTF2.Enabled = false;
                }
                else
                {
                    txtDirectory.Text = TF2Directory;
                    btnInstall.Enabled = true;
                    btnPlayTF2.Enabled = true;
                    settings.v_TF2Directory = TF2Directory;
                    CheckHUDDirectory();
                }
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"{settings.directory_not_found_hud}\n{ex.Message}", "Error: Checking tf/custom directory", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CheckHUDDirectory()
        {
            try
            {
                // If a tagged version of rayshud exists, remove the tag
                if (Directory.Exists($"{TF2Directory}\\rayshud-master"))
                    Directory.Delete($"{TF2Directory}\\rayshud-master");
                if (Directory.Exists($"{TF2Directory}\\rayshud"))
                {
                    btnUninstall.Enabled = true;
                    btnSaveChanges.Enabled = true;
                    btnOpenDirectory.Enabled = true;
                    btnSetDefault.Enabled = true;
                    txtDirectory.Text = $"{TF2Directory}\\rayshud";
                    // Compare the live and installed version numbers to determine if rayshud is updated
                    if (File.ReadLines($"{TF2Directory}\\rayshud\\README.md").Last().ToString().Trim() == StatusBarVersion.ToString().Trim())
                    {
                        btnInstall.Text = "Refresh";
                        StatusBarStatus.Text = "Installed, Updated";
                    }
                    else
                    {
                        btnInstall.Text = "Update";
                        StatusBarStatus.Text = "Installed, Outdated";
                    }
                }
                else
                {
                    btnUninstall.Enabled = false;
                    btnSaveChanges.Enabled = false;
                    btnOpenDirectory.Enabled = false;
                    btnSetDefault.Enabled = false;
                    btnInstall.Text = "Install";
                    StatusBarStatus.Text = "Not Installed";
                }
                DisplayHUDSettings();
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"{settings.error_version_local}\n{ex.Message}", "Error: Checking rayshud directory", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DisplayHUDSettings()
        {
            try
            {
                // Set installer controls to default rayshud values
                SetDefaultHUDSettings();

                // Main Menu Style - Modern (0) or Classic (1)
                if (settings.v_HUDVersion)
                    cbHUDVersion.SelectedIndex = 1;

                // Scoreboard Style - Normal (0) or Minimal (1)
                if (settings.v_Scoreboard)
                    cbScoreboard.SelectedIndex = 1;

                // Chatbox Position - Top-Left (false) or Bottom-Left (true)
                if (settings.v_ChatBox)
                    rbChatBoxBottom.Checked = true;

                // Team/Class Select Position - Left (false) or Center (true)
                if (settings.v_TeamSelect)
                    rbTeamSelectCenter.Checked = true;

                // Spy Disguise Image - off (false) or on (true)
                cbDisguiseImage.Checked = settings.v_DisguiseImage;

                // Default Background Image - off (false) or on (true)
                cbDefaultMenuBG.Checked = settings.v_DefaultMenuBG;

                // Main Menu Class Images - off (false) or on (true)
                cbMenuClassImages.Checked = settings.v_MenuClassImages;

                // Damage Value Above Health - health (false) or ammo (true)
                cbDamageValuePos.Checked = settings.v_DamageValuePos;

                // Ubercharge Animation - Flash (1), Solid (2) or Rainbow (3)
                switch (settings.v_UberAnimation)
                {
                    default:
                        rbUberAnimation1.Checked = true;
                        break;

                    case 2:
                        rbUberAnimation2.Checked = true;
                        break;

                    case 3:
                        rbUberAnimation3.Checked = true;
                        break;
                }

                // Ubercharge Bar Color (RGB)
                var split = settings.v_UberBarColor.Split(new char[0]);
                btnUberBarColor.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                // Ubercharge Solid Color (RGB)
                split = settings.v_UberFullColor.Split(new char[0]);
                btnUberFullColor.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                // Ubercharge Flash Colors (RGB)
                split = settings.v_UberFlashColor1.Split(new char[0]);
                btnUberFlashColor1.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));
                split = settings.v_UberFlashColor2.Split(new char[0]);
                btnUberFlashColor2.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                // Crosshair Style
                lbXHairStyles.SelectedIndex = settings.v_XHairStyle - 1;

                // Crosshair Enable - off (false) or on (true)
                cbXHairEnabled.Checked = settings.v_XHairEnabled;

                // Crosshair Outline - off (false) or on (true)
                cbXHairOutline.Checked = settings.v_XHairOutline;

                // Crosshair Pulse - off (false) or on (true)
                cbXHairPulse.Checked = settings.v_XHairPulse;

                // Crosshair Sizes - based on crosshair style
                cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf(settings.v_XHairSize.ToString());

                // Crosshair Stock Color (RGB)
                split = settings.v_XHairColor.Split(new char[0]);
                btnXHairColor.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));
                lblCrosshair.ForeColor = btnXHairColor.BackColor;

                // Crosshair Pulse Color (RGB)
                split = settings.v_XHairPulseColor.Split(new char[0]);
                btnXHairPulseColor.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                // Player Health Style - Default (1), TeamBar (2), Cross (3) or Broesel (4)
                lbHealthStyle.SelectedIndex = settings.v_HealthStyle - 1;

                // Healing Done Color (RGB)
                split = settings.v_HealingDone.Split(new char[0]);
                btnHealingDone.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                // Health Stock Color (RGB)
                split = settings.v_HealthNormal.Split(new char[0]);
                btnHealthNormal.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                // Health Buff Color (RGB)
                split = settings.v_HealthBuff.Split(new char[0]);
                btnHealthBuff.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                // Health Low Color (RGB)
                split = settings.v_HealthLow.Split(new char[0]);
                btnHealthLow.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                // Ammo Clip Stock Color (RGB)
                split = settings.v_AmmoClip.Split(new char[0]);
                btnAmmoClip.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                // Ammo Reserve Stock Color (RGB)
                split = settings.v_AmmoReserve.Split(new char[0]);
                btnAmmoReserve.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                // Ammo Clip Low Color (RGB)
                split = settings.v_AmmoClipLow.Split(new char[0]);
                btnAmmoClipLow.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));

                // Ammo Reserve Low Color (RGB)
                split = settings.v_AmmoReserveLow.Split(new char[0]);
                btnAmmoReserveLow.BackColor = Color.FromArgb(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"{settings.error_load}\n{ex.Message}", "Error: Loading Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateModificationDate()
        {
            settings.v_LastModified = DateTime.Now.ToString(CultureInfo.CurrentCulture);
            StatusBarLastModified.Text = $"Last Modified: {DateTime.Now.ToString(CultureInfo.CurrentCulture)}";
        }

        private void btnInstall_Click(object sender, EventArgs e)
        {
            try
            {
                var client = new WebClient();
                // Remove the temporary downloaded rayshud files
                if (File.Exists($"{Application.StartupPath}\\{settings.temp_name}"))
                    File.Delete($"{Application.StartupPath}\\{settings.temp_name}");
                // Download the latest rayshud from GitHub and extract into the tf/custom directory
                client.DownloadFile("https://github.com/raysfire/rayshud/archive/master.zip", "rayshud.zip");
                ZipFile.ExtractToDirectory($"{Application.StartupPath}\\{settings.temp_name}", TF2Directory);
                // Either do a clean install or refresh/update of rayshud
                switch (btnInstall.Text)
                {
                    case "Update":
                    case "Refresh":
                        // Replace the installed rayshud with a fresh copy
                        Directory.Delete($"{TF2Directory}\\rayshud", true);
                        Directory.Move($"{TF2Directory}\\rayshud-master", $"{TF2Directory}\\rayshud");
                        MessageBox.Show(settings.msg_refreshed, "rayshud Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;

                    default:
                        Directory.Move($"{TF2Directory}\\rayshud-master", $"{TF2Directory}\\rayshud");
                        MessageBox.Show(settings.msg_installed, "rayshud Installed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                }
                // Remove the temporary downloaded rayshud files
                if (File.Exists($"{Application.StartupPath}\\{settings.temp_name}"))
                    File.Delete($"{Application.StartupPath}\\{settings.temp_name}");
                CheckHUDDirectory();
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"{settings.error_install}\n{ex.Message}", "Error: Installing rayshud", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnUninstall_Click(object sender, EventArgs e)
        {
            try
            {
                if (Directory.Exists($"{TF2Directory}\\rayshud"))
                {
                    Directory.Delete($"{TF2Directory}\\rayshud", true);
                    MessageBox.Show(settings.msg_removeed, "rayshud Uninstalled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtDirectory.Text = TF2Directory;
                    StatusBarLastModified.Text = "";
                    CheckHUDDirectory();
                }
                else
                    btnUninstall.Enabled = false;
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"{settings.error_remove}\n{ex.Message}", "Error: Uninstalling rayshud", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSaveChanges_Click(object sender, EventArgs e)
        {
            try
            {
                // Update the installer configuration file
                UpdateModificationDate();
                // Set the directories to be used during the file reads/writes
                var console = $"{TF2Directory}\\rayshud\\materials\\console";
                var resource = $"{TF2Directory}\\rayshud\\resource\\ui";
                var scripts = $"{TF2Directory}\\rayshud\\scripts";
                var colorScheme = $"{TF2Directory}\\rayshud\\resource\\scheme\\clientscheme_colors.res";
                var animations = $"{TF2Directory}\\rayshud\\scripts\\hudanimations_custom.txt";
                var chat = $"{TF2Directory}\\rayshud\\resource\\ui\\basechat.res";
                var layout = $"{TF2Directory}\\rayshud\\scripts\\hudlayout.res";
                var classicMaterials = $"{TF2Directory}\\rayshud\\customizations\\Main Menu\\Classic\\materials\\console";
                var classicResources = $"{TF2Directory}\\rayshud\\customizations\\Main Menu\\Classic\\resource\\";
                var modernMaterials = $"{TF2Directory}\\rayshud\\customizations\\Main Menu\\Modern\\materials\\console";
                var modernResources = $"{TF2Directory}\\rayshud\\customizations\\Main Menu\\Modern\\resource\\";
                var scoreboard = $"{TF2Directory}\\rayshud\\customizations\\Scoreboard";
                var teammenu = $"{TF2Directory}\\rayshud\\customizations\\Team Menu";
                var playerhealth = $"{TF2Directory}\\rayshud\\customizations\\Player Health";
                var mainmenu = $"{TF2Directory}\\rayshud\\resource\\ui\\mainmenuoverride.res";
                var damage = $"{TF2Directory}\\rayshud\\resource\\ui\\huddamageaccount.res";

                // 1. Main Menu Style - either classic or modern, copy and replace existing files
                if (settings.v_HUDVersion)
                {
                    if (Directory.Exists($"{console}_off"))
                    {
                        File.Copy($"{classicMaterials}\\background_upward.vtf", $"{console}_off\\background_upward.vtf", true);
                        File.Copy($"{classicMaterials}\\background_upward_widescreen.vtf", $"{console}_off\\background_upward_widescreen.vtf", true);
                    }
                    else
                    {
                        File.Copy($"{classicMaterials}\\background_upward.vtf", $"{console}\\background_upward.vtf", true);
                        File.Copy($"{classicMaterials}\\background_upward_widescreen.vtf", $"{console}\\background_upward_widescreen.vtf", true);
                    }

                    File.Copy($"{classicResources}\\ui\\mainmenuoverride.res", $"{resource}\\mainmenuoverride.res", true);
                    File.Copy($"{classicResources}\\gamemenu.res", $"{TF2Directory}\\rayshud\\resource\\gamemenu.res", true);
                }
                else
                {
                    if (Directory.Exists($"{console}_off"))
                    {
                        File.Copy($"{modernMaterials}\\background_upward.vtf", $"{console}_off\\background_upward.vtf", true);
                        File.Copy($"{modernMaterials}\\background_upward_widescreen.vtf", $"{console}_off\\background_upward_widescreen.vtf", true);
                    }
                    else
                    {
                        File.Copy($"{modernMaterials}\\background_upward.vtf", $"{console}\\background_upward.vtf", true);
                        File.Copy($"{modernMaterials}\\background_upward_widescreen.vtf", $"{console}\\background_upward_widescreen.vtf", true);
                    }

                    File.Copy($"{modernResources}\\ui\\mainmenuoverride.res", $"{resource}\\mainmenuoverride.res", true);
                    File.Copy($"{modernResources}\\gamemenu.res", $"{TF2Directory}\\rayshud\\resource\\gamemenu.res", true);
                }

                // 2. Scoreboard Style - either normal or minimal (6v6), copy and replace existing files
                if (settings.v_Scoreboard)
                    File.Copy($"{scoreboard}\\scoreboard-minimal.res", $"{resource}\\scoreboard.res", true);
                else
                    File.Copy($"{scoreboard}\\scoreboard-default.res", $"{resource}\\scoreboard.res", true);

                // 3. Default Background - enable or disable the custom backgrounds files by renaming them
                if (settings.v_DefaultMenuBG)
                {
                    if (Directory.Exists(console) && File.Exists($"{scripts}\\chapterbackgrounds.txt"))
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
                if (settings.v_TeamSelect)
                {
                    File.Copy($"{teammenu}\\TeamMenu-center.res", $"{resource}\\TeamMenu.res", true);
                    File.Copy($"{teammenu}\\ClassSelection-center.res", $"{resource}\\ClassSelection.res", true);
                }
                else
                {
                    File.Copy($"{teammenu}\\TeamMenu-left.res", $"{resource}\\TeamMenu.res", true);
                    File.Copy($"{teammenu}\\ClassSelection-left.res", $"{resource}\\ClassSelection.res", true);
                }

                // 5. Player Health Style - either default, cross, teambar or broesel, copy and replace existing files
                switch (settings.v_HealthStyle)
                {
                    default:
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
                var crosshairPulseIndex = 80;
                var disguiseImageIndex = 91;
                var uberAnimationIndex = 107;
                var lines = File.ReadAllLines(animations);
                lines[(disguiseImageIndex + 0) - 1] = lines[(disguiseImageIndex + 0) - 1].Replace("//", string.Empty);
                lines[(disguiseImageIndex + 1) - 1] = lines[(disguiseImageIndex + 1) - 1].Replace("//", string.Empty);
                lines[(disguiseImageIndex + 2) - 1] = lines[(disguiseImageIndex + 2) - 1].Replace("//", string.Empty);
                lines[(disguiseImageIndex + 7) - 1] = lines[(disguiseImageIndex + 7) - 1].Replace("//", string.Empty);
                lines[(disguiseImageIndex + 8) - 1] = lines[(disguiseImageIndex + 8) - 1].Replace("//", string.Empty);
                lines[(disguiseImageIndex + 9) - 1] = lines[(disguiseImageIndex + 9) - 1].Replace("//", string.Empty);
                lines[(uberAnimationIndex + 0) - 1] = lines[(uberAnimationIndex + 0) - 1].Replace("//", string.Empty);
                lines[(uberAnimationIndex + 1) - 1] = lines[(uberAnimationIndex + 1) - 1].Replace("//", string.Empty);
                lines[(uberAnimationIndex + 2) - 1] = lines[(uberAnimationIndex + 2) - 1].Replace("//", string.Empty);
                File.WriteAllLines(animations, lines);

                // 6. Spy Disguise Image - enable or disable by commenting out the lines
                if (settings.v_DisguiseImage)
                {
                    lines[(disguiseImageIndex + 0) - 1] = $"\t{lines[(disguiseImageIndex + 0) - 1].Replace("//", string.Empty).Trim()}";
                    lines[(disguiseImageIndex + 1) - 1] = $"\t{lines[(disguiseImageIndex + 1) - 1].Replace("//", string.Empty).Trim()}";
                    lines[(disguiseImageIndex + 2) - 1] = $"\t{lines[(disguiseImageIndex + 2) - 1].Replace("//", string.Empty).Trim()}";
                    lines[(disguiseImageIndex + 7) - 1] = $"\t{lines[(disguiseImageIndex + 7) - 1].Replace("//", string.Empty).Trim()}";
                    lines[(disguiseImageIndex + 8) - 1] = $"\t{lines[(disguiseImageIndex + 8) - 1].Replace("//", string.Empty).Trim()}";
                    lines[(disguiseImageIndex + 9) - 1] = $"\t{lines[(disguiseImageIndex + 9) - 1].Replace("//", string.Empty).Trim()}";
                }
                else
                {
                    lines[(disguiseImageIndex + 0) - 1] = $"\t//{lines[(disguiseImageIndex + 0) - 1].Trim()}";
                    lines[(disguiseImageIndex + 1) - 1] = $"\t//{lines[(disguiseImageIndex + 1) - 1].Trim()}";
                    lines[(disguiseImageIndex + 2) - 1] = $"\t//{lines[(disguiseImageIndex + 2) - 1].Trim()}";
                    lines[(disguiseImageIndex + 7) - 1] = $"\t//{lines[(disguiseImageIndex + 7) - 1].Trim()}";
                    lines[(disguiseImageIndex + 8) - 1] = $"\t//{lines[(disguiseImageIndex + 8) - 1].Trim()}";
                    lines[(disguiseImageIndex + 9) - 1] = $"\t//{lines[(disguiseImageIndex + 9) - 1].Trim()}";
                }

                // 7. Uber Animation - enable or disable by commenting out the lines
                switch (settings.v_UberAnimation)
                {
                    default:
                        lines[(uberAnimationIndex + 0) - 1] = $"\t{lines[(uberAnimationIndex + 0) - 1].Replace("//", string.Empty).Trim()}";
                        lines[(uberAnimationIndex + 1) - 1] = $"\t//{lines[(uberAnimationIndex + 1) - 1].Trim()}";
                        lines[(uberAnimationIndex + 2) - 1] = $"\t//{lines[(uberAnimationIndex + 2) - 1].Trim()}";
                        break;

                    case 2:
                        lines[(uberAnimationIndex + 0) - 1] = $"\t//{lines[(uberAnimationIndex + 0) - 1].Trim()}";
                        lines[(uberAnimationIndex + 1) - 1] = $"\t{lines[(uberAnimationIndex + 1) - 1].Replace("//", string.Empty).Trim()}";
                        lines[(uberAnimationIndex + 2) - 1] = $"\t//{lines[(uberAnimationIndex + 2) - 1].Trim()}";
                        break;

                    case 3:
                        lines[(uberAnimationIndex + 0) - 1] = $"\t//{lines[(uberAnimationIndex + 0) - 1].Trim()}";
                        lines[(uberAnimationIndex + 1) - 1] = $"\t//{lines[(uberAnimationIndex + 1) - 1].Trim()}";
                        lines[(uberAnimationIndex + 2) - 1] = $"\t{lines[(uberAnimationIndex + 2) - 1].Replace("//", string.Empty).Trim()}";
                        break;
                }

                // 8. Crosshair Pulse - enable or disable by commenting out the lines
                if (settings.v_XHairPulse)
                {
                    lines[(crosshairPulseIndex + 0) - 1] = lines[(crosshairPulseIndex + 0) - 1].Replace("//", string.Empty);
                    lines[(crosshairPulseIndex + 1) - 1] = lines[(crosshairPulseIndex + 1) - 1].Replace("//", string.Empty);
                    lines[(crosshairPulseIndex + 2) - 1] = lines[(crosshairPulseIndex + 2) - 1].Replace("//", string.Empty);
                    lines[(crosshairPulseIndex + 3) - 1] = lines[(crosshairPulseIndex + 3) - 1].Replace("//", string.Empty);
                    lines[(crosshairPulseIndex + 4) - 1] = lines[(crosshairPulseIndex + 4) - 1].Replace("//", string.Empty);
                    lines[(crosshairPulseIndex + 5) - 1] = lines[(crosshairPulseIndex + 5) - 1].Replace("//", string.Empty);
                }
                else
                {
                    lines[(crosshairPulseIndex + 0) - 1] = $"//{lines[(crosshairPulseIndex + 0) - 1]}";
                    lines[(crosshairPulseIndex + 1) - 1] = $"//{lines[(crosshairPulseIndex + 1) - 1]}";
                    lines[(crosshairPulseIndex + 2) - 1] = $"//{lines[(crosshairPulseIndex + 2) - 1]}";
                    lines[(crosshairPulseIndex + 3) - 1] = $"//{lines[(crosshairPulseIndex + 3) - 1]}";
                    lines[(crosshairPulseIndex + 4) - 1] = $"//{lines[(crosshairPulseIndex + 4) - 1]}";
                    lines[(crosshairPulseIndex + 5) - 1] = $"//{lines[(crosshairPulseIndex + 5) - 1]}";
                }

                File.WriteAllLines(animations, lines);

                // 9. Mein Menu Class Image - enable or disable by commenting out the lines
                lines = File.ReadAllLines(mainmenu);
                int index = 248;
                if (settings.v_HUDVersion)
                    index = 241;
                if (settings.v_MenuClassImages)
                {
                    lines[(index + 0) - 1] = "\t\t\"xpos\"\t\t\"c-250\"";
                    lines[(index + 1) - 1] = "\t\t\"ypos\"\t\t\"-80\"";
                }
                else
                {
                    lines[(index + 0) - 1] = "\t\t\"xpos\"\t\t\"9999\"";
                    lines[(index + 1) - 1] = "\t\t\"ypos\"\t\t\"9999\"";
                }
                File.WriteAllLines(mainmenu, lines);

                // 10. Chat box position - either top or bottom, change the ypos value of basechat.res
                lines = File.ReadAllLines(chat);
                if (settings.v_ChatBox)
                    lines[10 - 1] = "\t\t\"ypos\"\t\t\t\t\"360\"";
                else
                    lines[10 - 1] = "\t\t\"ypos\"\t\t\t\t\"30\"";
                File.WriteAllLines(chat, lines);

                // Crosshairs - disable all and remove outlining
                lines = File.ReadAllLines(layout);
                for (int x = 13; x <= 51; x += 19)
                {
                    lines[x - 1] = "\t\t\"visible\"\t\t\"0\"";
                    lines[(x + 1) - 1] = "\t\t\"enabled\"\t\t\"0\"";
                    lines[(x + 7) - 1] = lines[x + 7 - 1].Replace("Outline", string.Empty);
                    File.WriteAllLines(layout, lines);
                }

                // 11. Crosshairs - either enabled or disabled with or without outlines, change the visible, enabled and font values of hudlayout.res
                if (settings.v_XHairEnabled)
                {
                    if (settings.v_XHairStyle >= 1 && settings.v_XHairStyle <= 15)
                    {
                        lines[13 - 1] = "\t\t\"visible\"\t\t\"1\"";
                        lines[14 - 1] = "\t\t\"enabled\"\t\t\"1\"";
                        if (settings.v_XHairOutline)
                            lines[20 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{cbXHairSizes.Text}Outline\"";
                        else
                            lines[20 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{cbXHairSizes.Text}\"";
                    }
                    else if (settings.v_XHairStyle == 16)
                    {
                        lines[32 - 1] = "\t\t\"visible\"\t\t\"1\"";
                        lines[33 - 1] = "\t\t\"enabled\"\t\t\"1\"";
                        if (settings.v_XHairOutline)
                            lines[39 - 1] = $"\t\t\"font\"\t\t\t\"KonrWings{cbXHairSizes.Text}Outline\"";
                        else
                            lines[39 - 1] = $"\t\t\"font\"\t\t\t\"KonrWings{cbXHairSizes.Text}\"";
                    }
                    else if (settings.v_XHairStyle >= 17 && settings.v_XHairStyle <= 84)
                    {
                        lines[51 - 1] = "\t\t\"visible\"\t\t\"1\"";
                        lines[52 - 1] = "\t\t\"enabled\"\t\t\"1\"";
                        if (settings.v_XHairOutline)
                            lines[58 - 1] = $"\t\t\"font\"\t\t\t\"size:{cbXHairSizes.Text},outline:on\"";
                        else
                            lines[58 - 1] = $"\t\t\"font\"\t\t\t\"size:{cbXHairSizes.Text},outline:off\"";
                    }

                    var crosshairStyleIndex = 16;
                    var konrwingsStyleIndex = 35;
                    switch (settings.v_XHairStyle)
                    {
                        case 1: // BasicCross
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-102\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-99\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"2\"";
                            break;

                        case 2: // BasicCrossLarge
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-102\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-98\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"2\"";
                            break;

                        case 3: // BasicCrossSmall
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-101\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-99\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"2\"";
                            break;

                        case 4: // BasicDot
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-103\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-100\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"3\"";
                            break;

                        case 5: // CircleDot
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-103\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-96\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"8\"";
                            break;

                        case 6: // OpenCross
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-85\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-100\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"i\"";
                            break;

                        case 7: // OpenCrossDot
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-85\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-100\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"h\"";
                            break;

                        case 8: // ScatterSpread
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-99\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-99\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"0\"";
                            break;

                        case 9: // ThinCircle
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-100\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-96\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"9\"";
                            break;

                        case 10: // ThinCross
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-100\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-103\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"+\"";
                            break;

                        case 11: // Wings
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-100\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-97\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"d\"";
                            break;

                        case 12: // WingsPlus
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-100\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-97\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"c\"";
                            break;

                        case 13: // WingsSmall
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-100\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-97\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"g\"";
                            break;

                        case 14: // WingsSmallDot
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-100\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-97\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"f\"";
                            break;

                        case 15: // xHairCircle
                            lines[(crosshairStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-100\"";
                            lines[(crosshairStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-102\"";
                            lines[(crosshairStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"0\"";
                            break;

                        case 16: // KonrWings
                            lines[(konrwingsStyleIndex + 0) - 1] = "\t\t\"xpos\"\t\t\t\"c-100\"";
                            lines[(konrwingsStyleIndex + 1) - 1] = "\t\t\"ypos\"\t\t\t\"c-102\"";
                            lines[(konrwingsStyleIndex + 5) - 1] = "\t\t\"labelText\"\t\t\"0\"";
                            break;

                        default:
                            lines[crosshairStyleIndex - 1] = "\t\t\"labelText\"\t\t\"2\"";
                            break;
                    }
                    File.WriteAllLines(layout, lines);
                }

                // 12. Color Values - replace the color RGB values in clientscheme_colors.res file
                lines = File.ReadAllLines(colorScheme);
                lines[7 - 1] = $"\t\t\"Ammo In Clip\"\t\t\t\t\t\"{settings.v_AmmoClip}\"";
                lines[8 - 1] = $"\t\t\"Ammo In Reserve\"\t\t\t\t\"{settings.v_AmmoReserve}\"";
                lines[9 - 1] = $"\t\t\"Ammo In Clip Low\"\t\t\t\t\"{settings.v_AmmoClipLow}\"";
                lines[10 - 1] = $"\t\t\"Ammo In Reserve Low\"\t\t\t\"{settings.v_AmmoReserveLow}\"";
                lines[23 - 1] = $"\t\t\"Health Normal\"\t\t\t\t\t\"{settings.v_HealthNormal}\"";
                lines[24 - 1] = $"\t\t\"Health Buff\"\t\t\t\t\t\"{settings.v_HealthBuff}\"";
                lines[25 - 1] = $"\t\t\"Health Hurt\"\t\t\t\t\t\"{settings.v_HealthLow}\"";
                lines[32 - 1] = $"\t\t\"Uber Bar Color\"\t\t\t\t\"{settings.v_UberBarColor}\"";
                lines[35 - 1] = $"\t\t\"Solid Color Uber\"\t\t\t\t\"{settings.v_UberFullColor}\"";
                lines[37 - 1] = $"\t\t\"Flashing Uber Color1\"\t\t\t\"{settings.v_UberFlashColor1}\"";
                lines[38 - 1] = $"\t\t\"Flashing Uber Color2\"\t\t\t\"{settings.v_UberFlashColor2}\"";
                lines[41 - 1] = $"\t\t\"Heal Numbers\"\t\t\t\t\t\"{settings.v_HealingDone}\"";
                lines[45 - 1] = $"\t\t\"Crosshair\"\t\t\t\t\t\t\"{settings.v_XHairColor}\"";
                lines[46 - 1] = $"\t\t\"CrosshairDamage\"\t\t\t\t\"{settings.v_XHairPulseColor}\"";
                File.WriteAllLines(colorScheme, lines);

                // 13. Damage Value Position - either above health or ammo, change the xpos value in huddamageaccount.res
                lines = File.ReadAllLines(damage);
                if (settings.v_DamageValuePos)
                    lines[22 - 1] = "\t\t\"xpos\"\t\t\t\"c+108\"";
                else
                    lines[22 - 1] = "\t\t\"xpos\"\t\t\t\"c-188\"";
                File.WriteAllLines(damage, lines);
                // Save the changes to the settings file
                settings.Save();
                MessageBox.Show(settings.msg_updateed, "Changes Saved!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SentryEvent(ex));
                MessageBox.Show($"{settings.error_update}\n{ex.Message}", "Error: Updating rayshud", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
            var DirectoryBrowser = new FolderBrowserDialog();
            DirectoryBrowser.Description = $"Please select your tf\\custom folder. Example:\n{settings.directory_base}";
            DirectoryBrowser.ShowNewFolderButton = true;
            while (IsDirectoryValid == false)
            {
                // Until the correct path is provided or the user clicks 'Cancel' - keep prompting for a valid tf/custom directory.
                if (DirectoryBrowser.ShowDialog() == DialogResult.OK)
                {
                    if (!DirectoryBrowser.SelectedPath.Contains("tf\\custom")) continue;
                    TF2Directory = DirectoryBrowser.SelectedPath;
                    txtDirectory.Text = TF2Directory;
                    settings.v_TF2Directory = txtDirectory.Text;
                    btnInstall.Enabled = true;
                    btnPlayTF2.Enabled = true;
                    CheckHUDDirectory();
                    IsDirectoryValid = true;
                }
                else
                    break;
            }
        }

        private void btnGitRepo_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/CriticalFlaw/rayshud-Installer");
        }

        private void btnGitIssue_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/CriticalFlaw/rayshud-Installer/issues");
        }

        private void btnSteamGroup_Click(object sender, EventArgs e)
        {
            Process.Start("https://steamcommunity.com/groups/rayshud");
        }

        private void btnAndKnuckles_Click(object sender, EventArgs e)
        {
            var bitmap = new Bitmap(Properties.Resources.KnucklesCrosses);
            var directory = $"{Application.StartupPath}\\KnuckleCrosses.jpg";
            if (File.Exists(directory))
                File.Delete(directory);
            bitmap.Save(directory);
            Process.Start(directory);
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
                    settings.v_UberBarColor = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnUberFullColor":
                    btnUberFullColor.BackColor = colorPicker.Color;
                    settings.v_UberFullColor = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnUberFlashColor1":
                    btnUberFlashColor1.BackColor = colorPicker.Color;
                    settings.v_UberFlashColor1 = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnUberFlashColor2":
                    btnUberFlashColor2.BackColor = colorPicker.Color;
                    settings.v_UberFlashColor2 = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnXHairColor":
                    btnXHairColor.BackColor = colorPicker.Color;
                    settings.v_XHairColor = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    // Update the crosshair preview color
                    lblCrosshair.ForeColor = btnXHairColor.BackColor;
                    break;

                case "btnXHairPulseColor":
                    btnXHairPulseColor.BackColor = colorPicker.Color;
                    settings.v_XHairPulseColor = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnHealingDone":
                    btnHealingDone.BackColor = colorPicker.Color;
                    settings.v_HealingDone = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnHealthNormal":
                    btnHealthNormal.BackColor = colorPicker.Color;
                    settings.v_HealthNormal = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnHealthBuff":
                    btnHealthBuff.BackColor = colorPicker.Color;
                    settings.v_HealthBuff = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnHealthLow":
                    btnHealthLow.BackColor = colorPicker.Color;
                    settings.v_HealthLow = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnAmmoClip":
                    btnAmmoClip.BackColor = colorPicker.Color;
                    settings.v_AmmoClip = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnAmmoReserve":
                    btnAmmoReserve.BackColor = colorPicker.Color;
                    settings.v_AmmoReserve = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnAmmoClipLow":
                    btnAmmoClipLow.BackColor = colorPicker.Color;
                    settings.v_AmmoClipLow = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                case "btnAmmoReserveLow":
                    btnAmmoReserveLow.BackColor = colorPicker.Color;
                    settings.v_AmmoReserveLow = $"{colorPicker.Color.R} {colorPicker.Color.G} {colorPicker.Color.B} 255";
                    break;

                default:
                    btnAndKnuckles.BackColor = colorPicker.Color;
                    break;
            }
        }

        private void cbHUDVersion_SelectedIndexChanged(object sender, EventArgs e)
        {
            settings.v_HUDVersion = cbHUDVersion.SelectedIndex > 0;
        }

        private void cbScoreboard_SelectedIndexChanged(object sender, EventArgs e)
        {
            settings.v_Scoreboard = cbScoreboard.SelectedIndex > 0;
        }

        private void cbDisguiseImage_CheckedChanged(object sender, EventArgs e)
        {
            settings.v_DisguiseImage = cbDisguiseImage.Checked;
        }

        private void cbDefaultMenuBG_CheckedChanged(object sender, EventArgs e)
        {
            settings.v_DefaultMenuBG = cbDefaultMenuBG.Checked;
        }

        private void cbMenuClassImages_CheckedChanged(object sender, EventArgs e)
        {
            settings.v_MenuClassImages = cbMenuClassImages.Checked;
        }

        private void cbDamageValuePos_CheckedChanged(object sender, EventArgs e)
        {
            settings.v_DamageValuePos = cbDamageValuePos.Checked;
        }

        private void rbChatBox_CheckedChanged(object sender, EventArgs e)
        {
            settings.v_ChatBox = rbChatBoxBottom.Checked;
        }

        private void rbTeamSelect_CheckedChanged(object sender, EventArgs e)
        {
            settings.v_TeamSelect = rbTeamSelectCenter.Checked;
        }

        private void rbUberAnimation_CheckedChanged(object sender, EventArgs e)
        {
            if (rbUberAnimation1.Checked)
                settings.v_UberAnimation = 1;
            else if (rbUberAnimation2.Checked)
                settings.v_UberAnimation = 2;
            else if (rbUberAnimation3.Checked)
                settings.v_UberAnimation = 3;
        }

        private void cbXHairEnabled_CheckedChanged(object sender, EventArgs e)
        {
            settings.v_XHairEnabled = cbXHairEnabled.Checked;
        }

        private void cbXHairOutline_CheckedChanged(object sender, EventArgs e)
        {
            settings.v_XHairOutline = cbXHairOutline.Checked;
        }

        private void cbXHairPulse_CheckedChanged(object sender, EventArgs e)
        {
            settings.v_XHairPulse = cbXHairPulse.Checked;
        }

        private void cbXHairSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            settings.v_XHairSize = Convert.ToInt32(cbXHairSizes.Items[cbXHairSizes.SelectedIndex].ToString());
        }

        private void lbPlayerHealth_SelectedIndexChanged(object sender, EventArgs e)
        {
            settings.v_HealthStyle = lbHealthStyle.SelectedIndex + 1;
        }

        private void lbXHairStyles_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Save the crosshair style to the installer-config file
            settings.v_XHairStyle = lbXHairStyles.SelectedIndex + 1;
            // Reinitialize the crosshair size options
            cbXHairSizes.Items.Clear();
            switch (lbXHairStyles.SelectedItem.ToString())
            {
                default:
                    for (int x = 8; x <= 40; x += 2)
                        cbXHairSizes.Items.Add(x.ToString());
                    break;

                case "KonrWings":
                    for (int x = 16; x <= 40; x += 8)
                        cbXHairSizes.Items.Add(x.ToString());
                    break;

                case "KnuckleCrosses":
                    for (int x = 10; x <= 50; x += 1)
                        cbXHairSizes.Items.Add(x.ToString());
                    break;
            }

            // Update the crosshair settings on the UI
            switch (settings.v_XHairStyle)
            {
                case 1: // BasicCross
                    lblCrosshair.Text = @"2";
                    lblCrosshair.Location = new Point(97, 33);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("26");
                    break;

                case 2: // BasicCrossLarge
                    lblCrosshair.Text = @"2";
                    lblCrosshair.Location = new Point(97, 33);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("32");
                    break;

                case 3: // BasicCrossSmall
                    lblCrosshair.Text = @"2";
                    lblCrosshair.Location = new Point(97, 33);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("18");
                    break;

                case 4: // BasicDot
                    lblCrosshair.Text = @"3";
                    lblCrosshair.Location = new Point(103, 31);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("24");
                    break;

                case 5: // CircleDot
                    lblCrosshair.Text = @"8";
                    lblCrosshair.Location = new Point(103, 31);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("34");
                    break;

                case 6: // OpenCross
                    lblCrosshair.Text = @"i";
                    lblCrosshair.Location = new Point(95, 28);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("24");
                    break;

                case 7: // OpenCrossDot
                    lblCrosshair.Text = @"h";
                    lblCrosshair.Location = new Point(95, 28);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("24");
                    break;

                case 8: // ScatterSpread
                    lblCrosshair.Text = @"0";
                    lblCrosshair.Location = new Point(104, 30);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("32");
                    break;

                case 9: // ThinCircle
                    lblCrosshair.Text = @"9";
                    lblCrosshair.Location = new Point(105, 32);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("34");
                    break;

                case 10: // ThinCross
                    lblCrosshair.Text = @"+";
                    lblCrosshair.Location = new Point(108, 30);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("24");
                    break;

                case 11: // Wings
                    lblCrosshair.Text = @"d";
                    lblCrosshair.Location = new Point(95, 32);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("34");
                    break;

                case 12: // WingsPlus
                    lblCrosshair.Text = @"c";
                    lblCrosshair.Location = new Point(95, 32);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("34");
                    break;

                case 13: // WingsSmall
                    lblCrosshair.Text = @"g";
                    lblCrosshair.Location = new Point(95, 32);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("34");
                    break;

                case 14: // WingsSmallDot
                    lblCrosshair.Text = @"f";
                    lblCrosshair.Location = new Point(95, 32);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("34");
                    break;

                case 15: // XHairCirle
                    lblCrosshair.Text = @"o";
                    lblCrosshair.Location = new Point(97, 32);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("34");
                    break;

                case 16: // KonrWings
                    lblCrosshair.Text = @"i";
                    lblCrosshair.Location = new Point(97, 32);
                    cbXHairSizes.SelectedIndex = cbXHairSizes.Items.IndexOf("24");
                    break;

                default:
                    lblCrosshair.Text = string.Empty;
                    cbXHairSizes.SelectedIndex = 0;
                    break;
            }
        }

        private void btnOpenDirectory_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(TF2Directory))
                Process.Start("explorer.exe", TF2Directory);
        }

        private void btnSetDefault_Click(object sender, EventArgs e)
        {
            UpdateModificationDate();
            SetDefaultHUDSettings();
            settings.Save();
        }

        private void SetDefaultHUDSettings()
        {

            // Set installer controls to default rayshud values
            cbHUDVersion.SelectedIndex = 0;
            cbScoreboard.SelectedIndex = 0;
            rbChatBoxTop.Checked = true;
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
            StatusBarLastModified.Text = $"Last Modified: {settings.v_LastModified}";
        }
    }
}