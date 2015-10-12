namespace Workshop.Recipes
{
    public class Ingredient
    {
        public string Name;

        public double Ratio;

        public Ingredient(string name, double ratio)
        {
            this.Name = name;

            this.Ratio = ratio;
        }

        public Ingredient(ConfigNode.Value ingredient)
        {
            this.Name = ingredient.name;
            if (double.TryParse(ingredient.value, out this.Ratio))
            {
                this.Ratio = 0;
            }
        }
    }
}
