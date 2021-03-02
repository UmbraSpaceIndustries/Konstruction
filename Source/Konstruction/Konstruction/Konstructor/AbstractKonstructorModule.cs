using Konstruction.Utilities;
using KonstructionUI;
using KSP.Localization;
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using USITools;

namespace Konstruction
{
    public abstract class AbstractKonstructorModule : PartModule, IKonstructor
    {
        protected List<CostData> _cachedCostData;
        protected float _cachedDryMass;
        protected float _cachedFundsCost;
        protected ProtoVessel _cachedProtoVessel;
        protected ShipConstruct _cachedShipConstruct;
        protected Dictionary<string, KonstructorResourceMetadata> _cachedResources;
        protected Texture2D _cachedThumbnail;
        protected ConfigNode _craftConfigNode;
        protected string _dryCostText;
        protected string _dryMassText;
        protected bool _hasLaunchClamp;
        protected string _invalidVesselErrorText;
        private double _nextRefreshTime;
        protected string _noVesselSelectedErrorText;
        protected string _selectedCraftFilePath;
        protected KonstructionScenario _scenario;
        protected ShipThumbnailService _thumbnailService;
        protected string _unavailablePartsErrorText;
        protected KonstructorWindow _window;

        public string AvailableAmountHeaderText { get; private set; }
        public string BuildShipButtonText { get; private set; }
        public Canvas Canvas
        {
            get
            {
                return MainCanvasUtil.MainCanvas;
            }
        }
        public string Column1HeaderText { get; private set; }
        public string Column2HeaderText { get; private set; }
        public string Column3HeaderText { get; private set; }
        public string Column1Instructions { get; private set; }
        public string Column2Instructions { get; private set; }
        public string Column3Instructions { get; private set; }
        public string InsufficientResourcesErrorText { get; private set; }
        public string RequiredAmountHeaderText { get; private set; }
        public string ResourceHeaderText { get; private set; }
        public string SelectShipButtonText { get; private set; }
        public string SelectedShipHeaderText { get; private set; }
        public string TitleBarText { get; private set; }

        [KSPEvent(
            guiName = "#LOC_USI_Konstructor_ShowUIButtonText",
            active = true,
            guiActive = true,
            guiActiveEditor = false)]
        public void ShowWindow()
        {
            if (_window != null)
            {
                _window.ShowWindow();
            }
        }

        protected ProtoVessel CreateProtoVessel()
        {
            ProtoVessel protoVessel = null;
            ShipConstruct construct = null;
            Vessel vessel = null;
            try
            {
                // Backup the ship config from the VAB/SPH, load the selected .craft file
                //   and restore the cached config from the VAB/SPH
                var constructBak = ShipConstruction.ShipConfig;
                construct = ShipConstruction.LoadShip(_selectedCraftFilePath);
                ShipConstruction.ShipConfig = constructBak;

                // Calculate vessel cost and mass and generate a thumbnail
                construct.GetShipCosts(out _cachedFundsCost, out _);
                construct.GetShipMass(out _cachedDryMass, out _);
                _cachedThumbnail = _thumbnailService.GetThumbnail(construct);

                // Create an emtpy Vessel and copy the parts from the loaded .craft file
                vessel = new GameObject().AddComponent<Vessel>();
                vessel.parts = construct.parts;

                // Create an empty ProtoVessel that we'll ultimately use to create the template
                //   for the vessel to be spawned in-game later
                protoVessel = new ProtoVessel(new ConfigNode(), null)
                {
                    vesselName = construct.shipName,
                    vesselRef = vessel
                };

                // Setup necessary Vessel and Part parameters for the template (also check for launch clamps)
                var launchId = HighLogic.CurrentGame.launchID++;
                var missionId = (uint)Guid.NewGuid().GetHashCode();
                var rootPart = construct.parts.First();
                _hasLaunchClamp = false;
                foreach (var part in construct.parts)
                {
                    _hasLaunchClamp |= part.HasModuleImplementing<LaunchClamp>();

                    part.flagURL = construct.missionFlag ?? HighLogic.CurrentGame.flagURL;
                    part.flightID = ShipConstruction.GetUniqueFlightID(HighLogic.CurrentGame.flightState);
                    part.launchID = launchId;
                    part.missionID = missionId;
                    part.temperature = Math.Abs(part.temperature);
                    part.UpdateOrgPosAndRot(rootPart);

                    part.vessel = vessel;
                    var partSnapshot = new ProtoPartSnapshot(part, protoVessel);
                    foreach (var resource in partSnapshot.resources)
                    {
                        if (resource.resourceName != "ElectricCharge")
                        {
                            resource.amount = 0d;
                        }
                    }
                    protoVessel.protoPartSnapshots.Add(partSnapshot);
                }
                foreach (var snapshot in protoVessel.protoPartSnapshots)
                {
                    snapshot.storePartRefs();
                }

                // Cache the ProtoVessel to use as the template for spawning the vessel later
                _cachedProtoVessel = protoVessel;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                // ShipConstruction.LoadShip seems to load in all the part meshes for the vessel
                //   (presumably for use in the VAB/SPH), so we need to destroy them
                if (construct != null && construct.parts != null && construct.parts.Count > 0)
                {
                    foreach (var part in construct.parts)
                    {
                        Destroy(part.gameObject);
                    }
                }
                // Destroy the temporary Vessel we created as well
                if (vessel != null)
                {
                    Destroy(vessel.gameObject);
                }
            }

            return protoVessel;
        }

        protected virtual void GetLocalizedPropertyValues()
        {
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_Konstructor_AvailableAmountHeaderText",
                out string availableAmountHeaderText))
            {
                AvailableAmountHeaderText = availableAmountHeaderText;
            }
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_Konstructor_BuildShipButtonText",
                out string buildShipButtonText))
            {
                BuildShipButtonText = buildShipButtonText;
            }
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_Konstructor_Column1HeaderText",
                out string column1HeaderText))
            {
                Column1HeaderText = column1HeaderText;
            }
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_Konstructor_Column2HeaderText",
                out string column2HeaderText))
            {
                Column2HeaderText = column2HeaderText;
            }
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_Konstructor_Column3HeaderText",
                out string column3HeaderText))
            {
                Column3HeaderText = column3HeaderText;
            }
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_Konstructor_Column1Instructions",
                out string column1Instructions))
            {
                Column1Instructions = column1Instructions;
            }
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_Konstructor_Column2Instructions",
                out string column2Instructions))
            {
                Column2Instructions = column2Instructions;
            }
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_Konstructor_Column3Instructions",
                out string column3Instructions))
            {
                Column3Instructions = column3Instructions;
            }
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_Konstructor_DryCostText",
                out string dryCostText))
            {
                _dryCostText = dryCostText;
            }
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_Konstructor_DryMassText",
                out string dryMassText))
            {
                _dryMassText = dryMassText;
            }
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_Konstructor_InsufficientResourcesErrorText",
                out string insufficientResourcesErrorText))
            {
                InsufficientResourcesErrorText = insufficientResourcesErrorText;
            }
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_Konstructor_InvalidVesselErrorText",
                out string invalidVesselErrorText))
            {
                _invalidVesselErrorText = invalidVesselErrorText;
            }
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_Konstructor_NoVesselSelectedErrorText",
                out string noVesselSelectedErrorText))
            {
                _noVesselSelectedErrorText = noVesselSelectedErrorText;
            }
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_Konstructor_RequiredAmountHeaderText",
                out string requiredAmountHeaderText))
            {
                RequiredAmountHeaderText = requiredAmountHeaderText;
            }
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_Konstructor_ResourceHeaderText",
                out string resourceHeaderText))
            {
                ResourceHeaderText = resourceHeaderText;
            }
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_Konstructor_SelectShipButtonText",
                out string selectShipButtonText))
            {
                SelectShipButtonText = selectShipButtonText;
            }
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_Konstructor_SelectedShipHeaderText",
                out string selectedShipHeaderText))
            {
                SelectedShipHeaderText = selectedShipHeaderText;
            }
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_Konstructor_ShowUIButtonText",
                out string showUIButtonText))
            {
                Events["ShowWindow"].guiName = showUIButtonText;
            }
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_Konstructor_TitleBarText",
                out string titleBarText))
            {
                TitleBarText = titleBarText;
            }
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_Konstructor_UnavailablePartsErrorText",
                out string unavailablePartsErrorText))
            {
                _unavailablePartsErrorText = unavailablePartsErrorText;
            }
        }

        protected List<KonstructorResourceMetadata> GetResourceCosts()
        {
            if (_cachedProtoVessel == null)
            {
                return null;
            }

            if (_cachedCostData == null)
            {
                var availableParts = PartLoader.LoadedPartsList;
                var persistenceLayer = _scenario.ServiceManager
                    .GetService<KonstructionPersistance>();
                var costData = new List<CostData>();
                foreach (var partSnapshot in _cachedProtoVessel.protoPartSnapshots)
                {
                    var part = availableParts.FirstOrDefault(p => p.name == partSnapshot.partName);
                    if (part != null)
                    {
                        var costs = PartUtilities.GetPartCost(part, persistenceLayer);
                        costData.AddRange(costs);
                    }
                }
                _cachedCostData = costData
                     .GroupBy(c => c.Resource.name)
                     .Select(g =>
                     {
                         var resource = g.First().Resource;
                         var quantity = g.Sum(c => c.Quantity);
                         return new CostData
                         {
                             Quantity = quantity,
                             Resource = resource,
                         };
                     })
                     .ToList();
            }
            if (_cachedResources == null)
            {
                _cachedResources = _cachedCostData
                    .Select(c =>
                    {
                        var available = PartUtilities.GetResourceQty(c.Resource.name);
                        return new KonstructorResourceMetadata(c.Resource.displayName, available, c.Quantity);
                    })
                    .ToDictionary(c => c.Name);
            }
            else
            {
                foreach (var resource in _cachedResources)
                {
                    resource.Value.Available = PartUtilities.GetResourceQty(resource.Key);
                }
            }

            _nextRefreshTime = Planetarium.GetUniversalTime() + 1d;
            
            return _cachedResources.Select(r => r.Value).ToList();
        }

        public abstract void LaunchVessel();

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            GetLocalizedPropertyValues();

            _scenario = FindObjectOfType<KonstructionScenario>();
            if (_scenario != null)
            {
                _thumbnailService = _scenario.ServiceManager.GetService<ShipThumbnailService>();
                var windowManager = _scenario.ServiceManager.GetService<WindowManager>();
                _window = windowManager.GetWindow<KonstructorWindow>();

                _window.Initialize(this, windowManager);
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (_cachedResources == null ||
                _nextRefreshTime > Planetarium.GetUniversalTime() ||
                (TimeWarp.CurrentRate > 1 && TimeWarp.WarpMode == TimeWarp.Modes.HIGH))
            {
                return;
            }

            GetResourceCosts();
            if (_window != null)
            {
                _window.UpdateResources();
            }
        }

        public void ShowShipSelector()
        {
            CraftBrowserDialog.Spawn(
                facility: EditorFacility.VAB,
                profile: HighLogic.SaveFolder,
                onFileSelected: VesselSelected,
                onCancel: () => { },
                showMergeOption: false);
        }

        protected abstract void VesselSelected(string filePath, CraftBrowserDialog.LoadType loadType);
    }
}
