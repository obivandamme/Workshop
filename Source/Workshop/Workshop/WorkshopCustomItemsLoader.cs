namespace Workshop
{
    using System;
    using System.Collections;
    using System.Linq;

    using UnityEngine;

    public class WorkshopCustomItemsLoader : LoadingSystem
    {
        public bool Done;

        private IEnumerator LoadCustomItems()
        {
            var nodes = GameDatabase.Instance.GetConfigNodes("OSE_ItemFilter");
            foreach (var configNode in nodes)
            {
                var items = configNode.GetValue("parts").Split(';');
                Debug.Log("[OSE] found custom items: " + String.Join(" - ", items));
                WorkshopCustomItemsDatabase.CustomItems.AddRange(items.Select(i => i.Replace('_', '.')));
                yield return null;
            }
            Done = true;
        }

        public override bool IsReady()
        {
            return this.Done;
        }

        public override float ProgressFraction()
        {
            return 0;
        }

        public override string ProgressTitle()
        {
            return "OSE Workshop Custom Items";
        }

        public override void StartLoad()
        {
            Done = false;
            StartCoroutine(LoadCustomItems());
        }
    }
}
