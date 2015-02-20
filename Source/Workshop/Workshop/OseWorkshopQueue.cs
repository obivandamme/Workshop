using System.Collections.Generic;
using System.Linq;

namespace Workshop
{
    public class OseWorkshopQueue : List<AvailablePart>
    {
        public AvailablePart Pop()
        {
            if (Count <= 0)
            {
                return null;
            }
            var availablePart = this.ElementAt(0);
            RemoveAt(0);
            return availablePart;
        }
    }
}
