using System.Linq;

namespace Workshop
{
    public class FilterBase
    {
        public virtual FilterResult Filter(WorkshopItem[] items, int skip)
        {
            return new FilterResult
            {
                Items = items.OrderBy(i => i.Part.title).Skip(skip).Take(30).ToArray(),
                MaxPages = items.Length/30
            };
        }
    }
}
