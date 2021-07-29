using KonstructionUI;
using System;

namespace Konstruction
{
    public class ResourceTransferController : IResourceTransferController
    {
        private const float FAST_XFER_SCALE = 0.1f;
        private const float MID_XFER_SCALE = 0.05f;
        private const float SLOW_XFER_SCALE = 0.01f;
        private bool _isTransferring;
        private ResourceTransferPanel _panel;
        private double _transferAmount;
        private readonly ResourceTransferTarget _targetA;
        private readonly ResourceTransferTarget _targetB;

        public TransferMode Mode { get; private set; } = TransferMode.None;
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
                Mode = TransferMode.FastAtoB;
                _isTransferring = true;
            }
            else if (Mode == TransferMode.FastAtoB)
            {
                Mode = TransferMode.None;
            }
        }

        public void SetFastBtoA(bool enabled)
        {
            if (enabled)
            {
                Mode = TransferMode.FastBtoA;
                _isTransferring = true;
            }
            else if (Mode == TransferMode.FastBtoA)
            {
                Mode = TransferMode.None;
            }
        }

        public void SetPanel(ResourceTransferPanel panel)
        {
            // This should only be called once per controller
            if (_panel == null)
            {
                _panel = panel;
            }
        }

        public void SetSlowAtoB(bool enabled)
        {
            if (enabled)
            {
                Mode = TransferMode.SlowAtoB;
                _isTransferring = true;
            }
            else if (Mode == TransferMode.SlowAtoB)
            {
                Mode = TransferMode.None;
            }
        }

        public void SetSlowBtoA(bool enabled)
        {
            if (enabled)
            {
                Mode = TransferMode.SlowBtoA;
                _isTransferring = true;
            }
            else if (Mode == TransferMode.SlowBtoA)
            {
                Mode = TransferMode.None;
            }
        }

        public void SetTransferAtoB(bool enabled, double amount = 0d)
        {
            _transferAmount = amount;
            if (enabled)
            {
                Mode = TransferMode.TransferAtoB;
                _isTransferring = true;
            }
            else if (Mode == TransferMode.TransferAtoB)
            {
                Mode = TransferMode.None;
            }
        }

        public void SetTransferBtoA(bool enabled, double amount = 0d)
        {
            _transferAmount = amount;
            if (enabled)
            {
                Mode = TransferMode.TransferBtoA;
                _isTransferring = true;
            }
            else if (Mode == TransferMode.TransferBtoA)
            {
                Mode = TransferMode.None;
            }
        }

        private bool TransferAtoB(double amount)
        {
            var available = _targetA.GetAvailableAmount(Resource);
            var storage = _targetB.GetStorageAvailable(Resource);
            var availability = Math.Min(available, storage);
            if (availability > ResourceUtilities.FLOAT_TOLERANCE)
            {
                if (availability < amount)
                {
                    amount = availability;
                }
                _targetA.SubtractResource(Resource, amount);
                _targetB.AddResource(Resource, amount);
                _transferAmount = Math.Max(_transferAmount - amount, 0d);
                return true;
            }
            return false;
        }

        private bool TransferBtoA(double amount)
        {
            var available = _targetB.GetAvailableAmount(Resource);
            var storage = _targetA.GetStorageAvailable(Resource);
            var availability = Math.Min(available, storage);
            if (availability > ResourceUtilities.FLOAT_TOLERANCE)
            {
                if (availability < amount)
                {
                    amount = availability;
                }
                _targetA.AddResource(Resource, amount);
                _targetB.SubtractResource(Resource, amount);
                _transferAmount = Math.Max(_transferAmount - amount, 0d);
                return true;
            }
            return false;
        }

        public void Update(float deltaTime)
        {
            if (_isTransferring)
            {
                double amount;
                switch (Mode)
                {
                    case TransferMode.FastAtoB:
                        amount = _targetB.GetResource(Resource).MaxAmount *
                            FAST_XFER_SCALE *
                            deltaTime;
                        if (!TransferAtoB(amount))
                        {
                            Mode = TransferMode.None;
                        }
                        break;
                    case TransferMode.FastBtoA:
                        amount = _targetA.GetResource(Resource).MaxAmount *
                            FAST_XFER_SCALE *
                            deltaTime;
                        if (!TransferBtoA(amount))
                        {
                            Mode = TransferMode.None;
                        }
                        break;
                    case TransferMode.SlowAtoB:
                        amount = _targetB.GetResource(Resource).MaxAmount *
                            SLOW_XFER_SCALE *
                            deltaTime;
                        if (!TransferAtoB(amount))
                        {
                            Mode = TransferMode.None;
                        }
                        break;
                    case TransferMode.SlowBtoA:
                        amount = _targetA.GetResource(Resource).MaxAmount *
                            SLOW_XFER_SCALE *
                            deltaTime;
                        if (!TransferBtoA(amount))
                        {
                            Mode = TransferMode.None;
                        }
                        break;
                    case TransferMode.TransferAtoB:
                        if (_transferAmount < ResourceUtilities.FLOAT_TOLERANCE)
                        {
                            _transferAmount = 0d;
                            Mode = TransferMode.None;
                        }
                        else
                        {
                            amount = Math.Min(
                                _transferAmount,
                                _targetB.GetResource(Resource).MaxAmount * MID_XFER_SCALE * deltaTime);
                            if (!TransferAtoB(amount))
                            {
                                Mode = TransferMode.None;
                            }
                        }
                        break;
                    case TransferMode.TransferBtoA:
                        if (_transferAmount < ResourceUtilities.FLOAT_TOLERANCE)
                        {
                            _transferAmount = 0d;
                            Mode = TransferMode.None;
                        }
                        else
                        {
                            amount = Math.Min(
                                _transferAmount,
                                _targetA.GetResource(Resource).MaxAmount * MID_XFER_SCALE * deltaTime);
                            if (!TransferBtoA(amount))
                            {
                                Mode = TransferMode.None;
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
