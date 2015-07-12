using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System.Text;

namespace Workshop
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    internal class DependancyChecker : MonoBehaviour
    {
        private const string AssemblyName = "KIS";
        private const int RequiredVersionMajor = 1;
        private const int RequiredVersionMinor = 1;
        private const int RequiredVersionBuild = 6;

        public void Start()
        {
            var requiredVersion = RequiredVersionMajor + "." + RequiredVersionMinor + "." + RequiredVersionBuild;
            var dependancyAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == AssemblyName);
            if (dependancyAssembly != null)
            {
                Debug.Log("[OSE] Assembly : " + dependancyAssembly.GetName().Name + " | Version : " + dependancyAssembly.GetName().Version + " found !");
                Debug.Log("[OSE] Version needed is : " + requiredVersion);

                if (AssemblyMatchesVersion(dependancyAssembly, requiredVersion))
                {
                    Debug.LogError("[OSE] " + AssemblyName + " version " + dependancyAssembly.GetName().Version + "is not compatible with OSE Workshop!");
                    var message = string.Format("Unsupported KIS version... please use {1}", AssemblyName, requiredVersion);
                    PopupDialog.SpawnPopupDialog("OSE Workshop / " + AssemblyName + " Version mismatch", message, "OK", false, HighLogic.Skin);
                }
            }
            else
            {
                Debug.Log("[OSE] Assembly : " + AssemblyName + " not found !");
                Debug.Log("[OSE] Disabling OSE Workshop!");
            }
        }

        private bool AssemblyMatchesVersion(Assembly assembly, string Version)
        {
            return
                assembly.GetName().Version.Major != RequiredVersionMajor ||
                assembly.GetName().Version.Minor != RequiredVersionMinor ||
                assembly.GetName().Version.Build != RequiredVersionBuild;
        }
    }
}
