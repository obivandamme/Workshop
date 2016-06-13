using System.Collections.Generic;

namespace Workshop
{
    using UnityEngine;

    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class WorkshopBlacklistItemsDatabase : MonoBehaviour
    {
        public static List<string> Blacklist;

        private void Awake()
        {
            Blacklist = new List<string>();

            var loaders = LoadingScreen.Instance.loaders;
            if (loaders != null)
            {
                for (var i = 0; i < loaders.Count; i++)
                {
                    var loadingSystem = loaders[i];
                    if (loadingSystem is WorkshopBlacklistItemsLoader)
                    {
                        print("[OSE] found WorkshopCustomItemsLoader: " + i);
                        (loadingSystem as WorkshopBlacklistItemsLoader).Done = false;
                        break;
                    }
                    if (loadingSystem is PartLoader)
                    {
                        print("[OSE] found PartLoader: " + i);
                        var go = new GameObject();
                        var recipeLoader = go.AddComponent<WorkshopBlacklistItemsLoader>();
                        loaders.Insert(i, recipeLoader);
                        break;
                    }
                }
            }
        }
    }
}
