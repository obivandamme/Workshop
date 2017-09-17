using KSP.Localization;
using System.Globalization;

namespace Workshop.Factory
{
    public class OseModuleZeroGravityResourceConverter : ModuleResourceConverter
    {
        [KSPField(guiActive = true, guiName = "#LOC_Workshop_ZeroGConverter_GForce")] // G-Force 
        public string GeeForce;

        public override void OnUpdate()
        {
            GeeForce = vessel.geeForce.ToString("0.000", CultureInfo.InvariantCulture);
            if (HasGeeForce() && IsActivated)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format(""), 5, ScreenMessageStyle.UPPER_CENTER); // "ResourceConverter in " + part.partInfo.title + " has been stopped because of too much g-force"
                StopResourceConverter();
            }
            else
            {
                base.OnUpdate();
            }
            var part = new Part();
            if (part.FindModuleImplementing<KerbalSeat>())
            {

            }
        }

        private bool HasGeeForce()
        {
            return vessel.geeForce > 0.001;
        }
    }
}
