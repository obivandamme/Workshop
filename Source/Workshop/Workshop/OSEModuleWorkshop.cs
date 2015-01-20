namespace Workshop
{
    using System;
    using System.Collections.Generic;

    using UnityEngine;

    public class OseModuleWorkshop : PartModule
    {
        private const double FLOAT_TOLERANCE = 0.000000001d;
        private const double MAX_DELTA_TIME = 86400d;

        private bool isProducing;

        private double lastUpdateTime;

        [KSPField(guiActiveEditor = true, guiName = "Production", guiUnits = "Spar Parts/s", isPersistant = true)]
        private float productionPerSecond = 0.01f;

        public override void OnUpdate()
        {
            try
            {
                if (isProducing)
                {
                    var res = PartResourceLibrary.Instance.GetDefinition("SpareParts");
                    var resList = new List<PartResource>();
                    part.GetConnectedResources(res.id, res.resourceFlowMode, resList);
                    var stuffLeft = GetProduction();

                    // stores resources first come first served
                    foreach (var partResource in resList)
                    {
                        var spaceAvailable = partResource.maxAmount - partResource.amount;
                        if (spaceAvailable >= stuffLeft)
                        {
                            partResource.amount += stuffLeft;
                            stuffLeft = 0;
                        }
                        else
                        {
                            partResource.amount += spaceAvailable;
                            stuffLeft -= spaceAvailable;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[OSE] - Error in OseModuleWorkshop_OnFixedUpdate - " + e.Message);
            }
            base.OnUpdate();
        }

        [KSPEvent(guiActive = true, guiName = "Start Production")]
        public void ContextMenuToggleProduction()
        {
            isProducing = !isProducing;
            lastUpdateTime = 0;
            Events["ContextMenuToggleProduction"].guiName = (isProducing ? "Stop Production" : "Start Production");
        }

        private double GetProduction()
        {
            if (Time.timeSinceLevelLoad < 1.0f || !FlightGlobals.ready)
            {
                return 0;
            }
            if (Math.Abs(lastUpdateTime) < FLOAT_TOLERANCE)
            {
                // Just started producing
                lastUpdateTime = Planetarium.GetUniversalTime();
                return 0;
            }

            var deltaTime = Math.Min(Planetarium.GetUniversalTime() - lastUpdateTime, MAX_DELTA_TIME);
            lastUpdateTime += deltaTime;
            return deltaTime * productionPerSecond;
        }
    }
}
