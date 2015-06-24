using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System.Text;

namespace Workshop
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    internal class DependancyChecker : MonoBehaviour
    {
        string assemblyName = "KIS";
        int minimalVersionMajor = 1;
        int minimalVersionMinor = 1;
        int minimalVersionBuild = 5;
        
        public void Start()
        {
            string minimalVersion = minimalVersionMajor + "." + minimalVersionMinor + "." + minimalVersionBuild;
            Assembly dependancyAssembly = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == assemblyName)
                {
                    dependancyAssembly = assembly;
                    break;
                }
            }
            if (dependancyAssembly != null)
            {
                Debug.Log("Assembly : " + dependancyAssembly.GetName().Name + " | Version : " + dependancyAssembly.GetName().Version + " found !");
                Debug.Log("Minimal version needed is : " + minimalVersion);

                if (dependancyAssembly.GetName().Version.Major < minimalVersionMajor || dependancyAssembly.GetName().Version.Minor < minimalVersionMinor || dependancyAssembly.GetName().Version.Build < minimalVersionBuild)
                {
                    Debug.LogError(assemblyName + " version " + dependancyAssembly.GetName().Version + "is not compatible with OSE Workshop!");
                    var sb = new StringBuilder();
                    sb.AppendFormat("{0} version must be {1} for this version of OSE Workshop.", assemblyName, minimalVersion); 
                    sb.AppendLine();
                    sb.AppendFormat("Please update {0} to Version {1}", assemblyName, minimalVersion); 
                    sb.AppendLine();
                    PopupDialog.SpawnPopupDialog("OSE Workshop/" + assemblyName + " Version mismatch", sb.ToString(), "OK", false, HighLogic.Skin);
                }
            }
            else
            {
                Debug.Log("Assembly : " + assemblyName + " not found !");
                Debug.Log("Disabling OSE Workshop!");
            }
        }
    }
}
