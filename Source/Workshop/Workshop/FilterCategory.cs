namespace Workshop
{
	using System.Linq;

	public class FilterCategory : FilterBase
	{
		public PartCategories Category;

		public FilterCategory(PartCategories category)
		{
			Category = category;
		}

		public override FilterResult Filter(WorkshopItem[] items, int skip)
		{
			var filteredItems = items.Where(i => i.Part.category == Category).ToArray();
			return new FilterResult
			{
				Items = filteredItems.OrderBy(i => i.Part.title).Skip(skip).Take(30).ToArray(),
				MaxPages = filteredItems.Length/30
			};
		}

		public override string ToString()
		{
			return Category.ToString();
		}
	}
}
