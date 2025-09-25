using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VRGreyboxing
{
    /**
     * Keyboard key logic 
     */
    public class KeyboardKey : MonoBehaviour
    {
        public string keyCharacter;
        public string shiftKeyCharacter;
        public bool isBackspace;
        public bool isEnter;
        public bool isShift;

        public event EventHandler OnSubmitted;
        public event EventHandler OnToggleShift;

        /**
         * Add configured character to current input field
         */
        public void PressKey(TMP_InputField inputField,bool shiftKey)
        {
            if (isEnter)
            {
                OnSubmitted?.Invoke(this, EventArgs.Empty);
                Debug.Log("Submitted");
                return;
            }

            if (isShift)
            {
                OnToggleShift?.Invoke(this, EventArgs.Empty);
                return;
            }
            
            if (isBackspace)
            {
                inputField.text = inputField.text.Substring(0, inputField.text.Length - 1);
            }
            else
            {
                inputField.text += (shiftKey ? shiftKeyCharacter : keyCharacter);
            }
        }
    }
}
