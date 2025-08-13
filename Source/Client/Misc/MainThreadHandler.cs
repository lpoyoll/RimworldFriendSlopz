using GameClient.Values;
using Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace GameClient.Misc
{
    public class MainThreadHandler : MonoBehaviour
    {
        public static MainThreadHandler Instance { get; private set; }

        private static Queue<Action> ActionQueue { get; set; } = new Queue<Action>();

        public MainThreadHandler() { Instance = this; }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            Printer.Warning($"Created dispatcher for version > {CommonValues.ExecutableVersion}");
        }

        private void Update() 
        { 
            ExecuteAllQueue();

            if (SessionValues.CurrentNetworkState == CommonEnumerators.ClientNetworkState.Connected)
            {
                DoNetworkUpdates();

                DoPlayerUpdates();
            }
        }

        private void DoNetworkUpdates()
        {
            DiagnosticsHandler.CalculateNetworkRate();
        }

        private void DoPlayerUpdates()
        {
            foreach (MethodInfo method in MethodGatherer.CheckPerFrameMethods)
            {
                method.Invoke(null, null);
            }
        }

        public void Enqueue(Action action) { Enqueue(ActionWrapper(action)); }

        private void Enqueue(IEnumerator action)
        {
            lock (ActionQueue)
            {
                ActionQueue.Enqueue(() =>
                {
                    StartCoroutine(action);
                });
            }
        }

        private void ExecuteAllQueue()
        {
            lock (ActionQueue)
            {
                while (ActionQueue.Count > 0)
                {
                    ActionQueue.Dequeue().Invoke();
                }
            }
        }

        IEnumerator ActionWrapper(Action action)
        {
            action();
            yield return null;
        }
    }
}
