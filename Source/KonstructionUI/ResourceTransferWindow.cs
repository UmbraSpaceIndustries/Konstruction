using UnityEngine;
using UnityEngine.UI;
using USIToolsUI;
using USIToolsUI.Interfaces;

namespace KonstructionUI
{
    [RequireComponent(typeof(RectTransform))]
    public class ResourceTransferWindow : AbstractWindow
    {
        private IPrefabInstantiator _prefabInstantiator;
        private IResourceTransferController _resourceTransferController;

        #region Unity editor fields
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable 0169 // Field is never used
#pragma warning disable 0649 // Field is never assigned to

        [SerializeField]
        private Text AlertText;

        [SerializeField]
        private GameObject Column1;

        [SerializeField]
        private GameObject Column2;

        [SerializeField]
        private GameObject Column3;

        [SerializeField]
        private Text Column1HeaderText;

        [SerializeField]
        private Text Column2HeaderText;

        [SerializeField]
        private Text Column3HeaderText;

        [SerializeField]
        private Text Column1Instructions;

        [SerializeField]
        private Text Column2Instructions;

        [SerializeField]
        private Text Column3Instructions;

        [SerializeField]
        private Text TitleBarText;

#pragma warning restore 0649
#pragma warning restore 0169
#pragma warning restore IDE0051
#pragma warning restore IDE0044
        #endregion

        public override Canvas Canvas => _resourceTransferController?.Canvas;

        public void CloseWindow()
        {
            HideAlert();
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        public void Initialize(
            IResourceTransferController controller,
            IPrefabInstantiator prefabInstantiator)
        {
            _prefabInstantiator = prefabInstantiator;
            _resourceTransferController = controller;

            HideAlert();
            HideColumns();
            
            if (Column1HeaderText != null)
            {
                Column1HeaderText.text = controller.Column1HeaderText;
            }
            if (Column2HeaderText != null)
            {
                Column2HeaderText.text = controller.Column2HeaderText;
            }
            if (Column3HeaderText != null)
            {
                Column3HeaderText.text = controller.Column3HeaderText;
            }
            if (Column1Instructions != null)
            {
                Column1Instructions.text = controller.Column1Instructions;
            }
            if (Column2Instructions != null)
            {
                Column2Instructions.text = controller.Column2Instructions;
            }
            if (Column3Instructions != null)
            {
                Column3Instructions.text = controller.Column3Instructions;
            }
            if (TitleBarText != null)
            {
                TitleBarText.text = controller.TitleBarText;
            }
        }

        public void HideAlert()
        {
            if (AlertText != null && AlertText.gameObject.activeSelf)
            {
                AlertText.gameObject.SetActive(false);
            }
        }

        public void HideColumns()
        {
            if (Column2 != null && Column2.activeSelf)
            {
                Column2.SetActive(false);
            }
            if (Column3 != null && Column3.activeSelf)
            {
                Column3.SetActive(false);
            }
        }

        public override void Reset()
        {
            HideAlert();
            HideColumns();
        }

        public void ShowAlert(string message)
        {
            if (AlertText != null)
            {
                AlertText.text = message;
                if (!AlertText.gameObject.activeSelf)
                {
                    AlertText.gameObject.SetActive(true);
                }
            }
        }

        public void ShowColumns()
        {
            if (Column2 != null && !Column2.activeSelf)
            {
                Column2.SetActive(true);
            }
            if (Column3 != null && !Column3.activeSelf)
            {
                Column3.SetActive(true);
            }
        }

        public void ShowWindow()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }
    }
}
