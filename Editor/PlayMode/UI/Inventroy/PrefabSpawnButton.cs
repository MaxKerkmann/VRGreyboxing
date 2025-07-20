using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace VRGreyboxing
{

    public class PrefabSpawnButton : MonoBehaviour, IPointerClickHandler
    {

        public GameObject prefab;
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
            ActionManager.Instance.SpawnObjectOnHand(prefab, transform, _handedness == InteractorHandedness.Left ? Handedness.Left: Handedness.Right);
        }

    }
}
