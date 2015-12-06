namespace Workshop
{
    using System.Collections.Generic;
    using System.Linq;

    public class FilterCategory : FilterBase
    {
        public PartCategories Category;

        public FilterCategory(PartCategories category)
        {
            Category = category;
        }

        public override WorkshopItem[] Filter(IEnumerable<WorkshopItem> items, int skip)
        {
            return items.Where(i => i.Part.category == Category).OrderBy(i => i.Part.title).Skip(skip).Take(30).ToArray();
        }
    }
}
