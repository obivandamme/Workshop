using System;
using System.Linq;
using UnityEngine;
using System.Text;

namespace Workshop
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    internal class DependancyChecker : MonoBehaviour
    {
        private const string AssemblyName = "KIS";
        private const int MinimalVersionMajor = 1;
        private const int MinimalVersionMinor = 1;
        private const int MinimalVersionBuild = 5;

        public void Start()
        {
            var minimalVersion = MinimalVersionMajor + "." + MinimalVersionMinor + "." + MinimalVersionBuild;
            var dependancyAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == AssemblyName);
            if (dependancyAssembly != null)
            {
                Debug.Log("Assembly : " + dependancyAssembly.GetName().Name + " | Version : " + dependancyAssembly.GetName().Version + " found !");
                Debug.Log("Minimal version needed is : " + minimalVersion);

                if (dependancyAssembly.GetName().Version.Major < MinimalVersionMajor || dependancyAssembly.GetName().Version.Minor < MinimalVersionMinor || dependancyAssembly.GetName().Version.Build < MinimalVersionBuild)
                {
                    Debug.LogError(AssemblyName + " version " + dependancyAssembly.GetName().Version + "is not compatible with OSE Workshop!");
                    var sb = new StringBuilder();
                    sb.AppendFormat("{0} version must be at least {1} for this version of OSE Workshop!", AssemblyName, minimalVersion); 
                    sb.AppendLine();
                    sb.AppendFormat("Please update {0} to the latest Version!", AssemblyName); 
                    sb.AppendLine();
                    PopupDialog.SpawnPopupDialog("OSE Workshop / " + AssemblyName + " Version mismatch", sb.ToString(), "OK", false, HighLogic.Skin);
                }
            }
            else
            {
                Debug.Log("Assembly : " + AssemblyName + " not found !");
                Debug.Log("Disabling OSE Workshop!");
            }
        }
    }
}
