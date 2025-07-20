using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace VRGreyboxing
{ 
    public class ScaleController : MonoBehaviour
    {
        public TextMeshProUGUI currentSize;

        public Button maxSizeButton;
        public Button minSizeButton;
        public Button quarterSizeButton;
        public Button threequarterSizeButton;
        
        public Button increasingSizeButton;
        public Button decreasingSizeButton;
        
        public Button confirmButton;
        
        public Slider scaleSlider;

        private void Start()
        {
            scaleSlider.minValue = PlayModeManager.Instance.editorDataSO.minimumZoom;
            scaleSlider.maxValue = PlayModeManager.Instance.editorDataSO.maximumZoom;
            scaleSlider.value = ActionManager.Instance.xROrigin.transform.localScale.x;
            currentSize.text = ActionManager.Instance.xROrigin.transform.localScale.x.ToString();

            scaleSlider.onValueChanged.AddListener((value) => currentSize.text = value.ToString("0.000"));
            currentSize.text = ActionManager.Instance.xROrigin.transform.localScale.x.ToString("0.000");
            maxSizeButton.GetComponentInChildren<TextMeshProUGUI>().text = PlayModeManager.Instance.editorDataSO.maximumZoom.ToString();
            minSizeButton.GetComponentInChildren<TextMeshProUGUI>().text = PlayModeManager.Instance.editorDataSO.minimumZoom.ToString();
            threequarterSizeButton.GetComponentInChildren<TextMeshProUGUI>().text = (PlayModeManager.Instance.editorDataSO.maximumZoom*0.75f).ToString();
            quarterSizeButton.GetComponentInChildren<TextMeshProUGUI>().text = (PlayModeManager.Instance.editorDataSO.maximumZoom*0.25f).ToString();
            
            maxSizeButton.onClick.AddListener(delegate { ActionManager.Instance.ScalePlayer(PlayModeManager.Instance.editorDataSO.maximumZoom); });
            minSizeButton.onClick.AddListener(delegate { ActionManager.Instance.ScalePlayer(PlayModeManager.Instance.editorDataSO.minimumZoom); });
            quarterSizeButton.onClick.AddListener(delegate { ActionManager.Instance.ScalePlayer(PlayModeManager.Instance.editorDataSO.maximumZoom*0.25f); });
            threequarterSizeButton.onClick.AddListener(delegate { ActionManager.Instance.ScalePlayer(PlayModeManager.Instance.editorDataSO.maximumZoom*0.75f); });
            increasingSizeButton.onClick.AddListener(delegate { scaleSlider.value = PlayModeManager.Instance.editorDataSO.maximumZoom/10 + scaleSlider.value; });
            decreasingSizeButton.onClick.AddListener(delegate { scaleSlider.value = scaleSlider.value - PlayModeManager.Instance.editorDataSO.maximumZoom/10; });

            confirmButton.onClick.AddListener(delegate { ActionManager.Instance.ScalePlayer(scaleSlider.value); });
            
        }
        
    }
}
