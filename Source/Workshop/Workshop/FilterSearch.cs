using System.Linq;

namespace Workshop
{
    public class FilterSearch : FilterBase
    {
        public string FilterText = "";

        public FilterSearch() { }

        public override FilterResult Filter(WorkshopItem[] items, int skip)
        {
            var filteredItems = items.Where(i => i.Part.title.Contains(FilterText) || i.Part.description.Contains(FilterText) || i.Part.tags.Contains(FilterText)).ToArray();
            return new FilterResult
            {
                Items = filteredItems.OrderBy(i => i.Part.title).Skip(skip).Take(30).ToArray(),
                MaxPages = filteredItems.Length / 30
            };
        }

        public override string ToString()
        {
            return FilterText;
        }
    }
}
