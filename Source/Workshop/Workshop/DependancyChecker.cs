using System;
using UnityEngine;
using KIS;

namespace Workshop
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    internal class DependancyChecker : MonoBehaviour
    {
        public void Start()
        {
            try
            {
                KIS_Shared.DebugLog("[OSE] - Can use KIS");
            }
            catch (Exception ex)
            {
                Debug.LogError("[OSE] - DependancyChecker - " + ex.Message);
                Debug.LogError("[OSE] - DependancyChecker - " + ex.StackTrace);
                PopupDialog.SpawnPopupDialog("OSE Workshop / KIS incompatible", "Your KIS is incompatible to OSE Workshop! Please download the latest versions of both mods!", "OK", false, HighLogic.Skin);
            }   
        }
    }
}
