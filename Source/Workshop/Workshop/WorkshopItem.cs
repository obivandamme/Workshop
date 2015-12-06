namespace Workshop
{
    using System.Text;

    using KIS;

    using UnityEngine;

    using Workshop.Recipes;

    public class WorkshopItem : IConfigNode
    {
        public AvailablePart Part;

        public KIS_IconViewer Icon;

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
            Icon = null;
        }

        public string GetKisStats()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Mass: " + Part.partPrefab.mass + " tons");
            sb.AppendLine("Volume: " + KIS_Shared.GetPartVolume(Part.partPrefab).ToString("0.0") + " litres");
            sb.AppendLine("Costs: " + Part.cost + "$");

            foreach (var resourceInfo in Part.partPrefab.Resources.list)
            {
                if (WorkshopRecipeDatabase.HasResourceRecipe(resourceInfo.resourceName))
                {
                    sb.AppendLine(resourceInfo.resourceName + ": " + resourceInfo.maxAmount + " / " + resourceInfo.maxAmount);
                }
                else
                {
                    sb.AppendLine(resourceInfo.resourceName + ": 0 / " + resourceInfo.maxAmount);
                }
            }
            return sb.ToString();
        }

        public string GetDescription()
        {
            var sb = new StringBuilder();
            sb.AppendLine(Part.title);
            sb.AppendLine(Part.description);
            return sb.ToString();
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
