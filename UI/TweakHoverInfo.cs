using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using WOTBO.App;
using WOTBO.Services.Registry;
using WOTBO.Services.State;

namespace WOTBO.UI
{
    /// <summary>
    /// Подсказка при наведении на применённый твик:
    /// путь, старое/новое значение и время применения.
    ///
    /// Работает и для выключенных чекбоксов: событие берётся с любого
    /// контрола, а цель определяется рекурсивным hit-test'ом по координатам.
    /// </summary>
    public static class TweakHoverInfo
    {
        private static readonly Dictionary<string, string> ControlToTweak =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Применение
                { "checkBox_disabledefender", "main.defender" },
                { "checkBox_reg", "main.regpack" },
                { "checkBox_dism", "main.reservedstorage" },
                { "checkBox_gibernate", "main.hibernate" },
                { "checkBox_scheme", "main.powerscheme" },
                { "checkBox_mpo", "main.mpo" },
                { "checkBox_usb", "main.usb" },
                { "checkBox_mtiititit", "main.mitigations" },
                { "checkBox_mmagent", "main.mmagent" },
                { "checkBox_page", "main.pagefile" },
                { "checkBox_mousefix", "main.mousefix" },
                { "checkBox_audio", "main.audio" },
                { "checkBox_dwninput", "main.dwminput" },
                { "checkBox_tolerate", "main.powerlatency" },
                { "checkBox_pro_11", "pro.win32priority" },
                { "checkBox_ansel", "gpu.ansel" },
                { "checkBox_hdcp", "gpu.hdcp" },
                { "checkBox_dopNVIDIA_tweaks", "gpu.nvidia" },
                { "checkBox_directplay", "gpu.directplay" },
                { "checkBox_bluefolders", "ui.bluefolders" },
                { "checkBox_contex", "ui.contextmenu" },
                { "checkBox_shapka", "ui.win11header" },
                { "checkBox_wotboincontex", "ui.wotbocontext" },
                { "checkBox_explorer", "ui.explorerrestart" },
                { "checkBox_ffmpeg", "ui.ffmpeg" },
                { "checkBox_mica", "ui.mica" },
                { "checkBox_animation", "ui.animations" },
                { "checkBox_onedrive", "extra.onedrive" },
                { "checkBox_compactos", "extra.compactos" },
                { "checkBox_WinSxS", "extra.winsxs" },
                { "checkBox_temp", "extra.tempclean" },
                { "checkBox_updclean", "extra.updateclean" },
                { "checkBox_picture_cache", "extra.iconcache" },
                { "checkBox_mobile_traffic", "extra.mobiletraffic" },
                { "checkBox_nastroyka", "extra.scoobe" },
                { "checkBox_zalipanie", "extra.stickykeys" },
                { "checkBox_dwm", "extra.dwmpriority" },
                { "checkBox_edge", "extra.edge" },
                { "checkBox_move_temp", "extra.movetemp" },
                { "checkBox_pro_1", "pro.homegallery" },
                { "checkBox_pro_3", "pro.windowsupdate" },
                { "checkBox_CSRSS", "pro.csrss" },
                { "checkBox_pro_12", "pro.bufferbloat" },
                { "checkBox_pro_13", "pro.unlocker" },
                { "checkBox_pro_14", "pro.alwaysontop" },
                { "checkBoxUI_Buttons_1", "ui.buttons" },
                { "checkBoxUI_Buttons_2", "ui.explorerdetails" },
                { "checkBoxUI_Buttons_4", "ui.jpegquality" },

                // Стрелки отката
                { "back_main_1", "main.regpack" },
                { "back_main_2", "main.reservedstorage" },
                { "back_main_3", "main.hibernate" },
                { "back_main_4", "main.powerscheme" },
                { "back_main_5", "main.mousefix" },
                { "back_main_6", "main.mpo" },
                { "back_main_7", "main.usb" },
                { "back_main_9", "main.mitigations" },
                { "back_main_10", "main.mmagent" },
                { "back_main_11", "main.pagefile" },
                { "back_main_12", "main.defender" },
                { "back_main_13", "main.audio" },
                { "back_main_14", "main.dwminput" },
                { "back_main_16", "main.powerlatency" },
                { "back_ui_1", "ui.buttons" },
                { "back_ui_2", "ui.explorerdetails" },
                { "back_ui_4", "ui.jpegquality" },
                { "back_ui_5", "ui.bluefolders" },
                { "back_ui_6", "ui.contextmenu" },
                { "back_ui_7", "ui.win11header" },
                { "back_ui_8", "ui.wotbocontext" },
                { "back_ui_9", "ui.explorerrestart" },
                { "back_ui_10", "ui.ffmpeg" },
                { "back_ui_11", "ui.mica" },
                { "back_animations", "ui.animations" },
                { "back_gpu_1", "gpu.directplay" },
                { "back_gpu_2", "gpu.hdcp" },
                { "back_gpu_3", "gpu.nvidia" },
                { "back_gpu_4", "gpu.ansel" },
                { "back_pro_1", "pro.homegallery" },
                { "back_pro_3", "pro.windowsupdate" },
                { "back_pro_11", "pro.win32priority" },
                { "back_pro_12", "pro.bufferbloat" },
                { "back_pro_13", "pro.unlocker" },
                { "back_pro_14", "pro.alwaysontop" },
                { "back_dop_1", "extra.iconcache" },
                { "back_dop_2", "extra.mobiletraffic" },
                { "back_dop_edge", "extra.edge" },
                { "back_dop_3", "extra.scoobe" },
                { "back_dop_4", "extra.stickykeys" },
                { "back_dop_6", "extra.dwmpriority" },
                { "back_dop_7", "pro.csrss" },
                { "back_dop_movetemp", "extra.movetemp" },
            };

        private const int MaxRegistryValues = 5;
        private const int MaxExportValues = 3;
        private const int MaxFiles = 3;

        private static Form _form;
        private static ToolTip _toolTip;
        private static Control _current;
        private static bool _attached;

        public static void Attach(Form form)
        {
            if (_attached || form == null)
            {
                return;
            }

            _attached = true;
            _form = form;
            _toolTip = new ToolTip
            {
                AutoPopDelay = 20000,
                InitialDelay = 350,
                ReshowDelay = 200,
                ShowAlways = true,
                UseFading = false,
                UseAnimation = false,
                IsBalloon = false,
            };

            AttachRecursive(form);
        }

        private static void AttachRecursive(Control control)
        {
            control.MouseMove += OnMouseMove;
            control.MouseLeave += OnMouseLeave;

            foreach (Control child in control.Controls)
            {
                AttachRecursive(child);
            }
        }

        private static void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_form == null || _toolTip == null)
            {
                return;
            }

            Point clientPoint = _form.PointToClient(Cursor.Position);
            if (!_form.ClientRectangle.Contains(clientPoint))
            {
                Hide();
                return;
            }

            Control target = HitTest(_form, clientPoint);
            if (target == _current)
            {
                return;
            }

            _current = target;
            string text = BuildTooltip(target);
            if (string.IsNullOrEmpty(text))
            {
                Hide();
                return;
            }

            _toolTip.Show(text, _form, clientPoint.X + 18, clientPoint.Y + 18, 20000);
        }

        private static void OnMouseLeave(object sender, EventArgs e)
        {
            if (_form == null)
            {
                return;
            }

            Point clientPoint = _form.PointToClient(Cursor.Position);
            if (!_form.ClientRectangle.Contains(clientPoint))
            {
                Hide();
            }
        }

        private static void Hide()
        {
            _current = null;
            if (_toolTip != null)
            {
                _toolTip.Hide(_form);
            }
        }

        private static Control HitTest(Control parent, Point pointInParent)
        {
            Control child = parent.GetChildAtPoint(pointInParent, GetChildAtPointSkip.Invisible);
            if (child == null || child == parent)
            {
                return parent;
            }

            Point childPoint = child.PointToClient(parent.PointToScreen(pointInParent));
            return HitTest(child, childPoint);
        }

        private static string BuildTooltip(Control target)
        {
            if (target == null)
            {
                return null;
            }

            string tweakId = null;
            Control mapped = target;
            while (mapped != null)
            {
                if (ControlToTweak.TryGetValue(mapped.Name, out tweakId))
                {
                    break;
                }

                mapped = mapped.Parent;
            }

            if (mapped == null || string.IsNullOrEmpty(tweakId))
            {
                return null;
            }

            TweakStateStore store = AppServices.Tweaks;
            if (!store.IsApplied(tweakId))
            {
                return null;
            }

            TweakBackup backup = store.LoadBackup(tweakId);
            string title = mapped is CheckBox && !string.IsNullOrEmpty(mapped.Text)
                ? mapped.Text
                : tweakId;

            var builder = new StringBuilder();
            builder.Append(title).Append(" (").Append(tweakId).AppendLine(")");

            string appliedAt = backup != null && !string.IsNullOrEmpty(backup.AppliedAt)
                ? backup.AppliedAt
                : store.GetAppliedAt(tweakId);

            DateTimeOffset when;
            if (!string.IsNullOrEmpty(appliedAt) &&
                DateTimeOffset.TryParse(appliedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out when))
            {
                builder.AppendLine("Применён: " + when.LocalDateTime.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture));
            }
            else if (!string.IsNullOrEmpty(appliedAt))
            {
                builder.AppendLine("Применён: " + appliedAt);
            }

            if (backup == null || !backup.HasData)
            {
                builder.Append("Бэкап не сохранён: твик применён до перехода на новый формат.");
                return builder.ToString();
            }

            int shown = 0;
            if (backup.RegistryValues != null)
            {
                foreach (RegistryBackupValue value in backup.RegistryValues)
                {
                    if (shown >= MaxRegistryValues)
                    {
                        break;
                    }

                    RegistryValueSnapshot snapshot = value.ToSnapshot();
                    builder.AppendLine("Путь: " + FormatPath(snapshot.Hive, snapshot.KeyPath, snapshot.Name));
                    builder.AppendLine("Было: " + (snapshot.Exists ? RegistryValueSnapshot.Describe(snapshot.Data) : "<отсутствовало>"));

                    RegistryValueSnapshot current = AppServices.Registry.ReadValue(snapshot.Hive, snapshot.KeyPath, snapshot.Name);
                    builder.AppendLine("Стало: " + (current.Exists ? RegistryValueSnapshot.Describe(current.Data) : "<удалено>"));
                    shown++;
                }
            }

            int exportShown = 0;
            if (backup.RegistryExports != null)
            {
                foreach (RegistryExportEntry export in backup.RegistryExports)
                {
                    if (exportShown >= MaxExportValues)
                    {
                        break;
                    }

                    if (!export.Existed)
                    {
                        continue;
                    }

                    AppendExportValues(builder, export, ref exportShown);
                }
            }

            int filesShown = 0;
            if (backup.Files != null)
            {
                foreach (FileBackupEntry file in backup.Files)
                {
                    if (filesShown >= MaxFiles)
                    {
                        break;
                    }

                    builder.AppendLine("Файл: " + file.OriginalPath);
                    builder.AppendLine("Было: " + (file.Existed ? "сохранена копия" : "<файла не было>"));
                    builder.AppendLine("Стало: " + (File.Exists(file.OriginalPath) ? "файл на месте" : "<удалён>"));
                    filesShown++;
                }
            }

            if (backup.Directories != null && backup.Directories.Count > 0)
            {
                foreach (DirectoryBackupEntry directory in backup.Directories)
                {
                    builder.AppendLine("Каталог: " + directory.Path);
                    builder.AppendLine("Было: " + (directory.Existed ? "существовал" : "<каталога не было>"));
                    break;
                }
            }

            int totalValues = backup.RegistryValues != null ? backup.RegistryValues.Count : 0;
            if (totalValues > shown)
            {
                builder.AppendLine("… ещё значений: " + (totalValues - shown));
            }

            return builder.ToString().TrimEnd();
        }

        private static void AppendExportValues(StringBuilder builder, RegistryExportEntry export, ref int shown)
        {
            if (string.IsNullOrEmpty(export.ExportPath) || !File.Exists(export.ExportPath))
            {
                return;
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(export.ExportPath);
            }
            catch
            {
                return;
            }

            string section = null;
            int added = 0;
            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal))
                {
                    section = line.Substring(1, line.Length - 2).TrimStart('-');
                    continue;
                }

                if (section == null || added >= 2 || line.Length == 0 || line.StartsWith(";", StringComparison.Ordinal))
                {
                    continue;
                }

                int equals = line.IndexOf('=');
                if (equals <= 0)
                {
                    continue;
                }

                string name = line.Substring(0, equals).Trim().Trim('"');
                string oldValue = line.Substring(equals + 1).Trim();
                if (name == "@")
                {
                    name = string.Empty;
                }

                string keyPath = StripHive(section, export.Hive);

                builder.AppendLine("Путь: " + section + "\\" + (name.Length == 0 ? "(default)" : name));
                builder.AppendLine("Было: " + FormatRegValue(oldValue));

                RegistryHive hive;
                if (Enum.TryParse(export.Hive, true, out hive))
                {
                    RegistryValueSnapshot current = AppServices.Registry.ReadValue(hive, keyPath, name);
                    builder.AppendLine("Стало: " + (current.Exists ? RegistryValueSnapshot.Describe(current.Data) : "<удалено>"));
                }

                added++;
                shown++;
            }
        }

        private static string StripHive(string section, string hive)
        {
            string prefix = hive == null ? string.Empty : HivePrefix(hive);
            if (!string.IsNullOrEmpty(prefix) && section.StartsWith(prefix + "\\", StringComparison.OrdinalIgnoreCase))
            {
                return section.Substring(prefix.Length + 1);
            }

            int slash = section.IndexOf('\\');
            return slash < 0 ? section : section.Substring(slash + 1);
        }

        private static string HivePrefix(string hiveName)
        {
            RegistryHive hive;
            if (!Enum.TryParse(hiveName, true, out hive))
            {
                return string.Empty;
            }

            switch (hive)
            {
                case RegistryHive.CurrentUser: return "HKEY_CURRENT_USER";
                case RegistryHive.ClassesRoot: return "HKEY_CLASSES_ROOT";
                case RegistryHive.Users: return "HKEY_USERS";
                case RegistryHive.CurrentConfig: return "HKEY_CURRENT_CONFIG";
                default: return "HKEY_LOCAL_MACHINE";
            }
        }

        private static string FormatRegValue(string raw)
        {
            if (raw.StartsWith("\"", StringComparison.Ordinal))
            {
                return raw;
            }

            if (raw.StartsWith("dword:", StringComparison.OrdinalIgnoreCase))
            {
                int value;
                if (int.TryParse(raw.Substring(6), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
                {
                    return value.ToString(CultureInfo.InvariantCulture) + " (DWORD)";
                }
            }

            if (raw.StartsWith("hex:", StringComparison.OrdinalIgnoreCase))
            {
                return "двоичные данные";
            }

            return raw;
        }

        private static string FormatPath(RegistryHive hive, string keyPath, string name)
        {
            string path = ShortHive(hive) + "\\" + keyPath;
            if (string.IsNullOrEmpty(name))
            {
                return path + " (default)";
            }

            return path + "\\" + name;
        }

        private static string ShortHive(RegistryHive hive)
        {
            switch (hive)
            {
                case RegistryHive.LocalMachine: return "HKLM";
                case RegistryHive.CurrentUser: return "HKCU";
                case RegistryHive.ClassesRoot: return "HKCR";
                case RegistryHive.Users: return "HKU";
                case RegistryHive.CurrentConfig: return "HKCC";
                default: return hive.ToString();
            }
        }
    }
}
