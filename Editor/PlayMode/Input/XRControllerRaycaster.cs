using System;
using UnityEngine;

namespace VRGreyboxing
{
    public class XRControllerRaycaster : MonoBehaviour
    {
        public Transform pokePointTransform;
        public Handedness handedness;
        private GameObject _lastHit;
        [HideInInspector]
        public RaycastHit hit;
        
        private void Update()
        {
            if (Physics.Raycast(transform.position, transform.forward, out hit))
            {
                if (hit.collider != null && hit.collider.gameObject != _lastHit)
                {
                    ActionManager.Instance.AssignHoverObject(hit.transform.gameObject, handedness);
                }
            }
            else
            {
                ActionManager.Instance.AssignHoverObject(null, handedness);
                _lastHit = null;
            }
        }
    }
}