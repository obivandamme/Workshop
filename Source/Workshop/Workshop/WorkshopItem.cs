namespace Workshop
{
    using KIS;

    using UnityEngine;
    
    public class WorkshopItem : IConfigNode
    {
        public AvailablePart Part;

        public KIS_IconViewer Icon { get; set; }

        public WorkshopItem()
        {
            
        }

        public WorkshopItem(AvailablePart part)
        {
            Part = part;
        }

        public void EnableIcon(int resultion)
        {
            Debug.Log("[OSE] - EnableIcon for " + Part.name);
            Icon = new KIS_IconViewer(Part.partPrefab, resultion);
        }

        public void DisableIcon()
        {
            Debug.Log("[OSE] - DisableIcon for " + Part.name);
            Icon = null;
        }

        public void Load(ConfigNode node)
        {
            if (node.HasValue("Name"))
            {
                var partName = node.GetValue("Name");
                Part = PartLoader.getPartInfoByName(partName);
            }
        }

        public void Save(ConfigNode node)
        {
            node.AddValue("Name", Part.name);
        }
    }
}
