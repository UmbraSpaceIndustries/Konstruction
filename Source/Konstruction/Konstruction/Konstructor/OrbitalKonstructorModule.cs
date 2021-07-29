using Konstruction.Utilities;
using KonstructionUI;
using KSP.Localization;
using KSP.UI.Screens;
using System;
using System.Linq;
using UnityEngine;
using USITools;

namespace Konstruction
{
    public class OrbitalKonstructorModule : AbstractKonstructorModule
    {
        private const double KEEPOUT_ZONE_RADIUS = 200d;
        private const double SPAWN_LOCATION_OFFSET = 100d;

        private string _launchClampErrorText;
        private string _nearbyVesselsErrorText;
        private string _notInOrbitErrorText;

        protected override void GetLocalizedPropertyValues()
        {
            base.GetLocalizedPropertyValues();

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
        }

        public override void LaunchVessel()
        {
            if (FlightGlobals.ActiveVessel.situation != Vessel.Situations.ORBITING)
            {
                throw new Exception(_notInOrbitErrorText);
            }

            if (string.IsNullOrEmpty(_selectedCraftFilePath))
            {
                throw new Exception(_noVesselSelectedErrorText);
            }

            if (LogisticsTools.AnyNearbyVessels(KEEPOUT_ZONE_RADIUS, FlightGlobals.ActiveVessel))
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
            position.x += SPAWN_LOCATION_OFFSET;
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

            _window.CloseWindow();

            var spawnedVessel = FlightGlobals.Vessels.Last();
            spawnedVessel.protoVessel.stage = int.MaxValue;
            FlightGlobals.SetActiveVessel(spawnedVessel);
        }

        protected override void VesselSelected(string filePath, CraftBrowserDialog.LoadType loadType)
        {
            if (filePath == _selectedCraftFilePath)
            {
                return;
            }

            // Clear out previously selected vessel
            _cachedCostData = null;
            _cachedResources = null;
            _window.ShipSelected(null);

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
