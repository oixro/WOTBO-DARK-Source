namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// Отключает индексацию поиска Windows (служба WSearch).
    /// Поиск по файлам продолжает работать, но без индекса — медленнее.
    /// </summary>
    public sealed class SearchIndexTweak : ServiceStartTweak
    {
        private static readonly string[] Names = { "WSearch" };

        public override string Id
        {
            get { return "main.searchindex"; }
        }

        public override string Category
        {
            get { return "Main"; }
        }

        protected override string[] Services
        {
            get { return Names; }
        }
    }
}
