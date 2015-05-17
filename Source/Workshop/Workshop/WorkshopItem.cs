using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KIS;

namespace Workshop
{
    class WorkshopItem
    {
        public AvailablePart Part;

        public KIS_IconViewer Icon;

        public WorkshopItem(AvailablePart part)
        {
            this.Part = part;
        }

        public void EnableIcon()
        {
            this.Icon = new KIS_IconViewer(this.Part.partPrefab, 128);
        }

        public void DisableIcon()
        {
            this.Icon = null;
        }
    }
}
