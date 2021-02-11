using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using USIToolsUI.Interfaces;

namespace KonstructionUI
{
    [RequireComponent(typeof(RectTransform))]
    public class KonstructorWindow : MonoBehaviour, IWindow
    {
        private bool _initialized;
        private IKonstructor _konstructor;
        private IPrefabInstantiator _prefabInstantiator;
        private readonly Dictionary<string, RequiredResourcePanel> _resourcePanels
            = new Dictionary<string, RequiredResourcePanel>();
        private ShipMetadata _shipMetadata;

        [SerializeField]
        private Text AlertText;

        [SerializeField]
        private Text AvailableAmountHeaderText;

        [SerializeField]
        private Button BuildShipButton;

        [SerializeField]
        private Text BuildShipButtonText;

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
        private Text RequiredAmountHeaderText;

        [SerializeField]
        private GameObject ResourcesPanel;

        [SerializeField]
        private Text ResourceHeaderText;

        [SerializeField]
        private Text SelectShipButtonText;

        [SerializeField]
        private Text SelectedShipHeaderText;

        [SerializeField]
        private GameObject SelectedShipPanel;

        [SerializeField]
        private Text ShipCostText;

        [SerializeField]
        private Text ShipMassText;

        [SerializeField]
        private Text ShipNameText;

        [SerializeField]
        private Image ShipThumbnail;

        [SerializeField]
        private Text TitleBarText;

        public Canvas Canvas
        {
            get
            {
                return _konstructor.Canvas ?? null;
            }
        }

        private RectTransform _rectTransform;
        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                {
                    _rectTransform = GetComponent<RectTransform>();
                }
                return _rectTransform;
            }
        }

        public void BuildShip()
        {
            if (_konstructor != null &&
                _shipMetadata != null &&
                _shipMetadata.KonstructorMetadata.CanSpawn)
            {
                try
                {
                    _konstructor.SpawnVessel();
                    HideColumns();
                    HideSelectedShip();
                    _shipMetadata = null;
                }
                catch (Exception ex)
                {
                    ShowAlert(ex.Message);
                }
            }
        }

        private void ClearResources()
        {
            if (_resourcePanels.Count > 0)
            {
                var panels = _resourcePanels.Select(p => p.Value).ToArray();
                for (int i = 0; i < panels.Length; i++)
                {
                    Destroy(panels[i].gameObject);
                }
                _resourcePanels.Clear();
            }
        }

        public void CloseWindow()
        {
            HideAlert();
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        public void DisableBuildButton()
        {
            if (BuildShipButton != null)
            {
                BuildShipButton.interactable = false;
            }
        }

        public void EnableBuildButton()
        {
            if (BuildShipButton != null)
            {
                BuildShipButton.interactable = true;
            }
        }

        public void Initialize(IKonstructor konstructor, IPrefabInstantiator prefabInstantiator)
        {
            _konstructor = konstructor;
            _prefabInstantiator = prefabInstantiator;

            HideAlert();
            HideColumns();
            HideSelectedShip();

            if (AvailableAmountHeaderText != null)
            {
                AvailableAmountHeaderText.text = konstructor.AvailableAmountHeaderText;
            }
            if (BuildShipButtonText != null)
            {
                BuildShipButtonText.text = konstructor.BuildShipButtonText;
            }
            if (Column1HeaderText != null)
            {
                Column1HeaderText.text = konstructor.Column1HeaderText;
            }
            if (Column2HeaderText != null)
            {
                Column2HeaderText.text = konstructor.Column2HeaderText;
            }
            if (Column3HeaderText != null)
            {
                Column3HeaderText.text = konstructor.Column3HeaderText;
            }
            if (Column1Instructions != null)
            {
                Column1Instructions.text = konstructor.Column1Instructions;
            }
            if (Column2Instructions != null)
            {
                Column2Instructions.text = konstructor.Column2Instructions;
            }
            if (Column3Instructions != null)
            {
                Column3Instructions.text = konstructor.Column3Instructions;
            }
            if (RequiredAmountHeaderText != null)
            {
                RequiredAmountHeaderText.text = konstructor.RequiredAmountHeaderText;
            }
            if (ResourceHeaderText != null)
            {
                ResourceHeaderText.text = konstructor.ResourceHeaderText;
            }
            if (SelectShipButtonText != null)
            {
                SelectShipButtonText.text = konstructor.SelectShipButtonText;
            }
            if (SelectedShipHeaderText != null)
            {
                SelectedShipHeaderText.text = konstructor.SelectedShipHeaderText;
            }
            if (TitleBarText != null)
            {
                TitleBarText.text = konstructor.TitleBarText;
            }

            _initialized = true;
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

        public void HideSelectedShip()
        {
            if (SelectedShipPanel != null && SelectedShipPanel.activeSelf)
            {
                SelectedShipPanel.SetActive(false);
            }
        }

        public void Reset()
        {
            ClearResources();
            HideAlert();
            HideColumns();
            HideSelectedShip();
        }

        public void SelectShip()
        {
            HideAlert();

            if (_konstructor != null)
            {
                _konstructor.ShowShipSelector();
            }
        }

        public void ShipSelected(ShipMetadata shipMetadata)
        {
            _shipMetadata = shipMetadata;

            if (shipMetadata != null && SelectedShipPanel != null)
            {
                if (ShipCostText != null)
                {
                    ShipCostText.text = shipMetadata.Cost;
                }
                if (ShipMassText != null)
                {
                    ShipMassText.text = shipMetadata.Mass;
                }
                if (ShipNameText != null)
                {
                    ShipNameText.text = shipMetadata.Name;
                }
                if (ShipThumbnail != null)
                {
                    ShipThumbnail.sprite = Sprite.Create(
                        shipMetadata.Thumbnail,
                        new Rect(0f, 0f, 256f, 256f),
                        Vector2.zero);
                }
                if (!shipMetadata.KonstructorMetadata.CanSpawn)
                {
                    DisableBuildButton();
                    ShowAlert(_konstructor.InsufficientResourcesErrorText);
                }
                else
                {
                    EnableBuildButton();
                }

                ShowColumns();
                ClearResources();
                UpdateResources();

                if (!SelectedShipPanel.activeSelf)
                {
                    SelectedShipPanel.SetActive(true);
                }
            }
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

        public void UpdateResources()
        {
            if (_shipMetadata == null || _prefabInstantiator == null)
            {
                return;
            }
            for (int i = 0; i < _shipMetadata.KonstructorMetadata.Resources.Count; i++)
            {
                var resource = _shipMetadata.KonstructorMetadata.Resources[i];
                if (!_resourcePanels.ContainsKey(resource.Name))
                {
                    var prefab = _prefabInstantiator
                        .InstantiatePrefab<RequiredResourcePanel>(ResourcesPanel.transform);
                    _resourcePanels.Add(resource.Name, prefab);
                }
                var panel = _resourcePanels[resource.Name];
                panel.SetValues(resource);
            }
        }
    }
}
