using KonstructionUI;
using System;
using UnityEngine;
using USITools;

namespace Konstruction
{
    [KSPScenario(
        ScenarioCreationOptions.AddToAllGames,
        GameScenes.FLIGHT)]
    public class KonstructionScenario : ScenarioModule
    {
        public GameObject KonstructorResourcePanelPrefab { get; private set; }
        public GameObject KonstructorWindowPrefab { get; private set; }
        public ServiceManager ServiceManager { get; private set; }

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

                    // Setup UI prefabs
                    var filePath = KSPUtil.ApplicationRootPath + "GameData/UmbraSpaceIndustries/Konstruction/Assets/Konstruction.prefabs";

                    var prefabs = AssetBundle.LoadFromFile(filePath);
                    KonstructorWindowPrefab = prefabs.LoadAsset<GameObject>("KonstructorWindow");
                    KonstructorResourcePanelPrefab = prefabs.LoadAsset<GameObject>("RequiredResourcePanel");

                    // Register UI prefabs in window manager
                    var windowManager = ServiceManager.GetService<WindowManager>();
                    windowManager.RegisterWindow<KonstructorWindow>(KonstructorWindowPrefab);
                    windowManager.RegisterPrefab<RequiredResourcePanel>(KonstructorResourcePanelPrefab);
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
