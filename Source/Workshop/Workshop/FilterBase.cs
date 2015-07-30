using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Workshop
{
    public class FilterBase
    {
        public virtual WorkshopItem[] Filter(IEnumerable<WorkshopItem> items, int skip)
        {
            return items.OrderBy(i => i.Part.title).Skip(skip).Take(30).ToArray();
        }
    }
}
