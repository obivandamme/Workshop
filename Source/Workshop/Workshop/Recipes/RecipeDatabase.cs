using System.Collections.Generic;
using System.Linq;

namespace Workshop.Recipes
{
    public class RecipeDatabase
    {
        public Dictionary<string, Recipe> PartRecipes;

        public Dictionary<string, Recipe> ResourceRecipes;

        public List<WorkshopResource> ProcessPart(Part part)
        {
            var resources = new Dictionary<string, WorkshopResource>();
            if (this.PartRecipes.ContainsKey(part.name))
            {
                var recipe = this.PartRecipes[part.name];
                foreach (var workshopResource in recipe.Prepare(part.mass))
                {
                    if (resources.ContainsKey(workshopResource.Name))
                    {
                        resources[workshopResource.Name].Merge(workshopResource);
                    }
                    else
                    {
                        resources[workshopResource.Name] = workshopResource;
                    }
                }
            }

            foreach (PartResource partResource in part.Resources)
            {
                if (this.ResourceRecipes.ContainsKey(partResource.resourceName))
                {
                    var recipe = this.ResourceRecipes[partResource.resourceName];
                    foreach (var workshopResource in recipe.Prepare(part.mass))
                    {
                        if (resources.ContainsKey(workshopResource.Name))
                        {
                            resources[workshopResource.Name].Merge(workshopResource);
                        }
                        else
                        {
                            resources[workshopResource.Name] = workshopResource;
                        }
                    }
                }
            }
            
            return resources.Values.ToList();
        }
    }
}
