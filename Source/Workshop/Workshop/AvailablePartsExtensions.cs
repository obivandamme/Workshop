namespace Workshop
{
    using System.Linq;

    using KIS;

    public static class AvailablePartsExtensions
    {
        public static float GetRocketPartsNeeded(this AvailablePart part)
        {
            var resource = PartResourceLibrary.Instance.GetDefinition("RocketParts");
            var density = resource.density;
            return part.partPrefab.mass / density;
        }

        public static bool HasStorableKasModule(this AvailablePart part)
        {
            return part.partPrefab.Modules != null && part.partPrefab.Modules.OfType<ModuleKISItem>().Any();
        }
    }
}
