using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;
using VRGreyboxing;

namespace VRGreyboxing
{
    public class CircleConstrainGrabTransformer : ConstrainGrabTransformer, IFarAttachProvider
    {

        public XRGeneralGrabTransformer.ManipulationAxes circleNormalAxis;
        private Vector3 _center;
        private float _radius;
        private Vector3 _planeNormal;


        public InteractableFarAttachMode farAttachMode { get; set; }

        public override void OnLink(XRGrabInteractable grabInteractable)
        {
            transWidgetEditPoint = gameObject.GetComponentInParent<TransWidgetEditPoint>();
            base.OnLink(grabInteractable);
            _center = ActionManager.Instance.GetSelectedObject().transform.position;
            _radius = Vector3.Distance(_center, transWidgetEditPoint.transform.position);
            _planeNormal = circleNormalAxis switch
            {
                XRGeneralGrabTransformer.ManipulationAxes.Y => ActionManager.Instance.GetSelectedObject()
                    .transform.up,
                XRGeneralGrabTransformer.ManipulationAxes.X => ActionManager.Instance.GetSelectedObject()
                    .transform.right,
                XRGeneralGrabTransformer.ManipulationAxes.Z => ActionManager.Instance.GetSelectedObject()
                    .transform.forward,
                _ => _planeNormal
            };
        }

        public override void OnGrab(XRGrabInteractable grabInteractable)
        {
            base.OnGrab(grabInteractable);
            transWidgetEditPoint.playerTransformation.currentEditPoint = transWidgetEditPoint;
            transWidgetEditPoint.playerTransformation.DisableTransWidget(true);
            transWidgetEditPoint.playerTransformation.currentStartRotation = ActionManager.Instance.GetSelectedObject().transform.rotation;

        }
        
        public override void OnUnlink(XRGrabInteractable grabInteractable)
        {
            base.OnUnlink(grabInteractable);
            transWidgetEditPoint.playerTransformation.currentEditPoint = null;
        }

        public override void Process(XRGrabInteractable grabInteractable, XRInteractionUpdateOrder.UpdatePhase updatePhase, ref Pose targetPose, ref Vector3 localScale)
        {
            Vector3 origVec = transWidgetEditPoint.transform.position - _center;

            // Project the grab position onto the plane
            Vector3 projected = Vector3.ProjectOnPlane(targetPose.position - _center, _planeNormal).normalized * _radius;
            Vector3 lockedPos = _center + projected;

            // Lock the gizmo's position to the circular path
            targetPose.position = lockedPos;

            // Vector from center to current (constrained) gizmo position
            Vector3 alteredVec = lockedPos - _center;

            // Compute rotation between original and current vector
            Quaternion deltaRotation = Quaternion.FromToRotation(origVec, alteredVec);

            transWidgetEditPoint.playerTransformation.TransformSelectedObject(transWidgetEditPoint.transWidgetTransformType,Vector3.zero, deltaRotation);
        }

        private void OnDrawGizmos()
        {
            if (transWidgetEditPoint.playerTransformation.currentEditPoint != null)
            {
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, _planeNormal);

                // Save the old Gizmos matrix
                Matrix4x4 oldMatrix = Gizmos.matrix;

                // Apply transformation matrix
                Gizmos.matrix = Matrix4x4.TRS(_center, rotation, Vector3.one);

                // Draw the tilted wire sphere at local origin (which becomes `position` in world space)
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(Vector3.zero, _radius);

                // Restore original matrix
                Gizmos.matrix = oldMatrix;
            }
        }
    }
}
