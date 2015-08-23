using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Workshop.Recipes
{
    public class PartRecipe
    {
        public float Total;

        public List<Ingredient> Ingredients;

        public PartRecipe()
        {
            this.Total = 0;
            this.Ingredients = new List<Ingredient>(); 
        }

        public void AddIngredient(Ingredient ingredient)
        {
            this.Total += ingredient.Ratio;
            this.Ingredients.Add(ingredient);
        }

        public Blueprint PrepareBlueprint(float partMass)
        {
            var report = new Blueprint();

            foreach (var ingredient in this.Ingredients)
            {
                var mass = partMass * ingredient.Ratio / this.Total;
                var resource = new WorkshopResource(ingredient.Name, mass);
                report.AddResource(resource);
            }

            return report;
        }
    }
}
