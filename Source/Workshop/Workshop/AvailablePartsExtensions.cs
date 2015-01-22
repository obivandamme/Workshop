namespace Workshop
{
    public static class AvailablePartsExtensions
    {
        public static double GetSparePartsNeeded(this AvailablePart part)
        {
            var resource = PartResourceLibrary.Instance.GetDefinition("SpareParts");
            var density = resource.density;
            return part.partPrefab.mass / density;
        }
    }
}
