using System.Collections.Generic;

namespace Workshop
{
    internal class OseModuleCategoryAddonEqualityComparer : IEqualityComparer<OseModuleCategoryAddon>
    {
        public bool Equals(OseModuleCategoryAddon x, OseModuleCategoryAddon y)
        {
            return x.Category == y.Category;
        }

        public int GetHashCode(OseModuleCategoryAddon obj)
        {
            return obj.Category.GetHashCode();
        }
    }
}
