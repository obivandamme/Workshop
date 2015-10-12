using System.Collections.Generic;
using System.Linq;

namespace Workshop.Recipes
{
    using UnityEngine;

    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class WorkshopRecipeDatabase : MonoBehaviour
    {
        public static Recipe DefaultRecipe;

        public static Dictionary<string, Recipe> PartRecipes;

        public static Dictionary<string, Recipe> ResourceRecipes;

        public static List<WorkshopResource> ProcessPart(Part part)
        {
            var resources = new Dictionary<string, WorkshopResource>();
            if (PartRecipes.ContainsKey(part.name))
            {
                var recipe = PartRecipes.ContainsKey(part.name) ? PartRecipes[part.name] : DefaultRecipe;
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
            else
            {
                foreach (var workshopResource in DefaultRecipe.Prepare(part.mass))
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
                if (ResourceRecipes.ContainsKey(partResource.resourceName))
                {
                    var recipe = ResourceRecipes[partResource.resourceName];
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

        void Awake()
        {
            PartRecipes = new Dictionary<string, Recipe>();
            ResourceRecipes = new Dictionary<string, Recipe>();

            var loaders = LoadingScreen.Instance.loaders;
            if (loaders != null)
            {
                for (var i = 0; i < loaders.Count; i++)
                {
                    var loadingSystem = loaders[i];
                    if (loadingSystem is WorkshopRecipeLoader)
                    {
                        (loadingSystem as WorkshopRecipeLoader).Done = false;
                        break;
                    }
                    if (loadingSystem is PartLoader)
                    {
                        var go = new GameObject();
                        var recipeLoader = go.AddComponent<WorkshopRecipeLoader>();
                        loaders.Insert(i, recipeLoader);
                        break;
                    }
                }
            }
        }
    }
}
