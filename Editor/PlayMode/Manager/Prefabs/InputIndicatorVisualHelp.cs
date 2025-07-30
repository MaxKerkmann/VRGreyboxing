using UnityEngine;
using UnityEngine.UI;

namespace VRGreyboxing
{
    public class InputIndicatorVisualHelp : MonoBehaviour
    {
        public Image turnLeftIndicator;
        public Image turnRightIndicator;
        public Image movementDirectionIndicator;
        public Canvas indicatorCanvas;
        public GameObject teleportIndicatorAnchor;
        public GameObject teleportIndicatorFigure;

        public void DisplayIndicators(Vector3 movementInput, float activationThreshold, int rotationMode)
        {
            if (rotationMode == 2)
            {
                teleportIndicatorAnchor.transform.forward = movementInput.normalized;
                teleportIndicatorAnchor.transform.localScale = new Vector3(1, 1, movementInput.magnitude / activationThreshold * 5);
                teleportIndicatorAnchor.transform.GetChild(0).GetComponent<Renderer>().material.color = movementInput.magnitude / activationThreshold >= 1f ? Color.green : Color.red;
                if (movementInput.magnitude > activationThreshold)
                {
                    teleportIndicatorFigure.SetActive(true);
                    teleportIndicatorFigure.transform.position = movementInput.normalized * 6;
                    teleportIndicatorFigure.transform.LookAt(indicatorCanvas.transform);
                }
                else
                {
                    teleportIndicatorFigure.SetActive(false);
                }
            }
            else
            {
                Vector3 toDirection = movementInput.normalized;
                Vector3 forward = indicatorCanvas.transform.forward;
                Vector3 right = indicatorCanvas.transform.right;

                float dot = Vector3.Dot(right, toDirection);
                var isRight = dot > 0f;
                if (isRight)
                {
                    turnLeftIndicator.fillAmount = 0;
                    float angle = Vector3.Angle(-forward, toDirection);
                    if (angle > 90) angle = 180 - angle;
                    turnRightIndicator.fillAmount = angle / 360;
                    turnRightIndicator.color = movementInput.magnitude / activationThreshold >= 1f ? Color.green : Color.red;
                }
                else
                {
                    turnRightIndicator.fillAmount = 0;
                    float angle = Vector3.Angle(-forward, toDirection);
                    if (angle > 90) angle = 180 - angle;
                    turnLeftIndicator.fillAmount = angle / 360;
                    turnLeftIndicator.color =
                        movementInput.magnitude / activationThreshold >= 1f ? Color.green : Color.red;
                }

                if (rotationMode == 1)
                {
                    Vector3 projected = Vector3.ProjectOnPlane(movementInput, forward).normalized;
                    float planeAngle = Vector3.SignedAngle(right, projected, forward);
                    indicatorCanvas.transform.Rotate(forward, planeAngle, Space.World);
                }

                Vector3 eulerCam = ActionManager.Instance.xROrigin.GetComponentInChildren<Camera>().transform.rotation.eulerAngles;
                Vector3 eulerOrigin = ActionManager.Instance.xROrigin.transform.rotation.eulerAngles;
                Vector3 combinedEuler = new Vector3(eulerOrigin.x, eulerCam.y, eulerOrigin.z);
                transform.rotation = Quaternion.Euler(combinedEuler);
                if (rotationMode == 1)
                {
                    movementDirectionIndicator.enabled = true;
                    movementDirectionIndicator.transform.parent.forward = movementInput.normalized;
                    movementDirectionIndicator.color =
                        Mathf.Clamp((movementInput.magnitude / activationThreshold) * 0.25f, 0, 0.25f) > 0.24f
                            ? Color.green
                            : Color.red;
                    movementDirectionIndicator.transform.GetChild(0).GetComponent<Image>().color =
                        movementDirectionIndicator.color;
                    movementDirectionIndicator.transform.GetChild(0).GetComponent<Image>().fillAmount =
                        movementInput.magnitude / activationThreshold;
                }
                else
                {
                    movementDirectionIndicator.enabled = false;
                }
            }
        }
    }
}
