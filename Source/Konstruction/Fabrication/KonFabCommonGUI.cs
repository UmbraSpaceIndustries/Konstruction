using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using USITools.UI;

namespace Konstruction.Fabrication
{
    public abstract class KonFabCommonGUI : Window
    {
        protected List<AvailablePart> _aParts;
        protected KonstructionScenario _scenario;
        protected GUIStyle _windowStyle;
        protected GUIStyle _labelStyle;
        protected GUIStyle _detailStyle;
        protected GUIStyle _buttonStyle;
        protected GUIStyle _scrollStyle;
        protected GUIStyle _centeredLabelStyle;
        protected KonstructionPersistance _persistence;

        protected KonFabCommonGUI(string windowTitle, float defaultWidth, float defaultHeight) : base(windowTitle, defaultWidth, defaultHeight)
        {
        }

        public AvailablePart GetPartByName(string partName)
        {
            var p = AllCargoParts.Where(x => x.name == partName).FirstOrDefault();
            if (p != null)
                return p;
            return null;
        }

        public List<AvailablePart> AllCargoParts
        {
            get
            {
                if (_aParts == null)
                {
                    _aParts = PartLoader.LoadedPartsList
                        .Where(x => x.partPrefab.HasModuleImplementing<ModuleCargoPart>()
                       && x.TechHidden == false).ToList();
                    for(int i = _aParts.Count; i-- > 0;)
                    {
                        var p = _aParts[i];
                        var m = p.partPrefab.FindModuleImplementing<ModuleCargoPart>();
                        if (m.packedVolume < 0)
                            _aParts.RemoveAt(i);
                    }
                }
                return _aParts;
            }
        }

        string ColorToHex(Color32 color)
        {
            string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
            return hex;
        }

        private AvailablePart LoadPart(string partName)
        {
            var p = AllCargoParts.Where(x => x.name == partName).Single();
            return p;
        }

        protected override void ConfigureStyles()
        {
            base.ConfigureStyles();
            _windowStyle = new GUIStyle(HighLogic.Skin.window)
            {
                fixedWidth = 950f,
                fixedHeight = 600f
            };
            _labelStyle = new GUIStyle(HighLogic.Skin.label);
            _detailStyle = new GUIStyle(HighLogic.Skin.label);
            _detailStyle.fixedHeight = 20f;
            _centeredLabelStyle = new GUIStyle(HighLogic.Skin.label)
            {
                alignment = TextAnchor.MiddleCenter
            };
            _buttonStyle = new GUIStyle(HighLogic.Skin.button);
            _scrollStyle = new GUIStyle(HighLogic.Skin.scrollView);
        }
    }
}