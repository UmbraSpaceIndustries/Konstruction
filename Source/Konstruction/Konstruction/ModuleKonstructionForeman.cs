using System.Text;
using USITools;

namespace Konstruction
{
    public class ModuleKonstructionForeman : PartModule
    {
        [KSPField]
        public float constructionWeightValue = 1000f;
        [KSPField]
        public float packedVolumeLimit = 1100;
        [KSPField]
        public float massLimit = 1;
        [KSPField]
        public float KerbalsRequired = 4;
        [KSPField]
        public float KonstructionPointsRequired = 4;

        [KSPEvent(guiName = "Enable Konstruction", guiActive = true, externalToEVAOnly = true, guiActiveEditor = false, active = true, guiActiveUnfocused = true, unfocusedRange = 1000.0f)]
        public void SetupKonstruction()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (HasSufficientResources())
            {
                ApplyResults();
                ScreenMessages.PostScreenMessage("Konstruction successfully enabled", 5f, ScreenMessageStyle.UPPER_CENTER);
            }
        }

        public bool HasSufficientResources()
        {
            var ret = true; 
            //First - We need an Engineer.
            var hasEngineer = DoesVesselHaveEngineer();
            if (!hasEngineer)
            {
                ScreenMessages.PostScreenMessage("Unable to commence Konstruction - Engineer not present in module", 5f, ScreenMessageStyle.UPPER_CENTER);
                ret = false;
            }

            //Second - We need more Kerbals
            var numKerbals = GetKerbalQuantity();
            if (numKerbals < KerbalsRequired)
            {
                ScreenMessages.PostScreenMessage(string.Format("Unable to commence Konstruction - Insufficient Kerbals {0} of {1} needed.",numKerbals,KerbalsRequired), 5f, ScreenMessageStyle.UPPER_CENTER);
                ret = false;
            }

            //Third - we need Konstruction Points.           
            var cp = GetConstructionPoints();
            if (cp < KonstructionPointsRequired)
            {
                ScreenMessages.PostScreenMessage(string.Format("Unable to commence Konstruction - Insufficient Konstruction Points {0} of {1} needed.",cp,KonstructionPointsRequired), 5f, ScreenMessageStyle.UPPER_CENTER);
                ret = false;
            }

            return ret;
        }

        public bool DoesVesselHaveEngineer()
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


        public void ApplyResults()
        {
            //Adjust EVA Construction Limit based on gravity
            PhysicsGlobals.ConstructionWeightLimit = PhysicsGlobals.GravitationalAcceleration * constructionWeightValue;

            //Leaving out the Kerbal code as it really is not necessary and things may or may not 
            //get sketchy messing with some of their parameters.  Leaving it here for potential reuse.

            //AdjustKerbals
        
        }

        private void AdjustKerbals()
        {
            //Adust parms for our EVA Kerbals
            var vessels = LogisticsTools.GetNearbyVessels(2000, true, vessel, false);
            var count = vessels.Count;
            for (int i = 0; i < count; ++i)
            {
                var v = vessels[i];
                if (v.isEVA)
                {
                    ModuleInventoryPart invModule = v.FindPartModuleImplementing<ModuleInventoryPart>();
                    var items = new DictionaryValueList<int, StoredPart>(); //Can we avoid eating our inventory??
                    bool hasItems = invModule.storedParts.Count > 0;
                    if (hasItems)
                    {
                        for (int z = 0; z < invModule.storedParts.Count; z++)
                        {
                            items.Add(z, invModule.storedParts[z]);
                        }
                    }
                    invModule.packedVolumeLimit = packedVolumeLimit;
                    invModule.massLimit = massLimit;
                    if (invModule.InventoryItemCount == 0 && hasItems)
                    {
                        for (int x = 0; x < items.Count; x++)
                        {
                            invModule.storedParts.Add(x, items[x]);
                        }
                    }
                }
            }
        }

        public override string GetInfo()
        {
            var output = new StringBuilder();
            output.AppendLine("Increases EVA Construction capabilities if an Engineeer is on board.");
            return output.ToString();
        }
    }
}
