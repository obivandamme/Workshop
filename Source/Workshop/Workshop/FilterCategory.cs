namespace Workshop
{
    using System.Collections.Generic;
    using System.Linq;

    public class FilterCategory : FilterBase
    {
        public PartCategories Category;

        public FilterCategory(string texturePath, string name, PartCategories category) : base(texturePath, name)
        {
            this.Category = category;
        }

        public override WorkshopItem[] Filter(IEnumerable<WorkshopItem> items)
        {
            return items.Where(i => i.Part.category == Category).OrderBy(i => i.Part.title).ToArray();
        }
    }
}
