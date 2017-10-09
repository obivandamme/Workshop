namespace Workshop
{
    public class WorkshopOptions : GameParameters.CustomParameterNode
    {
        [GameParameters.CustomParameterUI("Experience affects efficiency", toolTip = "If enabled, then engineering skills can improve efficiency.", autoPersistance = true)]
        public bool enableEfficiency = true;

        [GameParameters.CustomParameterUI("Stupidity affects efficiency", toolTip = "If enabled, stupidity affects efficiency; the lower the better.", autoPersistance = true)]
        public bool stupidityAffectsEfficiency = false;

        public override string DisplaySection => Section;

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
        public override string Section =>"Workshop";

        public override string Title => "Efficiency";

        public override int SectionOrder => 0;

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            base.SetDifficultyPreset(preset);
        }

        public override GameParameters.GameMode GameMode => GameParameters.GameMode.ANY;

        public override bool HasPresets => false;

        public override bool Enabled(System.Reflection.MemberInfo member, GameParameters parameters)
        {
            if (member.Name == "stupidityAffectsEfficiency" && enableEfficiency)
                return true;
            if (member.Name == "stupidityAffectsEfficiency")
                return false;

            return base.Enabled(member, parameters);
        }
        #endregion
    }
}
