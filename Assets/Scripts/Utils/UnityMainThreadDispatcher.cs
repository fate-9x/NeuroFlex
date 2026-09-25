using System;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private readonly Queue<Action> _actions = new Queue<Action>();
    private readonly object _lock = new object();

    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("UnityMainThreadDispatcher");
            _instance = go.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(go);
        }
        return _instance;
    }

    void Update()
    {
        lock (_lock)
        {
            while (_actions.Count > 0)
            {
                _actions.Dequeue()?.Invoke();
            }
        }
    }

    public void Enqueue(Action action)
    {
        lock (_lock)
        {
            _actions.Enqueue(action);
        }
    }
}
