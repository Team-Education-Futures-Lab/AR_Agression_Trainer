using UnityEngine;
using TMPro;
#if WINDOWS_UWP
using Microsoft.MixedReality.Toolkit.Experimental.UI;
#endif

public class InputHandler : MonoBehaviour
{
    [SerializeField]
    private TMP_Text textHandler;

    #if WINDOWS_UWP
        private SystemKeyboard holoKeyboard;
    #endif

    private void Start()
    {
        if (textHandler != null)
        {
            textHandler.text = "";   
        }
    }

    private void Update()
    {
        #if WINDOWS_UWP
                if (holoKeyboard != null)
                {
                    // Update the TMP text as user types
                    textHandler.text = holoKeyboard.Text;
                }
        #endif
    }

    public void OpenKeyboard()
    {
        #if WINDOWS_UWP
                if (holoKeyboard == null || !holoKeyboard.Visible)
                {
                    holoKeyboard = SystemKeyboard.Open();
                }
        #else
                Debug.Log("System Keyboard only works on UWP (HoloLens).");
        #endif
    }
}