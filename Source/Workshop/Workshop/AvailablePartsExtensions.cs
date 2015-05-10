namespace Workshop
{
    using System.Linq;
    using System.Collections.Generic;

    public static class AvailablePartsExtensions
    {
        public static float GetResourcesNeeded(this AvailablePart part, List<Demand> input)
        {
            var totaldensity = 0f;
            var totalratio = 0f;
            foreach (var res in input)
            {
                totaldensity += PartResourceLibrary.Instance.GetDefinition(res.ResourceName).density * res.Ratio;
                totalratio += res.Ratio;
            }

            var density = totaldensity / totalratio;
            
            return part.partPrefab.mass / density;
        }

        public static bool HasRecipeModule(this AvailablePart part)
        {
            return part.partPrefab.Modules != null && part.partPrefab.Modules.OfType<OseModuleRecipe>().Any();
        }
    }
}
