namespace Workshop
{
    using System;
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

        private IEnumerable<Demand> PrepareRecipe(string[] inputs)
        {
            for (int i = 0; i < inputs.Length; i += 2)
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
