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
    [KSPModule("Konstructor")]
    public class KonstructorModule : PartModule, IKonstructor
    {
        private List<CostData> _cachedCostData;
        private float _cachedDryMass;
        private float _cachedFundsCost;
        private ProtoVessel _cachedProtoVessel;
        private Dictionary<string, KonstructorResourceMetadata> _cachedResources;
        private Texture2D _cachedThumbnail;
        private ConfigNode _craftConfigNode;
        private string _dryCostText;
        private string _dryMassText;
        private bool _hasLaunchClamp;
        private string _invalidVesselErrorText;
        private string _launchClampErrorText;
        private string _nearbyVesselsErrorText;
        private double _nextRefreshTime;
        private string _notInOrbitErrorText;
        private string _noVesselSelectedErrorText;
        private string _selectedCraftFilePath;
        private KonstructionScenario _scenario;
        private ThumbnailService _thumbnailService;
        private string _unavailablePartsErrorText;
        private KonstructorWindow _window;

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

        private ProtoVessel CreateProtoVessel()
        {
            var cachedConstruct = ShipConstruction.ShipConfig;

            var construct = ShipConstruction.LoadShip(_selectedCraftFilePath);
            construct.GetShipCosts(out _cachedFundsCost, out _);
            construct.GetShipMass(out _cachedDryMass, out _);
            _cachedThumbnail = _thumbnailService.GetShipThumbnail(construct);

            var vessel = new GameObject().AddComponent<Vessel>();
            vessel.parts = construct.parts;

            var protoVessel = new ProtoVessel(new ConfigNode(), null)
            {
                vesselName = construct.shipName,
                vesselRef = vessel
            };

            ShipConstruction.ShipConfig = cachedConstruct;

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
                protoVessel.protoPartSnapshots.Add(new ProtoPartSnapshot(part, protoVessel));

                Destroy(part.gameObject);
            }
            foreach (var snapshot in protoVessel.protoPartSnapshots)
            {
                snapshot.storePartRefs();
            }
            Destroy(vessel.gameObject);

            _cachedProtoVessel = protoVessel;
            return protoVessel;
        }

        private void GetLocalizedPropertyValues()
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
                "#LOC_USI_Konstructor_LaunchClampErrorText",
                out string launchClampErrorText))
            {
                _launchClampErrorText = launchClampErrorText;
            }
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_Konstructor_NearbyVesselsErrorText",
                out string nearbyVesselsErrorText))
            {
                _nearbyVesselsErrorText = nearbyVesselsErrorText;
            }
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_Konstructor_NotInOrbitErrorText",
                out string notInOrbitErrorText))
            {
                _notInOrbitErrorText = notInOrbitErrorText;
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

        private List<KonstructorResourceMetadata> GetResourceCosts()
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

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            GetLocalizedPropertyValues();

            _scenario = FindObjectOfType<KonstructionScenario>();
            if (_scenario != null)
            {
                _thumbnailService = _scenario.ServiceManager.GetService<ThumbnailService>();
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

        public void SpawnVessel()
        {
            if (FlightGlobals.ActiveVessel.situation != Vessel.Situations.ORBITING)
            {
                throw new Exception(_notInOrbitErrorText);
            }

            if (string.IsNullOrEmpty(_selectedCraftFilePath))
            {
                throw new Exception(_noVesselSelectedErrorText);
            }

            if (LogisticsTools.AnyNearbyVessels(100d, FlightGlobals.ActiveVessel))
            {
                throw new Exception(_nearbyVesselsErrorText);
            }

            PartUtilities.ConsumeResources(_cachedCostData);

            var vesselOrbit = FlightGlobals.ActiveVessel.orbit;
            var now = Planetarium.GetUniversalTime();
            vesselOrbit.GetOrbitalStateVectorsAtUT(
                now,
                out Vector3d position,
                out Vector3d velocity);
            position.x += 50d;
            var orbit = new Orbit(vesselOrbit);
            orbit.UpdateFromStateVectors(
                position,
                velocity,
                vesselOrbit.referenceBody,
                now);

            var partNodes = _cachedProtoVessel.protoPartSnapshots
                .Select(s =>
                {
                    var node = new ConfigNode("PART");
                    s.Save(node);
                    return node;
                })
                .ToArray();
            var type = VesselType.Ship;
            _craftConfigNode.TryGetEnum("type", ref type, VesselType.Ship);

            var vesselConfigNode = ProtoVessel.CreateVesselNode(
                _cachedProtoVessel.GetDisplayName(),
                type,
                orbit,
                0,
                partNodes);

            var spawnedProtoVessel = new ProtoVessel(vesselConfigNode, HighLogic.CurrentGame);
            spawnedProtoVessel.Load(HighLogic.CurrentGame.flightState);

            var spawnedVessel = FlightGlobals.Vessels.Last();
            spawnedVessel.currentStage = 1;

            _window.CloseWindow();
        }

        private void VesselSelected(string filePath, CraftBrowserDialog.LoadType loadType)
        {
            _selectedCraftFilePath = filePath;
            _craftConfigNode = ConfigNode.Load(filePath);
            if (_craftConfigNode == null || _craftConfigNode.CountNodes < 1)
            {
                _window.ShowAlert(_invalidVesselErrorText);
                return;
            }

            var error = string.Empty;
            if (!ShipConstruction.AllPartsFound(_craftConfigNode, ref error))
            {
                Debug.LogError($"[KONSTRUCTION] Failed to load vessel at {_selectedCraftFilePath}");
                Debug.LogError($"[KONSTRUCTION] {error}");
                _window.ShowAlert(_unavailablePartsErrorText);
                return;
            }

            var protoVessel = CreateProtoVessel();
            if (protoVessel == null)
            {
                _window.ShowAlert(_invalidVesselErrorText);
                return;
            }
            else if (_hasLaunchClamp)
            {
                _window.ShowAlert(_launchClampErrorText);
                return;
            }
            _cachedProtoVessel = protoVessel;
            _cachedCostData = null;

            var partResources = GetResourceCosts();

            var konstructorMetadata = new KonstructorMetadata(partResources);
            var shipName = Localizer.Format(_craftConfigNode.GetValue("ship"));
            var shipMetadata = new ShipMetadata(
                shipName,
                $"{_dryMassText}: {_cachedDryMass:N1} t",
                $"{_dryCostText}: {_cachedFundsCost:N0}",
                konstructorMetadata,
                _cachedThumbnail);
            _window.ShipSelected(shipMetadata);
        }
    }
}
