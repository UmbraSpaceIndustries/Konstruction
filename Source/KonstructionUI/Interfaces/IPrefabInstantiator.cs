using UnityEngine;

namespace KonstructionUI
{
    public interface IPrefabInstantiator
    {
        T InstantiatePrefab<T>(Transform parent);
    }
}
