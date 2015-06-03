namespace Workshop
{
    using System.Linq;

    public class WorkshopUtils
    {
        public static bool HasRecipeModule(AvailablePart part)
        {
            return part.partPrefab.Modules != null && part.partPrefab.Modules.OfType<OseModuleRecipe>().Any();
        }
    }
}
