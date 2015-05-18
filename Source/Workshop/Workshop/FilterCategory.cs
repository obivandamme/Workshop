namespace Workshop
{
    using System.Linq;

    public class FilterCategory : FilterBase
    {
        public PartCategories Category;

        public FilterCategory(string texturePath, PartCategories category) : base(texturePath)
        {
            this.Category = category;
        }

        public override WorkshopItem[] Filter(WorkshopItem[] items)
        {
            return items.Where(i => i.Part.category == Category).ToArray();
        }
    }
}
