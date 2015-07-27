using KIS;

namespace Workshop
{
    public class WorkshopItem
    {
        public AvailablePart Part;

        public KIS_IconViewer Icon;

        public WorkshopItem(AvailablePart part)
        {
            this.Part = part;
        }

        public void EnableIcon(int resultion)
        {
            this.Icon = new KIS_IconViewer(this.Part.partPrefab, resultion);
        }

        public void DisableIcon()
        {
            this.Icon = null;
        }
    }
}
