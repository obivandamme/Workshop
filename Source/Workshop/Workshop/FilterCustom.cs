namespace Workshop
{
    using System.Collections.Generic;
    using System.Linq;

    public class FilterCustom : FilterBase
    {
        public override WorkshopItem[] Filter(IEnumerable<WorkshopItem> items, int skip)
        {
            return items.Where(i => WorkshopCustomItemsDatabase.CustomItems.Contains(i.Part.name))
                .OrderBy(i => i.Part.title)
                .Skip(skip)
                .Take(30)
                .ToArray();
        }
    }
}
