using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly Dictionary<DispatchTarget, Queue<Action>> s_actionQueue = new Dictionary<DispatchTarget, Queue<Action>>();
    private static MainThreadDispatcher s_instance;

    static MainThreadDispatcher()
    {
        s_actionQueue.Add(DispatchTarget.UPDATE, new Queue<Action>());
        s_actionQueue.Add(DispatchTarget.LATE_UPDATE, new Queue<Action>());
        s_actionQueue.Add(DispatchTarget.FIXED_UPDATE, new Queue<Action>());
    }

    private void Awake()
    {
        s_instance = this;
    }

    public static void Dispatch(Action action, DispatchTarget target)
    {
        if (s_instance == null)
            Debug.LogError("MainThreadDispatcher has a dispatch but no component instance exists.");

        s_actionQueue[target].Enqueue(action);
    }

    private void Update()
    {
        while (s_actionQueue[DispatchTarget.UPDATE].Any())
        {
            s_actionQueue[DispatchTarget.UPDATE].Dequeue().Invoke();
        }
    }

    private void LateUpdate()
    {
        while (s_actionQueue[DispatchTarget.LATE_UPDATE].Any())
        {
            s_actionQueue[DispatchTarget.LATE_UPDATE].Dequeue().Invoke();
        }
    }

    private void FixedUpdate()
    {
        while (s_actionQueue[DispatchTarget.FIXED_UPDATE].Any())
        {
            s_actionQueue[DispatchTarget.FIXED_UPDATE].Dequeue().Invoke();
        }
    }

    public enum DispatchTarget
    {
        UPDATE,
        LATE_UPDATE,
        FIXED_UPDATE,
    }
}