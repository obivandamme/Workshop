namespace Workshop
{
    using System;
    using System.Collections;
    using System.Linq;

    using UnityEngine;

    public class WorkshopBlacklistItemsLoader : LoadingSystem
    {
        public bool Done;

        private IEnumerator LoadCustomItems()
        {
            var nodes = GameDatabase.Instance.GetConfigNodes("OSE_Blacklist");
            foreach (var configNode in nodes)
            {
                var items = configNode.GetValue("parts").Split(';');
                Debug.Log("[OSE] found blacklist items: " + String.Join(" - ", items));
                WorkshopBlacklistItemsDatabase.Blacklist.AddRange(items.Select(i => i.Replace('_', '.')));
                yield return null;
            }
            Done = true;
        }

        public override bool IsReady()
        {
            return Done;
        }

        public override float ProgressFraction()
        {
            return 0;
        }

        public override string ProgressTitle()
        {
            return "OSE Workshop blacklist";
        }

        public override void StartLoad()
        {
            Done = false;
            StartCoroutine(LoadCustomItems());
        }
    }
}
