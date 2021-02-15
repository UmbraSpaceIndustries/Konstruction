namespace KonstructionUI
{
    public interface IResourceTransferController
    {
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
