using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using KSP.UI.Screens;
using System.Collections.Generic;

namespace KerbalFabricator
{
    public struct InventoryConstraints
    {
        public float MassAvailable;
        public float VolumeAvailable;
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class FabricationMonitor : MonoBehaviour
    {
        private ApplicationLauncherButton fabButton;
        private Rect _windowPosition = new Rect(300, 60, 830, 400);
        private GUIStyle _windowStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _scrollStyle;
        private Vector2 scrollPos = Vector2.zero;
        private bool _hasInitStyles = false;
        private bool windowVisible;
        public static bool renderDisplay = false;
        private static IEnumerable<AvailablePart> _aParts;
        private static IEnumerable<Part> _vParts;
        private static List<string> _cckTags;
        private static Guid _curVesselId;
        private string currentCat = "";

        public IEnumerable<Part> VesselInventoryParts
        {
            get
            {
                if(_vParts == null || _curVesselId != FlightGlobals.ActiveVessel.id)
                {
                    _curVesselId = FlightGlobals.ActiveVessel.id;
                    _vParts = FlightGlobals.ActiveVessel.parts.Where(x=>x.HasModuleImplementing<ModuleInventoryPart>());
                }
                return _vParts;
            }
        }

        public IEnumerable<AvailablePart> AllCargoParts
        {
            get
            {
                if (_aParts == null)
                {
                    _aParts = PartLoader.LoadedPartsList
                        .Where(x => x.partPrefab.HasModuleImplementing<ModuleCargoPart>());
                }
                return _aParts;
            }
        }

        public List<string> GetCCKCategories()
        {
            if (_cckTags == null)
            {
                _cckTags = new List<string>();
                foreach(var part in AllCargoParts)
                {
                    var tag = part.tags.ToLower().Split(' ').Where(t=>t.StartsWith("cck-")).FirstOrDefault();
                    if (String.IsNullOrEmpty(tag))
                        continue;
                    _cckTags.Add(tag.Substring(5));
                }
            }
            return _cckTags;
        }


        void Awake()
        {
            var texture = new Texture2D(36, 36, TextureFormat.RGBA32, false);
            var textureFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "GearWrench.png");
            print("Loading " + textureFile);
            texture.LoadImage(File.ReadAllBytes(textureFile));
            this.fabButton = ApplicationLauncher.Instance.AddModApplication(GuiOn, GuiOff, null, null, null, null,
                ApplicationLauncher.AppScenes.ALWAYS, texture);
        }

        private void GuiOn()
        {
            renderDisplay = true;
        }

        public void Start()
        {
            if (!_hasInitStyles)
                InitStyles();
        }

        private void GuiOff()
        {
            renderDisplay = false;
        }

        private void OnGUI()
        {
            try
            {
                if (!renderDisplay)
                    return;

                if (!HighLogic.LoadedSceneIsFlight)
                    GuiOff();

                if (Event.current.type == EventType.Repaint || Event.current.isMouse)
                {
                    //preDrawQueue
                }
                Ondraw();
            }
            catch (Exception ex)
            {
                print("ERROR in FabricationMonitor (OnGui) " + ex.Message);
            }
        }

        private void Ondraw()
        {
            _windowPosition = GUILayout.Window(10, _windowPosition, OnWindow, "Fabrication Controller", _windowStyle);
        }

        private void OnWindow(int windowId)
        {
            GenerateWindow();
        }

        string ColorToHex(Color32 color)
        {
            string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
            return hex;
        }

        private Part GetPartByName(string partName)
        {
            var p = AllCargoParts.Where(x => x.name == partName).FirstOrDefault();
            if (p != null)
                return p.partPrefab;
            return null;
        }

        private void BuildAThing(string name)
        {
            var iPart = GetPartByName(name);
            if (iPart == null)
                return;

            var modCP = iPart.FindModuleImplementing<ModuleCargoPart>();

            foreach (var p in VesselInventoryParts)
            {
                var inv = p.FindModuleImplementing<ModuleInventoryPart>();
                if(inv.TotalEmptySlots() > 0)
                {
                    var con = GetCapacity(inv);
                    if (con.MassAvailable < iPart.mass)
                        continue;

                    if (con.VolumeAvailable >= modCP.packedVolume)
                    { 
                        for (int z = 0; z < inv.InventorySlots; z++)
                        {
                            if (inv.IsSlotEmpty(z))
                            {
                                inv.StoreCargoPartAtSlot(iPart, z);
                                return;
                            }
                        }
                    }
                }
            }
        }

        private InventoryConstraints GetCapacity(ModuleInventoryPart inv)
        {
            float totVol = 0f;
            float totMass = 0f;
            for (int z = 0; z < inv.InventorySlots; z++)
            {
                if (!inv.IsSlotEmpty(z))
                {
                    var invPart = GetPartByName(inv.storedParts[z].partName);
                    totVol += invPart.FindModuleImplementing<ModuleCargoPart>().packedVolume;
                    totMass += invPart.mass;
                    totMass += invPart.resourceMass;
                }
            }

            InventoryConstraints con;

            if (inv.HasPackedVolumeLimit)
                con.VolumeAvailable = inv.packedVolumeLimit - totVol;
            else
                con.VolumeAvailable = 999f;

            if (inv.HasMassLimit)
                con.MassAvailable = inv.massLimit - totMass;
            else
                con.MassAvailable = 999f;

            return con;
        }

        private List<string> GetInventoryCategories()
        {
            var cats = new List<string>();
            cats.AddRange(AllCargoParts.Select(x => x.category.ToStringCached()).Distinct());
            return cats;
        }

        private void GenerateWindow()
        {
            GUILayout.BeginVertical();
            //scrollPos = GUILayout.BeginScrollView(scrollPos, _scrollStyle, GUILayout.Width(810), GUILayout.Height(350));
            //GUILayout.BeginVertical();

            try
            {
                //Calculate current values
                float invVolume = 0f;
                float invMass = 0f;
                float printVolume = 0f;
                float printMass = 0f;
                double credits = 0;


                foreach (var p in FlightGlobals.ActiveVessel.parts)
                {
                    credits += p.Resources.Where(r => r.resourceName == "KonstructionCredits").Sum(r => r.amount);
                    var invList = p.FindModulesImplementing<ModuleInventoryPart>();
                    foreach(var i in invList.Where(x=>x.TotalEmptySlots() > 0))
                    {
                        var con = GetCapacity(i);
                        if (con.VolumeAvailable > invVolume)
                        {
                            invVolume = con.VolumeAvailable;
                        }
                        if (con.MassAvailable > invMass)
                        {
                            invMass = con.MassAvailable;
                        }
                    }

                    var plist = p.FindModulesImplementing<ModuleFabricatorPart>();
                    printVolume += plist.Sum(x => x.volLimit);
                    printMass += plist.Sum(x => x.massLimit);
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label(String.Format("<color=#ffd900>Printer Capacity:</color>"), _labelStyle, GUILayout.Width(120));
                GUILayout.Label(String.Format("<color=#FFFFFF>{0} L/{1} t</color>", printVolume,printMass), _labelStyle, GUILayout.Width(100));
                GUILayout.Label(String.Format("", 30), _labelStyle, GUILayout.Width(50));
                GUILayout.Label(String.Format("<color=#ffd900>Inventory Capacity:</color>"), _labelStyle, GUILayout.Width(120));
                GUILayout.Label(String.Format("<color=#FFFFFF>{0} L/{1} t</color>", invVolume,invMass), _labelStyle, GUILayout.Width(100));
                GUILayout.Label(String.Format("", 30), _labelStyle, GUILayout.Width(50));
                GUILayout.Label(String.Format("<color=#ffd900>Construction Credits:</color>"), _labelStyle, GUILayout.Width(120));
                GUILayout.Label(String.Format("<color=#FFFFFF>{0}</color>", credits), _labelStyle, GUILayout.Width(100));
                GUILayout.Label(String.Format("", 30), _labelStyle, GUILayout.Width(50));


                List<string> cats = GetInventoryCategories();
                cats.AddRange(GetCCKCategories());

                //Category Buttons
                for(int i = 0; i < cats.Count; ++i)
                {
                    var cat = cats[i];
                    if(i % 8 == 0)
                    {
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                    }
                    var catCol = "ffffff";
                    if (currentCat == cat)
                    {
                        catCol = "ffd900";
                    }
                    if (GUILayout.Button(String.Format("<color=#{0}>{1}</color>",catCol, cat), GUILayout.Width(95)))
                    {
                        currentCat = cat;
                    }
                        
                }
                GUILayout.EndHorizontal();

                //GUILayout.BeginHorizontal();
                //if (GUILayout.Button("Build a Thing!", GUILayout.Width(120)))
                //    BuildAThing("Ranger.AnchorHub");
                //GUILayout.EndHorizontal();



                //var numServos = 0;
                //foreach (var p in GetParts())
                //{
                //    var servos = p.FindModulesImplementing<ModuleServo>();
                //    if (servos.Any())
                //    {
                //        numServos++;
                //        bool setPos = false;
                //        int setGoalVal = -1;
                //        bool stopAll = false;
                //        float speedMult = 1f;

                //        if (showServo.Count < numServos)
                //            showServo.Add(true);

                //        GUILayout.BeginHorizontal();

                //        if (showServo[numServos - 1])
                //        {
                //            if (GUILayout.Button("-", GUILayout.Width(35)))
                //                showServo[numServos - 1] = false;
                //        }
                //        else
                //        {
                //            if (GUILayout.Button("+", GUILayout.Width(35)))
                //                showServo[numServos - 1] = true;
                //        }


                //        GUILayout.Label(String.Format("<color=#FFFFFF>[{0}] {1}</color>", numServos,p.partInfo.title), _labelStyle, GUILayout.Width(230));

                //        if (p.HighlightActive)
                //        {
                //            if (GUILayout.Button("-H", GUILayout.Width(35)))
                //            {
                //                p.highlightColor = Color.magenta;
                //                p.HighlightActive = false;
                //                p.Highlight(false);
                //            }
                //        }
                //        else
                //        {
                //            if (GUILayout.Button("+H", GUILayout.Width(35)))
                //            {
                //                p.highlightColor = Color.magenta;
                //                p.HighlightActive = true;
                //                p.Highlight(true);
                //            }
                //        }


                //        var sGroup = p.FindModuleImplementing<ModuleServoGroup>();
                //        if (sGroup != null)
                //        {
                //            if (sGroup.GroupState == 0)
                //            {
                //                if (GUILayout.Button("Group: None", GUILayout.Width(140)))
                //                    sGroup.GroupState++;
                //            }
                //            else if (sGroup.GroupState == 1)
                //            {
                //                if (GUILayout.Button("Group: Slave", GUILayout.Width(140)))
                //                    sGroup.GroupState++;
                //            }
                //            else
                //            {
                //                if (GUILayout.Button("Group: Master", GUILayout.Width(140)))
                //                    sGroup.GroupState = 0;
                //            }

                //            if (sGroup.GroupID < 6)
                //            {
                //                if (GUILayout.Button("ID: " + sGroup.GroupID, GUILayout.Width(70)))
                //                    sGroup.GroupID++;
                //            }
                //            else
                //            {
                //                if (GUILayout.Button("ID: " + sGroup.GroupID, GUILayout.Width(70)))
                //                    sGroup.GroupID = 0;
                //            }
                //        }


                //        if (showServo[numServos - 1])
                //        {
                //            if (GUILayout.Button("All Free", GUILayout.Width(70)))
                //                setGoalVal = 0;
                //            if (GUILayout.Button("All Goal", GUILayout.Width(70)))
                //                setGoalVal = 1;
                //            if (GUILayout.Button("All Stop", GUILayout.Width(70)))
                //                stopAll = true;

                //        }




                //        GUILayout.EndHorizontal();

                //        if (showServo[numServos - 1])
                //        {
                //            foreach (var servo in servos)
                //            {
                //                servo.ServoSpeed *= speedMult;

                //                if (stopAll)
                //                    servo.ServoSpeed = 0;
                //                if (setGoalVal == 0)
                //                    servo.MoveToGoal = false;
                //                if (setGoalVal == 1)
                //                    servo.MoveToGoal = true;
                //                GUILayout.BeginHorizontal();
                //                GUILayout.Label("", _labelStyle, GUILayout.Width(30));
                //                GUILayout.Label(String.Format("{0}", servo.menuName), _labelStyle, GUILayout.Width(130));
                //                var goal = GUILayout.TextField(servo.GoalString, 10, GUILayout.Width(50));
                //                GUILayout.Label(String.Format("<color=#fce700>G: [{0}]</color>", servo.goalValue), _labelStyle, GUILayout.Width(80));
                //                servo.GoalString = goal;
                //                var tmp = 0f;
                //                if (float.TryParse(goal, out tmp))
                //                    servo.goalValue = tmp;
                //                GUILayout.Label(String.Format("{0:0.00}", servo.DisplayPosition), _labelStyle, GUILayout.Width(50));
                //                if (GUILayout.Button("<->", GUILayout.Width(35)))
                //                    servo.ServoSpeed *= -1;
                //                if (GUILayout.Button("-0-", GUILayout.Width(35)))
                //                    servo.ServoSpeed = 0;
                //                if(servo.MoveToGoal)
                //                {
                //                    if (GUILayout.Button("-F-", GUILayout.Width(35)))
                //                        servo.MoveToGoal = false;
                //                }
                //                else
                //                {
                //                    if (GUILayout.Button("-G-", GUILayout.Width(35)))
                //                        servo.MoveToGoal = true;
                //                }
                //                GUILayout.Label("", _labelStyle, GUILayout.Width(30));

                //                if (GUILayout.Button("-10", GUILayout.Width(35)))
                //                    servo.ServoSpeed -= 10;
                //                if (GUILayout.Button("-1", GUILayout.Width(35)))
                //                    servo.ServoSpeed -= 1;

                //                if (servo.GroupBehavior == 0)
                //                {
                //                    if (GUILayout.Button("+", GUILayout.Width(25)))
                //                        servo.GroupBehavior += 1;
                //                }
                //                else if (servo.GroupBehavior == 1)
                //                {
                //                    if (GUILayout.Button("-", GUILayout.Width(25)))
                //                        servo.GroupBehavior += 1;
                //                }
                //                else
                //                {
                //                    if (GUILayout.Button("o", GUILayout.Width(25)))
                //                        servo.ServoSpeed = 0;
                //                }


                //                GUILayout.Label("", _labelStyle, GUILayout.Width(5));
                //                GUILayout.Label(String.Format("<color=#FFD900>{0:0}</color>", servo.ServoSpeed), _labelStyle, GUILayout.Width(40));
                //                GUILayout.Label("", _labelStyle, GUILayout.Width(5));
                //                if (GUILayout.Button("+1", GUILayout.Width(35)))
                //                    servo.ServoSpeed += 1;
                //                if (GUILayout.Button("+10", GUILayout.Width(35)))
                //                    servo.ServoSpeed += 10;
                //                GUILayout.EndHorizontal();
                //            }
                //        }
                //    }
                //}
            }
            catch (Exception ex)
            {
                Debug.Log(ex.StackTrace);
            }
            finally
            {
                //GUILayout.EndVertical();
                //GUILayout.EndScrollView();
                GUILayout.EndVertical();
                GUI.DragWindow();
            }
        }

        internal void OnDestroy()
        {
            if (fabButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(fabButton);
                fabButton = null;
            }
        }

        private void InitStyles()
        {
            _windowStyle = new GUIStyle(HighLogic.Skin.window);
            _windowStyle.fixedWidth = 830f;
            _windowStyle.fixedHeight = 400f;
            _labelStyle = new GUIStyle(HighLogic.Skin.label);
            _buttonStyle = new GUIStyle(HighLogic.Skin.button);
            _scrollStyle = new GUIStyle(HighLogic.Skin.scrollView);
            _hasInitStyles = true;
        }
    }
}