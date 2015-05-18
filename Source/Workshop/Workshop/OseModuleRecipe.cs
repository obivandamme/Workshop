namespace Workshop
{
    using System.Linq;
    using System.Collections.Generic;

    public class OseModuleRecipe : PartModule
    {
        public IEnumerable<Demand> Demand;

        public float TotalRatio;

        [KSPField]
        public string Input = "RocketParts,1";

        public override void OnLoad(ConfigNode node)
        {
            Demand = PrepareRecipe(Input.Split(','));
            TotalRatio = Demand.Sum(r => r.Ratio);
            base.OnLoad(node);
        }

        private static IEnumerable<Demand> PrepareRecipe(IList<string> inputs)
        {
            for (var i = 0; i < inputs.Count; i += 2)
            {
                yield return new Demand
                {
                    ResourceName = inputs[i],
                    Ratio = float.Parse(inputs[i + 1])
                };
            }
        }
    }
}
