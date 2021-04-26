using System;
using System.Collections.Generic;
using System.Linq;
using USITools;

namespace Konstruction.Utilities
{
    public class CostData
    {
        public int Quantity;
        public PartResourceDefinition Resource;
    }

    public static class PartUtilities
    {
        public static List<CostData> GetPartCost(AvailablePart part, KonstructionPersistance settings)
        {
            var costs = new List<CostData>();
            var runningCost = 0d;
            var runningMass = 0d;

            var config = settings.GetSettings();
            var baseMult = config.massMultiplier;
            var baseResourceName = config.massResourceName;
            var costResourceName = config.costResourceName;
            var baseResource = PartResourceLibrary.Instance.resourceDefinitions[baseResourceName];
            var costResource = PartResourceLibrary.Instance.resourceDefinitions[costResourceName];

            //Parts always begin with a base resource that we use at the start of our calc.
            var baseCost = new CostData
            {
                Quantity = (int)Math.Ceiling(part.partPrefab.mass * baseMult / baseResource.density),
                Resource = baseResource
            };
            costs.Add(baseCost);

            runningCost += baseCost.Quantity * baseCost.Resource.unitCost;
            runningMass += baseCost.Quantity * baseCost.Resource.density;

            //Next, we have to allocate materials based on the part's partmodules.
            foreach (var module in settings.GetModuleResources())
            {
                if (part.partPrefab.Modules.Contains(module.moduleName))
                {
                    var moduleResource = PartResourceLibrary.Instance.resourceDefinitions[module.resourceName];
                    var modCost = new CostData
                    {
                        Resource = moduleResource,
                        Quantity = (int)Math.Ceiling(part.partPrefab.mass * module.massMultiplier / moduleResource.density)
                    };
                    costs.Add(modCost);

                    runningCost += modCost.Quantity * modCost.Resource.unitCost;
                    runningMass += modCost.Quantity * modCost.Resource.density;
                }
            }

            //We now need to see if we have a shortfall between the value of our current resources,
            //and the value of this part.  If so, we can start applying additional resources.
            foreach (var costRes in settings.GetCostResources())
            {
                if (runningCost < part.cost)
                {
                    var res = PartResourceLibrary.Instance.resourceDefinitions[costRes.resourceName];
                    var addCost = new CostData
                    {
                        Resource = res
                    };

                    var quantityNeeded = (int)Math.Ceiling((part.cost - runningCost) / res.unitCost);
                    var maxQuantity = (int)Math.Ceiling(part.partPrefab.mass * costRes.maxMass / res.density) + 1;

                    if (quantityNeeded < maxQuantity)
                    {
                        addCost.Quantity = quantityNeeded;
                    }
                    else
                    {
                        addCost.Quantity = maxQuantity;
                    }
                    costs.Add(addCost);

                    runningCost += addCost.Quantity * addCost.Resource.unitCost;
                    runningMass += addCost.Quantity * addCost.Resource.density;
                }
            }
            //If we are still unable to achieve the cost, we go into our overflow resource. This has no mass restrictions.
            if (runningCost < part.cost)
            {
                var finCost = new CostData
                {
                    Resource = costResource
                };

                var quantityNeeded = (int)Math.Ceiling((part.cost - runningCost) / costResource.unitCost);
                finCost.Quantity = quantityNeeded;
                costs.Add(finCost);

                runningCost += finCost.Quantity * finCost.Resource.unitCost;
                runningMass += finCost.Quantity * finCost.Resource.density;
            }
            return costs;
        }

        public static void ConsumeResources(List<CostData> costs)
        {
            foreach (var c in costs)
            {
                ConsumeResource(c.Resource, c.Quantity);
            }
        }

        public static bool ResourcesExist(string resName, double needed)
        {
            double foundAmount = GetResourceQty(resName);
            return foundAmount >= needed;
        }

        public static double GetResourceQty(string resName)
        {
            double foundAmount = 0;
            var whpList = LogisticsTools.GetRegionalWarehouses(FlightGlobals.ActiveVessel, "USI_ModuleResourceWarehouse");
            var count = whpList.Count;

            for (int i = 0; i < count; ++i)
            {
                var whp = whpList[i];
                if (whp.Modules.Contains("USI_ModuleResourceWarehouse"))
                {
                    var wh = whp.FindModuleImplementing<USI_ModuleResourceWarehouse>();
                    if (!wh.localTransferEnabled)
                        continue;
                }
                if (whp.Resources.Contains(resName))
                {
                    var res = whp.Resources[resName];
                    foundAmount += res.amount;
                }
            }
            return foundAmount;
        }

        public static void AddResource(PartResourceDefinition resource, double amtToGive)
        {
            double remaining = amtToGive;
            var resName = resource.name;
            var whpList = LogisticsTools.GetRegionalWarehouses(FlightGlobals.ActiveVessel, "USI_ModuleResourceWarehouse");
            var count = whpList.Count;

            for (int i = 0; i < count; ++i)
            {
                if (remaining <= ResourceUtilities.FLOAT_TOLERANCE)
                    break;

                var whp = whpList[i];
                if (whp.Modules.Contains("USI_ModuleResourceWarehouse"))
                {
                    var wh = whp.FindModuleImplementing<USI_ModuleResourceWarehouse>();
                    if (!wh.localTransferEnabled)
                        continue;
                }
                if (whp.Resources.Contains(resName))
                {
                    var res = whp.Resources[resName];
                    var space = res.maxAmount - res.amount;
                    if ( space >= remaining)
                    {
                        res.amount += remaining;
                        remaining = 0;
                        break;
                    }
                    else
                    {
                        remaining -= res.amount;
                        res.amount = res.maxAmount;
                    }
                }
            }

        }

        public static void ConsumeResource(PartResourceDefinition resource, double amtToTake)
        {
            double needed = amtToTake;
            var resName = resource.name;
            var whpList = LogisticsTools.GetRegionalWarehouses(FlightGlobals.ActiveVessel, "USI_ModuleResourceWarehouse");
            var count = whpList.Count;

            for (int i = 0; i < count; ++i)
            {
                var whp = whpList[i];
                if (whp.Modules.Contains("USI_ModuleResourceWarehouse"))
                {
                    var wh = whp.FindModuleImplementing<USI_ModuleResourceWarehouse>();
                    if (!wh.localTransferEnabled)
                        continue;
                }
                if (whp.Resources.Contains(resName))
                {
                    var res = whp.Resources[resName];
                    if (res.amount >= needed)
                    {
                        res.amount -= needed;
                        needed = 0;
                        break;
                    }
                    else
                    {
                        needed -= res.amount;
                        res.amount = 0;
                    }
                }
            }
        }

        public static double GetStorageSpace(string resName)
        {
            double foundStorage = 0;
            var whpList = LogisticsTools.GetRegionalWarehouses(FlightGlobals.ActiveVessel, "USI_ModuleResourceWarehouse");
            var count = whpList.Count;

            for (int i = 0; i < count; ++i)
            {
                var whp = whpList[i];
                if (whp.Modules.Contains("USI_ModuleResourceWarehouse"))
                {
                    var wh = whp.FindModuleImplementing<USI_ModuleResourceWarehouse>();
                    if (!wh.localTransferEnabled)
                        continue;
                }
                if (whp.Resources.Contains(resName))
                {
                    var res = whp.Resources[resName];
                    foundStorage += res.maxAmount - res.amount;
                }
            }
            return foundStorage;
        }
    }

    public static class CrewUtilities
    {
        public static bool DoesVesselHaveCrewType(string type)
        {
            foreach (var part in FlightGlobals.ActiveVessel.Parts)
            {
                var cCount = part.protoModuleCrew.Count;
                for (int i = 0; i < cCount; ++i)
                {
                    if (part.protoModuleCrew[i].experienceTrait.TypeName == type)
                        return true;
                }
            }
            return false;
        }
    }
}
