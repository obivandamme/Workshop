using KIS;

namespace Workshop
{
    using UnityEngine;

    public class WorkshopItem
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

        public float GetRequiredRocketParts()
        {
            var density = PartResourceLibrary.Instance.GetDefinition("RocketParts").density;
            return this.Part.partPrefab.mass / density;
        }
    }
}
