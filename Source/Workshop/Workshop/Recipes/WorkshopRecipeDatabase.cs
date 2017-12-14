using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Workshop.Recipes
{
    using UnityEngine;

    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class WorkshopRecipeDatabase : MonoBehaviour
    {
        public static Recipe DefaultPartRecipe;

        public static Dictionary<string, Recipe> PartRecipes;

        public static Dictionary<string, Recipe> ResourceRecipes;

        public static Dictionary<string, Recipe> FactoryRecipes; 

        public static bool HasResourceRecipe(string name)
        {
            return ResourceRecipes.ContainsKey(name);
        }

        public static Blueprint ProcessPart(AvailablePart part)
        {
            var resources = new Dictionary<string, WorkshopResource>();
            List<WorkshopResource> prepResources = null;

            if (PartRecipes.ContainsKey(part.name))
            {
                prepResources = PartRecipes[part.name].Prepare(part.partPrefab.mass);
            }
            else
            {
                prepResources = DefaultPartRecipe.Prepare(part.partPrefab.mass);
            }
            foreach (var workshopResource in prepResources)
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

            foreach (PartResource partResource in part.partPrefab.Resources)
            {
                if (ResourceRecipes.ContainsKey(partResource.resourceName))
                {
                    var definition = PartResourceLibrary.Instance.GetDefinition(partResource.resourceName);
                    var recipe = ResourceRecipes[partResource.resourceName];
                    foreach (var workshopResource in recipe.Prepare(partResource.maxAmount * definition.density))
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

            var blueprint = new Blueprint();
            blueprint.AddRange(resources.Values);
            blueprint.Funds = Mathf.Max(0, part.cost - (float)blueprint.ResourceCosts());
            return blueprint;
        }

        public static Blueprint ProcessFactoryPart(AvailablePart part)
        {
            var resources = new Dictionary<string, WorkshopResource>();
            if (PartRecipes.ContainsKey(part.name))
            {
                var recipe = FactoryRecipes[part.name];
                foreach (var workshopResource in recipe.Prepare(part.partPrefab.mass))
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
                WorkshopUtils.LogError($"No FactoryRecipeFound for {part.title}");
                return null;
            }
            var blueprint = new Blueprint();
            blueprint.AddRange(resources.Values);
            blueprint.Funds = Mathf.Max(part.cost - (float)blueprint.ResourceCosts(), 0); 
            return blueprint;
        }

        void Awake()
        {
            PartRecipes = new Dictionary<string, Recipe>();
            ResourceRecipes = new Dictionary<string, Recipe>();
            FactoryRecipes = new Dictionary<string, Recipe>();

            var loaders = LoadingScreen.Instance.loaders;
            if (loaders != null)
            {
                for (var i = 0; i < loaders.Count; i++)
                {
                    var loadingSystem = loaders[i];
                    if (loadingSystem is WorkshopRecipeLoader)
                    {
                        print("[OSE] found WorkshopRecipeLoader: " + i);
                        (loadingSystem as WorkshopRecipeLoader).Done = false;
                        break;
                    }
                    if (loadingSystem is PartLoader)
                    {
                        print("[OSE] found PartLoader: " + i);
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
