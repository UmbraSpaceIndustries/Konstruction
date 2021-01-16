using UnityEngine;
using UnityEngine.UI;

namespace KonstructionUI
{
    [RequireComponent(typeof(RectTransform))]
    public class RequiredResourcePanel : MonoBehaviour
    {
        private static readonly Color _defaultColor = Color.white;
        private static readonly Color _insufficientColor = Color.red;

        [SerializeField]
        private Text ResourceText;

        [SerializeField]
        private Text RequiredAmountText;

        [SerializeField]
        private Text AvailableAmountText;

        public void SetValues(KonstructorResourceMetadata resource)
        {
            if (ResourceText != null)
            {
                ResourceText.text = resource.Name;
            }
            if (RequiredAmountText != null)
            {
                RequiredAmountText.text = $"{resource.Needed:N0}";
            }
            if (AvailableAmountText != null)
            {
                if (resource.Available - resource.Needed < 0.0001d)
                {
                    AvailableAmountText.color = _insufficientColor;
                    AvailableAmountText.text = $"{resource.Available:N1}";
                }
                else
                {
                    AvailableAmountText.color = _defaultColor;
                    AvailableAmountText.text = $"{resource.Available:N1}";
                }
            }
        }
    }
}
