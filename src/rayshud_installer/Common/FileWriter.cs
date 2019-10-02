using rayshud_installer.Properties;
using System;
using System.IO;
using System.Linq;

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
        public void Crosshair(int size, int? xpos = 0, int? ypos = 0)
        {
            try
            {
                MainWindow.logger.Info("Setting Crosshair...");
                var hudlayout = hudPath + Resources.file_hudlayout;
                var lines = File.ReadAllLines(hudlayout);
                for (var x = 12; x <= 50; x += 19)
                {
                    lines[x] = "\t\t\"visible\"\t\t\"0\"";
                    lines[x + 1] = "\t\t\"enabled\"\t\t\"0\"";
                    lines[x + 7] = lines[x + 7].Replace("Outline", null);
                    File.WriteAllLines(hudlayout, lines);
                }

                if (rayshud.Default.toggle_xhair_enable)
                {
                    var index = 0;
                    var style = "Crosshairs";
                    var outline = (rayshud.Default.toggle_xhair_outline) ? "Outline" : string.Empty;

                    switch (rayshud.Default.val_xhair_style)
                    {
                        default:
                            index = 12;
                            style = "Crosshairs";
                            break;

                        case (int)CrosshairStyles.KonrWings:
                            index = 31;
                            style = "KonrWings";
                            break;

                        case (int)CrosshairStyles.KnuckleCrosses:
                            index = 50;
                            style = "KnucklesCrosses";
                            break;
                    }
                    lines[index] = "\t\t\"visible\"\t\t\"1\"";
                    lines[index + 1] = "\t\t\"enabled\"\t\t\"1\"";
                    lines[index + 7] = $"\t\t\"font\"\t\t\t\"{style}{size}{outline}\"";
                    CrosshairStyle(lines, xpos, ypos);
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
                lines[133] = CommentOutTextLine(lines[133]);
                lines[134] = CommentOutTextLine(lines[134]);
                if (rayshud.Default.toggle_xhair_pulse)
                {
                    lines[133] = lines[133].Replace("//", string.Empty);
                    lines[134] = lines[134].Replace("//", string.Empty);
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
        public void CrosshairStyle(string[] lines, int? xpos, int? ypos)
        {
            try
            {
                MainWindow.logger.Info("Setting Crosshair Style...");
                var style = MainWindow.crosshairs[(CrosshairStyles)rayshud.Default.val_xhair_style].Item3;
                lines[15] = $"\t\t\"xpos\"\t\t\t\"c-{xpos}\"";
                lines[16] = $"\t\t\"ypos\"\t\t\t\"c-{ypos}\"";
                lines[20] = $"\t\t\"labelText\"\t\t\"{style}\"";
                File.WriteAllLines(hudPath + Resources.file_hudlayout, lines);
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
                lines[150] = CommentOutTextLine(lines[150]);
                lines[151] = CommentOutTextLine(lines[151]);
                lines[156] = CommentOutTextLine(lines[156]);
                lines[157] = CommentOutTextLine(lines[157]);

                if (rayshud.Default.toggle_disguise_image)
                {
                    lines[150] = lines[150].Replace("//", string.Empty);
                    lines[151] = lines[151].Replace("//", string.Empty);
                    lines[156] = lines[156].Replace("//", string.Empty);
                    lines[157] = lines[157].Replace("//", string.Empty);
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
                var index = rayshud.Default.val_health_style - 1;
                var lines = File.ReadAllLines(hudplayerhealth);
                lines[0] = CommentOutTextLine(lines[0]);
                lines[1] = CommentOutTextLine(lines[1]);
                lines[2] = CommentOutTextLine(lines[2]);
                if (rayshud.Default.val_health_style > 0)
                    lines[index] = lines[index].Replace("//", string.Empty);
                File.WriteAllLines(hudplayerhealth, lines);
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
                var directory = new DirectoryInfo(hudPath + Resources.dir_console);
                var background_base = hudPath + Resources.file_custom_background + "upward.vtf";
                var background_wide = hudPath + Resources.file_custom_background + "upward_widescreen.vtf";
                var chapterbackgrounds = hudPath + Resources.file_chapterbackgrounds;
                var chapterbackgrounds_temp = hudPath + (Resources.file_chapterbackgrounds.Replace(".txt", ".file"));

                if (rayshud.Default.toggle_stock_backgrounds)
                {
                    foreach (FileInfo file in directory.GetFiles())
                        file.Delete();
                    if (File.Exists(chapterbackgrounds))
                        File.Move(chapterbackgrounds, chapterbackgrounds_temp);
                }
                else
                {
                    if (Directory.GetFiles(directory.ToString()).Count() == 0)
                        CopyBackgroundFiles();
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
                var mainmenuoverride = hudPath + Resources.file_custom_mainmenu;
                var lines = File.ReadAllLines(mainmenuoverride);
                var value = (rayshud.Default.toggle_menu_images) ? "-80" : "9999";
                lines[971] = $"\t\t\"ypos\"\t\t\t\"{value}\"";
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
                var mainmenuoverride = hudPath + Resources.file_mainmenuoverride;
                var index = (rayshud.Default.toggle_classic_menu) ? 0 : 1;
                var lines = File.ReadAllLines(mainmenuoverride);
                lines[0] = CommentOutTextLine(lines[0]);
                lines[1] = CommentOutTextLine(lines[1]);
                lines[index] = lines[index].Replace("//", string.Empty);
                File.WriteAllLines(mainmenuoverride, lines);

                // SET THE CLASSIC BACKGROUND
                if (!rayshud.Default.toggle_stock_backgrounds)
                    CopyBackgroundFiles(rayshud.Default.toggle_classic_menu);
                else
                {
                    var directory = new DirectoryInfo(hudPath + Resources.dir_console);
                    foreach (FileInfo file in directory.GetFiles())
                        file.Delete();
                }
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
                var scoreboard = hudPath + Resources.file_scoreboard;
                var lines = File.ReadAllLines(scoreboard);
                lines[0] = CommentOutTextLine(lines[0]);
                if (rayshud.Default.toggle_min_scoreboard)
                    lines[0] = lines[0].Replace("//", string.Empty);
                File.WriteAllLines(scoreboard, lines);
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

                // CLASS SELECT
                var classselection = hudPath + Resources.file_classselection;
                var lines = File.ReadAllLines(classselection);
                lines[0] = CommentOutTextLine(lines[0]);
                if (rayshud.Default.toggle_center_select)
                    lines[0] = lines[0].Replace("//", string.Empty);
                File.WriteAllLines(classselection, lines);

                // TEAM MENU
                var teammenu = hudPath + Resources.file_teammenu;
                lines = File.ReadAllLines(teammenu);
                lines[0] = CommentOutTextLine(lines[0]);
                if (rayshud.Default.toggle_center_select)
                    lines[0] = lines[0].Replace("//", string.Empty);
                File.WriteAllLines(teammenu, lines);
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
                var index = 72 + rayshud.Default.val_uber_animaton;
                lines[72] = CommentOutTextLine(lines[72]);
                lines[73] = CommentOutTextLine(lines[73]);
                lines[74] = CommentOutTextLine(lines[74]);
                lines[index] = lines[index].Replace("//", string.Empty);
                File.WriteAllLines(hudAnimations, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Uber Animation", Resources.error_writer_uber_animation, ex.Message);
            }
        }

        /// <summary>
        /// Set the weapon viewmodel transparency
        /// </summary>
        /// <remarks>TODO: Add the transparent viewmodels configuration file.</remarks>
        public void TransparentViewmodels()
        {
            try
            {
                MainWindow.logger.Info("Setting Transparent Viewmodels...");
                var hudlayout = hudPath + Resources.file_hudlayout;
                var lines = File.ReadAllLines(hudlayout);
                lines[699] = "\t\t\"visible\"\t\t\t\"0\"";
                lines[700] = "\t\t\"enabled\"\t\t\t\"0\"";
                if (rayshud.Default.toggle_transparent_viewmodels)
                {
                    lines[699] = "\t\t\"visible\"\t\t\t\"1\"";
                    lines[700] = "\t\t\"enabled\"\t\t\t\"1\"";
                }
                File.WriteAllLines(hudlayout, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Transparent Viewmodels", Resources.error_writer_transparent_viewmodel, ex.Message);
            }
        }

        /// <summary>
        /// Set the player model position and orientation
        /// </summary>
        public void PlayerModelPos()
        {
            try
            {
                MainWindow.logger.Info("Setting Player Model Position...");
                var hudplayerclass = hudPath + Resources.file_hudplayerclass;
                var lines = File.ReadAllLines(hudplayerclass);
                lines[0] = CommentOutTextLine(lines[0]);
                if (rayshud.Default.toggle_alt_player_model)
                    lines[0] = lines[0].Replace("//", string.Empty);
                File.WriteAllLines(hudplayerclass, lines);
            }
            catch (Exception ex)
            {
                MainWindow.ShowErrorMessage("Player Model Position", Resources.error_writer_player_model_pos, ex.Message);
            }
        }

        /// <summary>
        /// Copy the background images to the materials/console folder.
        /// </summary>
        public void CopyBackgroundFiles(bool classic = false)
        {
            var directory = new DirectoryInfo(hudPath + Resources.dir_console);
            var background_base = hudPath + Resources.file_custom_background + ((classic) ? "classic.vtf" : "upward.vtf");
            var background_wide = hudPath + Resources.file_custom_background + ((classic) ? "classic_widescreen.vtf" : "upward_widescreen.vtf");

            foreach (FileInfo file in directory.GetFiles())
                file.Delete();
            File.Copy(background_base, directory + "background_upward.vtf");
            File.Copy(background_wide, directory + "background_upward_widescreen.vtf");
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
        private static string RGBConverter(string hex)
        {
            var color = System.Drawing.ColorTranslator.FromHtml(hex);
            return $"{color.R} {color.G} {color.B} {color.A}";
        }
    }
}