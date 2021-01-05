using UnityEngine;
using System.Linq;

namespace Konstruction
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class AddScenarioModules : MonoBehaviour
    {
        void Start()
        {
            var game = HighLogic.CurrentGame;

            var ksm = game.scenarios.Find(s => s.moduleName == typeof(KonstructionScenario).Name);
            if (ksm == null)
            {
                game.AddProtoScenarioModule(typeof(KonstructionScenario), GameScenes.SPACECENTER,
                    GameScenes.FLIGHT, GameScenes.EDITOR);
            }
            else
            {
                if (ksm.targetScenes.All(s => s != GameScenes.SPACECENTER))
                {
                    ksm.targetScenes.Add(GameScenes.SPACECENTER);
                }
                if (ksm.targetScenes.All(s => s != GameScenes.FLIGHT))
                {
                    ksm.targetScenes.Add(GameScenes.FLIGHT);
                }
                if (ksm.targetScenes.All(s => s != GameScenes.EDITOR))
                {
                    ksm.targetScenes.Add(GameScenes.EDITOR);
                }
            }
        }
    }
}