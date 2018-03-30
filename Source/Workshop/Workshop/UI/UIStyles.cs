using UnityEngine;

namespace Workshop.UI
{
    public static class UIStyles
    {
        public static GUIStyle StatsStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 11,
            alignment = TextAnchor.UpperLeft,
            padding = new RectOffset(5, 0, 5, 0)
        };

        public static GUIStyle TooltipDescriptionStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 11,
            alignment = TextAnchor.UpperLeft,
            padding = new RectOffset(0, 0, 5, 0)
        };

        public static GUIStyle TitleDescriptionStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 13,
            alignment = TextAnchor.UpperLeft,
            padding = new RectOffset(0, 0, 5, 0)
        };

        public static GUIStyle QueueSkin = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperCenter,
            padding = new RectOffset(0, 0, 5, 0)
        };

        public static GUIStyle lowerRightStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.LowerRight,
            fontSize = 10,
            padding = new RectOffset(4, 4, 4, 4),
            normal = new GUIStyleState()
            {
                textColor = Color.white
            }
        };

    }
}
