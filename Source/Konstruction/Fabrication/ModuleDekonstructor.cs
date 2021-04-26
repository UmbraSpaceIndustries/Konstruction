using System;
using UnityEngine;

namespace Konstruction.Fabrication
{
    public class ModuleDekonstructor : PartModule
    {
        protected DekonstructorGUI _mainGui;
        protected KonstructionScenario _scenario;

        [KSPField]
        public float DekonstructRatio = 0.25f;

        [KSPEvent(name = "Dekonstructor", isDefault = false, guiActive = true, guiName = "Dekonstructor")]
        public void OpenWindow()
        {
            if (_mainGui == null)
                _mainGui = new DekonstructorGUI(this, _scenario);

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
                Debug.LogError("[Konstruction] ERROR in ModuleDekonstructor.OnGUI: " + ex.Message);
            }
        }
    }


}
