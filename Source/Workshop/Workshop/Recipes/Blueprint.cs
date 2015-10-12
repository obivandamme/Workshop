using System.Collections.Generic;

namespace Workshop.Recipes
{
    public class Blueprint : List<WorkshopResource>, IConfigNode
    {
        public void Load(ConfigNode node)
        {
            foreach (var configNode in node.GetNodes("WorkshopResource"))
            {
                var resource = new WorkshopResource();
                resource.Load(configNode);
                this.Add(resource);
            }
        }

        public void Save(ConfigNode node)
        {
            foreach (var resource in this)
            {
                var n = node.AddNode("WorkshopResource");
                resource.Save(n);
            }
        }
    }
}
