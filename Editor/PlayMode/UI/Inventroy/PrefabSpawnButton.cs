using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace VRGreyboxing
{
    /**
     * Menu item in inventory to spawn set prefab
     */
    public class PrefabSpawnButton : MonoBehaviour, IPointerClickHandler
    {

        public GameObject prefab;
        private InteractorHandedness _handedness;

        public void OnPointerClick(PointerEventData eventData)
        {
            var trackedData = eventData as TrackedDeviceEventData;
            if (trackedData != null)
            {
                var interactor = trackedData.interactor;
                if (interactor is XRBaseInteractor xrInteractor)
                {
                    GameObject controllerObject = xrInteractor.gameObject;
                    _handedness = controllerObject.GetComponent<NearFarInteractor>().handedness;
                }

            }
            ActionManager.Instance.SpawnObjectOnHand(prefab, transform, _handedness == InteractorHandedness.Left ? Handedness.Left: Handedness.Right);
        }

    }
}
