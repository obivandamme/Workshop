namespace Workshop
{
    using System;
    using System.Linq;

    using global::KIS;

    using UnityEngine;

    public class WorkshopUtils
    {
        public static bool IsOccupied(ModuleKISInventory inventory)
        {
            return
                inventory.invType != ModuleKISInventory.InventoryType.Pod ||
                inventory.part.protoModuleCrew.Any(protoCrewMember => protoCrewMember.seatIdx == inventory.podSeat);
        }

        public static bool HasFreeSpace(ModuleKISInventory inventory, WorkshopItem item)
        {
            return inventory.GetContentVolume() + KIS_Shared.GetPartVolume(item.Part.partPrefab) < inventory.maxVolume;
        }

        public static bool HasFreeSlot(ModuleKISInventory inventory)
        {
            return !inventory.isFull();
        }

        public static bool PartResearched(AvailablePart p)
        {
            return ResearchAndDevelopment.PartTechAvailable(p) && ResearchAndDevelopment.PartModelPurchased(p);
        }

        public static Texture2D LoadTexture(string path)
        {
            var textureInfo = GameDatabase.Instance.databaseTexture.FirstOrDefault(t => t.name == path);
            if (textureInfo == null)
            {
                Debug.LogError("[OSE] - Filter - Unable to load texture file " + path);
                return new Texture2D(25, 25);
            }
            return textureInfo.texture;
        }
    }
}
