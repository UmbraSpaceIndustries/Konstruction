using KonstructionUI;
using KonstructionUI.Interfaces;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Konstruction
{
    public class WindowManager : IPrefabInstantiator
    {
        private readonly Dictionary<Type, GameObject> _prefabs
            = new Dictionary<Type, GameObject>();
        private readonly EventData<GameScenes>.OnEvent _sceneChangeDelegate;
        private readonly Dictionary<Type, GameObject> _windows
            = new Dictionary<Type, GameObject>();

        public WindowManager()
        {
            _sceneChangeDelegate = new EventData<GameScenes>.OnEvent(CloseWindows);
            GameEvents.onGameSceneLoadRequested.Add(_sceneChangeDelegate);
        }

        public void CloseWindows(GameScenes target)
        {
            foreach (var window in _windows)
            {
                if (window.Value != null)
                {
                    if (window.Value.GetComponent(window.Key) is IWindow windowComponent)
                    {
                        windowComponent.Reset();
                    }
                    window.Value.SetActive(false);
                }
            }
        }

        public T GetWindow<T>()
            where T: IWindow
        {
            var type = typeof(T);
            if (!_windows.ContainsKey(type))
            {
                return default;
            }
            var window = _windows[type];
            return window.GetComponent<T>();
        }

        public T InstantiatePrefab<T>(Transform parent)
        {
            var type = typeof(T);
            if (parent == null || !_prefabs.ContainsKey(type))
            {
                return default;
            }

            var prefab = _prefabs[type];
            var obj = GameObject.Instantiate(prefab, parent);
            var component = obj.GetComponent<T>();
            return component;
        }

        public void RegisterPrefab<T>(GameObject prefab)
        {
            var type = typeof(T);
            if (type == null || prefab == null || _prefabs.ContainsKey(type))
            {
                return;
            }
            var component = prefab.GetComponent<T>();
            if (component == null)
            {
                throw new Exception(
                    $"WindowManager.RegisterPrefab: Prefab does not contain a {type.Name} component.");
            }
            _prefabs.Add(type, prefab);
        }

        public void RegisterWindow<T>(GameObject prefab)
            where T: IWindow
        {
            var type = typeof(T);
            if (type == null || prefab == null || _windows.ContainsKey(type))
            {
                return;
            }
            var obj = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            obj.transform.SetParent(MainCanvasUtil.MainCanvas.transform);
            obj.SetActive(false);

            _windows.Add(type, obj);
        }
    }
}
