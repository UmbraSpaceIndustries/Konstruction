using System;
using UnityEngine;
using UnityEngine.UI;

namespace KonstructionUI
{
    public class ResourceTransferPanel : MonoBehaviour
    {
        private IResourceTransferController _controller;

        #region Unity editor fields
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable 0169 // Field is never used
#pragma warning disable 0649 // Field is never assigned to

        [SerializeField]
        private Toggle FastTransferToAToggle;

        [SerializeField]
        private Toggle FastTransferToBToggle;

        [SerializeField]
        private Text HeaderLabel;

        [SerializeField]
        private Slider SliderA;

        [SerializeField]
        private Slider SliderB;

        [SerializeField]
        private Text SliderALabel;

        [SerializeField]
        private Text SliderBLabel;

        [SerializeField]
        private Image SliderALockIcon;

        [SerializeField]
        private Image SliderBLockIcon;

        [SerializeField]
        private Toggle SlowTransferToAToggle;

        [SerializeField]
        private Toggle SlowTransferToBToggle;

        [SerializeField]
        private ToggleGroup ToggleGroup;

        [SerializeField]
        private InputField TransferAmountInput;

        [SerializeField]
        private Toggle TransferAmountToAToggle;

        [SerializeField]
        private Toggle TransferAmountToBToggle;

#pragma warning restore 0649
#pragma warning restore 0169
#pragma warning restore IDE0051
#pragma warning restore IDE0044
        #endregion

        private double GetTransferAmount()
        {
            if (TransferAmountInput != null)
            {
                if (double.TryParse(TransferAmountInput.text, out var amount))
                {
                    return amount;
                }
            }
            return 0d;
        }

        public void Initialize(IResourceTransferController controller)
        {
            _controller = controller;
            if (HeaderLabel != null)
            {
                HeaderLabel.text = _controller.Resource;
            }
            if (SliderALockIcon != null && SliderALockIcon.gameObject.activeSelf)
            {
                SliderALockIcon.gameObject.SetActive(false);
            }
            if (SliderBLockIcon != null && SliderBLockIcon.gameObject.activeSelf)
            {
                SliderBLockIcon.gameObject.SetActive(false);
            }
        }

        public void OnFastTransferToAToggled(bool isOn)
        {
            _controller.SetFastBtoA(isOn);
        }

        public void OnFastTransferToBToggled(bool isOn)
        {
            _controller.SetFastAtoB(isOn);
        }

        public void OnSlowTransferToAToggled(bool isOn)
        {
            _controller.SetSlowBtoA(isOn);
        }

        public void OnSlowTransferToBToggled(bool isOn)
        {
            _controller.SetSlowAtoB(isOn);
        }

        public void OnTransferAmountToAToggled(bool isOn)
        {
            _controller.SetTransferBtoA(isOn, GetTransferAmount());
        }

        public void OnTransferAmountToBToggled(bool isOn)
        {
            _controller.SetTransferAtoB(isOn, GetTransferAmount());
        }

        public void UpdateRemainingTransferAmount(double amount, bool isLocked)
        {
            if (TransferAmountInput != null)
            {
                TransferAmountInput.interactable = !isLocked;
                if (isLocked)
                {
                    TransferAmountInput.text = amount >= 100d ?
                        $"{amount:N0}" :
                        $"{Math.Max(amount, 0):N1}";
                }
            }
        }

        public void UpdateResourceDisplay(
            ResourceMetadata targetAResource,
            ResourceMetadata targetBResource)
        {
            if (SliderA != null && targetAResource.MaxAmount > 0d)
            {
                var percentage
                    = targetAResource.AvailableAmount / targetAResource.MaxAmount;
                SliderA.SetValueWithoutNotify((float)percentage);
            }
            if (SliderB != null && targetBResource.MaxAmount > 0d)
            {
                var percentage
                    = targetBResource.AvailableAmount / targetBResource.MaxAmount;
                SliderB.SetValueWithoutNotify((float)percentage);
            }
            if (SliderALabel != null)
            {
                SliderALabel.text = string.Format(
                    "{0:N1} / {1:N1}",
                    targetAResource.AvailableAmount,
                    targetAResource.MaxAmount);
            }
            if (SliderBLabel != null)
            {
                SliderBLabel.text = string.Format(
                    "{0:N1} / {1:N1}",
                    targetBResource.AvailableAmount,
                    targetBResource.MaxAmount);
            }
            if (SliderALockIcon != null &&
                SliderALockIcon.gameObject.activeSelf != targetAResource.IsLocked)
            {
                SliderALockIcon.gameObject.SetActive(targetAResource.IsLocked);
            }
            if (SliderBLockIcon != null &&
                SliderBLockIcon.gameObject.activeSelf != targetBResource.IsLocked)
            {
                SliderBLockIcon.gameObject.SetActive(targetBResource.IsLocked);
            }

            // Automatically toggle buttons off if transfer is finished
            if (_controller.Mode == TransferMode.None)
            {
                if (ToggleGroup.AnyTogglesOn())
                {
                    ToggleGroup.SetAllTogglesOff(true);
                }
            }
        }
    }
}
