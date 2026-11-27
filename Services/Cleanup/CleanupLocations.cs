using System;
using System.IO;
using Microsoft.Win32;

namespace WOTBO.Services.Cleanup
{
    /// <summary>Известные папки, которые нужны сканеру очистки.</summary>
    internal static class CleanupLocations
    {
        private static string _downloads;

        public static string Local
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData); }
        }

        public static string Roaming
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); }
        }

        public static string ProgramData
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData); }
        }

        public static string Windows
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.Windows); }
        }

        public static string UserProfile
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); }
        }

        public static string ProgramFiles
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles); }
        }

        public static string ProgramFilesX86
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86); }
        }

        public static string Temp
        {
            get { return Path.GetTempPath(); }
        }

        public static string Downloads
        {
            get
            {
                if (!string.IsNullOrEmpty(_downloads))
                {
                    return _downloads;
                }

                _downloads = KnownDownloads();
                return _downloads;
            }
        }

        public static string LocalPath(params string[] parts)
        {
            return Combine(Local, parts);
        }

        public static string RoamingPath(params string[] parts)
        {
            return Combine(Roaming, parts);
        }

        public static string ProgramDataPath(params string[] parts)
        {
            return Combine(ProgramData, parts);
        }

        public static string WindowsPath(params string[] parts)
        {
            return Combine(Windows, parts);
        }

        private static string Combine(string root, string[] parts)
        {
            string result = root;
            foreach (string part in parts)
            {
                result = Path.Combine(result, part);
            }

            return result;
        }

        private static string KnownDownloads()
        {
            try
            {
                using (RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders"))
                {
                    string value = key == null
                        ? null
                        : key.GetValue("{374DE290-123F-4565-9164-39C4925E467B}") as string;
                    if (!string.IsNullOrEmpty(value))
                    {
                        string expanded = Environment.ExpandEnvironmentVariables(value);
                        if (Directory.Exists(expanded))
                        {
                            return expanded;
                        }
                    }
                }
            }
            catch
            {
            }

            return Path.Combine(UserProfile, "Downloads");
        }
    }
}
