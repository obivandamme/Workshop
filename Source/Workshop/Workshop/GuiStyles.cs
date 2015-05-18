namespace Workshop
{
    using UnityEngine;

    public class GuiStyles
    {
        public static GUIStyle Button()
        {
            var button = new GUIStyle(GUI.skin.button);
            button.normal.textColor = button.focused.textColor = Color.white;
            button.hover.textColor = button.active.textColor = Color.yellow;
            button.onNormal.textColor = button.onFocused.textColor = button.onHover.textColor = button.onActive.textColor = Color.green;
            button.padding = new RectOffset(4, 4, 4, 4);
            button.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        public static GUIStyle Databox()
        {
            var databox = new GUIStyle(GUI.skin.box);
            databox.padding.top = databox.padding.bottom = 5;
            databox.margin.top = databox.margin.bottom = 5;
            databox.border.top = databox.border.bottom = 5;
            databox.wordWrap = false;
            databox.alignment = TextAnchor.MiddleCenter;
            return databox;
        }

        public static GUIStyle Center()
        {
            var center = new GUIStyle(GUI.skin.label);
            center.wordWrap = false;
            center.alignment = TextAnchor.MiddleCenter;
            return center;
        }

        public static GUIStyle Heading()
        {
            var heading = new GUIStyle(GUI.skin.label);
            heading.wordWrap = false;
            heading.fontStyle = FontStyle.Bold;
            heading.normal.textColor = Color.cyan;
            heading.alignment = TextAnchor.MiddleCenter;
            return heading;
        }
    }
}
