using System.Collections.Generic;
using System.Linq;

namespace Workshop
{
    public class ResourceBroker
    {
        public double AmountAvailable(Part part, string resource)
        {
            var res = PartResourceLibrary.Instance.GetDefinition(resource);
            var resList = new List<PartResource>();
            part.GetConnectedResources(res.id, res.resourceFlowMode, resList);
            return resList.Sum(r => r.amount);
        }

        public double RequestResource(Part part, string resource, double amount)
        {
            var res = PartResourceLibrary.Instance.GetDefinition(resource);
            var resList = new List<PartResource>();
            part.GetConnectedResources(res.id, res.resourceFlowMode, resList);
            var demandLeft = amount;
            var amountTaken = 0d;
         
            foreach (var r in resList)
            {
                if (r.amount >= demandLeft)
                {
                    amountTaken += demandLeft;
                    r.amount -= demandLeft;
                    demandLeft = 0;
                }
                else
                {
                    amountTaken += r.amount;
                    demandLeft -= r.amount;
                    r.amount = 0;
                }
            }

            return amountTaken;
        }
    }
}
