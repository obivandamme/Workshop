namespace Workshop.Recipes
{
    public class WorkshopResource : IConfigNode
    {
        public string Name;

        public double Units;

        public double Processed;

        public WorkshopResource()
        {
            
        }

        public WorkshopResource(string name, double units)
        {
            Name = name;
            Units = units;
            Processed = 0;
        }

        public double Costs()
        {
            var definition = PartResourceLibrary.Instance.GetDefinition(Name);
            return definition.unitCost * Units;
        }

        public void Merge(WorkshopResource res)
        {
            Units += res.Units;
        }

        public void Load(ConfigNode node)
        {
            Name = node.GetValue("Name");
            Units = double.Parse(node.GetValue("Units"));
            Processed = double.Parse(node.GetValue("Processed"));
        }

        public void Save(ConfigNode node)
        {
            node.AddValue("Name", Name);
            node.AddValue("Units", Units);
            node.AddValue("Processed", Processed);
        }
    }
}