using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using USIToolsUI;
using USIToolsUI.Interfaces;

namespace KonstructionUI
{
    [RequireComponent(typeof(RectTransform))]
    public class ResourceTransferWindow : AbstractWindow
    {
        private Action _onCloseCallback;
        private IPrefabInstantiator _prefabInstantiator;
        private readonly Dictionary<string, ResourceTransferPanel> _resourcePanels
            = new Dictionary<string, ResourceTransferPanel>();
        private Dictionary<string, IResourceTransferController> _transferControllers;
        private ResourceTransferTargetMetadata _transferTargetA;
        private ResourceTransferTargetMetadata _transferTargetB;
        private readonly Dictionary<string, ResourceTransferTargetMetadata> _transferTargets
            = new Dictionary<string, ResourceTransferTargetMetadata>();
        private ITransferTargetsController _transferTargetsController;

        #region Unity editor fields
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable 0169 // Field is never used
#pragma warning disable 0649 // Field is never assigned to

        [SerializeField]
        private Text AlertText;

        [SerializeField]
        private GameObject ResourcesPanel;

        [SerializeField]
        private GameObject Row1;

        [SerializeField]
        private GameObject Row2;

        [SerializeField]
        private Text Row1HeaderLabel;

        [SerializeField]
        private Text Row2HeaderLabel;

        [SerializeField]
        private Dropdown TargetADropdown;

        [SerializeField]
        private Dropdown TargetBDropdown;

        [SerializeField]
        private Text TitleBarText;

#pragma warning restore 0649
#pragma warning restore 0169
#pragma warning restore IDE0044
        #endregion

        public override Canvas Canvas => _transferTargetsController?.Canvas;

        private void ClearResourcePanels()
        {
            if (_resourcePanels.Count > 0)
            {
                // foreach gets grumpy if any of its elements go missing while
                //   iterating over them, so copy them into an array to be safe
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
                _onCloseCallback.Invoke();
            }
        }

        public void HideAlert()
        {
            if (AlertText != null && AlertText.gameObject.activeSelf)
            {
                AlertText.gameObject.SetActive(false);
            }
        }

        private void HideRow(GameObject row)
        {
            if (row != null && row.activeSelf)
            {
                row.SetActive(false);
            }
        }

        private void HideRows()
        {
            HideRow(Row1);
            HideRow(Row2);
        }

        public void Initialize(
            ITransferTargetsController controller,
            IPrefabInstantiator prefabInstantiator,
            Action onCloseCallback)
        {
            _prefabInstantiator = prefabInstantiator;
            _transferTargetsController = controller;
            _onCloseCallback = onCloseCallback;

            HideAlert();
            HideRows();
            
            if (Row1HeaderLabel != null)
            {
                Row1HeaderLabel.text = controller.Row1HeaderLabel;
            }
            if (Row2HeaderLabel != null)
            {
                Row2HeaderLabel.text = controller.Row2HeaderLabel;
            }
            if (TitleBarText != null)
            {
                TitleBarText.text = controller.TitleBarText;
            }
            if (TargetADropdown != null)
            {
                TargetADropdown.ClearOptions();
            }
            if (TargetBDropdown != null)
            {
                TargetBDropdown.ClearOptions();
            }
        }

        public void OnResourceTransferTargetsUpdated(
            List<ResourceTransferTargetMetadata> targets,
            float deltaTime)
        {
            _transferTargets.Clear();
            if (targets != null)
            {
                if (targets.Count < 2)
                {
                    ShowAlert(_transferTargetsController.InsufficientTransferTargetsMessage);
                    OnTargetASelected(0);
                    OnTargetBSelected(0);
                    HideRows();
                }
                else
                {
                    HideAlert();
                    ShowRow(Row1);
                    var currentVessel = targets
                        .FirstOrDefault(t => t.IsCurrentVessel);
                    var sorted = currentVessel == null ?
                        targets.OrderBy(t => t.DisplayName) :
                        targets.Where(t => !t.IsCurrentVessel).OrderBy(t => t.DisplayName);
                    if (currentVessel != null)
                    {
                        _transferTargets.Add(currentVessel.Id, currentVessel);
                    }
                    foreach (var target in sorted)
                    {
                        _transferTargets.Add(target.Id, target);
                    }
                }
            }
            UpdateDropdowns(deltaTime);
        }

        public void OnTargetASelected(int index)
        {
            if (_transferControllers != null)
            {
                _transferControllers.Clear();
            }
            if (index == 0 && _transferTargetA != null)
            {
                _transferTargetA = null;
                UpdateResources();
            }
            else if (TargetADropdown.options.Count > index)
            {
                var selectedOption = TargetADropdown.options[index] as DropdownOptionWithId;
                if (selectedOption.Id != "0")
                {
                    var target = _transferTargets[selectedOption.Id];
                    if (_transferTargetA != target)
                    {
                        _transferTargetA = target;
                        UpdateResources();
                    }
                }
            }
        }

        public void OnTargetBSelected(int index)
        {
            if (_transferControllers != null)
            {
                _transferControllers.Clear();
            }
            if (index == 0 && _transferTargetB != null)
            {
                _transferTargetB = null;
                UpdateResources();
            }
            else if (TargetBDropdown.options.Count > index)
            {
                var selectedOption = TargetBDropdown.options[index] as DropdownOptionWithId;
                if (selectedOption.Id != "0")
                {
                    var target = _transferTargets[selectedOption.Id];
                    if (_transferTargetB != target)
                    {
                        _transferTargetB = target;
                        UpdateResources();
                    }
                }
            }
        }

        public override void Reset()
        {
            _transferTargetA = null;
            _transferTargetB = null;
            ClearResourcePanels();
            HideAlert();
            HideRow(Row2);
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

        private void ShowRow(GameObject row)
        {
            if (row != null && !row.activeSelf)
            {
                row.SetActive(true);
            }
        }

        private void ShowRows()
        {
            ShowRow(Row1);
            ShowRow(Row2);
        }

        public void ShowWindow()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }

        private void UpdateDropdowns(float deltaTime)
        {
            if (TargetADropdown != null && TargetBDropdown != null)
            {
                var dropdownOptions = new List<Dropdown.OptionData>
                {
                    new DropdownOptionWithId
                    {
                        text =  _transferTargetsController.DropdownDefaultText,
                        Id = "0"
                    },
                };

                // We need at least 2 vessels in order to do transfers...
                if (_transferTargets.Count < 2)
                {
                    ClearResourcePanels();
                    TargetADropdown.ClearOptions();
                    TargetBDropdown.ClearOptions();
                    TargetADropdown.AddOptions(dropdownOptions);
                    TargetBDropdown.AddOptions(dropdownOptions);
                    TargetADropdown.SetValueWithoutNotify(0);
                    TargetBDropdown.SetValueWithoutNotify(0);
                }
                // Dropdowns will be empty initially
                else if (TargetADropdown.options.Count < 1 ||
                    TargetBDropdown.options.Count < 1)
                {
                    foreach (var target in _transferTargets)
                    {
                        dropdownOptions.Add(new DropdownOptionWithId
                        {
                            text = target.Value.DisplayName,
                            Id = target.Key,
                        });
                    }
                    TargetADropdown.AddOptions(dropdownOptions);
                    TargetBDropdown.AddOptions(dropdownOptions);
                    TargetADropdown.SetValueWithoutNotify(0);
                    TargetBDropdown.SetValueWithoutNotify(0);
                }
                else
                {
                    // Cache the previously selected targets so we can re-select them
                    var selectedOptionA
                        = TargetADropdown.options[TargetADropdown.value] as DropdownOptionWithId;
                    var selectedOptionB
                        = TargetBDropdown.options[TargetBDropdown.value] as DropdownOptionWithId;

                    TargetADropdown.ClearOptions();
                    TargetBDropdown.ClearOptions();

                    foreach (var target in _transferTargets)
                    {
                        dropdownOptions.Add(new DropdownOptionWithId
                        {
                            text = target.Value.DisplayName,
                            Id = target.Key,
                        });
                    }

                    TargetADropdown.AddOptions(dropdownOptions);
                    TargetBDropdown.AddOptions(dropdownOptions);

                    if (selectedOptionA == null)
                    {
                        TargetADropdown.SetValueWithoutNotify(0);
                    }
                    else
                    {
                        var selectedIndexA = dropdownOptions
                            .FindIndex(t => (t as DropdownOptionWithId).Id == selectedOptionA.Id);
                        TargetADropdown.SetValueWithoutNotify(selectedIndexA > -1 ? selectedIndexA : 0);
                    }
                    if (selectedOptionB == null)
                    {
                        TargetBDropdown.SetValueWithoutNotify(0);
                    }
                    else
                    {
                        var selectedIndexB = dropdownOptions
                            .FindIndex(t => (t as DropdownOptionWithId).Id == selectedOptionB.Id);
                        TargetBDropdown.SetValueWithoutNotify(selectedIndexB > -1 ? selectedIndexB : 0);
                    }

                    UpdateResources(deltaTime, false);
                }

                TargetADropdown.RefreshShownValue();
                TargetBDropdown.RefreshShownValue();
            }
            else
            {
                Debug.LogError($"[Konstruction] {nameof(ResourceTransferWindow)}: One or more dropdowns misconfigured.");
            }
        }

        private void UpdateResources(float deltaTime = 0f, bool targetChanged = true)
        {
            if (_transferTargetA == null || _transferTargetB == null)
            {
                HideRow(Row2);
            }
            else if (_transferTargetA.Id == _transferTargetB.Id)
            {
                HideRow(Row2);
                ShowAlert(_transferTargetsController.SameVesselSelectedMessage);
            }
            else
            {
                ShowRow(Row2);
                var transferControllers = _transferTargetsController
                    .GetResourceTransferControllers(_transferTargetA, _transferTargetB);

                if (targetChanged)
                {
                    HideAlert();
                    ClearResourcePanels();
                    _transferControllers = transferControllers;

                    foreach (var controller in transferControllers)
                    {
                        var panel = _prefabInstantiator
                            .InstantiatePrefab<ResourceTransferPanel>(ResourcesPanel.transform);
                        controller.Value.SetPanel(panel);
                        panel.Initialize(controller.Value);
                        _resourcePanels.Add(controller.Key, panel);
                    }
                }
                else
                {
                    // Kill off any resource controllers that no longer exist
                    var newResources = transferControllers.Keys;
                    foreach (var controller in _transferControllers)
                    {
                        if (!newResources.Contains(controller.Key))
                        {
                            _transferControllers.Remove(controller.Key);
                            Destroy(_resourcePanels[controller.Key].gameObject);
                            _resourcePanels.Remove(controller.Key);
                        }
                    }
                    // Update resource displays and transfers and add any new controllers
                    var existingResources = _transferControllers.Keys;
                    foreach (var controller in transferControllers)
                    {
                        if (existingResources.Contains(controller.Key))
                        {
                            _transferControllers[controller.Key].Update(deltaTime);
                        }
                        else
                        {
                            var panel = _prefabInstantiator
                                .InstantiatePrefab<ResourceTransferPanel>(ResourcesPanel.transform);
                            controller.Value.SetPanel(panel);
                            panel.Initialize(controller.Value);
                            _resourcePanels.Add(controller.Key, panel);
                            _transferControllers.Add(controller.Key, controller.Value);
                        }
                    }
                }
            }
        }
    }
}
