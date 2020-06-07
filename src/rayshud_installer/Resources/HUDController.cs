using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using rayshud_installer.Properties;

namespace rayshud_installer
{
    public class HUDController
    {
        private readonly string _appPath = Application.StartupPath;
        private readonly string _hudPath = Settings.Default.hud_directory;

        /// <summary>
        ///     Set the position of the chatbox
        /// </summary>
        public void ChatBoxPos()
        {
            try
            {
                MainWindow.Logger.Info("Updating Chatbox Position.");
                var file = _hudPath + Resources.file_basechat;
                var lines = File.ReadAllLines(file);
                var start = FindIndex(lines, "HudChat");
                lines[FindIndex(lines, "ypos", start)] =
                    $"\t\t\"ypos\"\t\t\t\t\t\"{(Settings.Default.toggle_chat_bottom ? 315 : 25)}\"";
                File.WriteAllLines(file, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Updating Chatbox Position.", Resources.error_set_chatbox_pos, ex.Message);
            }
        }

        /// <summary>
        ///     Set the client scheme colors
        /// </summary>
        public void Colors()
        {
            try
            {
                MainWindow.Logger.Info("Updating Colors.");
                var file = _hudPath + Resources.file_clientscheme_colors;
                var lines = File.ReadAllLines(file);
                // Health
                lines[FindIndex(lines, "\"Health Normal\"")] =
                    $"\t\t\"Health Normal\"\t\t\t\t\"{RgbConverter(Settings.Default.color_health_normal)}\"";
                lines[FindIndex(lines, "\"Health Buff\"")] =
                    $"\t\t\"Health Buff\"\t\t\t\t\"{RgbConverter(Settings.Default.color_health_buffed)}\"";
                lines[FindIndex(lines, "\"Health Hurt\"")] =
                    $"\t\t\"Health Hurt\"\t\t\t\t\"{RgbConverter(Settings.Default.color_health_low)}\"";
                lines[FindIndex(lines, "\"Heal Numbers\"")] =
                    $"\t\t\"Heal Numbers\"\t\t\t\t\"{RgbConverter(Settings.Default.color_health_healed)}\"";
                // Ammo
                lines[FindIndex(lines, "\"Ammo In Clip\"")] =
                    $"\t\t\"Ammo In Clip\"\t\t\t\t\"{RgbConverter(Settings.Default.color_ammo_clip)}\"";
                lines[FindIndex(lines, "\"Ammo In Reserve\"")] =
                    $"\t\t\"Ammo In Reserve\"\t\t\t\"{RgbConverter(Settings.Default.color_ammo_reserve)}\"";
                lines[FindIndex(lines, "\"Ammo In Clip Low\"")] =
                    $"\t\t\"Ammo In Clip Low\"\t\t\t\"{RgbConverter(Settings.Default.color_ammo_clip_low)}\"";
                lines[FindIndex(lines, "\"Ammo In Reserve Low\"")] =
                    $"\t\t\"Ammo In Reserve Low\"\t\t\"{RgbConverter(Settings.Default.color_ammo_reserve_low)}\"";
                // Crosshair
                lines[FindIndex(lines, "\"Crosshair\"")] =
                    $"\t\t\"Crosshair\"\t\t\t\t\t\"{RgbConverter(Settings.Default.color_xhair_normal)}\"";
                lines[FindIndex(lines, "\"CrosshairDamage\"")] =
                    $"\t\t\"CrosshairDamage\"\t\t\t\"{RgbConverter(Settings.Default.color_xhair_pulse)}\"";
                // ÜberCharge
                lines[FindIndex(lines, "\"Uber Bar Color\"")] =
                    $"\t\t\"Uber Bar Color\"\t\t\t\"{RgbConverter(Settings.Default.color_uber_bar)}\"";
                lines[FindIndex(lines, "\"Solid Color Uber\"")] =
                    $"\t\t\"Solid Color Uber\"\t\t\t\"{RgbConverter(Settings.Default.color_uber_full)}\"";
                lines[FindIndex(lines, "\"Flashing Uber Color1\"")] =
                    $"\t\t\"Flashing Uber Color1\"\t\t\"{RgbConverter(Settings.Default.color_uber_flash1)}\"";
                lines[FindIndex(lines, "\"Flashing Uber Color2\"")] =
                    $"\t\t\"Flashing Uber Color2\"\t\t\"{RgbConverter(Settings.Default.color_uber_flash2)}\"";
                File.WriteAllLines(file, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Updating Colors.", Resources.error_set_colors, ex.Message);
            }
        }

        /// <summary>
        ///     Set the crosshair
        /// </summary>
        public void Crosshair(string style, int? size, bool isKnuckles = false)
        {
            try
            {
                MainWindow.Logger.Info("Updating Crosshair.");
                var file = _hudPath + Resources.file_hudlayout;
                var lines = File.ReadAllLines(file);
                var start = FindIndex(lines, "\"RaysCrosshair\"");
                lines[FindIndex(lines, "visible", start)] = "\t\t\"visible\"\t\t\"0\"";
                lines[FindIndex(lines, "enabled", start)] = "\t\t\"enabled\"\t\t\"0\"";
                start = FindIndex(lines, "\"KnucklesCrosses\"");
                lines[FindIndex(lines, "visible", start)] = "\t\t\"visible\"\t\t\"0\"";
                lines[FindIndex(lines, "enabled", start)] = "\t\t\"enabled\"\t\t\"0\"";
                File.WriteAllLines(file, lines);

                if (!Settings.Default.toggle_xhair_enable) return;
                start = FindIndex(lines, $"\"{(isKnuckles ? "KnucklesCrosses" : "RaysCrosshair")}\"");
                var type = isKnuckles ? "KnucklesCrosses" : "Crosshairs";
                var outline = Settings.Default.toggle_xhair_outline ? "Outline" : string.Empty;

                lines[FindIndex(lines, "visible", start)] = "\t\t\"visible\"\t\t\"1\"";
                lines[FindIndex(lines, "enabled", start)] = "\t\t\"enabled\"\t\t\"1\"";
                lines[FindIndex(lines, "xpos", start)] = $"\t\t\"xpos\"\t\t\t\"c-{Settings.Default.val_xhair_x}\"";
                lines[FindIndex(lines, "ypos", start)] = $"\t\t\"ypos\"\t\t\t\"c-{Settings.Default.val_xhair_y}\"";
                lines[FindIndex(lines, "font", start)] = $"\t\t\"font\"\t\t\t\"{type}{size}{outline}\"";
                File.WriteAllLines(file, lines);
                lines[FindIndex(lines, "labelText", start)] = $"\t\t\"labelText\"\t\t\"{style}\"";
                File.WriteAllLines(file, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Updating Crosshair.", Resources.error_set_xhair, ex.Message);
            }
        }

        /// <summary>
        ///     Set the crosshair hitmarker
        /// </summary>
        public void CrosshairPulse()
        {
            try
            {
                MainWindow.Logger.Info("Updating Crosshair Pulse.");
                var file = _hudPath + Resources.file_hudanimations;
                var lines = File.ReadAllLines(file);
                var start = FindIndex(lines, "DamagedPlayer");
                var index1 = FindIndex(lines, "StopEvent", start);
                var index2 = FindIndex(lines, "RunEvent", start);
                lines[index1] = CommentOutTextLine(lines[index1]);
                lines[index2] = CommentOutTextLine(lines[index2]);
                File.WriteAllLines(file, lines);

                if (!Settings.Default.toggle_xhair_pulse) return;
                lines[index1] = lines[index1].Replace("//", string.Empty);
                lines[index2] = lines[index2].Replace("//", string.Empty);
                File.WriteAllLines(file, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Updating Crosshair Pulse.", Resources.error_set_xhair_pulse, ex.Message);
            }
        }

        /// <summary>
        ///     Set the position of the damage value
        /// </summary>
        public void DamagePos()
        {
            try
            {
                MainWindow.Logger.Info("Updating Damage Value Position.");
                var file = _hudPath + Resources.file_huddamageaccount;
                var lines = File.ReadAllLines(file);
                var start = FindIndex(lines, "DamageAccountValue");
                var value = Settings.Default.toggle_damage_pos ? "c-188" : "c108";
                lines[FindIndex(lines, "\"xpos\"", start)] = $"\t\t\"xpos\"\t\t\t\t\"{value}\"";
                value = Settings.Default.toggle_damage_pos ? "c-138" : "c58";
                lines[FindIndex(lines, "\"xpos_minmode\"", start)] = $"\t\t\"xpos_minmode\"\t\t\"{value}\"";
                File.WriteAllLines(file, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Updating Damage Value Position.", Resources.error_set_damage_pos,
                    ex.Message);
            }
        }

        /// <summary>
        ///     Set the visibility of the Spy's disguise image
        /// </summary>
        public void DisguiseImage()
        {
            try
            {
                MainWindow.Logger.Info("Updating Spy Disguise Image.");
                var file = _hudPath + Resources.file_hudanimations;
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
                File.WriteAllLines(file, lines);

                if (!Settings.Default.toggle_disguise_image) return;
                lines[index1] = lines[index1].Replace("//", string.Empty);
                lines[index2] = lines[index2].Replace("//", string.Empty);
                lines[index3] = lines[index3].Replace("//", string.Empty);
                lines[index4] = lines[index4].Replace("//", string.Empty);
                File.WriteAllLines(file, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Updating Spy Disguise Image.", Resources.error_set_spy_disguise_image,
                    ex.Message);
            }
        }

        /// <summary>
        ///     Set the player health style
        /// </summary>
        public void HealthStyle()
        {
            try
            {
                MainWindow.Logger.Info("Updating Player Health Style.");
                var file = _hudPath + Resources.file_hudplayerhealth;
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
                MainWindow.ShowErrorMessage("Updating Player Health Style.", Resources.error_set_health_style,
                    ex.Message);
            }
        }

        /// <summary>
        ///     Set the main menu backgrounds
        /// </summary>
        public void MainMenuBackground()
        {
            try
            {
                MainWindow.Logger.Info("Updating Main Menu Backgrounds.");
                var directory = new DirectoryInfo(_hudPath + Resources.dir_console);
                var chapterbackgrounds = _hudPath + Resources.file_chapterbackgrounds;
                var chapterbackgroundsTemp = chapterbackgrounds.Replace(".txt", ".file");

                if (Settings.Default.toggle_stock_backgrounds)
                {
                    foreach (var file in directory.GetFiles())
                        File.Move(file.FullName, file.FullName.Replace("upward", "off"));
                    if (File.Exists(chapterbackgrounds))
                        File.Move(chapterbackgrounds, chapterbackgroundsTemp);
                }
                else
                {
                    foreach (var file in directory.GetFiles())
                        File.Move(file.FullName, file.FullName.Replace("off", "upward"));
                    if (File.Exists(chapterbackgroundsTemp))
                        File.Move(chapterbackgroundsTemp, chapterbackgrounds);
                }
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Updating Main Menu Backgrounds.", Resources.error_set_menu_backgrounds,
                    ex.Message);
            }
        }

        /// <summary>
        ///     Set the visibility of the main menu class image
        /// </summary>
        public void MainMenuClassImage()
        {
            try
            {
                MainWindow.Logger.Info("Updating Main Menu Class Image.");
                var file = _hudPath + (Settings.Default.toggle_classic_menu
                    ? Resources.file_custom_mainmenu_classic
                    : Resources.file_custom_mainmenu);
                var lines = File.ReadAllLines(file);
                var start = FindIndex(lines, "TFCharacterImage");
                var value = Settings.Default.toggle_menu_images ? "-80" : "9999";
                lines[FindIndex(lines, "ypos", start)] = $"\t\t\"ypos\"\t\t\t\"{value}\"";
                File.WriteAllLines(file, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Updating Main Menu Class Image.", Resources.error_set_menu_class_image,
                    ex.Message);
            }
        }

        /// <summary>
        ///     Set the weapon viewmodel transparency
        /// </summary>
        public void TransparentViewmodels()
        {
            try
            {
                MainWindow.Logger.Info("Updating Transparent Viewmodels.");
                var file = _hudPath + Resources.file_hudlayout;
                var lines = File.ReadAllLines(file);
                var start = FindIndex(lines, "\"TransparentViewmodel\"");
                var index1 = FindIndex(lines, "visible", start);
                var index2 = FindIndex(lines, "enabled", start);
                lines[index1] = "\t\t\"visible\"\t\t\t\"0\"";
                lines[index2] = "\t\t\"enabled\"\t\t\t\"0\"";
                if (File.Exists(_hudPath + Resources.file_cfg))
                    File.Delete(_hudPath + Resources.file_cfg);
                File.WriteAllLines(file, lines);

                if (!Settings.Default.toggle_transparent_viewmodels) return;
                lines[index1] = "\t\t\"visible\"\t\t\t\"1\"";
                lines[index2] = "\t\t\"enabled\"\t\t\t\"1\"";
                File.Copy(_appPath + "\\hud.cfg", _hudPath + Resources.file_cfg);
                File.WriteAllLines(file, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Updating Transparent Viewmodels.",
                    Resources.error_set_transparent_viewmodels, ex.Message);
            }
        }

        /// <summary>
        ///     Set the main menu style
        /// </summary>
        /// <remarks>Copy the correct background files</remarks>
        public void MainMenuStyle()
        {
            try
            {
                MainWindow.Logger.Info("Updating Main Menu Style.");
                var file = _hudPath + Resources.file_mainmenuoverride;
                var lines = File.ReadAllLines(file);
                var index = Settings.Default.toggle_classic_menu ? 1 : 2;
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
        ///     Set the scoreboard style
        /// </summary>
        public void ScoreboardStyle()
        {
            try
            {
                MainWindow.Logger.Info("Updating Scoreboard Style.");
                var file = _hudPath + Resources.file_scoreboard;
                var lines = File.ReadAllLines(file);
                lines[0] = Settings.Default.toggle_min_scoreboard
                    ? lines[0].Replace("//", string.Empty)
                    : CommentOutTextLine(lines[0]);
                File.WriteAllLines(file, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Updating Scoreboard Style.", Resources.error_set_scoreboard, ex.Message);
            }
        }

        /// <summary>
        ///     Set the team and class selection style
        /// </summary>
        public void TeamSelect()
        {
            try
            {
                MainWindow.Logger.Info("Updating Team Selection.");

                // CLASS SELECT
                var file = _hudPath + Resources.file_classselection;
                var lines = File.ReadAllLines(file);
                lines[0] = Settings.Default.toggle_center_select
                    ? lines[0].Replace("//", string.Empty)
                    : CommentOutTextLine(lines[0]);
                File.WriteAllLines(file, lines);

                // TEAM MENU
                file = _hudPath + Resources.file_teammenu;
                lines = File.ReadAllLines(file);
                lines[0] = Settings.Default.toggle_center_select
                    ? lines[0].Replace("//", string.Empty)
                    : CommentOutTextLine(lines[0]);
                File.WriteAllLines(file, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Updating Team Selection.", Resources.error_set_team_select, ex.Message);
            }
        }

        /// <summary>
        ///     Set the ÜberCharge style
        /// </summary>
        public void UberchargeStyle()
        {
            try
            {
                MainWindow.Logger.Info("Updating ÜberCharge Animation.");
                var file = _hudPath + Resources.file_hudanimations;
                var lines = File.ReadAllLines(file);
                var start = FindIndex(lines, "HudMedicCharged");
                var index1 = FindIndex(lines, "HudMedicOrangePulseCharge", start);
                var index2 = FindIndex(lines, "HudMedicSolidColorCharge", start);
                var index3 = FindIndex(lines, "HudMedicRainbowCharged", start);
                lines[index1] = CommentOutTextLine(lines[index1]);
                lines[index2] = CommentOutTextLine(lines[index2]);
                lines[index3] = CommentOutTextLine(lines[index3]);
                var index = Settings.Default.val_uber_animation switch
                {
                    2 => index2,
                    3 => index3,
                    _ => index1
                };
                lines[index] = lines[index].Replace("//", string.Empty);
                File.WriteAllLines(file, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Updating ÜberCharge Animation.", Resources.error_set_uber_animation,
                    ex.Message);
            }
        }

        /// <summary>
        ///     Set the player model position and orientation
        /// </summary>
        public void PlayerModelPos()
        {
            try
            {
                MainWindow.Logger.Info("Updating Player Model Position.");
                var file = _hudPath + Resources.file_hudplayerclass;
                var lines = File.ReadAllLines(file);
                lines[0] = Settings.Default.toggle_alt_player_model
                    ? lines[0].Replace("//", string.Empty)
                    : CommentOutTextLine(lines[0]);
                File.WriteAllLines(file, lines);

                file = _hudPath + Resources.file_hudlayout;
                lines = File.ReadAllLines(file);
                var start = FindIndex(lines, "DisguiseStatus");
                lines[FindIndex(lines, "xpos", start)] =
                    $"\t\t\"xpos\"\t\t\t\t\t\"{(Settings.Default.toggle_alt_player_model ? 100 : 20)}\"";
                File.WriteAllLines(file, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Updating Player Model Position.", Resources.error_set_player_model_pos,
                    ex.Message);
            }
        }

        /// <summary>
        ///     Retrieves the index of where a given value was found in a string array.
        /// </summary>
        public int FindIndex(string[] array, string value, int skip = 0)
        {
            var list = array.Skip(skip);
            var index = list.Select((v, i) => new {Index = i, Value = v}) // Pair up values and indexes
                .Where(p => p.Value.Contains(value)) // Do the filtering
                .Select(p => p.Index); // Keep the index and drop the value
            return index.First() + skip;
        }

        /// <summary>
        ///     Clear all existing comment identifiers, then apply a fresh one.
        /// </summary>
        public string CommentOutTextLine(string value)
        {
            value = value.Replace("//", string.Empty);
            return string.Concat("//", value);
        }

        /// <summary>
        ///     Convert color HEX code to RGB
        /// </summary>
        /// <param name="hex">The HEX code representing the color to convert to RGB</param>
        /// <param name="alpha">Flag the code as having a lower alpha value than normal</param>
        /// <param name="pulse">Flag the color as a pulse, slightly lowering the alpha</param>
        private static string RgbConverter(string hex, bool alpha = false, bool pulse = false)
        {
            var color = ColorTranslator.FromHtml(hex);
            var alphaNew = alpha ? "200" : color.A.ToString();
            var pulseNew = pulse && color.G >= 50 ? color.G - 50 : color.G;
            return $"{color.R} {pulseNew} {color.B} {alphaNew}";
        }
    }
}