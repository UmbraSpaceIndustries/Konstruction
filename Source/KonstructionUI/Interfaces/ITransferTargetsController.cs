using System.Collections.Generic;
using UnityEngine;

namespace KonstructionUI
{
    public interface ITransferTargetsController
    {
        Canvas Canvas { get; }
        string DropdownDefaultText { get; }
        Dictionary<string, IResourceTransferController> GetResourceTransferControllers(
            ResourceTransferTargetMetadata targetA,
            ResourceTransferTargetMetadata targetB);
        List<ResourceTransferTargetMetadata> GetResourceTransferTargets();
        string InsufficientTransferTargetsMessage { get; }
        string Row1HeaderLabel { get; }
        string Row2HeaderLabel { get; }
        string SameVesselSelectedMessage { get; }
        string TitleBarText { get; }
    }
}
