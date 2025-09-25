using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace VRGreyboxing
{


    public class ConstrainGrabTransformer : XRBaseGrabTransformer
    {

        [HideInInspector] public TransWidgetEditPoint transWidgetEditPoint;

        public override void Process(XRGrabInteractable grabInteractable,
            XRInteractionUpdateOrder.UpdatePhase updatePhase, ref Pose targetPose, ref Vector3 localScale)
        {
        }
    }

}
