using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using KSP.UI.Screens;
using System.Collections.Generic;
using USITools;
using USITools.UITools;

namespace Konstruction.Fabrication
{

    public class FabricationGUI : Window
    {
        private KonstructionScenario _scenario;
        //private Rect _windowPosition = new Rect(300, 60, 950, 600);
        private GUIStyle _windowStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _detailStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _scrollStyle;
        private GUIStyle _centeredLabelStyle;
        private Vector2 scrollPosCat = Vector2.zero;
        private Vector2 scrollPosPart = Vector2.zero;
        public static bool renderDisplay = false;
        private static List<AvailablePart> _aParts;
        private static List<Part> _vParts;
        private static List<string> _cckTags;
        private static List<PartScrollbarData> catParts;
        private static Guid _curVesselId;
        private string currentCat = "";
        private PartScrollbarData currentItem;
        private Texture nfTexture;
        private Texture thumbTexture;
        private AvailablePart currentPart;
        private KonstructionPersistance _persistence;
        private bool _isLoaded;
        public static Dictionary<string, string> PartTextureCache;
        public const int CONST_MATKIT_RATIO = 2;
        private readonly ModuleKonFabricator _module;

        #region Constructors
        public FabricationGUI(ModuleKonFabricator partModule, KonstructionScenario scenario)
            : base("Konfabricator Control Panel",950,570)
         {
            _module = partModule;
            _scenario = scenario;
            _persistence = _scenario.ServiceManager
                    .GetService<KonstructionPersistance>();
            SetVisible(true);
        }
        #endregion

        #region GUI Logic

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginVertical();
            try
            {
                //*****************
                //*   SETUP
                //*****************
                //Calculate current values
                float printVolume = 0f;
                float printMass = 0f;

                var invModule = _module.part.FindModulesImplementing<ModuleInventoryPart>().FirstOrDefault();
                var con = GetCapacity(invModule);
                printVolume = con.VolumeAvailable;
                printMass = con.MassAvailable;

                //*********************
                //*   MAIN WORK AREA
                //*********************
                GUILayout.BeginHorizontal();
                List<string> cats = GetInventoryCategories();
                cats.AddRange(GetCCKCategories());
                cats.Sort();
                if (catParts == null)
                    catParts = new List<PartScrollbarData>();

                if (string.IsNullOrEmpty(currentCat))
                {
                    currentCat = cats[0];
                    GetPartsForCategory(currentCat);
                    currentItem = catParts[0];
                    currentPart = GetPartByName(currentItem.partName);
                }


                //*****************
                //*   CATEGORIES
                //*****************
                GUILayout.BeginVertical();
                GUILayout.Label(string.Format("<color=#ffd900>Categories</color>"), _labelStyle, GUILayout.Width(120));
                scrollPosCat = GUILayout.BeginScrollView(scrollPosCat, _scrollStyle, GUILayout.Width(160), GUILayout.Height(480));

                for (int i = 0; i < cats.Count; ++i)
                {
                    var cat = cats[i];
                    var catCol = "ffffff";
                    if (currentCat == cat)
                    {
                        catCol = "ffd900";
                    }
                    if (GUILayout.Button(string.Format("<color=#{0}>{1}</color>", catCol, cat, "Label"), _labelStyle, GUILayout.Width(120)))
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
                GUILayout.Label(string.Format("<color=#ffd900>Parts</color>"), _labelStyle, GUILayout.Width(120));
                scrollPosPart = GUILayout.BeginScrollView(scrollPosPart, _scrollStyle, GUILayout.Width(380), GUILayout.Height(480));

                foreach (var item in catParts.OrderBy(x => x.partTitle))
                {
                    var itemCol = "ffffff";
                    if (currentItem.partTitle == item.partTitle)
                    {
                        itemCol = "ffd900";
                    }
                    if (GUILayout.Button(string.Format("<color=#{0}>{1}</color>", itemCol, item.partTitle, "Label"), _labelStyle, GUILayout.Width(340)))
                    {
                        currentItem = item;
                        currentPart = GetPartByName(currentItem.partName);
                        ResetThumbTexture();
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
                //   Cost:   100 Material Kits
                //           50 Specialized Parts
                //
                //   [ BUILD IT! ]
                //
                GUILayout.BeginVertical();
                GUILayout.Label(string.Format(" "), _labelStyle, GUILayout.Width(120)); //Spacer

                if (!string.IsNullOrEmpty(currentItem.partName))
                {
                    var costData = Utilities.PartUtilities.GetPartCost(currentPart, _persistence);

                    var mvColor = "ffffff";
                    var eColor = "ffffff";
                    var valMVIn = true;
                    var valMVOut = true;
                    var valE = true;
                    var valRes = true;

                    //*********************
                    //*  VALIDATION
                    //*********************
                    valMVOut = IsSlotAvailable(currentPart);
                    valMVIn = IsPrinterAvailable(currentPart, printMass, printVolume);
                    valE = Utilities.CrewUtilities.DoesVesselHaveCrewType("Engineer");

                    if (!valMVIn)
                        mvColor = "ff6e69";
                    if (!valMVOut)
                        mvColor = "ff6e69";

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(string.Format(currentItem.partTitle), _labelStyle, GUILayout.Width(300));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(string.Format("<color=#ffd900>Mass:</color>"), _labelStyle, GUILayout.Width(60));
                    GUILayout.Label(string.Format("<color=#{0}>{1}/{2} t</color>", mvColor, currentPart.partPrefab.mass - currentPart.partPrefab.resourceMass, printMass), _labelStyle, GUILayout.Width(200));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(string.Format("<color=#ffd900>Volume:</color>"), _labelStyle, GUILayout.Width(60));
                    GUILayout.Label(string.Format("<color=#{0}>{1}/{2} L</color>", mvColor, currentPart.partPrefab.FindModuleImplementing<ModuleCargoPart>().packedVolume, printVolume), _labelStyle, GUILayout.Width(200));
                    GUILayout.EndHorizontal();

                    foreach (var cost in costData)
                    {
                        var valThisRes = Utilities.PartUtilities.ResourcesExist(cost.Resource.name, cost.Quantity);
                        var resColor = "ffffff";
                        if (!valThisRes)
                        {
                            resColor = "ff6e69";
                            valRes = false;
                        }
                        var qoh = Utilities.PartUtilities.GetResourceQty(cost.Resource.name);
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(String.Format("<color=#{0}>{1} {2}/{3}</color>", resColor, cost.Resource.name, qoh, cost.Quantity), _detailStyle, GUILayout.Width(250));
                        GUILayout.EndHorizontal();
                    }

                    GUILayout.Box(thumbTexture, GUILayout.Height(100));

                    if (valRes && valMVIn && valMVOut)
                    {
                        if (GUILayout.Button("Start KonFabricator!", GUILayout.Width(300), GUILayout.Height(50)))
                            BuildAThing(currentItem.partName);
                    }
                    if (!valMVIn)
                        GUILayout.Label(string.Format("<color=#ff6e69>Engineer not present in active vessel.</color>"), _labelStyle, GUILayout.Width(350));
                    if (!valMVIn)
                        GUILayout.Label(string.Format("<color=#ff6e69>Insufficient KonFabricator capacity to build this part.</color>"), _labelStyle, GUILayout.Width(350));
                    if (!valMVOut)
                        GUILayout.Label(string.Format("<color=#ff6e69>Cannot find an inventory slot that will fit this part.</color>"), _labelStyle, GUILayout.Width(350));
                    if (!valRes)
                        GUILayout.Label(string.Format("<color=#ff6e69>Insufficient resources.</color>"), _labelStyle, GUILayout.Width(350));

                    GUILayout.EndVertical();
                }
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

        #endregion


        #region Fabrication Logic

        private string GetThumbFile()
        {
            var path = Path.GetFullPath(Path.Combine(KSPUtil.ApplicationRootPath, "GameData"));
            var files = Directory.GetFiles(path, currentPart.name + "_icon*.png", SearchOption.AllDirectories);
            var thumbFile = files.Where(f => f.Contains("@thumbs")).FirstOrDefault();
            return thumbFile;
        }

        private void ResetThumbTexture()
        {
            if (PartTextureCache == null)
                PartTextureCache = new Dictionary<string, string>();

            //Is our thumb in cache?
            if(!PartTextureCache.ContainsKey(currentPart.name))
            {
                var thumbFile = GetThumbFile();
                PartTextureCache.Add(currentPart.name, thumbFile);
            }

            var thumb = PartTextureCache[currentPart.name];
            if(!string.IsNullOrEmpty(thumb))
            {
                thumbTexture = LoadTexture(thumb);
            }
            else
            {
                thumbTexture = nfTexture;
            }
        }

        public IEnumerable<Part> VesselInventoryParts
        {
            get
            {
                if(_vParts == null || _curVesselId != FlightGlobals.ActiveVessel.id)
                {
                    _curVesselId = FlightGlobals.ActiveVessel.id;
                    _vParts = FlightGlobals.ActiveVessel.parts.Where(x=>x.HasModuleImplementing<ModuleInventoryPart>()).ToList();
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

        public List<string> GetCCKCategories()
        {
            if (_cckTags == null)
            {
                _cckTags = new List<string>();
                foreach(var part in AllCargoParts)
                {
                    var tag = part.tags.ToLower().Split(' ').Where(t=>t.StartsWith("cck-")).FirstOrDefault();
                    if (string.IsNullOrEmpty(tag))
                        continue;

                    var newTag = tag.Substring(4);
                    if (!_cckTags.Contains(newTag))
                    _cckTags.Add(newTag);
                }
            }
            return _cckTags;
        }

        private Texture2D LoadTexture(string fullName)
        {
            var texture = new Texture2D(36, 36, TextureFormat.RGBA32, false);
            Debug.Log("Loading " + fullName);
            texture.LoadImage(File.ReadAllBytes(fullName));
            return texture;
        }

        string ColorToHex(Color32 color)
        {
            string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
            return hex;
        }

        private AvailablePart GetPartByName(string partName)
        {
            var p = AllCargoParts.Where(x => x.name == partName).FirstOrDefault();
            if (p != null)
                return p;
            return null;
        }

        private bool IsPrinterAvailable(AvailablePart part, float mass, float volume)
        {
            var modCP = part.partPrefab.FindModuleImplementing<ModuleCargoPart>();
            if (part.partPrefab.mass > mass)
                return false;

            if (modCP.packedVolume > volume)
                return false;

            return true;
        }

        private bool IsSlotAvailable(AvailablePart part)
        {
            var modCP = part.partPrefab.FindModuleImplementing<ModuleCargoPart>();

            foreach (var p in VesselInventoryParts)
            {
                var inv = p.FindModuleImplementing<ModuleInventoryPart>();

                if (inv.TotalEmptySlots() > 0)
                {
                    var con = GetCapacity(inv);
                    if (con.MassAvailable < part.partPrefab.mass)
                        continue;

                    if (con.VolumeAvailable >= modCP.packedVolume)
                    {
                        for (int z = 0; z < inv.InventorySlots; z++)
                        {
                            if (inv.IsSlotEmpty(z))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private bool BuildAThing(string name)
        {
            var iPart = GetPartByName(name);
            if (iPart == null)
                return false;

            var modCP = iPart.partPrefab.FindModuleImplementing<ModuleCargoPart>();

            foreach (var p in VesselInventoryParts)
            {
                var inv = p.FindModuleImplementing<ModuleInventoryPart>();

                if (inv.TotalEmptySlots() > 0)
                {
                    var con = GetCapacity(inv);
                    if (con.MassAvailable < iPart.partPrefab.mass)
                        continue;

                    if (con.VolumeAvailable >= modCP.packedVolume)
                    {
                        for (int z = 0; z < inv.InventorySlots; z++)
                        {
                            if (inv.IsSlotEmpty(z))
                            {
                                Utilities.PartUtilities.ConsumeResources(Utilities.PartUtilities.GetPartCost(iPart, _persistence));
                                foreach (var r in iPart.partPrefab.Resources)
                                {
                                    r.amount = 0;
                                }
                                inv.StoreCargoPartAtSlot(iPart.partPrefab, z);
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
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
                    totVol += invPart.partPrefab.FindModuleImplementing<ModuleCargoPart>().packedVolume;
                    totMass += invPart.partPrefab.mass;
                    totMass += invPart.partPrefab.resourceMass;
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

        private AvailablePart LoadPart(string partName)
        {
            var p = AllCargoParts.Where(x => x.name == partName).Single();
            return p;
        }

        private void GetPartsForCategory(string catName)
        {
            var cParts = AllCargoParts
                .Where(x => x.category.ToStringCached() == catName ||
                (x.tags.ToLower().Contains("cck-" + catName)));

            catParts = new List<PartScrollbarData>();
            foreach (var p in cParts)
            {
                PartScrollbarData pd;
                pd.partName = p.name;
                pd.partTitle = p.title;
                catParts.Add(pd);
            }
        }

        #endregion

        void Awake()
        {
            //Load up some default textures
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            nfTexture = LoadTexture(Path.Combine(path, "notfound.png"));
            thumbTexture = LoadTexture(Path.Combine(path, "notfound.png"));
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