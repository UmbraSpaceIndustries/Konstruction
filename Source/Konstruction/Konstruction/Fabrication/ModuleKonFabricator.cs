using System;
using UnityEngine;

namespace Konstruction.Fabrication
{
    public class ModuleKonFabricator : PartModule
    {
        protected FabricationGUI _mainGui;
        protected KonstructionScenario _scenario;

        [KSPEvent(name = "Konfabricator", isDefault = false, guiActive = true, guiName = "Konfabricator")]
        public void OpenWindow()
        {
            if (_mainGui == null)
                _mainGui = new FabricationGUI(this,_scenario);

            _mainGui.SetVisible(true);
        }

        void Start()
        {
            // Hook into the ScenarioModule
            if (_scenario == null)
                _scenario = HighLogic.FindObjectOfType<KonstructionScenario>();
        }

        void OnGUI()
        {
            try
            {
                if (_mainGui == null || !_mainGui.IsVisible())
                    return;

                // Draw main window and child windows, if available
                _mainGui.DrawWindow();
            }
            catch (Exception ex)
            {
                Debug.LogError("[Konstruction] ERROR in ModuleKonFabricator.OnGUI: " + ex.Message);
            }
        }
    }


}
