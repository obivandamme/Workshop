using System.Collections.Generic;
using System.Linq;
namespace Workshop
{
    public class FilterBase
    {
        public string TexturePath;

        public FilterBase(string texturePath)
        {
            this.TexturePath = texturePath;
        }

        public virtual WorkshopItem[] Filter(IEnumerable<WorkshopItem> items)
        {
            return items.OrderBy(i => i.Part.title).ToArray();
        }
    }
}
