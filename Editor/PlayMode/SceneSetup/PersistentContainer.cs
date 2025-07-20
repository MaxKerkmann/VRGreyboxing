using System;
using UnityEngine;

namespace VRGreyboxing
{
    public class PersistentContainer : MonoBehaviour
    {
        [HideInInspector]
        public PersistentContainer Instance;
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }
    }
}
