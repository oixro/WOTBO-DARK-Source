using System.Net;

namespace WOTBO
{
    internal class Internet
    {
        public static bool OK()
        {
            try
            {
                Dns.GetHostEntry("github.com");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
