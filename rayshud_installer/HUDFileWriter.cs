using System;
using System.IO;
using System.Windows.Forms;

namespace rayshud_installer
{
    public class HUDFileWriter
    {
        private Properties.Settings settings = new Properties.Settings();
        private string directory = new Properties.Settings().app_directory;
        private string console = "\\materials\\console";
        private string resource = "\\resource\\ui";
        private string scripts = "\\scripts";
        private string animations = "\\scripts\\hudanimations_custom.txt";

        public void mainMenuStyle()
        {
            try
            {
                var consoleDir = directory + console;
                var resourceDir = directory + resource;
                var classicMaterials = $"{directory}\\customizations\\Main Menu\\Classic\\materials\\console";
                var classicResources = $"{directory}\\customizations\\Main Menu\\Classic\\resource\\";
                var modernMaterials = $"{directory}\\customizations\\Main Menu\\Modern\\materials\\console";
                var modernResources = $"{directory}\\customizations\\Main Menu\\Modern\\resource\\";
                // Modern, Default Background
                if (settings.v_ClassicHUD && Directory.Exists($"{consoleDir}_off"))
                {
                    File.Copy($"{classicMaterials}\\background_upward.vtf", $"{consoleDir}_off\\background_upward.vtf", true);
                    File.Copy($"{classicMaterials}\\background_upward_widescreen.vtf", $"{consoleDir}_off\\background_upward_widescreen.vtf", true);
                    File.Copy($"{classicResources}\\ui\\mainmenuoverride.res", $"{resourceDir}\\mainmenuoverride.res", true);
                    File.Copy($"{classicResources}\\gamemenu.res", $"{directory}\\resource\\gamemenu.res", true);
                }
                // Modern, rayshud Background
                else if (settings.v_ClassicHUD && !Directory.Exists($"{consoleDir}_off"))
                {
                    File.Copy($"{classicMaterials}\\background_upward.vtf", $"{consoleDir}\\background_upward.vtf", true);
                    File.Copy($"{classicMaterials}\\background_upward_widescreen.vtf", $"{consoleDir}\\background_upward_widescreen.vtf", true);
                    File.Copy($"{classicResources}\\ui\\mainmenuoverride.res", $"{resourceDir}\\mainmenuoverride.res", true);
                    File.Copy($"{classicResources}\\gamemenu.res", $"{directory}\\resource\\gamemenu.res", true);
                }
                // Classic, Default Backgrounds
                else if (!settings.v_ClassicHUD && Directory.Exists($"{consoleDir}_off"))
                {
                    File.Copy($"{modernMaterials}\\background_upward.vtf", $"{consoleDir}_off\\background_upward.vtf", true);
                    File.Copy($"{modernMaterials}\\background_upward_widescreen.vtf", $"{consoleDir}_off\\background_upward_widescreen.vtf", true);
                    File.Copy($"{modernResources}\\ui\\mainmenuoverride.res", $"{resourceDir}\\mainmenuoverride.res", true);
                    File.Copy($"{modernResources}\\gamemenu.res", $"{directory}\\resource\\gamemenu.res", true);
                }
                // Classic, rayshud Backgrounds
                else if (!settings.v_ClassicHUD && !Directory.Exists($"{consoleDir}_off"))
                {
                    File.Copy($"{modernMaterials}\\background_upward.vtf", $"{consoleDir}\\background_upward.vtf", true);
                    File.Copy($"{modernMaterials}\\background_upward_widescreen.vtf", $"{consoleDir}\\background_upward_widescreen.vtf", true);
                    File.Copy($"{modernResources}\\ui\\mainmenuoverride.res", $"{resourceDir}\\mainmenuoverride.res", true);
                    File.Copy($"{modernResources}\\gamemenu.res", $"{directory}\\resource\\gamemenu.res", true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{settings.error_writer_main_menu}\n{ex.Message}", "Error: Main Menu Style", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void scoreboardStyle()
        {
            try
            {
                var resourceDir = directory + resource;
                var scoreboard = $"{directory}\\customizations\\Scoreboard";
                if (settings.v_Scoreboard)
                    File.Copy($"{scoreboard}\\scoreboard-minimal.res", $"{resourceDir}\\scoreboard.res", true);
                else
                    File.Copy($"{scoreboard}\\scoreboard-default.res", $"{resourceDir}\\scoreboard.res", true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{settings.error_writer_scoreboard}\n{ex.Message}", "Error: Scoreboard Style", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void defaultBackgrounds()
        {
            try
            {
                var consoleDir = directory + console;
                var scriptsDir = directory + scripts;
                if (settings.v_DefaultBG)
                {
                    if (Directory.Exists(consoleDir) && File.Exists($"{scriptsDir}\\chapterbackgrounds.txt"))
                    {
                        Directory.Move(consoleDir, $"{consoleDir}_off");
                        File.Move($"{scriptsDir}\\chapterbackgrounds.txt", $"{scriptsDir}\\chapterbackgrounds_off.txt");
                    }
                }
                else
                {
                    if (Directory.Exists($"{consoleDir}_off") && File.Exists($"{scriptsDir}\\chapterbackgrounds_off.txt"))
                    {
                        Directory.Move($"{consoleDir}_off", consoleDir);
                        File.Move($"{scriptsDir}\\chapterbackgrounds_off.txt", $"{scriptsDir}\\chapterbackgrounds.txt");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{settings.error_writer_backgrounds}\n{ex.Message}", "Error: Main Menu Backgrounds", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void teamSelect()
        {
            try
            {
                var resourceDir = directory + resource;
                var teammenuDir = directory + "\\customizations\\Team Menu";
                if (settings.v_TeamCenter)
                {
                    File.Copy($"{teammenuDir}\\TeamMenu-center.res", $"{resourceDir}\\TeamMenu.res", true);
                    File.Copy($"{teammenuDir}\\ClassSelection-center.res", $"{resourceDir}\\ClassSelection.res", true);
                }
                else
                {
                    File.Copy($"{teammenuDir}\\TeamMenu-left.res", $"{resourceDir}\\TeamMenu.res", true);
                    File.Copy($"{teammenuDir}\\ClassSelection-left.res", $"{resourceDir}\\ClassSelection.res", true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{settings.error_writer_team_select}\n{ex.Message}", "Error: Team Select Position", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void healthStyle()
        {
            try
            {
                var resourceDir = directory + resource;
                var fileName = resourceDir + "\\HudPlayerHealth.res";
                var playerhealth = $"{directory}\\customizations\\Player Health";
                switch (settings.v_HealthStyle)
                {
                    default:
                        File.Copy($"{playerhealth}\\HudPlayerHealth-default.res", fileName, true);
                        break;

                    case 2:
                        File.Copy($"{playerhealth}\\HudPlayerHealth-teambar.res", fileName, true);
                        break;

                    case 3:
                        File.Copy($"{playerhealth}\\HudPlayerHealth-cross.res", fileName, true);
                        break;

                    case 4:
                        File.Copy($"{playerhealth}\\HudPlayerHealth-broesel.res", fileName, true);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{settings.error_writer_health_style}\n{ex.Message}", "Error: Health Style", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void disguiseImage()
        {
            try
            {
                var animationsDir = directory + animations;
                var lines = File.ReadAllLines(animationsDir);
                var disguiseImageIndex = 91;
                lines[disguiseImageIndex + 0] = $"\t{lines[disguiseImageIndex + 0].Replace("//", string.Empty).Trim()}";
                lines[disguiseImageIndex + 1] = $"\t{lines[disguiseImageIndex + 1].Replace("//", string.Empty).Trim()}";
                lines[disguiseImageIndex + 2] = $"\t{lines[disguiseImageIndex + 2].Replace("//", string.Empty).Trim()}";
                lines[disguiseImageIndex + 7] = $"\t{lines[disguiseImageIndex + 7].Replace("//", string.Empty).Trim()}";
                lines[disguiseImageIndex + 8] = $"\t{lines[disguiseImageIndex + 8].Replace("//", string.Empty).Trim()}";
                lines[disguiseImageIndex + 9] = $"\t{lines[disguiseImageIndex + 9].Replace("//", string.Empty).Trim()}";

                if (!settings.v_DisguiseImage)
                {
                    lines[disguiseImageIndex + 0] = $"\t//{lines[disguiseImageIndex + 0].Trim()}";
                    lines[disguiseImageIndex + 1] = $"\t//{lines[disguiseImageIndex + 1].Trim()}";
                    lines[disguiseImageIndex + 2] = $"\t//{lines[disguiseImageIndex + 2].Trim()}";
                    lines[disguiseImageIndex + 7] = $"\t//{lines[disguiseImageIndex + 7].Trim()}";
                    lines[disguiseImageIndex + 8] = $"\t//{lines[disguiseImageIndex + 8].Trim()}";
                    lines[disguiseImageIndex + 9] = $"\t//{lines[disguiseImageIndex + 9].Trim()}";
                }
                File.WriteAllLines(animationsDir, lines);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{settings.error_writer_disguise_image}\n{ex.Message}", "Error: Disguise Image", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void uberAnimation()
        {
            try
            {
                var animationsDir = directory + animations;
                var lines = File.ReadAllLines(animationsDir);
                var uberAnimationIndex = 107;
                lines[uberAnimationIndex + 0] = $"\t{lines[uberAnimationIndex + 0].Replace("//", string.Empty).Trim()}";
                lines[uberAnimationIndex + 1] = $"\t{lines[uberAnimationIndex + 1].Replace("//", string.Empty).Trim()}";
                lines[uberAnimationIndex + 2] = $"\t{lines[uberAnimationIndex + 2].Replace("//", string.Empty).Trim()}";

                switch (settings.v_UberAnimation)
                {
                    default:
                        lines[uberAnimationIndex + 1] = $"\t//{lines[uberAnimationIndex + 1].Trim()}";
                        lines[uberAnimationIndex + 2] = $"\t//{lines[uberAnimationIndex + 2].Trim()}";
                        break;

                    case 2:
                        lines[uberAnimationIndex + 0] = $"\t//{lines[uberAnimationIndex + 0].Trim()}";
                        lines[uberAnimationIndex + 2] = $"\t//{lines[uberAnimationIndex + 2].Trim()}";
                        break;

                    case 3:
                        lines[uberAnimationIndex + 0] = $"\t//{lines[uberAnimationIndex + 0].Trim()}";
                        lines[uberAnimationIndex + 1] = $"\t//{lines[uberAnimationIndex + 1].Trim()}";
                        break;
                }
                File.WriteAllLines(animationsDir, lines);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{settings.error_writer_uber_animation}\n{ex.Message}", "Error: Uber Animation", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void crosshairPulse()
        {
            try
            {
                var animationsDir = directory + animations;
                var lines = File.ReadAllLines(animationsDir);
                var crosshairPulseIndex = 80;
                lines[crosshairPulseIndex + 0] = lines[crosshairPulseIndex + 0].Replace("//", string.Empty);
                lines[crosshairPulseIndex + 1] = lines[crosshairPulseIndex + 1].Replace("//", string.Empty);
                lines[crosshairPulseIndex + 2] = lines[crosshairPulseIndex + 2].Replace("//", string.Empty);
                lines[crosshairPulseIndex + 3] = lines[crosshairPulseIndex + 3].Replace("//", string.Empty);
                lines[crosshairPulseIndex + 4] = lines[crosshairPulseIndex + 4].Replace("//", string.Empty);
                lines[crosshairPulseIndex + 5] = lines[crosshairPulseIndex + 5].Replace("//", string.Empty);

                if (!settings.v_XHairPulse)
                {
                    lines[crosshairPulseIndex + 0] = $"\t//{lines[crosshairPulseIndex + 0]}";
                    lines[crosshairPulseIndex + 1] = $"\t//{lines[crosshairPulseIndex + 1]}";
                    lines[crosshairPulseIndex + 2] = $"\t//{lines[crosshairPulseIndex + 2]}";
                    lines[crosshairPulseIndex + 3] = $"\t//{lines[crosshairPulseIndex + 3]}";
                    lines[crosshairPulseIndex + 4] = $"\t//{lines[crosshairPulseIndex + 4]}";
                    lines[crosshairPulseIndex + 5] = $"\t//{lines[crosshairPulseIndex + 5]}";
                }
                File.WriteAllLines(animationsDir, lines);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{settings.error_writer_xhair_pulse}\n{ex.Message}", "Error: Crosshair Pulse", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void classImage()
        {
            try
            {
                var mainmenu = directory + "\\resource\\ui\\mainmenuoverride.res";
                var lines = File.ReadAllLines(mainmenu);
                var index = 247;
                if (settings.v_ClassicHUD)
                    index = 240;
                if (settings.v_ClassImage)
                {
                    lines[index + 0] = "\t\t\"xpos\"\t\t\"c-250\"";
                    lines[index + 1] = "\t\t\"ypos\"\t\t\"-80\"";
                }
                else
                {
                    lines[index + 0] = "\t\t\"xpos\"\t\t\"9999\"";
                    lines[index + 1] = "\t\t\"ypos\"\t\t\"9999\"";
                }
                File.WriteAllLines(mainmenu, lines);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{settings.error_writer_class_image}\n{ex.Message}", "Error: Class Image", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void chatboxPos()
        {
            try
            {
                var chatDir = directory + "\\resource\\ui\\basechat.res";
                var lines = File.ReadAllLines(chatDir);
                var value = 30;
                if (settings.v_ChatBottom)
                    value = 360;
                lines[9] = $"\t\t\"ypos\"\t\t\t\t\"{value}\"";
                File.WriteAllLines(chatDir, lines);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{settings.error_writer_chatbox_pos}\n{ex.Message}", "Error: Chatbox Position", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void crosshair(int xhairSize)
        {
            try
            {
                // TODO: Correct the index usage
                var layoutDir = directory + "\\scripts\\hudlayout.res";
                var lines = File.ReadAllLines(layoutDir);
                for (int x = 13; x <= 51; x += 19)
                {
                    lines[x - 1] = "\t\t\"visible\"\t\t\"0\"";
                    lines[(x + 1) - 1] = "\t\t\"enabled\"\t\t\"0\"";
                    lines[(x + 7) - 1] = lines[x + 7 - 1].Replace("Outline", string.Empty);
                    File.WriteAllLines(layoutDir, lines);
                }

                // 11. Crosshairs - either enabled or disabled with or without outlines, change the visible, enabled and font values of hudlayout.res
                if (settings.v_XHairEnable)
                {
                    if (settings.v_XHairStyle >= 1 && settings.v_XHairStyle <= 15)
                    {
                        lines[13 - 1] = "\t\t\"visible\"\t\t\"1\"";
                        lines[14 - 1] = "\t\t\"enabled\"\t\t\"1\"";
                        if (settings.v_XHairOutline)
                            lines[20 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{xhairSize}Outline\"";
                        else
                            lines[20 - 1] = $"\t\t\"font\"\t\t\t\"Crosshairs{xhairSize}\"";
                    }
                    else if (settings.v_XHairStyle == 16)
                    {
                        lines[32 - 1] = "\t\t\"visible\"\t\t\"1\"";
                        lines[33 - 1] = "\t\t\"enabled\"\t\t\"1\"";
                        if (settings.v_XHairOutline)
                            lines[39 - 1] = $"\t\t\"font\"\t\t\t\"KonrWings{xhairSize}Outline\"";
                        else
                            lines[39 - 1] = $"\t\t\"font\"\t\t\t\"KonrWings{xhairSize}\"";
                    }
                    else if (settings.v_XHairStyle >= 17 && settings.v_XHairStyle <= 84)
                    {
                        lines[51 - 1] = "\t\t\"visible\"\t\t\"1\"";
                        lines[52 - 1] = "\t\t\"enabled\"\t\t\"1\"";
                        if (settings.v_XHairOutline)
                            lines[58 - 1] = $"\t\t\"font\"\t\t\t\"size:{xhairSize},outline:on\"";
                        else
                            lines[58 - 1] = $"\t\t\"font\"\t\t\t\"size:{xhairSize},outline:off\"";
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
                    File.WriteAllLines(layoutDir, lines);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{settings.error_writer_xhair}\n{ex.Message}", "Error: Setting Crosshair", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void colors()
        {
            try
            {
                // TODO: Reposition the color values in rayshud to be closer together
                var colorScheme = directory + "\\resource\\scheme\\clientscheme_colors.res";
                var lines = File.ReadAllLines(colorScheme);
                lines[7 - 1] = $"\t\t\"Ammo In Clip\"\t\t\t\t\t\"{RGBConverter(settings.v_AmmoClip)}\"";
                lines[8 - 1] = $"\t\t\"Ammo In Reserve\"\t\t\t\t\"{RGBConverter(settings.v_AmmoReserve)}\"";
                lines[9 - 1] = $"\t\t\"Ammo In Clip Low\"\t\t\t\t\"{RGBConverter(settings.v_AmmoClipLow)}\"";
                lines[10 - 1] = $"\t\t\"Ammo In Reserve Low\"\t\t\t\"{RGBConverter(settings.v_AmmoReserveLow)}\"";
                lines[23 - 1] = $"\t\t\"Health Normal\"\t\t\t\t\t\"{RGBConverter(settings.v_HealthNormal)}\"";
                lines[24 - 1] = $"\t\t\"Health Buff\"\t\t\t\t\t\"{RGBConverter(settings.v_HealthBuff)}\"";
                lines[25 - 1] = $"\t\t\"Health Hurt\"\t\t\t\t\t\"{RGBConverter(settings.v_HealthLow)}\"";
                lines[32 - 1] = $"\t\t\"Uber Bar Color\"\t\t\t\t\"{RGBConverter(settings.v_UberBarColor)}\"";
                lines[35 - 1] = $"\t\t\"Solid Color Uber\"\t\t\t\t\"{RGBConverter(settings.v_UberFullColor)}\"";
                lines[37 - 1] = $"\t\t\"Flashing Uber Color1\"\t\t\t\"{RGBConverter(settings.v_UberFlash1)}\"";
                lines[38 - 1] = $"\t\t\"Flashing Uber Color2\"\t\t\t\"{RGBConverter(settings.v_UberFlash2)}\"";
                lines[41 - 1] = $"\t\t\"Heal Numbers\"\t\t\t\t\t\"{RGBConverter(settings.v_HealingDone)}\"";
                lines[45 - 1] = $"\t\t\"Crosshair\"\t\t\t\t\t\t\"{RGBConverter(settings.v_XHairBaseColor)}\"";
                lines[46 - 1] = $"\t\t\"CrosshairDamage\"\t\t\t\t\"{RGBConverter(settings.v_XHairPulseColor)}\"";
                File.WriteAllLines(colorScheme, lines);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{settings.error_writer_colors}\n{ex.Message}", "Error: Setting Colors", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void damagePos()
        {
            try
            {
                var damageDir = directory + "\\resource\\ui\\huddamageaccount.res";
                var lines = File.ReadAllLines(damageDir);
                var value = 188;
                if (settings.v_DamagePos)
                    value = 108;
                lines[21] = $"\t\t\"xpos\"\t\t\t\"c-{value}\"";
                File.WriteAllLines(damageDir, lines);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{settings.error_writer_damage_pos}\n{ex.Message}", "Error: Damage Position", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string RGBConverter(string hex)
        {
            var color = System.Drawing.ColorTranslator.FromHtml(hex);
            return $"{color.R} {color.G} {color.B} {color.A}";
        }
    }
}