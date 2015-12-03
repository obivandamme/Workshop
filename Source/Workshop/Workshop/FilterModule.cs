namespace Workshop
{
    using System.Collections.Generic;
    using System.Linq;

    public class FilterModule : FilterBase
    {
        public string Module;

        public FilterModule(string module)
        {
            Module = module;
        }

        public override WorkshopItem[] Filter(IEnumerable<WorkshopItem> items, int skip)
        {
            return items.Where(i => i.Part.partPrefab.GetComponent(Module) != null).OrderBy(i => i.Part.title).Skip(skip).Take(30).ToArray();
        }
    }
}
