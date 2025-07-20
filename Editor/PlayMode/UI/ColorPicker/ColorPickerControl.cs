using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VRGreyboxing
{
    public class ColorPickerControl : MonoBehaviour
    {
        public float currentHue;
        public float currentSaturation;
        public float currentValue;
        
        public RawImage hueImage;
        public RawImage satValImage;
        public RawImage valueImage;
        
        public Slider hueSlider;
        
        public Slider lineWidthSlider;
        public TextMeshProUGUI lineWidthText;
        
        private Texture2D _hueTexture;
        private Texture2D _svTexture;
        private Texture2D _outputTexture;

        public Color selectedColor;


        private void Start()
        {
            CreateHueImage();
            CreateSVImage();
            CreateOutputImage();
            UpdateOutPutImage();
            Button button = gameObject.GetComponentInChildren<Button>();
            button.onClick.AddListener(delegate { ActionManager.Instance.ChangeDrawColor(selectedColor); });
            lineWidthText.text = (ActionManager.Instance.GetCurrentLineWidth() * 100f).ToString("0.00");
            lineWidthSlider.value = ActionManager.Instance.GetCurrentLineWidth();

        }

        private void CreateHueImage()
        {
            _hueTexture = new Texture2D(1, 16);
            _hueTexture.wrapMode = TextureWrapMode.Clamp;
            _hueTexture.name = "HueTexture";

            for (int i = 0; i < _hueTexture.height; i++)
            {
                _hueTexture.SetPixel(0, i, Color.HSVToRGB((float)i / _hueTexture.height, 1, /*0.05f*/ 1));
            }
            _hueTexture.Apply();
            currentHue = 0;
            hueImage.texture = _hueTexture;
        }

        private void CreateSVImage()
        {
            _svTexture = new Texture2D(16, 16);
            _svTexture.wrapMode = TextureWrapMode.Clamp;
            _svTexture.name = "SVTexture";

            for (int y = 0; y < _svTexture.height; y++)
            {
                for (int x = 0; x < _svTexture.width; x++)
                {
                    _svTexture.SetPixel(x,y,Color.HSVToRGB(currentHue,(float)x / _svTexture.width, (float)y / _svTexture.height));
                }
            }
            _svTexture.Apply();
            currentSaturation = 0;
            currentValue = 0;
                
            satValImage.texture = _svTexture;
        }

        private void CreateOutputImage()
        {
            _outputTexture = new Texture2D(1, 16);
            _outputTexture.wrapMode = TextureWrapMode.Clamp;
            _outputTexture.name = "OutputTexture";
            
            Color currentColor = Color.HSVToRGB(currentHue, (float)currentSaturation, (float)currentValue);

            for (int i = 0; i < _outputTexture.height; i++)
            {
                _outputTexture.SetPixel(0, i, currentColor);
            }
            _outputTexture.Apply();
            valueImage.texture = _outputTexture;
        }

        private void UpdateOutPutImage()
        {
            Color currentColor = Color.HSVToRGB(currentHue, currentSaturation, currentValue);
            for (int i = 0; i < _outputTexture.height; i++)
            {
                _outputTexture.SetPixel(0, i, currentColor);
            }
            _outputTexture.Apply();
            selectedColor = currentColor;
        }

        public void SetSV(float s, float v)
        {
            currentSaturation = s;
            currentValue = v;
            UpdateOutPutImage();
        }

        public void UpdateSVImage()
        {
            currentHue = hueSlider.value;
            for (int y = 0; y < _svTexture.height; y++)
            {
                for (int x = 0; x < _svTexture.width; x++)
                {
                    _svTexture.SetPixel(x,y,Color.HSVToRGB(currentHue,(float)x / _svTexture.width, (float)y / _svTexture.height));
                }
            }
            _svTexture.Apply();
            UpdateOutPutImage();
        }

        public void UpdateLineWidth()
        {
            float lineWidth = lineWidthSlider.value;
            lineWidthText.text = (lineWidth * 100f).ToString("0.00");
            ActionManager.Instance.ChangeLineWidth(lineWidth);
        }
        
        
        
    }
}
