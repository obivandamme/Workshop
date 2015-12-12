using System.Text;
using Workshop.Recipes;

namespace Workshop
{
    using System.Linq;

    using UnityEngine;

    using KIS;

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

        public static string GetKisStats(AvailablePart part)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Mass: " + part.partPrefab.mass + " tons");
            sb.AppendLine("Volume: " + KIS_Shared.GetPartVolume(part.partPrefab).ToString("0.0") + " litres");
            sb.AppendLine("Costs: " + part.cost + "$");

            foreach (var resourceInfo in part.partPrefab.Resources.list)
            {
                if (WorkshopRecipeDatabase.HasResourceRecipe(resourceInfo.resourceName))
                {
                    sb.AppendLine(resourceInfo.resourceName + ": " + resourceInfo.maxAmount + " / " + resourceInfo.maxAmount);
                }
                else
                {
                    sb.AppendLine(resourceInfo.resourceName + ": 0 / " + resourceInfo.maxAmount);
                }
            }
            return sb.ToString();
        }

        public static string GetDescription(AvailablePart part)
        {
            var sb = new StringBuilder();
            sb.AppendLine(part.title);
            sb.AppendLine(part.description);
            return sb.ToString();
        }
    }
}
