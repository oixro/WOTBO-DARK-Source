using WOTBO.App;

namespace WOTBO
{
    internal class Internet
    {
        public static bool OK()
        {
            return AppServices.Network.IsAvailable();
        }
    }
}
