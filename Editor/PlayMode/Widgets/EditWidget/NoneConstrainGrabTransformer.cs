using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;
using VRGreyboxing;

public class NoneConstrainGrabTransformer : XRBaseGrabTransformer, IFarAttachProvider
{
    public InteractableFarAttachMode farAttachMode { get; set; }
    public Vector3 offset;
    public EditWidgetEditPoint editWidgetEditPoint { get; set; }

    public override void OnLink(XRGrabInteractable grabInteractable)
    {
        editWidgetEditPoint = gameObject.GetComponent<EditWidgetEditPoint>();
        base.OnLink(grabInteractable);
    }

    public override void OnGrab(XRGrabInteractable grabInteractable)
    {
        base.OnGrab(grabInteractable);
        editWidgetEditPoint.playerEdit.DisableEditWidget(gameObject);
    }

    public override void OnUnlink(XRGrabInteractable grabInteractable)
    {
        base.OnUnlink(grabInteractable);
        editWidgetEditPoint.playerEdit.currentEditPoint = null;
    }

    public override void Process(XRGrabInteractable grabInteractable, XRInteractionUpdateOrder.UpdatePhase updatePhase, ref Pose targetPose, ref Vector3 localScale)
    {
        Vector3 objectPos = grabInteractable.transform.position;
        Vector3 movement = targetPose.position - objectPos;

        targetPose.position = objectPos+movement;
        
        editWidgetEditPoint.playerEdit.EditSelectedObjectVertices(targetPose.position,editWidgetEditPoint.handledPositionIndices);
    }
}