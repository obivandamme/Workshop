namespace Workshop
{
    using System.Linq;

    public class FilterCustom : FilterBase
    {
        public override FilterResult Filter(WorkshopItem[] items, int skip)
        {
            var filteredItems = items.Where(i => WorkshopCustomItemsDatabase.CustomItems.Contains(i.Part.name)).ToArray();
            return new FilterResult
            {
                Items = filteredItems.OrderBy(i => i.Part.title).Skip(skip).Take(30).ToArray(),
                MaxPages = filteredItems.Length/30
            };
        }
    }
}
