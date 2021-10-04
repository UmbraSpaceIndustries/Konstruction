using KonstructionUI;
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using USITools;

namespace Konstruction
{
    [KSPScenario(
        ScenarioCreationOptions.AddToAllGames,
        GameScenes.FLIGHT)]
    public class KonstructionScenario : ScenarioModule
    {
        private static readonly List<PrefabDefinition> _prefabDefs = new List<PrefabDefinition>
        {
            new PrefabDefinition<KonstructorWindow>(PrefabType.Window),
            new PrefabDefinition<RequiredResourcePanel>(PrefabType.Prefab),
            new PrefabDefinition<ResourceTransferPanel>(PrefabType.Prefab),
            new PrefabDefinition<ResourceTransferWindow>(PrefabType.Window),
        };

        public GameObject KonstructorResourcePanelPrefab { get; private set; }
        public GameObject KonstructorWindowPrefab { get; private set; }
        public GameObject ResourceTransferPanelPrefab { get; private set; }
        public GameObject ResourceTransferWindowPrefab { get; private set; }
        public ServiceManager ServiceManager { get; private set; }

        public void LaunchVessel(Action spawnHandler)
        {
            spawnHandler.Invoke();
            var spawnedVessel = FlightGlobals.Vessels.Last();
            foreach (var part in spawnedVessel.parts)
            {
                foreach (var resource in part.Resources)
                {
                    if (resource.resourceName != "ElectricCharge")
                    {
                        resource.amount = 0d;
                    }
                }
            }
            FlightDriver.CanRevertToPostInit = false;
            FlightDriver.CanRevertToPrelaunch = false;
            FlightGlobals.SetActiveVessel(spawnedVessel);
            spawnedVessel.currentStage = -1;
            StageManager.BeginFlight();
        }

        public override void OnAwake()
        {
            base.OnAwake();

            var usiTools = USI_AddonServiceManager.Instance;
            if (usiTools != null)
            {
                ServiceManager = usiTools.ServiceManager;

                try
                {
                    // Setup dependency injection for Konstruction services
                    var serviceCollection = usiTools.ServiceCollection;
                    serviceCollection.AddSingletonService<KonstructionPersistance>();

                    // Load and register UI prefabs
                    var filePath = Path.Combine(KSPUtil.ApplicationRootPath,
                        "GameData/UmbraSpaceIndustries/Konstruction/Assets/UI/Konstruction.prefabs");

                    var prefabManager = ServiceManager.GetService<PrefabManager>();
                    prefabManager.LoadAssetBundle(filePath, _prefabDefs);
                }
                catch (ServiceAlreadyRegisteredException) { }
                catch (Exception ex)
                {
                    Debug.LogError("[KONSTRUCTION] KonstructionScenario: " + ex.Message);
                }
            }
        }

        public override void OnLoad(ConfigNode gameNode)
        {
            base.OnLoad(gameNode);

            var persister = ServiceManager.GetService<KonstructionPersistance>();
            persister.Load(gameNode);
        }

        public override void OnSave(ConfigNode gameNode)
        {
            base.OnSave(gameNode);

            var persister = ServiceManager.GetService<KonstructionPersistance>();
            persister.Save(gameNode);
        }
    }
}
