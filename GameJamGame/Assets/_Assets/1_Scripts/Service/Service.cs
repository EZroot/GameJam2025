using System;
using System.Collections.Generic;
using UnityEngine;

public static class Service
{
    private static Dictionary<Type, object> _registry = null;

    public static void ClearRegistry()
    {
        _registry.Clear();
    }

    public static void Register<T>() where T : IService
    {
        _registry ??= new Dictionary<Type, object>();

        //does service exist?
        if (_registry.ContainsKey(typeof(T))) return;

        var potentialService = GetClassFromAssemblyByInterface(typeof(T));

        if (potentialService == null)
        {
            var potentialObject = GetSceneObject<T>();

            //monobehaviour
            if (potentialObject != null)
            {
                Debug.Log($"Registered (Monobehaviour)<color=green>{typeof(T)}</color> success!");
                _registry[typeof(T)] = potentialObject;
            }
            return;
        }
        if (potentialService != null)
        {
            Debug.Log($"Registered (Pure C#)<color=green>{typeof(T)}</color> success!");
            var instance = Activator.CreateInstance(potentialService);
            _registry[typeof(T)] = instance;
            return;
        }

        Debug.LogError("Service failed to register.");
        //Logger.Log($"Service: Registered (Pure C# Class){typeof(T)} success!");
    }

    public static T Get<T>() where T : IService
    {
        Register<T>();
        return (T)_registry[typeof(T)];
    }

    private static T GetSceneObject<T>() where T : IService
    {
        for (var i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (scene.isLoaded)
            {
                foreach (var gameObj in scene.GetRootGameObjects())
                {
                    //check root
                    if (gameObj.TryGetComponent<T>(out var rootObj))
                    {
                        return rootObj;
                    }

                    //check children
                    var result = GetChildComponentsRecursively<T>(gameObj.transform);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
        }
        return default(T);
    }

    private static T GetChildComponentsRecursively<T>(Transform root) where T : IService
    {
        T result = default(T);
        for (var i = 0; i < root.childCount; i++)
        {
            var child = root.transform.GetChild(i);
            if (child.transform.childCount > 0)
                result = GetChildComponentsRecursively<T>(child.transform);

            if (result == null)
                child.TryGetComponent<T>(out result);

            if (result != null)
                break;
        }
        return result;
    }

    private static Type GetClassFromAssemblyByInterface(Type interfaceType)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in asm.GetTypes())
            {
                if (!type.IsClass || type.BaseType == typeof(MonoBehaviour)) continue;

                foreach (var iInterface in type.GetInterfaces())
                {
                    if (iInterface != interfaceType) continue;
                    return type;
                }
            }
        }

        return null;
    }
}
