using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Shared;

namespace GameClient.Misc
{
    public class MainThreadHandler : MonoBehaviour
    {
        public static MainThreadHandler Instance { get; private set; }

        private static readonly Queue<Action> ActionQueue = new Queue<Action>();

        public MainThreadHandler() { Instance = this; }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            Printer.Warning($"Created dispatcher for version > {CommonValues.ExecutableVersion}");
        }

        private void Update() 
        { 
            ExecuteAllQueue();
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
