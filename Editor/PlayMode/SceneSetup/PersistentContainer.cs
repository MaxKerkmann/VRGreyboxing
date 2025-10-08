using UnityEngine;

namespace VRGreyboxing
{
    /**
     * Container component to set all managers and vr rig to DontDestroyOnLoad
     */
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
