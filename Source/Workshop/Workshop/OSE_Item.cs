using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KIS;

namespace Workshop
{
    public class OSE_Item
    {
        public KIS_IconViewer Icon;

        public AvailablePart Part;

        public OSE_Item(AvailablePart part)
        {
            this.Part = part;
        }

        public void DisableIcon()
        {
            this.Icon = null;
        }

        public void EnableIcon()
        {
            this.Icon = new KIS_IconViewer(this.Part.partPrefab, 128);
        }
    }
}
