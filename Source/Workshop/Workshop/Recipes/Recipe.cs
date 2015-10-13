using System.Collections.Generic;
using System.Linq;

namespace Workshop.Recipes
{
    using UnityEngine;

    public class Recipe
    {
        public Dictionary<string, Ingredient> Ingredients;

        public Recipe()
        {
            this.Ingredients = new Dictionary<string, Ingredient>();
        }

        public Recipe(ConfigNode recipe):this()
        {
            foreach (var ingredient in recipe.values.Cast<ConfigNode.Value>().Select(v => new Ingredient(v)))
            {
                if (Ingredients.ContainsKey(ingredient.Name))
                {
                    Ingredients[ingredient.Name].Ratio += ingredient.Ratio;
                }
                else
                {
                    Ingredients[ingredient.Name] = ingredient;
                }
            }
        }

        public List<WorkshopResource> Prepare(double mass)
        {
            Debug.Log("[OSE] Preparing recipe for mass of " + mass + "kg.");
            var total = this.Ingredients.Sum(i => i.Value.Ratio);
            var resources = new List<WorkshopResource>();
            foreach (var ingredient in this.Ingredients.Values)
            {
                Debug.Log("[OSE] Preparing ingredient " + ingredient.Name);
                var amount = mass * ingredient.Ratio / total;
                var definition = PartResourceLibrary.Instance.GetDefinition(ingredient.Name);
                var units = amount / definition.density;

                Debug.Log(string.Format("[OSE] Ratio:{0}, Amount:{1}, Density:{2}, Units:{3}", ingredient.Ratio, amount, definition.density, units));
                resources.Add(new WorkshopResource(ingredient.Name, units));
            }
            return resources;
        }
    }
}
