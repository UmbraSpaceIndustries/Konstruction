using System.Collections.Generic;

namespace KonstructionUI
{
    public class ResourceTransferTargetMetadata
    {
        public string DisplayName { get; set; }
        public string Id { get; set; }
        public bool IsCurrentVessel { get; set; }
        public Dictionary<string, ResourceMetadata> Resources { get; set; }
    }
}
