namespace Workshop.KIS
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using global::KIS;

    public class KIS_Shared
    {
        private static Type KIS_Shared_class;

        private static MethodInfo kis_GetPartVolume;

        public static float GetPartVolume(Part partPrefab)
        {
            return (float)kis_GetPartVolume.Invoke(null, new object[] { partPrefab });
        }

        internal static void Initialize(Assembly kisAssembly)
        {
            KIS_Shared_class = kisAssembly.GetTypes().First(t => t.Name.Equals("KIS_Shared"));
            kis_GetPartVolume = KIS_Shared_class.GetMethod("GetPartVolume");
        }
    }

    public class KISWrapper
    {
        public static bool Initialize()
        {
            var kisAssembly = AssemblyLoader.loadedAssemblies.FirstOrDefault(a => a.assembly.GetName().Name.Equals("KIS", StringComparison.InvariantCultureIgnoreCase));
            if (kisAssembly == null)
            {
                return false;
            }

            KIS_Shared.Initialize(kisAssembly.assembly);
            return true;
        }

        public static List<ModuleKISInventory> GetInventories(Vessel vessel)
        {
            var inventories = new List<ModuleKISInventory>();
            foreach (var part in vessel.parts)
            {
                foreach (PartModule module in part.Modules)
                {
                    if (module.moduleName == "ModuleKISInventory")
                    {
                        inventories.Add(module as ModuleKISInventory);
                    }
                }
            }
            return inventories;
        }
    }
}
