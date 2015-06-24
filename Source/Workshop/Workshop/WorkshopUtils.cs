namespace Workshop
{
    using System;
    using System.Linq;

    using KIS;

    public class WorkshopUtils
    {
        public static bool IsNotOccupied(ModuleKISInventory inventory)
        {
            return
                inventory.invType != ModuleKISInventory.InventoryType.Pod ||
                inventory.part.protoModuleCrew.Any(protoCrewMember => protoCrewMember.seatIdx == inventory.podSeat);
        }

        public static bool IsToSmall(ModuleKISInventory inventory, WorkshopItem item)
        {
            return inventory.GetContentVolume() + KIS_Shared.GetPartVolume(item.Part.partPrefab) > inventory.maxVolume;
        }
    }
}
