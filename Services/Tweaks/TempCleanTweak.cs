using System;
using System.IO;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Очистка %TEMP%, кроме wotbo*.log.</summary>
    public sealed class TempCleanTweak : TweakBase
    {
        public override string Id { get { return "extra.tempclean"; } }
        public override string Category { get { return "Extra"; } }

        public override TweakState Detect(TweakContext context)
        {
            return context.Store.IsApplied(Id) ? TweakState.Applied : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            string tempPath = Path.GetTempPath();

            foreach (string file in Directory.GetFiles(tempPath))
            {
                try
                {
                    string name = Path.GetFileName(file);
                    if (name.StartsWith("wotbo", StringComparison.OrdinalIgnoreCase)
                        && name.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Log.Debug("Tweak:extra.tempclean", "Файл " + file + " пропущен.");
                        continue;
                    }

                    File.Delete(file);
                    context.Log.Info("Tweak:extra.tempclean", "Файл " + file + " успешно удален.");
                }
                catch (Exception ex)
                {
                    context.Log.Error("Tweak:extra.tempclean", "Ошибка при удалении файла " + file + ": " + ex.Message, ex);
                }
            }

            foreach (string dir in Directory.GetDirectories(tempPath))
            {
                try
                {
                    Directory.Delete(dir, true);
                    context.Log.Info("Tweak:extra.tempclean", "Папка " + dir + " успешно удалена.");
                }
                catch (Exception ex)
                {
                    context.Log.Error("Tweak:extra.tempclean", "Ошибка при удалении папки " + dir + ": " + ex.Message, ex);
                }
            }
        }

        public override void Rollback(TweakContext context)
        {
        }
    }
}
