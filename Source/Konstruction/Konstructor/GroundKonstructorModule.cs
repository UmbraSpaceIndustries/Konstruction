using Konstruction.Utilities;
using KonstructionUI;
using KSP.Localization;
using KSP.UI.Screens;
using System;
using System.Linq;
using UnityEngine;

namespace Konstruction
{
    public class GroundKonstructorModule : AbstractKonstructorModule
    {
        private string _notLandedErrorText;

        protected override void GetLocalizedPropertyValues()
        {
            base.GetLocalizedPropertyValues();

            _notLandedErrorText = "Shipyard must be landed to launch new vessels!";
        }

        public override void LaunchVessel()
        {
            // This is a little weird but we need to hand control over from the partmodule to the
            //   scenariomodule since we will be switching vessels and need to do a few things with
            //   the spawned vessel after it launches
            _scenario.LaunchVessel(SpawnVessel);
        }

        public void SpawnVessel()
        {
            //var allowedSituations = Vessel.Situations.LANDED | Vessel.Situations.PRELAUNCH | Vessel.Situations.SPLASHED;
            //if (allowedSituations != (allowedSituations & FlightGlobals.ActiveVessel.situation))
            //{
            //    throw new Exception(_notLandedErrorText);
            //}

            if (string.IsNullOrEmpty(_selectedCraftFilePath))
            {
                throw new Exception(_noVesselSelectedErrorText);
            }

            PartUtilities.ConsumeResources(_cachedCostData);

            // Backup the ship config from the VAB/SPH, load the selected .craft file
            //   and restore the cached config from the VAB/SPH
            var constructBak = ShipConstruction.ShipConfig;
            var construct = ShipConstruction.LoadShip(_selectedCraftFilePath);
            ShipConstruction.ShipConfig = constructBak;

            ShipConstruction.PutShipToGround(construct, transform);
            ShipConstruction.AssembleForLaunch(
                construct,
                vessel.landedAt,
                vessel.displaylandedAt,
                construct.missionFlag,
                HighLogic.CurrentGame,
                new VesselCrewManifest());

            _window.CloseWindow();
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
