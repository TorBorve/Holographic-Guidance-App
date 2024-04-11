using UnityEngine;
using UnityEngine.UI;

namespace Tutorials
{
    public class LogToUIText : MonoBehaviour
    {
        Text logText;

        void Start()
        {
            logText = GetComponent<Text>();
        }

        void Update()
        {
            // Clear log text
            logText.text = "";
        }
    }
}
