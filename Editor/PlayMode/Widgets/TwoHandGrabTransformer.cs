using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace VRGreyboxing
{
    public class TwoHandGrabTransformer : XRBaseGrabTransformer
    {
        public override void Process(XRGrabInteractable grabInteractable, XRInteractionUpdateOrder.UpdatePhase updatePhase, ref Pose targetPose, ref Vector3 localScale)
        {
            if (!ActionManager.Instance.twoHandGrab && !PlayModeManager.Instance.editorDataSO.oneHandGrabRotation)
            {
                targetPose.rotation = grabInteractable.transform.rotation;
            }
        }
    }
}
