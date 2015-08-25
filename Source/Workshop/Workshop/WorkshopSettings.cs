namespace Workshop
{
    using System;
    using System.Linq;
    using UnityEngine;

    [KSPScenario(ScenarioCreationOptions.AddToAllGames, new [] {
			GameScenes.SPACECENTER,
			GameScenes.EDITOR,
			GameScenes.FLIGHT,
			GameScenes.TRACKSTATION
		})]
    public class WorkshopSettings : ScenarioModule
    {
        public static bool IsKISAvailable
        {
            get;
            private set;
        }

        public override void OnAwake()
        {
            base.OnAwake();
            try
            {
                IsKISAvailable = AssemblyLoader.loadedAssemblies.Any(a => a.assembly.GetName().Name.Equals("KIS", StringComparison.InvariantCultureIgnoreCase));
            }
            catch (Exception)
            {
                IsKISAvailable = false;
                Debug.LogError("Error while checking for KIS. Workshop will be disabled");
            }
        }
    }
}
