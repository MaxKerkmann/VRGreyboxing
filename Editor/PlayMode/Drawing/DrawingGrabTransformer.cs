using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace VRGreyboxing
{
    public class DrawingGrabTransformer : XRBaseGrabTransformer, IFarAttachProvider
    {
        public InteractableFarAttachMode farAttachMode { get; set; }
        private Transform _oldParent;

        public override void OnLink(XRGrabInteractable grabInteractable)
        {
            _oldParent = grabInteractable.transform.parent;
            base.OnLink(grabInteractable);
        }

        public override void OnUnlink(XRGrabInteractable grabInteractable)
        {
            _oldParent = null;
            base.OnUnlink(grabInteractable);
        }

        public override void Process(XRGrabInteractable grabInteractable, XRInteractionUpdateOrder.UpdatePhase updatePhase, ref Pose targetPose, ref Vector3 localScale)
        {
            if (!ActionManager.Instance.twoHandGrab)
            {
                targetPose.rotation = grabInteractable.transform.rotation;
            }
            
            Vector3 movement = targetPose.position - grabInteractable.transform.position;
            Vector3 rotation = targetPose.rotation.eulerAngles - grabInteractable.transform.eulerAngles;
            targetPose.position = grabInteractable.transform.position;
            targetPose.rotation = grabInteractable.transform.rotation;
            _oldParent.position += movement;
            targetPose.position += movement;
            _oldParent.rotation *= Quaternion.Euler(rotation);
            targetPose.rotation *= Quaternion.Euler(rotation);
        }

    }
}