namespace Workshop.Recipes
{
    using System.Collections.Generic;
    using System.Linq;

    public class PartRecipe : Recipe
    {
        public PartRecipe(ConfigNode node):base(node)
        {

        }

        public List<WorkshopResource> Prepare(double partMass, double partCost)
        {
            var resources = Prepare(partMass);
            var totalResourceCosts = resources.Sum(r => r.Costs());
            if(partCost > totalResourceCosts)
            {
                var scale = partCost / totalResourceCosts;
                foreach (var resource in resources)
                {
                    resource.Units *= scale;
                }
            }
            return resources;
        }
    }
}
