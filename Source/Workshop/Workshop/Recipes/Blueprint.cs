using System.Collections.Generic;

namespace Workshop.Recipes
{
    using System.Linq;
    using System.Text;

    public class Blueprint : List<WorkshopResource>, IConfigNode
    {
        public double GetProgress()
        {
            var totalAmount = this.Sum(r => r.Units);
            var totalProcessed = this.Sum(r => r.Processed);
            return totalProcessed / totalAmount;
        }

        public string Print(double productivity)
        {
            var sb = new StringBuilder();
            foreach (var res in this)
            {
                sb.AppendLine(res.Name + " : " + res.Units.ToString("N1"));
            }

            var costs = this.Sum(r => r.Costs());
            sb.AppendLine("Resource costs: " + costs.ToString("N1"));

            var duration = this.Sum(r => r.Units) / productivity;
            sb.AppendFormat("Duration: {0:00}h {1:00}m {2:00}s", duration / 3600, (duration / 60) % 60, duration % 60);

            return sb.ToString();
        }

        public void Load(ConfigNode node)
        {
            foreach (var configNode in node.GetNodes("WorkshopResource"))
            {
                var resource = new WorkshopResource();
                resource.Load(configNode);
                Add(resource);
            }
        }

        public void Save(ConfigNode node)
        {
            foreach (var resource in this)
            {
                var n = node.AddNode("WorkshopResource");
                resource.Save(n);
            }
        }
    }
}
