using System.Globalization;

namespace Workshop.Factory
{
    public class OseModuleZeroGravityResourceConverter : ModuleResourceConverter
    {
        [KSPField(guiActive = true, guiName = "G-Force")] 
        public string GeeForce;

        public override void OnUpdate()
        {
            GeeForce = vessel.geeForce.ToString("0.000", CultureInfo.InvariantCulture);
            if (HasGeeForce() && IsActivated)
            {
                ScreenMessages.PostScreenMessage("ResourceConverter in " + part.partInfo.title + " has been stopped because of too much g-force", 5, ScreenMessageStyle.UPPER_CENTER);
                StopResourceConverter();
            }
            else
            {
                base.OnUpdate();
            }
        }

        private bool HasGeeForce()
        {
            return vessel.geeForce > 0.001;
        }
    }
}
