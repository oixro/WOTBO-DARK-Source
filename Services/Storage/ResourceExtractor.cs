using System;
using System.IO;
using System.IO.Compression;
using WOTBO.App.Logging;
using WOTBO.Properties;
using WOTBO.Services.Storage;

namespace WOTBO.Services.Storage
{
    /// <summary>Распаковка встроенных ресурсов в рабочую папку.</summary>
    public interface IResourceExtractor
    {
        void Extract(TempWorkspace workspace);
    }

    public sealed class ResourceExtractor : IResourceExtractor
    {
        private const string Category = "Temp";
        private const string ExplorerBat = @"c:\Windows\system32\explorer.bat";

        private readonly IAppLogger _log;

        public ResourceExtractor(IAppLogger log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public void Extract(TempWorkspace workspace)
        {
            if (workspace == null) throw new ArgumentNullException(nameof(workspace));

            Directory.CreateDirectory(workspace.Root);
            _log.Info(Category, "tempfolder создан - " + workspace.Root);

            File.WriteAllBytes(workspace.RegpackZip, Resources.regpack);
            _log.Info(Category, "path_regpack7z создан - " + workspace.RegpackZip);
            if (!Directory.Exists(workspace.Regpack))
            {
                ZipFile.ExtractToDirectory(workspace.RegpackZip, workspace.Regpack);
            }
            _log.Info(Category, "path_regpack7z распакован - " + workspace.Regpack);

            File.WriteAllBytes(workspace.SuExecutable, Resources.su);
            _log.Info(Category, "su.exe распакован");

            File.WriteAllText(Path.Combine(workspace.Root, "Audio_Lantency.reg"), Resources.Audio_Lantency);
            _log.Info(Category, "Audio_Lantency.reg распакован");

            File.WriteAllText(Path.Combine(workspace.Root, "Audio_Lantency_delete.reg"), Resources.Audio_Lantency_delete);
            _log.Info(Category, "Audio_Lantency_delete.reg распакован");

            File.WriteAllText(Path.Combine(workspace.Root, "updates.reg"), Resources.updates);
            _log.Info(Category, "updates.reg распакован");

            if (!Directory.Exists(workspace.Ui))
            {
                File.WriteAllBytes(workspace.UiZip, Resources.ui);
                ZipFile.ExtractToDirectory(workspace.UiZip, workspace.Ui);
                _log.Info(Category, "ui.zip распакован - " + workspace.Ui);
            }

            if (!File.Exists(ExplorerBat))
            {
                File.WriteAllText(ExplorerBat, Resources.explor);
            }
        }
    }
}
