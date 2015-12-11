using System.Linq;

namespace Workshop.Recipes
{
    using System.Collections;

    public class WorkshopRecipeLoader : LoadingSystem
    {
        public bool Done;

        private void LoadDefaultRecipe()
        {
            var db = GameDatabase.Instance;
            var configNode = db.GetConfigNodes("OSE_DefaultRecipe").LastOrDefault();
            if (configNode != null)
            {
                var recipeNode = configNode.GetNode("RESOURCES");
                var recipe = new PartRecipe(recipeNode);
                print("[OSE] - Loading DefaultRecipe");
                WorkshopRecipeDatabase.DefaultPartRecipe = recipe;
            }
        }

        private IEnumerator LoadResourceRecipes()
        {
            var db = GameDatabase.Instance;
            var nodes = db.GetConfigNodes("OSE_ResourceRecipe");
            foreach (var configNode in nodes)
            {
                var resourceName = configNode.GetValue("name");
                var recipeNode = configNode.GetNode("RESOURCES");
                var recipe = new Recipe(recipeNode);
                print("[OSE] - Loading ResourceRecipe " + resourceName);
                WorkshopRecipeDatabase.ResourceRecipes[resourceName] = recipe;
                yield return null;
            }

        }

        private IEnumerator LoadPartRecipes()
        {
            var db = GameDatabase.Instance;
            var nodes = db.GetConfigNodes("PART");
            foreach (var partNode in nodes)
            {
                var partName = partNode.GetValue("name").Replace('_', '.');
                if (partNode.HasNode("OSE_PartRecipe"))
                {
                    var recipeNode = partNode.GetNode("OSE_PartRecipe");
                    var recipe = new PartRecipe(recipeNode);
                    print("[OSE] - Loading PartRecipe for " + partName);
                    WorkshopRecipeDatabase.PartRecipes[partName] = recipe;
                }
                yield return null;
            }
        }

        private IEnumerator LoadFactoryRecipes()
        {
            var db = GameDatabase.Instance;
            var nodes = db.GetConfigNodes("PART");
            foreach (var partNode in nodes)
            {
                var partName = partNode.GetValue("name").Replace('_', '.');
                if (partNode.HasNode("OSE_FactoryRecipe"))
                {
                    var recipeNode = partNode.GetNode("OSE_FactoryRecipe");
                    var recipe = new PartRecipe(recipeNode);
                    print("[OSE] - Loading FactoryRecipe for " + partName);
                    WorkshopRecipeDatabase.FactoryRecipes[partName] = recipe;
                }
                yield return null;
            }
            Done = true;
        }

        private IEnumerator LoadRecipes()
        {
            LoadDefaultRecipe();
            yield return StartCoroutine(LoadResourceRecipes());
            yield return StartCoroutine(LoadPartRecipes());
            yield return StartCoroutine(LoadFactoryRecipes());
        }

        public override bool IsReady()
        {
            return Done;
        }

        public override float ProgressFraction()
        {
            return 0;
        }

        public override string ProgressTitle()
        {
            return "OSE Workshop Recipes";
        }

        public override void StartLoad()
        {
            Done = false;
            StartCoroutine(LoadRecipes());
        }
    }
}
