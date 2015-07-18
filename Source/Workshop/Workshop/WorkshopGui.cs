namespace Workshop
{
    using System.Linq;
    using System.Text;

    using KIS;

    using UnityEngine;

    public class WorkshopGui
    {
        public static void ProgressBar(float progress)
        {
            GUILayout.Box("", GUILayout.Width(750), GUILayout.Height(50));
            var boxRect = GUILayoutUtility.GetLastRect();
            if (progress >= 1)
            {
                var color = GUI.color;
                GUI.color = new Color(0, 1, 0, 1);
                GUI.Box(new Rect(boxRect.xMin, boxRect.yMin, boxRect.width * progress / 100, boxRect.height), "");
                GUI.color = color;
            }
            GUI.Label(boxRect, " " + progress.ToString("0.0") + " / 100", WorkshopStyles.Center());
        }

        public static void ItemThumbnail(KIS_IconViewer icon)
        {
            GUILayout.BeginVertical();
            GUILayout.Box("", GUILayout.Width(50), GUILayout.Height(50));
            var textureRect = GUILayoutUtility.GetLastRect();
            GUI.DrawTexture(textureRect, icon.texture, ScaleMode.ScaleToFit);
            GUILayout.EndVertical();
        }

        public static void ItemDescription(AvailablePart part, string resourceName)
        {
            GUILayout.BeginVertical();
            var text = new StringBuilder();
            text.AppendLine(part.title);
            var density = PartResourceLibrary.Instance.GetDefinition(resourceName).density;
            var requiredResources = part.partPrefab.mass / density;
            text.AppendLine(" " + requiredResources.ToString("0.00") + " " + resourceName);
            GUILayout.Box(text.ToString(), WorkshopStyles.Databox(), GUILayout.Width(250), GUILayout.Height(50));
            GUILayout.EndVertical();
        }

        public static bool FilterButton(FilterBase filter, Rect position)
        {
            if (filter.Texture != null)
            {
                return GUI.Button(position, filter.Texture, WorkshopStyles.Button());
            }
            else
            {
                return GUI.Button(position, filter.Name, WorkshopStyles.Button());
            }
        }
    }
}
