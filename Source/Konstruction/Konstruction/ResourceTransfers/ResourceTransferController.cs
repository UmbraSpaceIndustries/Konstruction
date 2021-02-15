using KonstructionUI;

namespace Konstruction
{
    enum TransferMode
    {
        None,
        FastAtoB,
        FastBtoA,
        SlowAtoB,
        SlowBtoA,
        TransferAtoB,
        TransferBtoA
    }

    public class ResourceTransferController : IResourceTransferController
    {
        private const float FAST_XFER_SCALE = 0.1f;
        private const float MID_XFER_SCALE = 0.05f;
        private const float SLOW_XFER_SCALE = 0.01f;
        private bool _isTransferring;
        private TransferMode _mode = TransferMode.None;
        private ResourceTransferPanel _panel;
        private double _transferAmount;
        private readonly ResourceTransferTarget _targetA;
        private readonly ResourceTransferTarget _targetB;

        public string Resource { get; private set; }

        public ResourceTransferController(
            ResourceTransferTarget targetA,
            ResourceTransferTarget targetB,
            string resource)
        {
            Resource = resource;
            _targetA = targetA;
            _targetB = targetB;
        }

        public void SetFastAtoB(bool enabled)
        {
            if (enabled)
            {
                _mode = TransferMode.FastAtoB;
                _isTransferring = true;
            }
            else if (_mode == TransferMode.FastAtoB)
            {
                _mode = TransferMode.None;
            }
        }

        public void SetFastBtoA(bool enabled)
        {
            if (enabled)
            {
                _mode = TransferMode.FastBtoA;
                _isTransferring = true;
            }
            else if (_mode == TransferMode.FastBtoA)
            {
                _mode = TransferMode.None;
            }
        }

        public void SetPanel(ResourceTransferPanel panel)
        {
            if (_panel == null)
            {
                _panel = panel;
            }
        }

        public void SetSlowAtoB(bool enabled)
        {
            if (enabled)
            {
                _mode = TransferMode.SlowAtoB;
                _isTransferring = true;
            }
            else if (_mode == TransferMode.SlowAtoB)
            {
                _mode = TransferMode.None;
            }
        }

        public void SetSlowBtoA(bool enabled)
        {
            if (enabled)
            {
                _mode = TransferMode.SlowBtoA;
                _isTransferring = true;
            }
            else if (_mode == TransferMode.SlowBtoA)
            {
                _mode = TransferMode.None;
            }
        }

        public void SetTransferAtoB(bool enabled, double amount = 0d)
        {
            _transferAmount = amount;
            if (enabled)
            {
                _mode = TransferMode.TransferAtoB;
                _isTransferring = true;
            }
            else if (_mode == TransferMode.TransferAtoB)
            {
                _mode = TransferMode.None;
            }
        }

        public void SetTransferBtoA(bool enabled, double amount = 0d)
        {
            _transferAmount = amount;
            if (enabled)
            {
                _mode = TransferMode.TransferBtoA;
                _isTransferring = true;
            }
            else if (_mode == TransferMode.TransferBtoA)
            {
                _mode = TransferMode.None;
            }
        }

        private bool TransferAtoB(double amount)
        {
            if (_targetA.CanSubtractResource(Resource) &&
                _targetB.CanAddResource(Resource))
            {
                _targetA.SubtractResource(Resource, amount);
                _targetB.AddResource(Resource, amount);
                return true;
            }
            return false;
        }

        private bool TransferBtoA(double amount)
        {
            if (_targetA.CanAddResource(Resource) &&
                _targetB.CanSubtractResource(Resource))
            {
                _targetA.AddResource(Resource, amount);
                _targetB.SubtractResource(Resource, amount);
                return true;
            }
            return false;
        }

        public void Update(float deltaTime)
        {
            var amount = 0d;
            if (_isTransferring)
            {
                switch (_mode)
                {
                    case TransferMode.FastAtoB:
                        amount = _targetB.GetResource(Resource).MaxAmount *
                            FAST_XFER_SCALE *
                            deltaTime;
                        if (!TransferAtoB(amount))
                        {
                            _mode = TransferMode.None;
                        }
                        break;
                    case TransferMode.FastBtoA:
                        amount = _targetA.GetResource(Resource).MaxAmount *
                            FAST_XFER_SCALE *
                            deltaTime;
                        if (!TransferBtoA(amount))
                        {
                            _mode = TransferMode.None;
                        }
                        break;
                    case TransferMode.SlowAtoB:
                        amount = _targetB.GetResource(Resource).MaxAmount *
                            SLOW_XFER_SCALE *
                            deltaTime;
                        if (!TransferAtoB(amount))
                        {
                            _mode = TransferMode.None;
                        }
                        break;
                    case TransferMode.SlowBtoA:
                        amount = _targetA.GetResource(Resource).MaxAmount *
                            SLOW_XFER_SCALE *
                            deltaTime;
                        if (!TransferBtoA(amount))
                        {
                            _mode = TransferMode.None;
                        }
                        break;
                    case TransferMode.TransferAtoB:
                        if (_transferAmount < ResourceUtilities.FLOAT_TOLERANCE)
                        {
                            _transferAmount = 0d;
                            _mode = TransferMode.None;
                        }
                        else
                        {
                            amount = _targetB.GetResource(Resource).MaxAmount *
                                MID_XFER_SCALE *
                                deltaTime;
                            if (TransferAtoB(amount))
                            {
                                _transferAmount -= amount;
                            }
                            else
                            {
                                _mode = TransferMode.None;
                            }
                        }
                        break;
                    case TransferMode.TransferBtoA:
                        if (_transferAmount < ResourceUtilities.FLOAT_TOLERANCE)
                        {
                            _transferAmount = 0d;
                            _mode = TransferMode.None;
                        }
                        else
                        {
                            amount = _targetA.GetResource(Resource).MaxAmount *
                                MID_XFER_SCALE *
                                deltaTime;
                            if (TransferBtoA(amount))
                            {
                                _transferAmount -= amount;
                            }
                            else
                            {
                                _mode = TransferMode.None;
                            }
                        }
                        break;
                    case TransferMode.None:
                    default:
                        _isTransferring = false;
                        break;
                }
            }

            if (_panel != null)
            {
                _panel.UpdateRemainingTransferAmount(_transferAmount, _isTransferring);
                _panel.UpdateResourceDisplay(
                    _targetA.GetResource(Resource),
                    _targetB.GetResource(Resource));
            }
        }
    }
}
