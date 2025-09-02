using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRGreyboxing;

public class RadialSelection : MonoBehaviour
{
    private int _numberOfParts;
    public GameObject radialPartPrefab;
    public Transform mainMenuCanvas;
    public float angleSpacing;
    private Transform _handTransform;
    
    public GameObject optionIconPrefab;
    public float optionRadius;
    
    [HideInInspector]
    public int currentSelectedPart = -1;
    private List<GameObject> _radialParts = new();
    private List<string> _optionNames = new();

    private void Update()
    {
        GetSelectedRadialParts();
    }

    private void Start()
    {
    }

    public void GetSelectedRadialParts()
    {
        Vector3 centerToHand = _handTransform.position-mainMenuCanvas.position;
        Vector3 centerToHandProjected = Vector3.ProjectOnPlane(centerToHand, mainMenuCanvas.forward);
        if (centerToHandProjected.magnitude < optionRadius *  mainMenuCanvas.localScale.x)
        {
            for (int i = 0; i < _radialParts.Count; i++)
            {
                    _radialParts[i].GetComponent<Image>().color = Color.grey;
                    _radialParts[i].transform.localScale = Vector3.one;
            }
            currentSelectedPart = -1;
            mainMenuCanvas.GetComponentInChildren<TextMeshPro>().text = "";
            return;
        }

        float angle = Vector3.SignedAngle(mainMenuCanvas.up, centerToHandProjected, -mainMenuCanvas.forward);
        
        angle = angle < 0 ? 360 + angle : angle;
        
        currentSelectedPart = (int) angle * _numberOfParts / 360;

        for (int i = 0; i < _radialParts.Count; i++)
        {
            if (currentSelectedPart == i)
            {
                _radialParts[i].GetComponent<Image>().color = Color.yellow;
                _radialParts[i].transform.localScale = 1.1f * Vector3.one;
                mainMenuCanvas.GetComponentInChildren<TextMeshPro>().text = _optionNames[i];
            }
            else
            {
                _radialParts[i].GetComponent<Image>().color = Color.grey;
                _radialParts[i].transform.localScale = Vector3.one;
            }
        }
    }

    public void SpawnRadialParts(List<Sprite> optionImages, List<string> optionNames,GameObject usedController)
    {

        foreach (var part in _radialParts)
        {
            Destroy(part);
        }
        
        _radialParts.Clear();
        currentSelectedPart = -1;
        _handTransform = usedController.transform;
        mainMenuCanvas.GetComponentInChildren<TextMeshPro>().text = "";
        _numberOfParts = optionImages.Count;
        _optionNames = optionNames;


        for (int i = 0; i < _numberOfParts; i++)
        {
            float angle = - i * 360 / _numberOfParts - angleSpacing / 2;
            Vector3 radialPartEulerAngle = new Vector3(0, 0, angle);
            
            GameObject radialPart = Instantiate(radialPartPrefab, mainMenuCanvas);
            radialPart.transform.position = mainMenuCanvas.position;
            radialPart.transform.localEulerAngles = radialPartEulerAngle;
            radialPart.GetComponent<Image>().color = Color.grey;

            
            Vector3 partVector = Quaternion.AngleAxis(angle - 360/_numberOfParts/2 + angleSpacing/2, transform.forward) * transform.up;
            GameObject optionIcon = Instantiate(optionIconPrefab, radialPart.transform);
            optionIcon.transform.position = transform.position + new Vector3(0,0,-1)* mainMenuCanvas.localScale.x + partVector * (optionRadius * mainMenuCanvas.localScale.x);
            optionIcon.transform.eulerAngles = Vector3.zero;
            optionIcon.GetComponent<Image>().sprite = optionImages[i];
            //optionIcon.transform.forward = -usedController.transform.forward;
            optionIcon.transform.right = usedController.transform.right;
            radialPart.GetComponent<Image>().fillAmount = (1 / (float)_numberOfParts) - (angleSpacing/360);
            
            _radialParts.Add(radialPart);
        }
    }
}
