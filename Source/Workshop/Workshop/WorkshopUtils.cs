using System;
using System.Collections.Generic;
using System.Text;
using KSPAchievements;
using Workshop.Recipes;

namespace Workshop
{
    using System.Linq;

    using UnityEngine;

    using KIS;

    public class WorkshopUtils
    {
        public static float GetProductivityBonus(Part part, string ExperienceEffect, float SpecialistEfficiencyFactor, float ProductivityFactor)
        {
            float adjustedProductivity = ProductivityFactor;

            try
            {
                int crewCount = part.protoModuleCrew.Count;
                if (crewCount == 0)
                    return ProductivityFactor;

                ProtoCrewMember worker;
                GameParameters.AdvancedParams advancedParams = HighLogic.CurrentGame.Parameters.CustomParams<GameParameters.AdvancedParams>();
                float productivityBonus = 1.0f;

                //Find all crews with the build skill and adjust productivity based upon their skill
                for (int index = 0; index < crewCount; index++)
                {
                    worker = part.protoModuleCrew[index];

                    //Adjust productivity if efficiency is enabled
                    if (WorkshopOptions.EfficiencyEnabled)
                    {
                        if (worker.HasEffect(ExperienceEffect))
                        {
                            if (advancedParams.EnableKerbalExperience)
                                productivityBonus = worker.experienceTrait.CrewMemberExperienceLevel() * SpecialistEfficiencyFactor;
                            else
                                productivityBonus = 5.0f * SpecialistEfficiencyFactor;
                        }

                        //Adjust for stupidity
                        if (WorkshopOptions.StupidityAffectsEfficiency)
                            productivityBonus *= (1 - worker.stupidity);

                        adjustedProductivity += productivityBonus;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log("[Workshop] Error encountered while trying to calculate productivity bonus: " + ex);
            }

            return adjustedProductivity;
        }

        public static float GetPackedPartVolume(AvailablePart part)
        {
            var moduleKisItem = KISWrapper.GetKisItem(part.partPrefab);
            return moduleKisItem != null ? moduleKisItem.volumeOverride : KIS_Shared.GetPartVolume(part);
        }

        public static bool IsOccupied(ModuleKISInventory inventory)
        {
            return
                inventory.invType != ModuleKISInventory.InventoryType.Pod ||
                inventory.part.protoModuleCrew.Any(protoCrewMember => protoCrewMember.seatIdx == inventory.podSeat);
        }

        public static bool HasFreeSpace(ModuleKISInventory inventory, WorkshopItem item)
        {
            return inventory.GetContentVolume() + KIS_Shared.GetPartVolume(item.Part) <= inventory.maxVolume;
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
            var texture = GameDatabase.Instance.GetTexture(path, false);
            if (texture == null)
            {
                Debug.LogError("[OSE] - Filter - Unable to load texture file " + path);
                return new Texture2D(25, 25);
            }
            return texture;
        }

        public static string GetKisStats(AvailablePart part)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Mass: " + part.partPrefab.mass + " tons");
            sb.AppendLine("Volume: " + KIS_Shared.GetPartVolume(part).ToString("0.0") + " litres");
            sb.AppendLine("Costs: " + part.cost + "$");

            foreach (var resourceInfo in part.partPrefab.Resources)
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
