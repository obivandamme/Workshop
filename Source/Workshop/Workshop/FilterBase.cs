namespace Workshop
{
    public class FilterBase
    {
        public string TexturePath;

        public FilterBase(string texturePath)
        {
            this.TexturePath = texturePath;
        }

        public virtual WorkshopItem[] Filter(WorkshopItem[] items)
        {
            return items;
        }
    }
}
