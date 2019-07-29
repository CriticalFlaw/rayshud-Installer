using rayshud_installer.Properties;
using System;
using System.Collections.Generic;
using System.IO;

namespace rayshud_installer
{
    public class FileWriter
    {
        private readonly string hudPath = rayshud.Default.hud_directory;
        private readonly string hudAnimations = rayshud.Default.hud_directory + Resources.file_hudanimations;

        /// <summary>
        /// Set the position of the chatbox
        /// </summary>
        public void ChatBoxPos()
        {
            try
            {
                MainWindow.logger.Info("Setting Chatbox Position...");
                var basechat = hudPath + Resources.file_basechat;
                var lines = File.ReadAllLines(basechat);
                lines[9] = $"\t\t\"ypos\"\t\t\t\t\t\"{((rayshud.Default.toggle_chat_bottom) ? 360 : 30)}\"";
                File.WriteAllLines(basechat, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Chatbox Position", Resources.error_writer_chatbox_pos, ex.Message);
            }
        }

        /// <summary>
        /// Set the client scheme colors
        /// </summary>
        public void Colors()
        {
            try
            {
                MainWindow.logger.Info("Setting Colors...");
                var clientscheme_colors = hudPath + Resources.file_clientscheme_colors;
                var lines = File.ReadAllLines(clientscheme_colors);
                // Health and Ammo
                lines[31] = $"\t\t\"Ammo In Clip\"\t\t\t\t\"{RGBConverter(rayshud.Default.color_ammo_clip)}\"";
                lines[32] = $"\t\t\"Ammo In Reserve\"\t\t\t\"{RGBConverter(rayshud.Default.color_ammo_reserve)}\"";
                lines[33] = $"\t\t\"Ammo In Clip Low\"\t\t\t\"{RGBConverter(rayshud.Default.color_ammo_clip_low)}\"";
                lines[34] = $"\t\t\"Ammo In Reserve Low\"\t\t\"{RGBConverter(rayshud.Default.color_ammo_reserve_low)}\"";
                lines[35] = $"\t\t\"Health Normal\"\t\t\t\t\"{RGBConverter(rayshud.Default.color_health_normal)}\"";
                lines[36] = $"\t\t\"Health Buff\"\t\t\t\t\"{RGBConverter(rayshud.Default.color_health_buffed)}\"";
                lines[37] = $"\t\t\"Health Hurt\"\t\t\t\t\"{RGBConverter(rayshud.Default.color_health_low)}\"";
                lines[38] = $"\t\t\"Heal Numbers\"\t\t\t\t\"{RGBConverter(rayshud.Default.color_health_healed)}\"";
                // Crosshair
                lines[45] = $"\t\t\"Crosshair\"\t\t\t\t\t\"{RGBConverter(rayshud.Default.color_xhair_normal)}\"";
                lines[46] = $"\t\t\"CrosshairDamage\"\t\t\t\"{RGBConverter(rayshud.Default.color_xhair_pulse)}\"";
                // Ubercharge
                lines[49] = $"\t\t\"Uber Bar Color\"\t\t\t\"{RGBConverter(rayshud.Default.color_uber_bar)}\"";
                lines[50] = $"\t\t\"Solid Color Uber\"\t\t\t\"{RGBConverter(rayshud.Default.color_uber_full)}\"";
                lines[51] = $"\t\t\"Flashing Uber Color1\"\t\t\"{RGBConverter(rayshud.Default.color_uber_flash1)}\"";
                lines[52] = $"\t\t\"Flashing Uber Color2\"\t\t\"{RGBConverter(rayshud.Default.color_uber_flash2)}\"";
                File.WriteAllLines(clientscheme_colors, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Setting Colors", Resources.error_writer_colors, ex.Message);
            }
        }

        /// <summary>
        /// Set the crosshair
        /// </summary>
        public void Crosshair(string xhairSize, int xpos = 0, int ypos = 0)
        {
            try
            {
                MainWindow.logger.Info("Setting Crosshair...");
                var hudlayout = hudPath + Resources.file_hudlayout;
                var lines = File.ReadAllLines(hudlayout);
                var size = int.Parse(xhairSize);
                for (var x = 12; x <= 50; x += 19)
                {
                    lines[x] = "\t\t\"visible\"\t\t\"0\"";
                    lines[x + 1] = "\t\t\"enabled\"\t\t\"0\"";
                    lines[x + 7] = lines[x + 7].Replace("Outline", null);
                    File.WriteAllLines(hudlayout, lines);
                }

                var index = 0;
                if (rayshud.Default.toggle_xhair_enable)
                {
                    switch (rayshud.Default.val_xhair_style)
                    {
                        default:
                            index = 12;
                            lines[index + 7] = (rayshud.Default.toggle_xhair_outline) ? $"\t\t\"font\"\t\t\t\"Crosshairs{size}Outline\"" : $"\t\t\"font\"\t\t\t\"Crosshairs{size}\"";
                            break;
                        case (int)CrosshairStyles.KonrWings:
                            index = 31;
                            lines[index + 7] = (rayshud.Default.toggle_xhair_outline) ? $"\t\t\"font\"\t\t\t\"KonrWings{size}Outline\"" : $"\t\t\"font\"\t\t\t\"KonrWings{size}\"";
                            break;
                        case (int)CrosshairStyles.KnuckleCrosses:
                            index = 50;
                            lines[index + 7] = (rayshud.Default.toggle_xhair_outline) ? $"\t\t\"font\"\t\t\t\"KnucklesCrosses{size}Outline\"" : $"\t\t\"font\"\t\t\t\"KnucklesCrosses{size}\"";
                            break;
                    }
                    lines[index] = "\t\t\"visible\"\t\t\"1\"";
                    lines[index + 1] = "\t\t\"enabled\"\t\t\"1\"";
                    CrosshairStyle(hudlayout, lines, xpos, ypos);
                    File.WriteAllLines(hudlayout, lines);
                }
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Setting Crosshair", Resources.error_writer_xhair, ex.Message);
            }
        }

        /// <summary>
        /// Set the crosshair hitmarker
        /// </summary>
        public void CrosshairPulse()
        {
            try
            {
                MainWindow.logger.Info("Setting Crosshair Pulse...");
                var lines = File.ReadAllLines(hudAnimations);
                var index1 = 133;
                var index2 = 134;
                lines[index1] = lines[index1].Replace("//", string.Empty);
                lines[index2] = lines[index2].Replace("//", string.Empty);
                if (!rayshud.Default.toggle_xhair_pulse)
                {
                    lines[index1] = string.Concat("//", lines[index1]);
                    lines[index2] = string.Concat("//", lines[index2]);
                }
                File.WriteAllLines(hudAnimations, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Crosshair Pulse", Resources.error_writer_xhair_pulse, ex.Message);
            }
        }

        /// <summary>
        /// Set the rayshud crosshair position and style
        /// </summary>
        public void CrosshairStyle(string hudlayout, string[] lines, int x, int y)
        {
            try
            {
                MainWindow.logger.Info("Setting Crosshair Style...");
                var xhairs = new Dictionary<CrosshairStyles, Tuple<int, int, string>>
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
                    { CrosshairStyles.xHairCircle, new Tuple<int, int, string>(100, 102, "0") }
                };
                var values = xhairs[(CrosshairStyles)rayshud.Default.val_xhair_style];
                var xpos = (x > 0) ? x : values.Item1;
                var ypos = (y > 0) ? y : values.Item2;

                lines[15] = $"\t\t\"xpos\"\t\t\t\"c-{xpos}\"";
                lines[16] = $"\t\t\"ypos\"\t\t\t\"c-{ypos}\"";
                lines[20] = $"\t\t\"labelText\"\t\t\"{values.Item3}\"";
                File.WriteAllLines(hudlayout, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Setting Crosshair", Resources.error_writer_xhair, ex.Message);
            }
        }

        /// <summary>
        /// Set the position of the damage value
        /// </summary>
        public void DamagePos()
        {
            try
            {
                MainWindow.logger.Info("Setting Damage Value Position...");
                var huddamageaccount = hudPath + Resources.file_huddamageaccount;
                var lines = File.ReadAllLines(huddamageaccount);
                lines[20] = $"\t\t\"xpos\"\t\t\t\t\"c-{((rayshud.Default.toggle_damage_pos) ? 108 : 188)}\"";
                File.WriteAllLines(huddamageaccount, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Damage Position", Resources.error_writer_damage_pos, ex.Message);
            }
        }

        /// <summary>
        /// Set the visibility of the Spy's disguise image
        /// </summary>
        public void DisguiseImage()
        {
            try
            {
                MainWindow.logger.Info("Setting Disguise Image...");
                var lines = File.ReadAllLines(hudAnimations);
                var index1 = 150;
                var index2 = 151;
                var index3 = 156;
                var index4 = 157;

                lines[index1] = lines[index1].Replace("//", string.Empty);
                lines[index2] = lines[index2].Replace("//", string.Empty);
                lines[index3] = lines[index3].Replace("//", string.Empty);
                lines[index4] = lines[index4].Replace("//", string.Empty);

                if (!rayshud.Default.toggle_disguise_image)
                {
                    lines[index1] = string.Concat("//", lines[index1]);
                    lines[index2] = string.Concat("//", lines[index2]);
                    lines[index3] = string.Concat("//", lines[index3]);
                    lines[index4] = string.Concat("//", lines[index4]);
                }
                File.WriteAllLines(hudAnimations, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Disguise Image", Resources.error_writer_disguise_image, ex.Message);
            }
        }

        /// <summary>
        /// Set the player health style
        /// </summary>
        public void HealthStyle()
        {
            try
            {
                MainWindow.logger.Info("Setting Player Health Style...");
                var hudplayerhealth = hudPath + Resources.file_hudplayerhealth;
                var hudplayerhealth_custom = hudPath + Resources.file_custom_hudplayerhealth;
                switch (rayshud.Default.val_health_style)
                {
                    case (int)HealthStyles.Default:
                        File.Copy(hudplayerhealth_custom + "-default.res", hudplayerhealth, true);
                        break;

                    case (int)HealthStyles.Teambar:
                        File.Copy(hudplayerhealth_custom + "-teambar.res", hudplayerhealth, true);
                        break;

                    case (int)HealthStyles.Cross:
                        File.Copy(hudplayerhealth_custom + "-cross.res", hudplayerhealth, true);
                        break;

                    case (int)HealthStyles.Broesel:
                        File.Copy(hudplayerhealth_custom + "-broesel.res", hudplayerhealth, true);
                        break;
                }
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Health Style", Resources.error_writer_health_style, ex.Message);
            }
        }

        /// <summary>
        /// Set the main menu backgrounds
        /// </summary>
        public void MainMenuBackground()
        {
            try
            {
                MainWindow.logger.Info("Setting Custom Backgrounds...");
                var console = hudPath + Resources.dir_console;
                var console_temp = hudPath + Resources.dir_temp + "\\console";
                var chapterbackgrounds = hudPath + Resources.file_chapterbackgrounds;
                var chapterbackgrounds_temp = hudPath + Resources.dir_temp + "\\chapterbackgrounds.txt";

                if (!Directory.Exists(hudPath + Resources.dir_temp))
                    Directory.CreateDirectory(hudPath + Resources.dir_temp);

                if (rayshud.Default.toggle_stock_backgrounds)
                {
                    if (Directory.Exists(console))
                        Directory.Move(console, console_temp);
                    if (File.Exists(chapterbackgrounds))
                        File.Move(chapterbackgrounds, chapterbackgrounds_temp);
                }
                else
                {
                    if (Directory.Exists(console_temp))
                        Directory.Move(console_temp, console);
                    if (File.Exists(chapterbackgrounds_temp))
                        File.Move(chapterbackgrounds_temp, chapterbackgrounds);
                }
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Main Menu Backgrounds", Resources.error_writer_backgrounds, ex.Message);
            }
        }

        /// <summary>
        /// Set the visibility of the main menu class image
        /// </summary>
        public void MainMenuClassImage()
        {
            try
            {
                MainWindow.logger.Info("Setting Main Menu Class Image...");
                var mainmenuoverride = hudPath + Resources.file_mainmenuoverride;
                var lines = File.ReadAllLines(mainmenuoverride);
                var index = (rayshud.Default.toggle_classic_menu) ? 933 : 971;
                var value = (rayshud.Default.toggle_menu_images) ? "-80" : "9999";
                lines[index] = $"\t\t\"ypos\"\t\t\t\"{value}\"";
                File.WriteAllLines(mainmenuoverride, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Class Image", Resources.error_writer_class_image, ex.Message);
            }
        }

        /// <summary>
        /// Set the main menu style
        /// </summary>
        public void MainMenuStyle()
        {
            try
            {
                MainWindow.logger.Info("Setting Main Menu Style...");
                var menu_stock = hudPath + Resources.dir_menu_modern;
                var menu_classic = hudPath + Resources.dir_menu_classic;
                CopyMenuFiles((rayshud.Default.toggle_classic_menu) ? menu_classic : menu_stock, (rayshud.Default.toggle_stock_backgrounds) ? true : false);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Main Menu Style", Resources.error_writer_main_menu, ex.Message);
            }
        }

        /// <summary>
        /// Set the scoreboard style
        /// </summary>
        public void ScoreboardStyle()
        {
            try
            {
                MainWindow.logger.Info("Setting Scoreboard Style...");
                var scoreboard_stock = hudPath + Resources.file_scoreboard;
                var scoreboard_custom = hudPath + Resources.file_custom_scoreboard + ((rayshud.Default.toggle_min_scoreboard) ? "-minimal.res" : "-default.res");
                File.Copy(scoreboard_custom, scoreboard_stock, true);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Scoreboard Style", Resources.error_writer_scoreboard, ex.Message);
            }
        }

        /// <summary>
        /// Set the team and class selection style
        /// </summary>
        public void TeamSelect()
        {
            try
            {
                MainWindow.logger.Info("Setting Team Selection...");
                var teammenu = hudPath + Resources.file_teammenu;
                var teammenu_custom = hudPath + Resources.file_custom_teammenu;
                var classselection = hudPath + Resources.file_classselection;
                var classselection_custom = hudPath + Resources.file_custom_classselection;

                File.Copy(teammenu_custom + ((rayshud.Default.toggle_center_select) ? "-center.res" : "-left.res"), teammenu, true);
                File.Copy(classselection_custom + ((rayshud.Default.toggle_center_select) ? "-center.res" : "-left.res"), classselection, true);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Team Select Position", Resources.error_writer_team_select, ex.Message);
            }
        }

        /// <summary>
        /// Set the ubercharge style
        /// </summary>
        public void UberchargeStyle()
        {
            try
            {
                MainWindow.logger.Info("Setting Ubercharge Animation...");
                var lines = File.ReadAllLines(hudAnimations);
                var index1 = 72;
                var index2 = 73;
                var index3 = 74;

                lines[index1] = lines[index1].Replace("//", string.Empty);
                lines[index2] = lines[index2].Replace("//", string.Empty);
                lines[index3] = lines[index3].Replace("//", string.Empty);

                lines[index1] = string.Concat("//", lines[index1]);
                lines[index2] = string.Concat("//", lines[index2]);
                lines[index3] = string.Concat("//", lines[index3]);

                lines[index1 + rayshud.Default.val_uber_animaton] = lines[index1 + rayshud.Default.val_uber_animaton].Replace("//", string.Empty);
                File.WriteAllLines(hudAnimations, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Uber Animation", Resources.error_writer_uber_animation, ex.Message);
            }
        }

        /// <summary>
        /// Copies the the selected main menu style into rayshud
        /// </summary>
        public void CopyMenuFiles(string source, bool defaultBG)
        {
            File.Copy(source + Resources.file_mainmenuoverride.Replace("\\rayshud", null), hudPath + Resources.file_mainmenuoverride, true);
            File.Copy(source + Resources.file_gamemenu.Replace("\\rayshud", null), hudPath + Resources.file_gamemenu, true);
            if (!defaultBG)
            {
                File.Copy(source + Resources.dir_console.Replace("\\rayshud", null) + "\\background_upward.vtf", hudPath + Resources.dir_console + "\\background_upward.vtf", true);
                File.Copy(source + Resources.dir_console.Replace("\\rayshud", null) + "\\background_upward_widescreen.vtf", hudPath + Resources.dir_console + "\\background_upward_widescreen.vtf", true);
            }
        }

        /// <summary>
        /// Convert color HEX code to RGB
        /// </summary>
        private static string RGBConverter(string hex)
        {
            var color = System.Drawing.ColorTranslator.FromHtml(hex);
            return $"{color.R} {color.G} {color.B} {color.A}";
        }
    }
}