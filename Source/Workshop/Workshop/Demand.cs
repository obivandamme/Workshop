using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Workshop
{
    public class Demand
    {
        public string ResourceName { get; set; }

        public float Ratio { get; set; }

        public float Density
        {
            get
            {
                return PartResourceLibrary.Instance.GetDefinition(this.ResourceName).density;
            }
        }
    }
}
