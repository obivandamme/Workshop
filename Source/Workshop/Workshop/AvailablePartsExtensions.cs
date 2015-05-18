namespace Workshop
{
    using System.Linq;

    public static class AvailablePartsExtensions
    {
        public static bool HasRecipeModule(this AvailablePart part)
        {
            return part.partPrefab.Modules != null && part.partPrefab.Modules.OfType<OseModuleRecipe>().Any();
        }
    }
}
