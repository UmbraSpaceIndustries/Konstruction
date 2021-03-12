using UnityEngine;

namespace KonstructionUI
{
    public interface IKonstructor
    {
        string AvailableAmountHeaderText { get; }
        string BuildShipButtonText { get; }
        Canvas Canvas { get; }
        string Column1HeaderText { get; }
        string Column2HeaderText { get; }
        string Column3HeaderText { get; }
        string Column1Instructions { get; }
        string Column2Instructions { get; }
        string Column3Instructions { get; }
        string InsufficientResourcesErrorText { get; }
        string RequiredAmountHeaderText { get; }
        string ResourceHeaderText { get; }
        string SelectShipButtonText { get; }
        string SelectedShipHeaderText { get; }
        string TitleBarText { get; }

        void LaunchVessel();
        void ShowShipSelector();
    }
}
