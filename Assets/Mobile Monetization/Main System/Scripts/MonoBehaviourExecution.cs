using System;
using System.Collections;
using System.Collections.Generic;
using MobileCore.Utilities;
using UnityEngine;

public class MonoBehaviourExecution : SingletonMonoBehaviour<MonoBehaviourExecution>
{
    private static readonly List<Action> updatables = new();
    private static readonly List<Action> fixedUpdatables = new();
    private static readonly List<Action> lateUpdatables = new();
    private static readonly List<Action> unscaledUpdatables = new();

    private void Update()
    {
        deltaTime = Time.deltaTime;

        foreach (var u in updatables)
            u?.Invoke();

        foreach (var u in unscaledUpdatables)
            u?.Invoke();
    }

    private void FixedUpdate()
    {
        foreach (var u in fixedUpdatables)
            u?.Invoke();
    }

    private void LateUpdate()
    {
        foreach (var u in lateUpdatables)
            u?.Invoke();
    }

    /// <summary>
    /// Invoke coroutine from non-monobehavior script
    /// </summary>
    public static Coroutine InvokeCoroutine(IEnumerator enumerator)
    {
        return Instance.StartCoroutine(enumerator);
    }

    /// <summary>
    /// Stop custom coroutine
    /// </summary>
    public static void StopCustomCoroutine(Coroutine coroutine)
    {
        Instance.StopCoroutine(coroutine);
    }
    #region find object
    public static T FindObject<T>() where T : MonoBehaviour
    {
        var obj = FindAnyObjectByType<T>();
        if (obj == null)
        {
            Debug.LogError($"No {typeof(T)} found in the scene.");
        }
        return obj;
    }
    public static T[] FindObjects<T>(FindObjectsSortMode findObjectsSortMode = FindObjectsSortMode.None) where T : MonoBehaviour
    {
        var obj = FindObjectsByType<T>(findObjectsSortMode);
        if (obj == null || obj.Length == 0)
        {
            Debug.LogError($"No {typeof(T)} found in the scene.");
        }
        return obj;
    }
    public static T FindObjectInChildren<T>(Transform parent) where T : MonoBehaviour
    {
        var obj = parent.GetComponentInChildren<T>();
        if (obj == null)
        {
            Debug.LogError($"No {typeof(T)} found in the children of {parent.name}.");
        }
        return obj;
    }
    public static T[] FindObjectsInChildren<T>(Transform parent) where T : MonoBehaviour
    {
        var obj = parent.GetComponentsInChildren<T>();
        if (obj == null || obj.Length == 0)
        {
            Debug.LogError($"No {typeof(T)} found in the children of {parent.name}.");
        }
        return obj;
    }
    public static T FindObjectInParent<T>(Transform child) where T : MonoBehaviour
    {
        var obj = child.GetComponentInParent<T>();
        if (obj == null)
        {
            Debug.LogError($"No {typeof(T)} found in the parent of {child.name}.");
        }
        return obj;
    }
    public static T[] FindObjectsInParent<T>(Transform child) where T : MonoBehaviour
    {
        var obj = child.GetComponentsInParent<T>();
        if (obj == null || obj.Length == 0)
        {
            Debug.LogError($"No {typeof(T)} found in the parent of {child.name}.");
        }
        return obj;
    }
    #endregion

    #region Coroutines
    public static void DelayedCall(float delay, Action callback)
    {
        Instance.StartCoroutine(Instance.DelayCoroutine(delay, callback));
    }

    public static void DelayedCall(float delay, Action callback, bool useUnscaledTime)
    {

        if (useUnscaledTime)
            Instance.StartCoroutine(Instance.DelayCoroutineUnscaled(delay, callback));
        else
            Instance.StartCoroutine(Instance.DelayCoroutine(delay, callback));
    }


    private IEnumerator DelayCoroutine(float delay, Action callback)
    {
        yield return new WaitForSeconds(delay);
        callback?.Invoke();
    }

    private IEnumerator DelayCoroutineUnscaled(float delay, Action callback)
    {
        float time = 0f;
        while (time < delay)
        {
            time += Time.unscaledDeltaTime;
            yield return null;
        }
        callback?.Invoke();
    }


    #endregion

    //Time
    private float deltaTime;
    public float DeltaTime => deltaTime;
    public float TimeScale => Time.timeScale;
    public float UnscaledDeltaTime => Time.unscaledDeltaTime;

    // Register
    public static void RegisterUpdate(Action obj) => updatables.Add(obj);
    public static void RegisterFixedUpdate(Action obj) => fixedUpdatables.Add(obj);
    public static void RegisterLateUpdate(Action obj) => lateUpdatables.Add(obj);
    public static void RegisterUnscaleUpdate(Action obj) => unscaledUpdatables.Add(obj);

    // Unregister
    public static void UnregisterUpdate(Action obj) => updatables.Remove(obj);
    public static void UnregisterFixedUpdate(Action obj) => fixedUpdatables.Remove(obj);
    public static void UnregisterLateUpdate(Action obj) => lateUpdatables.Remove(obj);
    public static void UnregisterUnscaleUpdate(Action obj) => unscaledUpdatables.Remove(obj);

}
