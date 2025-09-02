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
        public Button defaultSizeButton;
        public Button quarterSizeButton;
        public Button threequarterSizeButton;
        
        public Button increasingSizeButton;
        public Button decreasingSizeButton;
        
        public Button confirmButton;
        
        public Slider scaleSlider;

        private void Start()
        {
            scaleSlider.minValue = 1/PlayModeManager.Instance.editorDataSO.maximumZoom;
            scaleSlider.maxValue = 1/PlayModeManager.Instance.editorDataSO.minimumZoom;
            scaleSlider.value = 1/PlayModeManager.Instance.currentWorldScaler.scale;
            currentSize.text = (1/PlayModeManager.Instance.currentWorldScaler.scale).ToString("0.00");

            scaleSlider.onValueChanged.AddListener((value) => currentSize.text = value.ToString("0.00"));
            maxSizeButton.GetComponentInChildren<TextMeshProUGUI>().text = (1/PlayModeManager.Instance.editorDataSO.minimumZoom).ToString("0.00");
            minSizeButton.GetComponentInChildren<TextMeshProUGUI>().text = (1/PlayModeManager.Instance.editorDataSO.maximumZoom).ToString("0.00");
            defaultSizeButton.GetComponentInChildren<TextMeshProUGUI>().text = PlayModeManager.Instance.editorDataSO.startingSize.ToString("0.00");
            threequarterSizeButton.GetComponentInChildren<TextMeshProUGUI>().text = (1/Mathf.Sqrt(PlayModeManager.Instance.editorDataSO.minimumZoom*PlayModeManager.Instance.editorDataSO.startingSize)).ToString("0.00");
            quarterSizeButton.GetComponentInChildren<TextMeshProUGUI>().text = (1/Mathf.Sqrt(PlayModeManager.Instance.editorDataSO.startingSize*PlayModeManager.Instance.editorDataSO.maximumZoom)).ToString("0.00");
            
            maxSizeButton.onClick.AddListener(delegate { ActionManager.Instance.ScalePlayer(PlayModeManager.Instance.editorDataSO.minimumZoom); });
            minSizeButton.onClick.AddListener(delegate { ActionManager.Instance.ScalePlayer(PlayModeManager.Instance.editorDataSO.maximumZoom); });
            defaultSizeButton.onClick.AddListener(delegate { ActionManager.Instance.ScalePlayer(PlayModeManager.Instance.editorDataSO.startingSize); });
            threequarterSizeButton.onClick.AddListener(delegate { ActionManager.Instance.ScalePlayer((Mathf.Sqrt(PlayModeManager.Instance.editorDataSO.minimumZoom*PlayModeManager.Instance.editorDataSO.startingSize))); });
            quarterSizeButton.onClick.AddListener(delegate { ActionManager.Instance.ScalePlayer((Mathf.Sqrt(PlayModeManager.Instance.editorDataSO.startingSize*PlayModeManager.Instance.editorDataSO.maximumZoom))); });
            increasingSizeButton.onClick.AddListener(delegate { scaleSlider.value = PlayModeManager.Instance.editorDataSO.minimumZoom + scaleSlider.value; });
            decreasingSizeButton.onClick.AddListener(delegate { scaleSlider.value = scaleSlider.value - PlayModeManager.Instance.editorDataSO.minimumZoom; });

            confirmButton.onClick.AddListener(delegate { ActionManager.Instance.ScalePlayer(1/scaleSlider.value); });
            
        }
        
    }
}
