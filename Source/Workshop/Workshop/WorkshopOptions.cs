using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

namespace Workshop
{
    public class WorkshopOptions : GameParameters.CustomParameterNode
    {
        [GameParameters.CustomParameterUI("Experience affects efficiency", toolTip = "If enabled, then engineering skills can improve efficiency.", autoPersistance = true)]
        public bool enableEfficiency = true;

        [GameParameters.CustomParameterUI("Stupidity affects efficiency", toolTip = "If enabled, stupidity affects efficiency; the lower the better.", autoPersistance = true)]
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
        public override string Section
        {
            get
            {
                return "Workshop";
            }
        }

        public override string Title
        {
            get
            {
                return "Efficiency";
            }
        }

        public override int SectionOrder
        {
            get
            {
                return 0;
            }
        }

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            base.SetDifficultyPreset(preset);
        }

        public override GameParameters.GameMode GameMode
        {
            get
            {
                return GameParameters.GameMode.ANY;
            }
        }

        public override bool HasPresets
        {
            get
            {
                return false;
            }
        }

        public override bool Enabled(System.Reflection.MemberInfo member, GameParameters parameters)
        {
            Game.Modes gameMode = HighLogic.CurrentGame.Mode;

            if (member.Name == "stupidityAffectsEfficiency" && enableEfficiency)
                return true;
            else if (member.Name == "stupidityAffectsEfficiency")
                return false;

            return base.Enabled(member, parameters);
        }
        #endregion
    }
}
