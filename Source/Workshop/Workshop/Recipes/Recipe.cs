using System.Collections.Generic;
using System.Linq;

namespace Workshop.Recipes
{
    public class Recipe
    {
        public Dictionary<string, Ingredient> Ingredients;

        public Recipe()
        {
            Ingredients = new Dictionary<string, Ingredient>();
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
            var total = Ingredients.Sum(i => i.Value.Ratio);
            var resources = new List<WorkshopResource>();
            foreach (var ingredient in Ingredients.Values)
            {
                var amount = mass * ingredient.Ratio / total;
                var definition = PartResourceLibrary.Instance.GetDefinition(ingredient.Name);
                var units = amount / definition.density;
                resources.Add(new WorkshopResource(ingredient.Name, units));
            }
            return resources;
        }
    }
}
