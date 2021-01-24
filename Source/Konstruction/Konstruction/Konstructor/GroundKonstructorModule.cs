using Konstruction.Utilities;
using KonstructionUI;
using KSP.Localization;
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using USITools;

namespace Konstruction
{
    public class GroundKonstructorModule : AbstractKonstructorModule
    {
        private string _notOnSurfaceErrorText;

        protected override void GetLocalizedPropertyValues()
        {
            base.GetLocalizedPropertyValues();

            if (Localizer.TryGetStringByTag(
               "#LOC_USI_Konstructor_NotOnSurfaceErrorText",
               out string notOnSurfaceErrorText))
            {
                _notOnSurfaceErrorText = notOnSurfaceErrorText;
            }
        }
        public override void SpawnVessel()
        {
            if (!GeneralUtils.IsOnSurface())
            {
                throw new Exception(_notOnSurfaceErrorText);
            }

            if (string.IsNullOrEmpty(_selectedCraftFilePath))
            {
                throw new Exception(_noVesselSelectedErrorText);
            }
            ShipConstruct construct = new ShipConstruct();
            //string craftText = File.ReadAllText(_selectedCraftFilePath);
            construct = ShipConstruction.LoadShip(_selectedCraftFilePath);// ConfigNode.Parse(craftText);
                                                                          //construct.LoadShip(craft);

            ApplyNodeVariants(construct);

            Game game = FlightDriver.FlightStateCache;
            VesselCrewManifest crew = new VesselCrewManifest();
            ShipConstruction.PutShipToGround(construct, _cachedShipTransform);
            ShipConstruction.AssembleForLaunch(construct, "", "", "", game, crew);

            var craftVessel = construct.parts[0].localRoot.GetComponent<Vessel>();

            craftVessel.launchedFrom = this.vessel.launchedFrom;

            FlightGlobals.ForceSetActiveVessel(craftVessel);
            SetCraftOrbit(craftVessel, OrbitDriver.UpdateMode.IDLE);
            //SetCraftOrbit(craftVessel, OrbitDriver.UpdateMode.IDLE);
            //builder.PostBuild(craftVessel);
            // if (builder.capture)
            //{
            //    craftVessel.Splashed = craftVessel.Landed = false;
            //}
            //else
            // {
            bool loaded = craftVessel.loaded;
            bool packed = craftVessel.packed;
            craftVessel.loaded = true;
            craftVessel.packed = false;
            // The default situation for new vessels is PRELAUNCH, but
            // that is not so good for bases because contracts check for
            // LANDED. XXX should this be selectable?
            craftVessel.situation = Vessel.Situations.LANDED;
            craftVessel.GetHeightFromTerrain();
            craftVessel.loaded = loaded;
            craftVessel.packed = packed;
            PartUtilities.ConsumeResources(_cachedCostData);
            //KSP.UI.Screens.StageManager.BeginFlight();
            //}
        }

        protected void VesselSelected2(string filePath, CraftBrowserDialog.LoadType loadType)
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

            var protoVessel = CreateProtoVessel2();
            //if (protoVessel == null)
            //{
            //    _window.ShowAlert(_invalidVesselErrorText);
            //    return;
            //}
            //else if (_hasLaunchClamp)
            //{
            //    _window.ShowAlert("launch clamp error");//_launchClampErrorText);
            //    return;
            //}
            //_cachedProtoVessel = protoVessel;

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
