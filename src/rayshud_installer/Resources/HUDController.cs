using rayshud_installer.Properties;
using System;
using System.IO;
using System.Linq;
using System.Windows.Controls;

namespace rayshud_installer
{
    public class HUDController
    {
        private readonly string hudPath = Settings.Default.hud_directory;
        private readonly string appPath = System.Windows.Forms.Application.StartupPath;

        /// <summary>
        /// Set the position of the chatbox
        /// </summary>
        public void ChatBoxPos()
        {
            try
            {
                MainWindow.logger.Info("Updating Chatbox Position.");
                var file = hudPath + Resources.file_basechat;
                var lines = File.ReadAllLines(file);
                var start = FindIndex(lines, "HudChat");
                lines[FindIndex(lines, "ypos", start)] = $"\t\t\"ypos\"\t\t\t\t\t\"{((Settings.Default.toggle_chat_bottom) ? 360 : 25)}\"";
                File.WriteAllLines(file, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Updating Chatbox Position.", Resources.error_set_chatbox_pos, ex.Message);
            }
        }

        /// <summary>
        /// Set the client scheme colors
        /// </summary>
        public void Colors()
        {
            try
            {
                MainWindow.logger.Info("Updating Colors.");
                var file = hudPath + Resources.file_clientscheme_colors;
                var lines = File.ReadAllLines(file);
                // Health
                lines[FindIndex(lines, "\"Health Normal\"")] = $"\t\t\"Health Normal\"\t\t\t\t\"{RGBConverter(Settings.Default.color_health_normal)}\"";
                lines[FindIndex(lines, "\"Health Buff\"")] = $"\t\t\"Health Buff\"\t\t\t\t\"{RGBConverter(Settings.Default.color_health_buffed)}\"";
                lines[FindIndex(lines, "\"Health Hurt\"")] = $"\t\t\"Health Hurt\"\t\t\t\t\"{RGBConverter(Settings.Default.color_health_low)}\"";
                lines[FindIndex(lines, "\"Heal Numbers\"")] = $"\t\t\"Heal Numbers\"\t\t\t\t\"{RGBConverter(Settings.Default.color_health_healed)}\"";
                // Ammo
                lines[FindIndex(lines, "\"Ammo In Clip\"")] = $"\t\t\"Ammo In Clip\"\t\t\t\t\"{RGBConverter(Settings.Default.color_ammo_clip)}\"";
                lines[FindIndex(lines, "\"Ammo In Reserve\"")] = $"\t\t\"Ammo In Reserve\"\t\t\t\"{RGBConverter(Settings.Default.color_ammo_reserve)}\"";
                lines[FindIndex(lines, "\"Ammo In Clip Low\"")] = $"\t\t\"Ammo In Clip Low\"\t\t\t\"{RGBConverter(Settings.Default.color_ammo_clip_low)}\"";
                lines[FindIndex(lines, "\"Ammo In Reserve Low\"")] = $"\t\t\"Ammo In Reserve Low\"\t\t\"{RGBConverter(Settings.Default.color_ammo_reserve_low)}\"";
                // Crosshair
                lines[FindIndex(lines, "\"Crosshair\"")] = $"\t\t\"Crosshair\"\t\t\t\t\t\"{RGBConverter(Settings.Default.color_xhair_normal)}\"";
                lines[FindIndex(lines, "\"CrosshairDamage\"")] = $"\t\t\"CrosshairDamage\"\t\t\t\"{RGBConverter(Settings.Default.color_xhair_pulse)}\"";
                // ÜberCharge
                lines[FindIndex(lines, "\"Uber Bar Color\"")] = $"\t\t\"Uber Bar Color\"\t\t\t\"{RGBConverter(Settings.Default.color_uber_bar)}\"";
                lines[FindIndex(lines, "\"Solid Color Uber\"")] = $"\t\t\"Solid Color Uber\"\t\t\t\"{RGBConverter(Settings.Default.color_uber_full)}\"";
                lines[FindIndex(lines, "\"Flashing Uber Color1\"")] = $"\t\t\"Flashing Uber Color1\"\t\t\"{RGBConverter(Settings.Default.color_uber_flash1)}\"";
                lines[FindIndex(lines, "\"Flashing Uber Color2\"")] = $"\t\t\"Flashing Uber Color2\"\t\t\"{RGBConverter(Settings.Default.color_uber_flash2)}\"";
                File.WriteAllLines(file, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Updating Colors.", Resources.error_set_colors, ex.Message);
            }
        }

        /// <summary>
        /// Set the crosshair
        /// </summary>
        public void Crosshair(int? xpos, int? ypos, ComboBoxItem sizeSelection)
        {
            try
            {
                MainWindow.logger.Info("Updating Crosshair.");
                var file = hudPath + Resources.file_hudlayout;
                var lines = File.ReadAllLines(file);
                var start = FindIndex(lines, "\"RaysCrosshair\"");
                var xHairList = Enum.GetValues(typeof(CrosshairStyles));
                foreach (var xHair in xHairList)
                {
                    start = FindIndex(lines, $"\"{xHair}\"");
                    lines[FindIndex(lines, "visible", start)] = "\t\t\"visible\"\t\t\"0\"";
                    lines[FindIndex(lines, "enabled", start)] = "\t\t\"enabled\"\t\t\"0\"";
                    lines[FindIndex(lines, "visible", start)] = lines[FindIndex(lines, "visible", start)].Replace("Outline", null);
                }
                File.WriteAllLines(file, lines);

                if (Settings.Default.toggle_xhair_enable)
                {
                    var index = Settings.Default.val_xhair_style switch
                    {
                        (int)CrosshairStyles.BasicCross => "BasicCross",
                        (int)CrosshairStyles.BasicDot => "BasicDot",
                        (int)CrosshairStyles.CircleDot => "CircleDot",
                        (int)CrosshairStyles.OpenCross => "OpenCross",
                        (int)CrosshairStyles.OpenCrossDot => "OpenCrossDot",
                        (int)CrosshairStyles.ScatterSpread => "ScatterSpread",
                        (int)CrosshairStyles.ThinCircle => "ThinCircle",
                        (int)CrosshairStyles.Wings => "Wings",
                        (int)CrosshairStyles.WingsPlus => "WingsPlus",
                        (int)CrosshairStyles.WingsSmall => "WingsSmall",
                        (int)CrosshairStyles.WingsSmallDot => "WingsSmallDot",
                        (int)CrosshairStyles.KonrWings => "KonrWings",
                        (int)CrosshairStyles.KnucklesCrosses => "KnucklesCrosses",
                        _ => "RaysCrosshair",
                    };
                    var style = Settings.Default.val_xhair_style switch
                    {
                        (int)CrosshairStyles.KonrWings => "KonrWings",
                        (int)CrosshairStyles.KnucklesCrosses => "KnucklesCrosses",
                        _ => "Crosshairs",
                    };
                    var size = (string)sizeSelection.Content;
                    var outline = (Settings.Default.toggle_xhair_outline) ? "Outline" : string.Empty;

                    start = FindIndex(lines, $"\"{index}\"");
                    lines[FindIndex(lines, "visible", start)] = "\t\t\"visible\"\t\t\"1\"";
                    lines[FindIndex(lines, "enabled", start)] = "\t\t\"enabled\"\t\t\"1\"";
                    lines[FindIndex(lines, "xpos", start)] = $"\t\t\"xpos\"\t\t\t\"c-{xpos}\"";
                    lines[FindIndex(lines, "ypos", start)] = $"\t\t\"ypos\"\t\t\t\"c-{ypos}\"";
                    lines[FindIndex(lines, "font", start)] = $"\t\t\"font\"\t\t\t\"{style}{size}{outline}\"";
                    File.WriteAllLines(file, lines);
                }
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Updating Crosshair.", Resources.error_set_xhair, ex.Message);
            }
        }

        /// <summary>
        /// Set the crosshair hitmarker
        /// </summary>
        public void CrosshairPulse()
        {
            try
            {
                MainWindow.logger.Info("Updating Crosshair Pulse.");
                var file = hudPath + Resources.file_hudanimations;
                var lines = File.ReadAllLines(file);
                var start = FindIndex(lines, "DamagedPlayer");
                var index1 = FindIndex(lines, "StopEvent", start);
                var index2 = FindIndex(lines, "RunEvent", start);
                lines[index1] = CommentOutTextLine(lines[index1]);
                lines[index2] = CommentOutTextLine(lines[index2]);

                if (Settings.Default.toggle_xhair_pulse)
                {
                    lines[index1] = lines[index1].Replace("//", string.Empty);
                    lines[index2] = lines[index2].Replace("//", string.Empty);
                }
                File.WriteAllLines(file, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Updating Crosshair Pulse.", Resources.error_set_xhair_pulse, ex.Message);
            }
        }

        /// <summary>
        /// Set the position of the damage value
        /// </summary>
        public void DamagePos()
        {
            try
            {
                MainWindow.logger.Info("Updating Damage Value Position.");
                var huddamageaccount = hudPath + Resources.file_huddamageaccount;
                var lines = File.ReadAllLines(huddamageaccount);
                var start = FindIndex(lines, "DamageAccountValue");
                var value = (Settings.Default.toggle_damage_pos) ? "c108" : "c-188";
                lines[FindIndex(lines, "xpos", start)] = $"\t\t\"xpos\"\t\t\t\t\"{value}\"";
                File.WriteAllLines(huddamageaccount, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Updating Damage Value Position.", Resources.error_set_damage_pos, ex.Message);
            }
        }

        /// <summary>
        /// Set the visibility of the Spy's disguise image
        /// </summary>
        public void DisguiseImage()
        {
            try
            {
                MainWindow.logger.Info("Updating Spy Disguise Image.");
                var file = hudPath + Resources.file_hudanimations;
                var lines = File.ReadAllLines(file);
                var start = FindIndex(lines, "HudSpyDisguiseFadeIn");
                var index1 = FindIndex(lines, "RunEvent", start);
                var index2 = FindIndex(lines, "Animate", start);
                start = FindIndex(lines, "HudSpyDisguiseFadeOut");
                var index3 = FindIndex(lines, "RunEvent", start);
                var index4 = FindIndex(lines, "Animate", start);
                lines[index1] = CommentOutTextLine(lines[index1]);
                lines[index2] = CommentOutTextLine(lines[index2]);
                lines[index3] = CommentOutTextLine(lines[index3]);
                lines[index4] = CommentOutTextLine(lines[index4]);

                if (Settings.Default.toggle_disguise_image)
                {
                    lines[index1] = lines[index1].Replace("//", string.Empty);
                    lines[index2] = lines[index2].Replace("//", string.Empty);
                    lines[index3] = lines[index3].Replace("//", string.Empty);
                    lines[index4] = lines[index4].Replace("//", string.Empty);
                }
                File.WriteAllLines(file, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Updating Spy Disguise Image.", Resources.error_set_spy_disguise_image, ex.Message);
            }
        }

        /// <summary>
        /// Set the player health style
        /// </summary>
        public void HealthStyle()
        {
            try
            {
                MainWindow.logger.Info("Updating Player Health Style.");
                var file = hudPath + Resources.file_hudplayerhealth;
                var lines = File.ReadAllLines(file);
                var index = Settings.Default.val_health_style - 1;
                lines[0] = CommentOutTextLine(lines[0]);
                lines[1] = CommentOutTextLine(lines[1]);
                if (Settings.Default.val_health_style > 0)
                    lines[index] = lines[index].Replace("//", string.Empty);
                File.WriteAllLines(file, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Updating Player Health Style.", Resources.error_set_health_style, ex.Message);
            }
        }

        /// <summary>
        /// Set the main menu backgrounds
        /// </summary>
        public void MainMenuBackground()
        {
            try
            {
                MainWindow.logger.Info("Updating Main Menu Backgrounds.");
                var directory = new DirectoryInfo(hudPath + Resources.dir_console);
                var chapterbackgrounds = hudPath + Resources.file_chapterbackgrounds;
                var chapterbackgrounds_temp = chapterbackgrounds.Replace(".txt", ".file");

                if (Settings.Default.toggle_stock_backgrounds)
                {
                    foreach (var file in directory.GetFiles())
                        File.Move(file.FullName, file.FullName.Replace("upward", "off"));
                    if (File.Exists(chapterbackgrounds))
                        File.Move(chapterbackgrounds, chapterbackgrounds_temp);
                }
                else
                {
                    foreach (var file in directory.GetFiles())
                        File.Move(file.FullName, file.FullName.Replace("off", "upward"));
                    if (File.Exists(chapterbackgrounds_temp))
                        File.Move(chapterbackgrounds_temp, chapterbackgrounds);
                }
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Updating Main Menu Backgrounds.", Resources.error_set_menu_backgrounds, ex.Message);
            }
        }

        /// <summary>
        /// Set the visibility of the main menu class image
        /// </summary>
        public void MainMenuClassImage()
        {
            try
            {
                MainWindow.logger.Info("Updating Main Menu Class Image.");
                var file = hudPath + ((Settings.Default.toggle_classic_menu) ? Resources.file_custom_mainmenu_classic : Resources.file_custom_mainmenu);
                var lines = File.ReadAllLines(file);
                var start = FindIndex(lines, "TFCharacterImage");
                var value = (Settings.Default.toggle_menu_images) ? "-80" : "9999";
                lines[FindIndex(lines, "ypos", start)] = $"\t\t\"ypos\"\t\t\t\"{value}\"";
                File.WriteAllLines(file, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Updating Main Menu Class Image.", Resources.error_set_menu_class_image, ex.Message);
            }
        }

        /// <summary>
        /// Set the weapon viewmodel transparency
        /// </summary>
        public void TransparentViewmodels()
        {
            try
            {
                MainWindow.logger.Info("Updating Transparent Viewmodels.");
                var file = hudPath + Resources.file_hudlayout;
                var lines = File.ReadAllLines(file);
                var start = FindIndex(lines, "\"TransparentViewmodel\"");
                var index1 = FindIndex(lines, "visible", start);
                var index2 = FindIndex(lines, "enabled", start);
                lines[index1] = "\t\t\"visible\"\t\t\t\"0\"";
                lines[index2] = "\t\t\"enabled\"\t\t\t\"0\"";
                if (File.Exists(hudPath + Resources.file_cfg))
                    File.Delete(hudPath + Resources.file_cfg);

                if (Settings.Default.toggle_transparent_viewmodels)
                {
                    lines[index1] = "\t\t\"visible\"\t\t\t\"1\"";
                    lines[index2] = "\t\t\"enabled\"\t\t\t\"1\"";
                    File.Copy(appPath + "\\hud.cfg", hudPath + Resources.file_cfg);
                }
                File.WriteAllLines(file, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Updating Transparent Viewmodels.", Resources.error_set_transparent_viewmodels, ex.Message);
            }
        }

        /// <summary>
        /// Set the main menu style
        /// </summary>
        /// <remarks>Copy the correct background files</remarks>
        public void MainMenuStyle()
        {
            try
            {
                MainWindow.logger.Info("Updating Main Menu Style.");
                var file = hudPath + Resources.file_mainmenuoverride;
                var lines = File.ReadAllLines(file);
                var index = (Settings.Default.toggle_classic_menu) ? 1 : 2;
                lines[1] = CommentOutTextLine(lines[1]);
                lines[2] = CommentOutTextLine(lines[2]);
                lines[index] = lines[index].Replace("//", string.Empty);
                File.WriteAllLines(file, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Updating Main Menu Style.", Resources.error_set_main_menu, ex.Message);
            }
        }

        /// <summary>
        /// Set the scoreboard style
        /// </summary>
        public void ScoreboardStyle()
        {
            try
            {
                MainWindow.logger.Info("Updating Scoreboard Style.");
                var file = hudPath + Resources.file_scoreboard;
                var lines = File.ReadAllLines(file);
                lines[0] = CommentOutTextLine(lines[0]);
                if (Settings.Default.toggle_min_scoreboard)
                    lines[0] = lines[0].Replace("//", string.Empty);
                File.WriteAllLines(file, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Updating Scoreboard Style.", Resources.error_set_scoreboard, ex.Message);
            }
        }

        /// <summary>
        /// Set the team and class selection style
        /// </summary>
        public void TeamSelect()
        {
            try
            {
                MainWindow.logger.Info("Updating Team Selection.");

                // CLASS SELECT
                var file = hudPath + Resources.file_classselection;
                var lines = File.ReadAllLines(file);
                lines[0] = CommentOutTextLine(lines[0]);
                if (Settings.Default.toggle_center_select)
                    lines[0] = lines[0].Replace("//", string.Empty);
                File.WriteAllLines(file, lines);

                // TEAM MENU
                file = hudPath + Resources.file_teammenu;
                lines = File.ReadAllLines(file);
                lines[0] = CommentOutTextLine(lines[0]);
                if (Settings.Default.toggle_center_select)
                    lines[0] = lines[0].Replace("//", string.Empty);
                File.WriteAllLines(file, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Updating Team Selection.", Resources.error_set_team_select, ex.Message);
            }
        }

        /// <summary>
        /// Set the ÜberCharge style
        /// </summary>
        public void UberchargeStyle()
        {
            try
            {
                MainWindow.logger.Info("Updating ÜberCharge Animation.");
                var file = hudPath + Resources.file_hudanimations;
                var lines = File.ReadAllLines(file);
                var start = FindIndex(lines, "HudMedicCharged");
                var index1 = FindIndex(lines, "HudMedicOrangePulseCharge", start);
                var index2 = FindIndex(lines, "HudMedicSolidColorCharge", start);
                var index3 = FindIndex(lines, "HudMedicRainbowCharged", start);
                lines[index1] = CommentOutTextLine(lines[index1]);
                lines[index2] = CommentOutTextLine(lines[index2]);
                lines[index3] = CommentOutTextLine(lines[index3]);
                var index = 1;
                index = Settings.Default.val_uber_animaton switch
                {
                    2 => index2,
                    3 => index3,
                    _ => index1,
                };
                lines[index] = lines[index].Replace("//", string.Empty);
                File.WriteAllLines(file, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Updating ÜberCharge Animation.", Resources.error_set_uber_animation, ex.Message);
            }
        }

        /// <summary>
        /// Set the player model position and orientation
        /// </summary>
        public void PlayerModelPos()
        {
            try
            {
                MainWindow.logger.Info("Updating Player Model Position.");
                var file = hudPath + Resources.file_hudplayerclass;
                var lines = File.ReadAllLines(file);
                lines[0] = CommentOutTextLine(lines[0]);
                if (Settings.Default.toggle_alt_player_model)
                    lines[0] = lines[0].Replace("//", string.Empty);
                File.WriteAllLines(file, lines);
                file = hudPath + Resources.file_hudlayout;
                lines = File.ReadAllLines(file);
                var start = FindIndex(lines, "DisguiseStatus");
                var value = (Settings.Default.toggle_alt_player_model) ? "100" : "20";
                lines[FindIndex(lines, "xpos", start)] = $"\t\t\"xpos\"\t\t\t\t\t\"{value}\"";
                File.WriteAllLines(file, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Updating Player Model Position.", Resources.error_set_player_model_pos, ex.Message);
            }
        }

        /// <summary>
        /// Retrieves the index of where a given value was found in a string array.
        /// </summary>
        public int FindIndex(string[] array, string value, int skip = 0)
        {
            var list = array.Skip(skip);
            var index = list.Select((v, i) => new { Index = i, Value = v }) // Pair up values and indexes
                .Where(p => p.Value.Contains(value)) // Do the filtering
                .Select(p => p.Index); // Keep the index and drop the value
            return index.First() + skip;
        }

        /// <summary>
        /// Clear all existing comment identifiers, then apply a fresh one.
        /// </summary>
        public string CommentOutTextLine(string value)
        {
            value = value.Replace("//", string.Empty);
            return string.Concat("//", value);
        }

        /// <summary>
        /// Convert color HEX code to RGB
        /// </summary>
        /// <param name="hex">The HEX code representing the color to convert to RGB</param>
        /// <param name="pulse">Flag the color as a pulse, slightly lowering the alpha</param>
        private static string RGBConverter(string hex, bool alpha = false, bool pulse = false)
        {
            var color = System.Drawing.ColorTranslator.FromHtml(hex);
            var alpha_new = (alpha == true) ? "200" : color.A.ToString();
            var pulse_new = (pulse == true && color.G >= 50) ? color.G - 50 : color.G;
            return $"{color.R} {pulse_new} {color.B} {alpha_new}";
        }
    }
}