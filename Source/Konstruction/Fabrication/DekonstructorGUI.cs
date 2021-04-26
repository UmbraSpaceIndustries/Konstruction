using Konstruction.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Konstruction.Fabrication
{
    public class DekonstructorGUI : KonFabCommonGUI
    {
        private Vector2 scrollPosPart = Vector2.zero;
        public static Dictionary<string, string> PartTextureCache;
        private readonly ModuleDekonstructor _module;
        private float totMass;
        private int totParts;
        private float totVol;
        private List<CostData> totCost;

        public DekonstructorGUI(ModuleDekonstructor partModule, KonstructionScenario scenario)
            : base("Dekonstructor Control Panel", 650, 560)
        {
            _module = partModule;
            _scenario = scenario;
            _persistence = _scenario.ServiceManager
                    .GetService<KonstructionPersistance>();
            SetVisible(true);
        }

        private List<string> LoadRecyclerParts()
        {
            var ret = new List<string>();
            totVol = 0;
            totMass = 0;
            totParts = 0;
            totCost = new List<CostData>();
            var inv = _module.part.FindModuleImplementing<ModuleInventoryPart>();
            for (int z = 0; z < inv.InventorySlots; z++)
            {
                if (!inv.IsSlotEmpty(z))
                {
                    var invPart = GetPartByName(inv.storedParts[z].partName);
                    ret.Add(invPart.title);
                    totParts++;
                    totVol += invPart.partPrefab.FindModuleImplementing<ModuleCargoPart>().packedVolume;
                    totMass += invPart.partPrefab.mass;
                    totMass += invPart.partPrefab.resourceMass;

                    //Add our cost data
                    var cost = PartUtilities.GetPartCost(invPart, _persistence);
                    foreach(var c in cost)
                    {
                        var cTot = totCost.Where(x => x.Resource.name == c.Resource.name).FirstOrDefault();
                        if(cTot == null)
                        {
                            cTot = new CostData();
                            cTot.Resource = c.Resource;
                            totCost.Add(cTot);
                        }
                        cTot.Quantity += c.Quantity;
                    }
                }
            }

            //Account for recycle ratio
            foreach (var cost in totCost)
            {
                double adjQuantity = Math.Floor(cost.Quantity * _module.DekonstructRatio);
                cost.Quantity = (int)adjQuantity;
            }

            ret.Sort();
            return ret; 
        }


        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginVertical();
            try
            {
                //*****************
                //*   SETUP
                //*****************
                List<string> recycleParts = LoadRecyclerParts();

                //*********************
                //*   MAIN WORK AREA
                //*********************
                GUILayout.BeginHorizontal();

                //*****************
                //*   PARTS
                //*****************
                GUILayout.BeginVertical();
                GUILayout.Label(string.Format("<color=#ffd900>Parts to Recycle</color>"), _labelStyle, GUILayout.Width(120));
                scrollPosPart = GUILayout.BeginScrollView(scrollPosPart, _scrollStyle, GUILayout.Width(380), GUILayout.Height(480));
                var itemCol = "ffffff";

                foreach (var item in recycleParts)
                {
                    GUILayout.Label(string.Format("<color=#{0}>{1}</color>", itemCol, item), _labelStyle, GUILayout.Width(340));
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();

                //*****************
                //*   ACTION WINDOW
                //*****************
                //   Part Count:    10
                //   Mass:          0.05 t
                //   Volume:        124 L
                //
                //   Recycler Output:
                //      100 Material Kits
                //      50 Specialized Parts
                //
                //   [ Recycle! ]
                //
                GUILayout.BeginVertical();
                GUILayout.Label(string.Format(" "), _labelStyle, GUILayout.Width(120)); //Spacer

                GUILayout.BeginHorizontal();
                GUILayout.Label(string.Format("<color=#ffd900>Parts:</color>"), _labelStyle, GUILayout.Width(60));
                GUILayout.Label(string.Format("<color=#ffffff>{0} t</color>", totParts), _labelStyle, GUILayout.Width(200));
                GUILayout.EndHorizontal();


                GUILayout.BeginHorizontal();
                GUILayout.Label(string.Format("<color=#ffd900>Mass:</color>"), _labelStyle, GUILayout.Width(60));
                GUILayout.Label($"<color=#ffffff>{totMass:N1} t</color>", _labelStyle, GUILayout.Width(200));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(string.Format("<color=#ffd900>Volume:</color>"), _labelStyle, GUILayout.Width(60));
                GUILayout.Label($"<color=#ffffff>{totVol:N1} L</color>", _labelStyle, GUILayout.Width(200));
                GUILayout.EndHorizontal();

                GUILayout.Label(string.Format(" "), _labelStyle, GUILayout.Width(120)); //Spacer
                GUILayout.Label(string.Format("Recycler Output: "), _labelStyle, GUILayout.Width(120)); //Spacer

                var valRes = true;

                foreach (var cost in totCost)
                {
                    var space = PartUtilities.GetStorageSpace(cost.Resource.name);
                    var valThisRes =  space >= cost.Quantity;
                    var resColor = "ffffff";
                    if (!valThisRes)
                    {
                        resColor = "ff6e69";
                        valRes = false;
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(
                        $"<color=#{resColor}>    - {cost.Resource.displayName} {cost.Quantity}/{space:N0}</color>",
                        _detailStyle,
                        GUILayout.Width(250));
                    GUILayout.EndHorizontal();
                }


                if (valRes)
                {
                    if (GUILayout.Button("Dekonstruct Parts", GUILayout.Width(300), GUILayout.Height(50)))
                        RecycleInventoryParts();
                }
                if (!valRes)
                    GUILayout.Label(string.Format("<color=#ff6e69>Insufficient space for resources.</color>"), _labelStyle, GUILayout.Width(320));

                GUILayout.Label(string.Format(" "), _labelStyle, GUILayout.Width(50)); //Spacer
                if (GUILayout.Button("Close Window"))
                    SetVisible(false);

                GUILayout.EndVertical();
                //*********************
                //*  CLEAN UP
                //*********************
                GUILayout.EndHorizontal();
            }
            catch (Exception ex)
            {
                Debug.Log(ex.StackTrace);
            }
            finally
            {
                GUILayout.EndVertical();
                GUI.DragWindow();
            }
        }

        private void RecycleInventoryParts()
        {
            //Destroy Items
            var inv = _module.part.FindModuleImplementing<ModuleInventoryPart>();
            for (int z = 0; z < inv.InventorySlots; z++)
            {
                if (!inv.IsSlotEmpty(z))
                {
                    inv.ClearPartAtSlot(z);
                }
            }
            //Refund materials
            foreach(var cost in totCost)
            {
                if (cost.Quantity > 0)
                {
                    PartUtilities.AddResource(cost.Resource, cost.Quantity);
                    ScreenMessages.PostScreenMessage(String.Format("Refunding {0} {1}", cost.Quantity, cost.Resource.displayName), 5f, ScreenMessageStyle.UPPER_CENTER);
                }
            }
        }
    }
}