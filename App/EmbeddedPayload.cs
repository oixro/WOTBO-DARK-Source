using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace WOTBO.App
{
    /// <summary>
    /// Текстовые ресурсы, которые лежат в сборке в сжатом виде
    /// (<c>Resources\payload.zip</c>): скрипт движка Defender и .reg схемы
    /// питания. В исходном виде они занимали ~50 КБ, в архиве — втрое меньше.
    /// </summary>
    public static class EmbeddedPayload
    {
        private const string ResourceSuffix = "payload.zip";

        /// <summary>Прочитать запись архива как текст (UTF-8).</summary>
        public static string ReadText(string entryName)
        {
            using (Stream stream = Open())
            {
                if (stream == null) return null;

                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    ZipArchiveEntry entry = archive.GetEntry(entryName);
                    if (entry == null) return null;

                    using (var reader = new StreamReader(entry.Open()))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }

        /// <summary>Распаковать запись архива в файл.</summary>
        public static bool Extract(string entryName, string path)
        {
            using (Stream stream = Open())
            {
                if (stream == null) return false;

                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    ZipArchiveEntry entry = archive.GetEntry(entryName);
                    if (entry == null) return false;

                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    entry.ExtractToFile(path, true);
                    return true;
                }
            }
        }

        private static Stream Open()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string name = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(ResourceSuffix, StringComparison.OrdinalIgnoreCase));

            return string.IsNullOrEmpty(name) ? null : assembly.GetManifestResourceStream(name);
        }
    }
}
