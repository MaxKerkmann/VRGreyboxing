using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace VRGreyboxing
{


    public class ObjectDuplicationButton : MonoBehaviour, IPointerClickHandler
    {

        private InteractorHandedness _handedness;

        public void OnPointerClick(PointerEventData eventData)
        {
            // XR Interaction Toolkit passes TrackedDeviceEventData
            var trackedData = eventData as TrackedDeviceEventData;
            if (trackedData != null)
            {
                // This is the interactor that triggered the click
                var interactor = trackedData.interactor;
                if (interactor is XRBaseInteractor xrInteractor)
                {
                    GameObject controllerObject = xrInteractor.gameObject;
                    _handedness = controllerObject.GetComponent<NearFarInteractor>().handedness;
                }

            }
            ActionManager.Instance.DuplicateSelectedObject(transform, _handedness == InteractorHandedness.Left ? Handedness.Left: Handedness.Right);
        }
    }
}