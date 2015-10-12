using System.Linq;

namespace Workshop.Recipes
{
    using System.Collections;

    public class WorkshopRecipeLoader : LoadingSystem
    {
        public bool Done;

        private ConfigNode LoadRecipeNode(string nodeName)
        {
            var db = GameDatabase.Instance;
            var nodes = db.GetConfigNodes(nodeName);
            return nodes.LastOrDefault();
        }

        private void LoadDefaultRecipe()
        {
            var node = this.LoadRecipeNode("OSE_DefaultRecipe");
            if (node != null)
            {
                WorkshopRecipeDatabase.DefaultRecipe = new Recipe(node);
            }
        }

        private IEnumerator LoadResourceRecipes()
        {
            var db = GameDatabase.Instance;
            var nodes = db.GetConfigNodes("OSE_ResourceRecipe");
            foreach (var configNode in nodes)
            {
                var resourceName = configNode.GetValue("name");
                var recipeNode = configNode.GetNode("Resources");
                var recipe = new Recipe(recipeNode);
                print("[OSE] - ResourceRecipe " + resourceName);
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
                var partName = partNode.GetValue("name");
                if (partNode.HasNode("OSE_PartRecipe"))
                {
                    var recipeNode = partNode.GetNode("OSE_PartRecipe");
                    var recipe = new Recipe(recipeNode);
                    print("[OSE] - PartRecipe " + partName);
                    WorkshopRecipeDatabase.PartRecipes[partName] = recipe;
                }
                yield return null;
            }
            Done = true;
        }

        private IEnumerator LoadRecipes()
        {
            this.LoadDefaultRecipe();
            yield return this.LoadResourceRecipes();
            yield return this.LoadPartRecipes();
        }

        public override bool IsReady()
        {
            return this.Done;
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
            StartCoroutine(this.LoadRecipes());
        }
    }
}
