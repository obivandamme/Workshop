using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Workshop
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, new GameScenes[] {
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
            IsKISAvailable = AssemblyLoader.loadedAssemblies.Any(a => a.assembly.GetName().Name.Equals("KIS", StringComparison.InvariantCultureIgnoreCase));
            base.OnAwake();
        }
    }
}
