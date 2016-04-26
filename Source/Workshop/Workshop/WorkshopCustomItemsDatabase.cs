using System.Collections.Generic;

namespace Workshop
{
    using UnityEngine;

    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class WorkshopCustomItemsDatabase : MonoBehaviour
    {
        public static List<string> CustomItems;

        private void Awake()
        {
            CustomItems = new List<string>();

            var loaders = LoadingScreen.Instance.loaders;
            if (loaders != null)
            {
                for (var i = 0; i < loaders.Count; i++)
                {
                    var loadingSystem = loaders[i];
                    if (loadingSystem is WorkshopCustomItemsLoader)
                    {
                        print("[OSE] found WorkshopCustomItemsLoader: " + i);
                        (loadingSystem as WorkshopCustomItemsLoader).Done = false;
                        break;
                    }
                    if (loadingSystem is PartLoader)
                    {
                        print("[OSE] found PartLoader: " + i);
                        var go = new GameObject();
                        var recipeLoader = go.AddComponent<WorkshopCustomItemsLoader>();
                        loaders.Insert(i, recipeLoader);
                        break;
                    }
                }
            }
        }
    }
}
