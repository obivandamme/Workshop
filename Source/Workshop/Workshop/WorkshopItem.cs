using global::KIS;

namespace Workshop
{
    using System.Text;

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

        public string GetKisStats()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Mass: " + this.Part.partPrefab.mass + " tons");
            sb.AppendLine("Volume: " + KIS_Shared.GetPartVolume(this.Part.partPrefab).ToString("0.0") + " litres");
            sb.AppendLine("Costs: " + this.Part.cost + "$");

            foreach (var resourceInfo in this.Part.partPrefab.Resources.list)
            {
                sb.AppendLine(resourceInfo.resourceName + ": 0 / " + resourceInfo.maxAmount);
            }
            return sb.ToString();
        }

        public string GetWorkshopStats(string resourceName, double productivity)
        {
            var sb = new StringBuilder();
            var resourceInfo = PartResourceLibrary.Instance.GetDefinition(resourceName);

            var density = resourceInfo.density;
            var requiredResources = this.Part.partPrefab.mass / density;
            sb.AppendLine(resourceName + ": " + requiredResources.ToString("0.00"));

            var costs = requiredResources * resourceInfo.unitCost;
            sb.AppendLine("Resource costs: " + costs.ToString("0.00") + "$");

            var seconds = requiredResources / productivity;
            sb.AppendFormat("Duration: {0:00}h {1:00}m {2:00}s", seconds / 3600, (seconds / 60) % 60, seconds % 60);

            return sb.ToString();
        }

        public string GetOseStats(string resourceName, double conversion, double productivity)
        {
            var sb = new StringBuilder();
            var resourceInfo = PartResourceLibrary.Instance.GetDefinition(resourceName);
            
            var density = resourceInfo.density;
            var requiredResources = (this.Part.partPrefab.mass / density) * conversion;
            sb.AppendLine(resourceName + ": " + requiredResources.ToString("0.00"));

            var costs = requiredResources * resourceInfo.unitCost;
            sb.AppendLine("Resource costs: " + costs.ToString("0.00") + "$");

            var seconds = requiredResources / productivity;
            sb.AppendFormat("Duration: {0:00}h {1:00}m {2:00}s", seconds / 3600, (seconds / 60) % 60, seconds % 60);

            return sb.ToString();
        }

        public string GetDescription()
        {
            var sb = new StringBuilder();
            sb.AppendLine(this.Part.title);
            sb.AppendLine(this.Part.description);
            return sb.ToString();
        }
    }
}
