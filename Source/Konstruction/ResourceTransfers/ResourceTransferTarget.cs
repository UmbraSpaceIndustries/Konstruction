using KonstructionUI;
using System;
using System.Collections.Generic;

namespace Konstruction
{
    public class ResourceTransferTarget
    {
        private readonly ResourceBroker _broker;
        private readonly Dictionary<string, ResourceMetadata> _resources;
        private readonly Vessel _vessel;

        public ResourceTransferTarget(Vessel vessel)
        {
            _broker = new ResourceBroker();
            _vessel = vessel;
            _resources = GetResourceMetadata(vessel.parts);
        }

        public void AddResource(string resourceName, double amount)
        {
            var storage = _broker.StorageAvailable(
                _vessel.rootPart,
                resourceName,
                TimeWarp.fixedDeltaTime,
                ResourceFlowMode.ALL_VESSEL,
                1d);
            var amountToStore = Math.Min(storage, amount);
            _broker.StoreResource(
                _vessel.rootPart,
                resourceName,
                amountToStore,
                TimeWarp.fixedDeltaTime,
                ResourceFlowMode.ALL_VESSEL);
        }

        public double GetAvailableAmount(string resourceName)
        {
            return _broker.AmountAvailable(
                _vessel.rootPart,
                resourceName,
                TimeWarp.fixedDeltaTime,
                ResourceFlowMode.ALL_VESSEL);
        }

        public ResourceMetadata GetResource(string resourceName)
        {
            if (!_resources.ContainsKey(resourceName))
            {
                _resources.Add(resourceName, new ResourceMetadata
                {
                    ResourceName = PartResourceLibrary
                        .Instance
                        .resourceDefinitions[resourceName]
                        .displayName,
                });
            }
            Part part;
            PartResource resource;
            var metadata = _resources[resourceName];
            metadata.AvailableAmount = 0d;
            metadata.MaxAmount = 0d;
            metadata.IsLocked = false;
            for (int i = 0; i < _vessel.parts.Count; i++)
            {
                part = _vessel.parts[i];
                for (int j = 0; j < part.Resources.Count; j++)
                {
                    resource = part.Resources[j];
                    if (resource.resourceName == resourceName)
                    {
                        metadata.AvailableAmount += resource.amount;
                        metadata.MaxAmount += resource.maxAmount;
                        metadata.IsLocked |= !resource.flowState;
                    }
                }
            }
            return metadata;
        }

        public static Dictionary<string, ResourceMetadata> GetResourceMetadata(List<Part> parts)
        {
            var resources = new Dictionary<string, ResourceMetadata>();
            foreach (var part in parts)
            {
                foreach (var resource in part.Resources)
                {
                    if (IsTransferable(resource))
                    {
                        if (resources.ContainsKey(resource.resourceName))
                        {
                            var metadata = resources[resource.resourceName];
                            metadata.AvailableAmount += resource.amount;
                            metadata.MaxAmount += resource.maxAmount;
                        }
                        else
                        {
                            var metadata = new ResourceMetadata
                            {
                                AvailableAmount = resource.amount,
                                MaxAmount = resource.maxAmount,
                                ResourceName = PartResourceLibrary
                                    .Instance
                                    .resourceDefinitions[resource.resourceName]
                                    .displayName,
                            };
                            resources.Add(resource.resourceName, metadata);
                        }
                    }
                }
            }
            return resources;
        }

        public double GetStorageAvailable(string resourceName)
        {
            return _broker.StorageAvailable(
                _vessel.rootPart,
                resourceName,
                TimeWarp.fixedDeltaTime,
                ResourceFlowMode.ALL_VESSEL,
                1d);
        }

        public static bool IsTransferable(PartResource resource)
        {
            return PartResourceLibrary
                .Instance
                .resourceDefinitions[resource.resourceName]
                .resourceFlowMode != ResourceFlowMode.NO_FLOW;
        }

        public double SubtractResource(string resourceName, double amount)
        {
            var available = _broker.AmountAvailable(
                _vessel.rootPart,
                resourceName,
                TimeWarp.fixedDeltaTime,
                ResourceFlowMode.ALL_VESSEL);
            var amountToRequest = Math.Min(available, amount);
            _broker.RequestResource(
                _vessel.rootPart,
                resourceName,
                amountToRequest,
                TimeWarp.fixedDeltaTime,
                ResourceFlowMode.ALL_VESSEL);
            return amountToRequest;
        }
    }
}
