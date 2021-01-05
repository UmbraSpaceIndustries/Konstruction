using System.Collections.Generic;
using UnityEngine;

namespace Konstruction
{
    public class KonstructionManager : MonoBehaviour
    {
        // Static singleton instance
        private static KonstructionManager instance;

        // Static singleton property
        public static KonstructionManager Instance
        {
            get { return instance ?? (instance = new GameObject("KonstructionManager").AddComponent<KonstructionManager>()); }
        }

        //Backing variables
        private List<KonstructionModuleResource> _moduleResources;
        private List<KonstructionCostResource> _costResources;

        public void ResetCache()
        {
            _moduleResources = null;
            _costResources = null;
        }

        public List<KonstructionModuleResource> ModuleResources
        {
            get
            {
                if (_moduleResources == null)
                {
                    _moduleResources = new List<KonstructionModuleResource>();
                    _moduleResources.AddRange(KonstructionScenario.Instance.settings.GetModuleResources());
                }
                return _moduleResources;
            }
        }

        public List<KonstructionCostResource> CostResources
        {
            get
            {
                if (_costResources == null)
                {
                    _costResources = new List<KonstructionCostResource>();
                    _costResources.AddRange(KonstructionScenario.Instance.settings.GetCostResources());
                }
                return _costResources;
            }
        }
    }

}
