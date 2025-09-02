using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRGreyboxing
{
    
    public class VectorConstrainGrabTransformer : ConstrainGrabTransformer, IFarAttachProvider
    {
        public InteractableFarAttachMode farAttachMode { get; set; }
        private Vector3 _vectorDirection;

        private GameObject _scaleObject;
        private Transform _oldParent;
        private Vector3 _fixedCorner;

        public override void OnLink(XRGrabInteractable grabInteractable)
        {
            transWidgetEditPoint = gameObject.GetComponentInParent<TransWidgetEditPoint>();
            base.OnLink(grabInteractable);
        }

        public override void OnGrab(XRGrabInteractable grabInteractable)
        {
            base.OnGrab(grabInteractable);
            _vectorDirection = transWidgetEditPoint.transform.position - ActionManager.Instance.GetSelectedObject().transform.position;
            transWidgetEditPoint.playerTransformation.currentEditPoint = transWidgetEditPoint;
            
            _scaleObject = new GameObject("ScaleObject");
            _fixedCorner = ActionManager.Instance.GetSelectedObject().transform.position + (ActionManager.Instance.GetSelectedObject().transform.position-transWidgetEditPoint.transform.position);
            _scaleObject.transform.position = _fixedCorner;
            _scaleObject.transform.rotation = ActionManager.Instance.GetSelectedObject().transform.rotation;
            _oldParent = ActionManager.Instance.GetSelectedObject().transform.parent;
            ActionManager.Instance.GetSelectedObject().transform.parent = _scaleObject.transform;
            transWidgetEditPoint.playerTransformation.DisableTransWidget(true);
        }
        

        public override void OnUnlink(XRGrabInteractable grabInteractable)
        {
            base.OnUnlink(grabInteractable);
            if (transWidgetEditPoint != null)
            {
                transWidgetEditPoint.playerTransformation.currentEditPoint = null;
                if (ActionManager.Instance.GetSelectedObject() != null && !PlayModeManager.Instance.currentWorldScaler.destroyed)
                {
                        ActionManager.Instance.GetSelectedObject().transform.parent =
                            _oldParent == null ? null : _oldParent;
                }
            }
            if(_scaleObject != null)
            {
                Destroy(_scaleObject);
                transWidgetEditPoint.playerTransformation.ApplyTransWidgetToSelectedObject();
            }

        }

        public override void Process(XRGrabInteractable grabInteractable, XRInteractionUpdateOrder.UpdatePhase updatePhase, ref Pose targetPose, ref Vector3 localScale)
        {
            Vector3 fromOrigin = targetPose.position-transWidgetEditPoint.transform.position;
            Vector3 dirNormalized = _vectorDirection.normalized;
            float distance = Vector3.Dot(fromOrigin, dirNormalized);
            Vector3 lockedPosition = transWidgetEditPoint.transform.position + dirNormalized * distance;
            transWidgetEditPoint.playerTransformation.ScaleSelectedObjectFromCorner(_fixedCorner,lockedPosition,transWidgetEditPoint.transform.position,_scaleObject);
            targetPose.position = lockedPosition;
        }
    }
}
