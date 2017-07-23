using KSP.Localization;

namespace Workshop
{
    public class WorkshopOptions : GameParameters.CustomParameterNode
    {
        [GameParameters.CustomParameterUI("#LOC_Workshop_Settings_Experience", toolTip = "#LOC_Workshop_Settings_Experience_Tooltip", autoPersistance = true)]
        public bool enableEfficiency = true;

        [GameParameters.CustomParameterUI("#LOC_Workshop_Settings_Stupidity", toolTip = "#LOC_Workshop_Settings_Stupidity_ToolTip", autoPersistance = true)]
        public bool stupidityAffectsEfficiency = false;


        public static bool EfficiencyEnabled
        {
            get
            {
                WorkshopOptions options = HighLogic.CurrentGame.Parameters.CustomParams<WorkshopOptions>();
                return options.enableEfficiency;
            }
        }

        public static bool StupidityAffectsEfficiency
        {
            get
            {
                WorkshopOptions options = HighLogic.CurrentGame.Parameters.CustomParams<WorkshopOptions>();
                return options.stupidityAffectsEfficiency;
            }
        }

        #region CustomParameterNode

        public override string Section => "Workshop";

        public override string DisplaySection => Localizer.GetStringByTag("#LOC_Workshop_Settings_DisplaySection");

        public override string Title => Localizer.GetStringByTag("#LOC_Workshop_Settings_SectionTitle");

        public override int SectionOrder => 0;

        public override void SetDifficultyPreset(GameParameters.Preset preset) => base.SetDifficultyPreset(preset);

        public override GameParameters.GameMode GameMode =>GameParameters.GameMode.ANY;

        public override bool HasPresets => false;

        public override bool Enabled(System.Reflection.MemberInfo member, GameParameters parameters)
        {
            if (member.Name == "stupidityAffectsEfficiency")
                return StupidityAffectsEfficiency;
            if (member.Name == "EfficiencyEnabled")
                return EfficiencyEnabled;

            return base.Enabled(member, parameters);
        }
        #endregion
    }
}
