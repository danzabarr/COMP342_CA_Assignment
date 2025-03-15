using UnityEngine;
using System.Collections.Generic;

public static class GameObjectExtension
{
    public static void SetLayerRecursively(this GameObject gameObject, int layer)
    {
        gameObject.layer = layer;
        foreach (Transform child in gameObject.transform)
        {
            child.gameObject.SetLayerRecursively(layer);
        }
    }

    public static List<T> GetChildrenOnLayer<T>(this GameObject gameObject, LayerMask layers) where T : Component
    {
        List<T> children = new List<T>();

        foreach (Transform child in gameObject.transform)
        {
            if ((layers & (1 << child.gameObject.layer)) == 0)
                continue;

            T component = child.GetComponent<T>();
            if (component != null)
                children.Add(component);
        }

        return children;
    }

    public static T GetFirstChildOnLayer<T>(this GameObject gameObject, LayerMask layers) where T : Component
    {
        foreach (Transform child in gameObject.transform)
        {
            if ((layers & (1 << child.gameObject.layer)) == 0)
                continue;

            T component = child.GetComponent<T>();
            if (component != null)
                return component;
        }

        return null;
    }
}