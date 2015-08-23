using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Workshop.Recipes
{
    public class Blueprint
    {
        public List<WorkshopResource> Resources;

        public Blueprint()
        {
            this.Resources = new List<WorkshopResource>();
        }

        public void AddResource(WorkshopResource resource)
        {
            this.Resources.Add(resource);
        }

        public string GetResourceStats()
        {
            var sb = new StringBuilder();
            foreach (var resource in this.Resources)
            {
                sb.AppendLine(resource.Name + ": " + resource.RequiredAmount);
            }
            return sb.ToString();
        }

        public float Cost()
        {
            return this.Resources.Sum(r => r.Cost);
        }
    }
}
