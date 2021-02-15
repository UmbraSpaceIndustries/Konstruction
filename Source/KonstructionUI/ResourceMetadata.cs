using System.Collections.Generic;

namespace KonstructionUI
{
    public class ResourceMetadataEqualityComparer
        : IEqualityComparer<ResourceMetadata>
    {
        public bool Equals(ResourceMetadata x, ResourceMetadata y)
        {
            return x.ResourceName == y.ResourceName;
        }

        public int GetHashCode(ResourceMetadata obj)
        {
            return obj.ResourceName.GetHashCode();
        }
    }

    public class ResourceMetadata
    {
        public double AvailableAmount { get; set; }
        public bool IsLocked { get; set; }
        public double MaxAmount { get; set; }
        public string ResourceName { get; set; }
    }
}
