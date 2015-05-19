using KIS;

namespace Workshop
{
    using UnityEngine;

    public class WorkshopItem
    {
        public AvailablePart Part;

        public KIS_IconViewer Icon;

        public WorkshopItem(AvailablePart part)
        {
            this.Part = part;
        }

        public void EnableIcon()
        {
            this.Icon = new KIS_IconViewer(this.Part.partPrefab, 128);
        }

        public void DisableIcon()
        {
            this.Icon = null;
        }

        public float GetRequiredRocketParts()
        {
            var density = PartResourceLibrary.Instance.GetDefinition("RocketParts").density;
            return this.Part.partPrefab.mass / density;
        }

        public void DrawListItem()
        {
            GUILayout.Box("", GUILayout.Width(50), GUILayout.Height(50));
            var textureRect = GUILayoutUtility.GetLastRect();
            GUI.DrawTexture(textureRect, this.Icon.texture, ScaleMode.ScaleToFit);
            GUILayout.BeginVertical();
            GUILayout.Label(" " + this.Part.title, GuiStyles.Center(), GUILayout.Width(250f));
            GUILayout.Label(" " + this.GetRequiredRocketParts() + " RocketParts", GuiStyles.Center(), GUILayout.Width(250f));
            GUILayout.EndVertical();
        }
    }
}
