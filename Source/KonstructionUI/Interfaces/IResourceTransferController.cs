namespace KonstructionUI
{
    public enum TransferMode
    {
        None,
        FastAtoB,
        FastBtoA,
        SlowAtoB,
        SlowBtoA,
        TransferAtoB,
        TransferBtoA
    }

    public interface IResourceTransferController
    {
        TransferMode Mode { get; }
        string Resource { get; }
        void SetFastAtoB(bool enabled);
        void SetFastBtoA(bool enabled);
        void SetPanel(ResourceTransferPanel panel);
        void SetSlowAtoB(bool enabled);
        void SetSlowBtoA(bool enabled);
        void SetTransferAtoB(bool enabled, double amount = 0d);
        void SetTransferBtoA(bool enabled, double amount = 0d);
        void Update(float deltaTime);
    }
}
