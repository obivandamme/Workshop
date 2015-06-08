namespace Workshop
{
    using System;
    using System.Linq;

    using KIS;

    public class WorkshopUtils
    {
        public static bool HasRecipeModule(AvailablePart part)
        {
            return part.partPrefab.Modules != null && part.partPrefab.Modules.OfType<OseModuleRecipe>().Any();
        }

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

        public static bool HasTech(string techid)
        {
            try
            {
                var persistentfile = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs";
                var config = ConfigNode.Load(persistentfile);
                var gameconf = config.GetNode("GAME");
                var scenarios = gameconf.GetNodes("SCENARIO");
                return scenarios
                    .Where(scenario => scenario.GetValue("name") == "ResearchAndDevelopment")
                    .SelectMany(scenario => scenario.GetNodes("Tech"))
                    .Any(technode => technode.GetValue("id") == techid);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
