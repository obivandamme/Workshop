namespace Workshop
{
    using UnityEngine;

    public class WorkshopStyles
    {
        public static GUIStyle Button()
        {
            var button = new GUIStyle(GUI.skin.button);
            button.normal.textColor = button.focused.textColor = Color.white;
            button.hover.textColor = button.active.textColor = Color.yellow;
            button.onNormal.textColor = button.onFocused.textColor = button.onHover.textColor = button.onActive.textColor = Color.green;
            button.padding = new RectOffset(4, 4, 4, 4);
            button.margin = new RectOffset(5, 5, 5, 5);
            button.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        public static GUIStyle Databox()
        {
            var databox = new GUIStyle(GUI.skin.box);
            databox.fontSize = 11;
            databox.padding.top = GUI.skin.box.padding.bottom = 5;
            databox.alignment = TextAnchor.MiddleCenter;
            return databox;
        }

        public static GUIStyle Center()
        {
            var center = new GUIStyle(GUI.skin.label);
            center.wordWrap = false;
            center.alignment = TextAnchor.MiddleCenter;
            center.margin.bottom = 0;
            return center;
        }

        public static GUIStyle CenterSmall()
        {
            var centerSmall = new GUIStyle(Center());
            centerSmall.margin.top = 0;
            centerSmall.fontSize = 12;
            return centerSmall;
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

        public static GUIStyle ProgressBar()
        {
            var progressBar = new GUIStyle(GUI.skin.box);
            progressBar.padding = new RectOffset(0, 0, 0, 0);
            progressBar.margin = new RectOffset(0, 0, 0, 0);
            progressBar.border = new RectOffset(0, 0, 0, 0);
            return progressBar;
        }
    }
}
