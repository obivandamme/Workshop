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
        int minimalVersionMajor = 1;
        int minimalVersionMinor = 2;
        int minimalVersionBuild = 1;

        public void Start()
        {
            string minimalVersion = minimalVersionMajor + "." + minimalVersionMinor + "." + minimalVersionBuild;
            Assembly dependancyAssembly = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "KIS")
                {
                    dependancyAssembly = assembly;
                    break;
                }
            }
            if (dependancyAssembly != null)
            {
                Debug.Log("Assembly : " + dependancyAssembly.GetName().Name + " | Version : " + dependancyAssembly.GetName().Version + " found !");
                Debug.Log("Minimal version needed is : " + minimalVersion);
                int dependancyAssemblyVersion = (dependancyAssembly.GetName().Version.Major * 100) + (dependancyAssembly.GetName().Version.Minor * 10) + (dependancyAssembly.GetName().Version.Build);
                int minimalAssemblyVersion = (minimalVersionMajor * 100) + (minimalVersionMinor * 10) + (minimalVersionBuild);
                Debug.Log("INT : " + dependancyAssemblyVersion + "/" + minimalAssemblyVersion);
                if (dependancyAssemblyVersion < minimalAssemblyVersion)
                {
                    Debug.LogError("KIS version " + dependancyAssembly.GetName().Version + "is not compatible with OSE Workshop!");
                    var sb = new StringBuilder();
                    sb.AppendFormat("KIS version must be " + minimalVersion + " or greater for this version of OSE Workshop."); sb.AppendLine();
                    sb.AppendFormat("Please update KIS to the latest version."); sb.AppendLine();
                    PopupDialog.SpawnPopupDialog("OSE Workshop/KIS version mismatch", sb.ToString(), "OK", false, HighLogic.Skin);
                }
            }
            else
            {
                Debug.LogError("KIS not found !");
                var sb = new StringBuilder();
                sb.AppendFormat("KIS is required for OSE Workshop."); 
                sb.AppendLine();
                sb.AppendFormat("Please install KIS before using OSE Workshop."); 
                sb.AppendLine();
                PopupDialog.SpawnPopupDialog("KIS not found !", sb.ToString(), "OK", false, HighLogic.Skin);
            } 
        }
    }
}
