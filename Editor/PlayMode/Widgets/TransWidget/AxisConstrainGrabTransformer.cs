using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace VRGreyboxing
{
    /**
     * Allow grab movement on one axis while applying position changes to selected object
     */
    public class AxisConstrainGrabTransformer : ConstrainGrabTransformer, IFarAttachProvider
    {
        public InteractableFarAttachMode farAttachMode { get; set; }
        public XRGeneralGrabTransformer.ManipulationAxes permittedMovementAxes;
        public Vector3 offset;

        public override void OnLink(XRGrabInteractable grabInteractable)
        {
            transWidgetEditPoint = gameObject.GetComponentInParent<TransWidgetEditPoint>();
            base.OnLink(grabInteractable);
        }
        
        public override void OnGrab(XRGrabInteractable grabInteractable)
        {
            base.OnGrab(grabInteractable);
            transWidgetEditPoint.playerTransformation.currentEditPoint = transWidgetEditPoint;
            transWidgetEditPoint.playerTransformation.DisableTransWidget(true);
            offset = ActionManager.Instance.GetSelectedObject().transform.position - grabInteractable.transform.position;
        }
        
        public override void OnUnlink(XRGrabInteractable grabInteractable)
        {
            base.OnUnlink(grabInteractable);
            transWidgetEditPoint.playerTransformation.currentEditPoint = null;
        }

        public override void Process(XRGrabInteractable grabInteractable, XRInteractionUpdateOrder.UpdatePhase updatePhase, ref Pose targetPose, ref Vector3 localScale)
        {
            var widgetTransform = transWidgetEditPoint.playerTransformation.selectedObject.transform;

            Vector3 axis =
                (permittedMovementAxes & XRGeneralGrabTransformer.ManipulationAxes.X) != 0 ? widgetTransform.right :
                (permittedMovementAxes & XRGeneralGrabTransformer.ManipulationAxes.Y) != 0 ? widgetTransform.up :
                (permittedMovementAxes & XRGeneralGrabTransformer.ManipulationAxes.Z) != 0 ? widgetTransform.forward :
                Vector3.zero;
            
            Vector3 objectPos = grabInteractable.transform.position;
            Vector3 movement = Vector3.Project(targetPose.position - objectPos, axis);

            targetPose.position = objectPos+movement;
            transWidgetEditPoint.playerTransformation.TransformSelectedObject(transWidgetEditPoint.transWidgetTransformType,targetPose.position+offset,Quaternion.identity);
            
        }

    }

}
