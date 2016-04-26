namespace Workshop.Recipes
{
    public class Ingredient
    {
        public string Name;

        public double Ratio;

        public Ingredient(string name, double ratio)
        {
            Name = name;
            Ratio = ratio;
        }

        public Ingredient(ConfigNode.Value ingredient)
        {
            Name = ingredient.name;
            if (double.TryParse(ingredient.value, out Ratio) == false)
            {
                Ratio = 0;
            }
        }
    }
}
