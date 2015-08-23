using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Workshop.Recipes
{
    public class WorkshopResource
    {
        public string Name;
        public float Cost;

        public float RequiredAmount;
        public float ProcessedAmount;

        public WorkshopResource(string name, float mass)
        {
            this.Name = name;

            var resourceLibrary = PartResourceLibrary.Instance;
            var resource = resourceLibrary.GetDefinition(name);
            var density = resource.density;

            if (density > 0)
            {
                this.RequiredAmount = mass / density;
            }
            else
            {
                this.RequiredAmount = mass;
            }

            this.Cost = this.RequiredAmount * resource.unitCost;
        }

        public float Progress()
        {
            return (this.RequiredAmount / this.ProcessedAmount) * 100;
        }
    }
}
