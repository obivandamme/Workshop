using System.Collections.Generic;
using System.Linq;

namespace Workshop
{
    public class WorkshopQueue : List<WorkshopItem>
    {
        public WorkshopItem Pop()
        {
            if (Count <= 0)
            {
                return null;
            }
            var item = this.ElementAt(0);
            RemoveAt(0);
            return item;
        }
    }
}
