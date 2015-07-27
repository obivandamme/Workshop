using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Workshop
{
    public class FilterBase
    {
        public Texture2D Texture;

        public string Name;

        public FilterBase(string texturePath, string name)
        {
            var textureInfo = GameDatabase.Instance.databaseTexture.FirstOrDefault(t => t.name == texturePath);
            if (textureInfo != null)
            {
                this.Texture = textureInfo.texture;
            }
            else
            {
                Debug.LogError("[OSE] - Filter - Unable to load texture file " + texturePath);
            }
            this.Name = name;

        }

        public virtual WorkshopItem[] Filter(IEnumerable<WorkshopItem> items, int skip)
        {
            return items.OrderBy(i => i.Part.title).Skip(skip).Take(30).ToArray();
        }
    }
}
