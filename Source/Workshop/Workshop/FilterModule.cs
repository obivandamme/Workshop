namespace Workshop
{
	using System.Linq;

	public class FilterModule : FilterBase
	{
		public string Module;

		public FilterModule(string module)
		{
			Module = module;
		}

		public override FilterResult Filter(WorkshopItem[] items, int skip)
		{
			var filteredItems = items.Where(i => i.Part.partPrefab.GetComponent(Module) != null).ToArray();
			return new FilterResult
			{
				Items = filteredItems.OrderBy(i => i.Part.title).Skip(skip).Take(30).ToArray(),
				MaxPages = filteredItems.Length/30
			};
		}

		public override string ToString()
		{
			return Module;
		}
	}
}
