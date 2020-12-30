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

    public struct PartScrollbarData
    {
        public string partTitle;
        public string partName;
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class FabricationMonitor : MonoBehaviour
    {
        private ApplicationLauncherButton fabButton;
        private Rect _windowPosition = new Rect(300, 60, 950, 600);
        private GUIStyle _windowStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _scrollStyle;
        private GUIStyle _centeredLabelStyle;
        private Vector2 scrollPosCat = Vector2.zero;
        private Vector2 scrollPosPart = Vector2.zero;
        private bool _hasInitStyles = false;
        private bool windowVisible;
        public static bool renderDisplay = false;
        private static IEnumerable<AvailablePart> _aParts;
        private static IEnumerable<Part> _vParts;
        private static List<string> _cckTags;
        private static List<PartScrollbarData> catParts;
        private static Guid _curVesselId;
        private string currentCat = "";
        private PartScrollbarData currentItem;
        private Texture gearTexture;
        private Texture boxTexture;
        private int progressBar;

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
            this.fabButton = ApplicationLauncher.Instance.AddModApplication(GuiOn, GuiOff, null, null, null, null,
                ApplicationLauncher.AppScenes.ALWAYS, LoadTexture("GearWrench.png"));
            gearTexture = LoadTexture("Gears.png");
            boxTexture = LoadTexture("Crate.png");
        }

        private Texture2D LoadTexture(string name)
        {
            var texture = new Texture2D(36, 36, TextureFormat.RGBA32, false);
            var textureFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), name);
            print("Loading " + textureFile);
            texture.LoadImage(File.ReadAllBytes(textureFile));
            return texture;
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
            _windowPosition = GUILayout.Window(10, _windowPosition, OnWindow, "KonFabricator Control Panel", _windowStyle);
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

            try
            {
                //*****************
                //*   SETUP
                //*****************
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


                //*****************
                //*   HEADER
                //*****************
                GUILayout.BeginHorizontal();
                GUILayout.Label(String.Format("<color=#ffd900>Printer Capacity:</color>"), _labelStyle, GUILayout.Width(120));
                GUILayout.Label(String.Format("<color=#FFFFFF>{0} L/{1} t</color>", printVolume,printMass), _labelStyle, GUILayout.Width(100));
                GUILayout.Label(String.Format("", 30), _labelStyle, GUILayout.Width(50));
                GUILayout.Label(String.Format("<color=#ffd900>Inventory Capacity:</color>"), _labelStyle, GUILayout.Width(120));
                GUILayout.Label(String.Format("<color=#FFFFFF>{0} L/{1} t</color>", invVolume,invMass), _labelStyle, GUILayout.Width(100));
                GUILayout.Label(String.Format("", 30), _labelStyle, GUILayout.Width(50));
                GUILayout.Label(String.Format("<color=#ffd900>Construction Credits:</color>"), _labelStyle, GUILayout.Width(140));
                GUILayout.Label(String.Format("<color=#FFFFFF>{0}</color>", credits), _labelStyle, GUILayout.Width(100));
                GUILayout.Label(String.Format("", 30), _labelStyle, GUILayout.Width(50));
                GUILayout.EndHorizontal();


                //*********************
                //*   MAIN WORK AREA
                //*********************
                GUILayout.BeginHorizontal();
                List<string> cats = GetInventoryCategories();
                cats.AddRange(GetCCKCategories());
                cats.Sort();
                if (catParts == null)
                    catParts = new List<PartScrollbarData>();


                //*****************
                //*   CATEGORIES
                //*****************
                GUILayout.BeginVertical();
                GUILayout.Label(String.Format("<color=#ffd900>Categories</color>"), _labelStyle, GUILayout.Width(120));
                scrollPosCat = GUILayout.BeginScrollView(scrollPosCat, _scrollStyle, GUILayout.Width(160), GUILayout.Height(450));

                for (int i = 0; i < cats.Count; ++i)
                {
                    var cat = cats[i];
                    var catCol = "ffffff";
                    if (currentCat == cat)
                    {
                        catCol = "ffd900";
                    }
                    if (GUILayout.Button(String.Format("<color=#{0}>{1}</color>", catCol, cat, "Label"), _labelStyle, GUILayout.Width(120)))
                    {
                        currentCat = cat;
                        GetPartsForCategory(cat);
                    }
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();


                //*****************
                //*   PARTS
                //*****************
                GUILayout.BeginVertical();
                GUILayout.Label(String.Format("<color=#ffd900>Parts</color>"), _labelStyle, GUILayout.Width(120));
                scrollPosPart = GUILayout.BeginScrollView(scrollPosPart, _scrollStyle, GUILayout.Width(400), GUILayout.Height(450));

                foreach(var item in catParts.OrderBy(x=>x.partTitle))
                {
                    var itemCol = "ffffff";
                    if (currentItem.partTitle == item.partTitle)
                    {
                        itemCol = "ffd900";
                    }
                    if (GUILayout.Button(String.Format("<color=#{0}>{1}</color>", itemCol, item.partTitle, "Label"), _labelStyle, GUILayout.Width(360)))
                    {
                        currentItem = item;
                    }
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();

                //*****************
                //*   ACTION WINDOW
                //*****************
                //   Part:   MyPartName
                //   Mass:   0.05 t
                //   Volume: 124 L
                //   Cost:   1500 Kc
                //   
                //   [ BUILD IT! ]
                //
                //   * =======  o
                GUILayout.BeginVertical();
                GUILayout.Label(String.Format(" "), _labelStyle, GUILayout.Width(120)); //Spacer
                





                if (GUILayout.Button("Start KonFabricator!" + currentItem.partName, GUILayout.Width(200), GUILayout.Height(30)))
                    progressBar = 0; //TODO

                GUILayout.BeginHorizontal();
                GUILayout.Box(gearTexture);
                GUILayout.Label(String.Format("READY"), _centeredLabelStyle, GUILayout.Width(100));
                GUILayout.Box(boxTexture);
                GUILayout.EndHorizontal();

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


        private void GetPartsForCategory(string catName)
        {
            var cParts = AllCargoParts
                .Where(x => x.category.ToStringCached() == catName ||
                (x.tags.ToLower().Contains("cck-" + catName)));

            catParts = new List<PartScrollbarData>();
            foreach(var p in cParts)
            {
                PartScrollbarData pd;
                pd.partName = p.name;
                pd.partTitle = p.title;
                catParts.Add(pd);
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
            _windowStyle.fixedWidth = 950f;
            _windowStyle.fixedHeight = 600f;
            _labelStyle = new GUIStyle(HighLogic.Skin.label);
            _centeredLabelStyle = new GUIStyle(HighLogic.Skin.label);
            _centeredLabelStyle.alignment = TextAnchor.MiddleCenter;
            _buttonStyle = new GUIStyle(HighLogic.Skin.button);
            _scrollStyle = new GUIStyle(HighLogic.Skin.scrollView);
            _hasInitStyles = true;
        }
    }
}