using System;
using System.Text;
using USITools;

namespace Konstruction
{
    public class ModuleKonstructionForeman : PartModule
    {
        [KSPField]
        public float constructionWeightMultiplier = 25f;

        [KSPEvent(guiName = "Enable Konstruction", guiActive = true, externalToEVAOnly = true, guiActiveEditor = false, active = true, guiActiveUnfocused = true, unfocusedRange = 1000.0f)]
        public void SetupKonstruction()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            var kPoints = CalculateKonstruction();
            if (kPoints > 0)
            {
                var max = ApplyResults(kPoints);
                ScreenMessages.PostScreenMessage($"EVA Construction set to {max / 1000:N2}t", 5f, ScreenMessageStyle.UPPER_CENTER);
            }
        }

        public int CalculateKonstruction()
        {
            var ret = 0;
            var numKerbals = GetKerbalQuantity();
            ret += numKerbals;

            var cp = GetConstructionPoints();
            ret += cp;

            var hasEngineer = DoesPartHaveEngineer();
            if (!hasEngineer)
            {
                ScreenMessages.PostScreenMessage("Engineer not present in module - Konstruction bonus reduced", 5f, ScreenMessageStyle.UPPER_CENTER);
                ret /= 2;
            }

            return ret;
        }

        public bool DoesPartHaveEngineer()
        {
            var cCount = part.protoModuleCrew.Count;
            for (int i = 0; i < cCount; ++i)
            {
                if (part.protoModuleCrew[i].experienceTrait.TypeName == "Engineer")
                    return true;
            }
            return false;
        }

        public int GetKerbalQuantity()
        {
            var kerbCount = 0;
            var vessels = LogisticsTools.GetNearbyVessels(2000, true, vessel, false);
            var count = vessels.Count;
            for (int i = 0; i < count; ++i)
            {
                var v = vessels[i];
                if (v.isEVA)
                {
                    kerbCount++;
                }
                else
                {
                    kerbCount += v.GetCrewCount();
                }
            }
            return kerbCount;
        }

        public int GetConstructionPoints()
        {
            var points = 0;

            //Adust parms for our EVA Kerbals
            var vessels = LogisticsTools.GetNearbyVessels(2000, true, vessel, false);
            var count = vessels.Count;
            for (int i = 0; i < count; ++i)
            {
                var v = vessels[i];
                var kModules = v.FindPartModulesImplementing<ModuleKonstructionHelper>();
                foreach(var m in kModules)
                {
                    points += m.KonstructionPoints;
                }
            }
            return points;
        }

        public double ApplyResults(int points)
        {
            // Calculate and set new mass limit
            var newMass = PhysicsGlobals.GravitationalAcceleration * points * constructionWeightMultiplier;
            PhysicsGlobals.ConstructionWeightLimit = newMass;

            // Determine the gravity-adjusted mass limit for the current body
            var surfaceGravity = vessel.mainBody.gravParameter / Math.Pow(vessel.mainBody.Radius, 2d);
            return newMass / surfaceGravity;
        }

        public override string GetInfo()
        {
            var output = new StringBuilder();
            output.AppendLine("Increases EVA Construction capabilities if an Engineeer is on board.");
            return output.ToString();
        }
    }
}
