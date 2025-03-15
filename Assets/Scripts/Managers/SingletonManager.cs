using System.Collections.Generic;
using UnityEngine;

public class SingletonManager : MonoBehaviour
{
    // Static instance of the SingletonManager.
    public static SingletonManager Instance { get; private set; }

    // Dictionary to store registered singleton instances keyed by type.
    private Dictionary<System.Type, object> registeredSingletons = new Dictionary<System.Type, object>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Optional: persist this manager across scene loads.
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Registers a singleton instance of type T.
    /// If an instance of the same type is already registered and the new instance is a Component, 
    /// the new instance's GameObject is destroyed.
    /// Otherwise, if no instance is registered, registers the instance and sets its transform parent to SingletonManager.
    /// </summary>
    public void Register<T>(T instance)
    {
        System.Type type = typeof(T);
        // Attempt to cast the instance to a Component.
        Component comp = instance as Component;

        if (registeredSingletons.ContainsKey(type))
        {
            // An instance is already registered.
            if (comp != null)
            {
                // Destroy the duplicate if it's a Component.
                Debug.Log("Singleton instance of type " + type + " already exists. Destroying duplicate.");
                Destroy(comp.gameObject);
            }
            else
            {
                // If not a component, update the reference.
                registeredSingletons[type] = instance;
                Debug.Log("Updated singleton registration for type: " + type);
            }
            return;
        }

        // Not already registered; add the instance.
        registeredSingletons.Add(type, instance);
        Debug.Log("Registered singleton of type: " + type);

        // If the instance is a Component, set its parent to this SingletonManager's transform.
        if (comp != null)
        {
            comp.transform.SetParent(this.transform);
        }
    }

    /// <summary>
    /// Unregisters the singleton instance of type T if it matches the provided instance.
    /// </summary>
    public void Unregister<T>(T instance)
    {
        System.Type type = typeof(T);
        if (registeredSingletons.TryGetValue(type, out object registeredInstance))
        {
            if (registeredInstance.Equals(instance))
            {
                registeredSingletons.Remove(type);
                Debug.Log("Unregistered singleton of type: " + type);
            }
            else
            {
                Debug.LogWarning("Attempted to unregister a singleton instance of type " + type + " that does not match the registered instance.");
            }
        }
        else
        {
            Debug.LogWarning("No singleton registered for type: " + type);
        }
    }

    /// <summary>
    /// Returns the registered singleton instance of type T.
    /// </summary>
    public T Get<T>()
    {
        System.Type type = typeof(T);
        if (registeredSingletons.TryGetValue(type, out object instance))
        {
            return (T)instance;
        }
        Debug.LogWarning("No singleton registered for type: " + type);
        return default;
    }
}
