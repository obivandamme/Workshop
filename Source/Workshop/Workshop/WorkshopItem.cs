namespace Workshop
{
	using KIS;

	using UnityEngine;
    using Workshop.Recipes;

    public class WorkshopItem : IConfigNode
	{
		public AvailablePart Part;
        public Blueprint PartBlueprint;

		public KIS_IconViewer Icon { get; set; }

		public WorkshopItem()
		{
			
		}

		public WorkshopItem(AvailablePart part)
		{
			Part = part;
            PartBlueprint = WorkshopRecipeDatabase.ProcessPart(part);
        }

		public void EnableIcon(int resultion)
		{
            WorkshopUtils.LogVerbose("EnableIcon for " + Part.name);
			DisableIcon();
			Icon = new KIS_IconViewer(Part.partPrefab, resultion);
		}

		public void DisableIcon()
		{
            WorkshopUtils.LogVerbose("DisableIcon for " + Part.name);
			if (Icon != null)
			{
				Icon.Dispose();
				Icon = null;
			}
		}


        public void Load(ConfigNode node)
		{
			if (node.HasValue("Name"))
			{
				var partName = node.GetValue("Name");
				Part = PartLoader.getPartInfoByName(partName);
			}
		}

		public void Save(ConfigNode node)
		{
			node.AddValue("Name", Part.name);
		}
	}
}
