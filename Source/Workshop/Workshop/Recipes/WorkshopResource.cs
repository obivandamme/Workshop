namespace Workshop.Recipes
{
    public class WorkshopResource
    {
        public string Name;

        public double Units;

        public double Processed;

        public WorkshopResource(string name, double units)
        {
            this.Name = name;
            this.Units = units;
            this.Processed = 0;
        }

        public void Merge(WorkshopResource res)
        {
            this.Units += res.Units;
        }
    }
}
