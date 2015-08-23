using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Workshop.Recipes
{
    public class Ingredient
    {
        public string Name;
        public float Ratio;

        public Ingredient(string name, float ratio)
        {
            this.Name = name;
            this.Ratio = ratio;
        }
    }
}
