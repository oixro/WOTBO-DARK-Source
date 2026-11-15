using System;
using System.IO;
using System.Net;
using System.Windows.Forms;
using WOTBO.UI;

namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// Загрузка ресурсов твиков с понятной ошибкой.
    ///
    /// GitHub (raw.githubusercontent.com) в части сетей недоступен, и раньше
    /// это выглядело как «твик применился, но ничего не изменилось».
    /// </summary>
    public static class ResourceDownload
    {
        public static void Ensure(TweakContext context, string url, string path, string title)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Download(context, url, path, title);
                }
            }
            catch (Exception exception)
            {
                context.Log.Error("Resource", "не удалось скачать " + url, exception);

                throw new TweakApplyException(
                    "Не удалось скачать «" + title + "».\n\n" +
                    "Похоже, нет доступа к GitHub (" + Host(url) + "). " +
                    "Включите VPN или другое подключение и попробуйте снова.\n\n" +
                    "Техническая причина: " + Short(exception),
                    exception);
            }

            if (!File.Exists(path))
            {
                throw new TweakApplyException("Файл «" + title + "» не скачался. Попробуйте ещё раз.");
            }
        }

        /// <summary>
        /// Скачивание в фоне с модальным окном: раньше DownloadFile вызывался
        /// в UI-потоке, и окно программы замирало на всё время загрузки.
        /// </summary>
        private static void Download(TweakContext context, string url, string path, string title)
        {
            if (!Application.MessageLoop)
            {
                context.Network.DownloadFile(url, path);
                return;
            }

            using (var busy = new BusyForm("Загрузка", "Скачиваем «" + title + "»…"))
            {
                busy.Run(delegate { context.Network.DownloadFile(url, path); });

                if (busy.Error != null)
                {
                    throw busy.Error;
                }
            }
        }

        private static string Host(string url)
        {
            try
            {
                return new Uri(url).Host;
            }
            catch
            {
                return url;
            }
        }

        private static string Short(Exception exception)
        {
            if (exception is WebException web && web.Status == WebExceptionStatus.ConnectFailure)
            {
                return "сервер отклонил подключение";
            }

            return exception.Message;
        }
    }
}
