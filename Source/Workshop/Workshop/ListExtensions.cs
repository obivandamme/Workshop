using System.Collections.Generic;

namespace Workshop
{
    public static class ListExtensions
    {
        public static T NextOf<T>(this List<T> list, T item)
        {
            var index = list.IndexOf(item);
            var nextIndex = (index + 1) % (list.Count);
            return list[nextIndex];
        }

        public static T PreviousOf<T>(this List<T> list, T item)
        {
            var index = list.IndexOf(item);
            var nextIndex = index > 0 ? index - 1 : (list.Count - 1);
            return list[nextIndex];
        }
    }
}
