﻿using System.Collections.Generic;
using System.Linq;

namespace Workshop
{
    public class WorkshopQueue : List<WorkshopItem>, IConfigNode
    {
        public WorkshopItem Pop()
        {
            if (Count <= 0)
            {
                return null;
            }
            var item = this.ElementAt(0);
            RemoveAt(0);
            return item;
        }

        public void MoveUp(int index)
        {

        }

        public void MoveDown(int index)
        {

        }

        public void Load(ConfigNode node)
        {
            var nodes = node.GetNodes("QueuedPart");
            foreach (var partNode in nodes)
            {
                var item = new WorkshopItem();
                item.Load(partNode);
                Add(item);
            }
        }

        public void Save(ConfigNode node)
        {
            foreach (var item in this)
            {
                var partNode = node.AddNode("QueuedPart");
                item.Save(partNode);
            }
        }
    }
}
