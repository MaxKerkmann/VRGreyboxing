using UnityEngine;

namespace VRGreyboxing
{
    /**
     * Raycasting from player controllers to find currently hovered object by player
     */
    public class XRControllerRaycaster : MonoBehaviour
    {
        public Transform pokePointTransform;
        public Handedness handedness;
        private GameObject _lastHit;
        public RaycastHit Hit;
        public LayerMask uILayerMask;
        public LayerMask defaultLayerMask;
        
        private void Update()
        {
            RaycastHit[] hits = Physics.RaycastAll(transform.position, transform.forward,float.MaxValue,uILayerMask);

            if (hits.Length == 0)
            {
                if (Physics.Raycast(transform.position, transform.forward, out Hit, float.MaxValue, defaultLayerMask))
                {
                    if (Hit.collider != null && Hit.collider.gameObject != _lastHit)
                    {
                        ActionManager.Instance.AssignHoverObject(Hit.transform.gameObject, handedness);
                    }
                }
                else
                {
                    ActionManager.Instance.AssignHoverObject(null, handedness);
                    _lastHit = null;
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