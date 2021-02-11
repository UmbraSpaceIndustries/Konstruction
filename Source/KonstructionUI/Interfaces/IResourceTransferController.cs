using UnityEngine;

namespace KonstructionUI
{
    public interface IResourceTransferController
    {
        Canvas Canvas { get; }
        string Column1HeaderText { get; }
        string Column2HeaderText { get; }
        string Column3HeaderText { get; }
        string Column1Instructions { get; }
        string Column2Instructions { get; }
        string Column3Instructions { get; }
        string TitleBarText { get; }
    }
}
