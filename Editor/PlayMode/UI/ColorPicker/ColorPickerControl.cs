using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VRGreyboxing
{
    
    /**
     * Menu to change drawing settings
     */
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


        /**
         * Initialize colour picker components
         */
        private void Start()
        {
            CreateHueImage();
            CreateSVImage();
            CreateOutputImage();
            UpdateOutputImage();
            Button button = gameObject.GetComponentInChildren<Button>();
            button.onClick.AddListener(delegate { ActionManager.Instance.ChangeDrawColor(selectedColor); });
            lineWidthText.text = (ActionManager.Instance.GetCurrentLineWidth() * 100f).ToString("0.00");
            lineWidthSlider.value = ActionManager.Instance.GetCurrentLineWidth();

        }

        /**
         * Create background image for hue selection
         */
        private void CreateHueImage()
        {
            _hueTexture = new Texture2D(1, 16);
            _hueTexture.wrapMode = TextureWrapMode.Clamp;
            _hueTexture.name = "HueTexture";

            for (int i = 0; i < _hueTexture.height; i++)
            {
                _hueTexture.SetPixel(0, i, Color.HSVToRGB((float)i / _hueTexture.height, 1, 1));
            }
            _hueTexture.Apply();
            currentHue = 0;
            hueImage.texture = _hueTexture;
        }

        /**
         * Create initial selection image for saturation and value
         */
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

        /**
         * Display initial selected colour in output image
         */
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

        /**
         * Update colour of output image depending on current hue,saturation and value
         */
        private void UpdateOutputImage()
        {
            Color currentColor = Color.HSVToRGB(currentHue, currentSaturation, currentValue);
            for (int i = 0; i < _outputTexture.height; i++)
            {
                _outputTexture.SetPixel(0, i, currentColor);
            }
            _outputTexture.Apply();
            selectedColor = currentColor;
        }

        /**
         * Set new saturation and value values
         */
        public void SetSV(float s, float v)
        {
            currentSaturation = s;
            currentValue = v;
            UpdateOutputImage();
        }

        /**
         * Update selection image of saturation and value depending on current hue
         */
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
            UpdateOutputImage();
        }

        /**
         * Change line width depending on slider value
         */
        public void UpdateLineWidth()
        {
            float lineWidth = lineWidthSlider.value;
            lineWidthText.text = (lineWidth * 100f).ToString("0.00");
            ActionManager.Instance.ChangeLineWidth(lineWidth);
        }
        
        
        
    }
}
