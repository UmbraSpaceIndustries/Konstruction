using KonstructionUI;
using KSP.Localization;
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using USITools;

namespace Konstruction
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT)]
    public class ResourceTransferScenario : ScenarioModule, ITransferTargetsController
    {
        private static readonly List<string> BLACKLIST = new List<string>
        {
            "ResourceLode",
        };

        private string _insufficientTargetsMessage
            = "#LOC_USI_ResourceTransfers_InsufficientTransferTargetsMessage";
        private double _nextLazyUpdate;
        private ServiceManager _serviceManager;
        private ApplicationLauncherButton _toolbarButton;
        private ResourceTransferWindow _window;

        public string CurrentVesselText { get; private set; }
            = "#LOC_USI_ResourceTransfers_CurrentVesselText";
        public string DropdownDefaultText { get; private set; }
            = "#LOC_USI_ResourceTransfers_DropdownDefaultText";
        public string InsufficientTransferTargetsMessage
        {
            get
            {
                if (_insufficientTargetsMessage.StartsWith("#"))
                {
                    return _insufficientTargetsMessage;
                }
                return string.Format(
                    _insufficientTargetsMessage,
                    Konstruction_GameParameters.ResourceTransferAllowedRadius.ToString("N0"));
            }
        }
        public string Row1HeaderLabel { get; private set; }
            = "#LOC_USI_ResourceTransfers_Row1HeaderLabel";
        public string Row2HeaderLabel { get; private set; }
            = "#LOC_USI_ResourceTransfers_Row2HeaderLabel";
        public string SameVesselSelectedMessage { get; private set; }
            = "#LOC_USI_ResourceTransfers_SameVesselSelectedMessage";
        public string TitleBarText { get; private set; }
            = "#LOC_USI_ResourceTransfers_TitleBarText";

        public Canvas Canvas => MainCanvasUtil.MainCanvas;

        public void CloseWindow()
        {
            if (_window != null)
            {
                _window.CloseWindow();
            }
        }

        private void GetLocalizedLabels()
        {
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_ResourceTransfers_CurrentVesselText",
                out string currentVesselText))
            {
                CurrentVesselText = currentVesselText;
            }
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_ResourceTransfers_DropdownDefaultText",
                out string dropdownDefaultText))
            {
                DropdownDefaultText = dropdownDefaultText;
            }
            Localizer.TryGetStringByTag(
                "#LOC_USI_ResourceTransfers_InsufficientTransferTargetsMessage",
                out _insufficientTargetsMessage);
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_ResourceTransfers_Row1HeaderLabel",
                out string row1HeaderLabel))
            {
                Row1HeaderLabel = row1HeaderLabel;
            }
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_ResourceTransfers_Row2HeaderLabel",
                out string row2HeaderLabel))
            {
                Row2HeaderLabel = row2HeaderLabel;
            }
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_ResourceTransfers_SameVesselSelectedMessage",
                out string sameVesselSelectedMessage))
            {
                SameVesselSelectedMessage = sameVesselSelectedMessage;
            }
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_ResourceTransfers_TitleBarText",
                out string titleBarText))
            {
                TitleBarText = titleBarText;
            }
        }

        public List<ResourceTransferTargetMetadata> GetResourceTransferTargets()
        {
            var nearbyVessels = LogisticsTools.GetNearbyVessels(
                Konstruction_GameParameters.ResourceTransferAllowedRadius,
                true,
                FlightGlobals.ActiveVessel,
                false);

            return nearbyVessels
                .Where(v => v.Parts.Any(p => p.Resources.Any(r =>
                    !BLACKLIST.Contains(r.resourceName) &&
                    ResourceTransferTarget.IsTransferable(r))))
                .Select(v => new ResourceTransferTargetMetadata
                {
                    DisplayName = GetVesselDisplayName(v),
                    Id = v.id.ToString("N"),
                    IsCurrentVessel = v == FlightGlobals.ActiveVessel,
                    Resources = ResourceTransferTarget.GetResourceMetadata(v.Parts),
                })
                .ToList();
        }

        public Dictionary<string, IResourceTransferController> GetResourceTransferControllers(
            ResourceTransferTargetMetadata targetA,
            ResourceTransferTargetMetadata targetB)
        {
            var resourcesInCommon = targetA.Resources.Keys
                .Intersect(targetB.Resources.Keys)
                .OrderBy(r => r);

            var vesselA = FlightGlobals.Vessels
                .FirstOrDefault(v => v.id.ToString("N") == targetA.Id);
            var vesselB = FlightGlobals.Vessels
                .FirstOrDefault(v => v.id.ToString("N") == targetB.Id);
            var resourceTargetA = new ResourceTransferTarget(vesselA);
            var resourceTargetB = new ResourceTransferTarget(vesselB);

            var controllers = new Dictionary<string, IResourceTransferController>();
            foreach (var resource in resourcesInCommon)
            {
                controllers.Add(
                    resource,
                    new ResourceTransferController(resourceTargetA, resourceTargetB, resource));
            }
            return controllers;
        }

        private string GetVesselDisplayName(Vessel vessel)
        {
            var displayName = vessel.GetDisplayName();
            if (FlightGlobals.ActiveVessel == vessel)
            {
                displayName += $" {CurrentVesselText}";
            }
            return displayName;
        }

        private void LazyUpdate(float deltaTime)
        {
            var targets = GetResourceTransferTargets();
            _window.OnResourceTransferTargetsUpdated(targets, deltaTime);
        }

        public override void OnAwake()
        {
            base.OnAwake();

            GetLocalizedLabels();
        }

        private void Start()
        {
            var usiTools = USI_AddonServiceManager.Instance;
            if (usiTools != null)
            {
                _serviceManager = usiTools.ServiceManager;

                try
                {
                    var windowManager = _serviceManager.GetService<WindowManager>();
                    _window = windowManager.GetWindow<ResourceTransferWindow>();
                    _window.Initialize(this, windowManager, () =>
                    {
                        if (_toolbarButton != null)
                        {
                            // The app launcher button behaves like a toggle and will
                            //  remain in the 'on' state if the window closes itself
                            //  so we need to reset the button when that happens
                            _toolbarButton.SetFalse(false);
                        }
                    });

                    // Do an initial update before the Update method takes over
                    LazyUpdate(0f);
                    _nextLazyUpdate = Planetarium.GetUniversalTime() + 1d;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Konstruction] {ClassName}: {ex.Message}");
                    enabled = false;
                    return;
                }
            }

            // Create toolbar button
            var textureService = _serviceManager.GetService<TextureService>();
            var toolbarIcon = textureService.GetTexture(
                "GameData/UmbraSpaceIndustries/Konstruction/Assets/UI/Logistics_36x36.png", 36, 36);
            var showInScenes = ApplicationLauncher.AppScenes.FLIGHT;
            _toolbarButton = ApplicationLauncher.Instance.AddModApplication(
                ShowWindow,
                CloseWindow,
                null,
                null,
                null,
                null,
                showInScenes,
                toolbarIcon);
        }

        /// <summary>
        /// Implementation of <see cref="MonoBehaviour"/>.OnDestroy().
        /// </summary>
        [SuppressMessage("CodeQuality",
            "IDE0051:Remove unused private members",
            Justification = "Because MonoBehaviour")]
        private void OnDestroy()
        {
            if (_toolbarButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(_toolbarButton);
                _toolbarButton = null;
            }
        }

        public void ShowWindow()
        {
            if (_window != null)
            {
                _window.ShowWindow();
            }
        }

        /// <summary>
        /// Implementation of <see cref="MonoBehaviour"/>.Update().
        /// </summary>
        [SuppressMessage("CodeQuality",
            "IDE0051:Remove unused private members",
            Justification = "Because MonoBehaviour")]
        private void Update()
        {
            if (_window.gameObject.activeSelf &&
                TimeWarp.CurrentRateIndex == 0)
            {
                var now = Planetarium.GetUniversalTime();
                if (_nextLazyUpdate <= now)
                {
                    LazyUpdate(0.5f);
                    _nextLazyUpdate = now + 0.5d;
                }
            }
        }
    }
}
