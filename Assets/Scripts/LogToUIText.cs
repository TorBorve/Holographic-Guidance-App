using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

namespace Tutorials
{
    public class LogToUIText : MonoBehaviour
    {
        [SerializeField]
        private GameObject textWindowObject;

        private string _displayContent = "";
        private bool _logHasChanged = false;

        // Avoid obvoius overflow
        private const int MAX_LOG_LINES = 20;
        private int _currentAmountLogLines = 0;

        public void logInfo(string logMsg)
        {
            log("[INFO]: " + logMsg);
        }

        public void logWarn(string logMsg)
        {
            log("[WARN]: " + logMsg);
        }

        public void logError(string logMsg)
        {
            log("[Error]: " + logMsg);
        }

        public void log(string logMsg)
        {
            Debug.Log(logMsg);
            if (textWindowObject.active) // Do not log if the panel is not active
            {
                _displayContent += logMsg + "\n";
                _currentAmountLogLines += 1;
                _logHasChanged = true;
            } else
            {
                Debug.Log("Debug Panel not active");
            }
        }

        void Update()
        {
            if (_logHasChanged)
            {
                // Fixed problem with isTextOverflowing is not updated if panel is not shown.
                int overflowingLines = _currentAmountLogLines - MAX_LOG_LINES;
                for (int i = 0; i < overflowingLines; i++)
                {
                     int firstNewline = _displayContent.IndexOf('\n');
                    if (firstNewline == -1)
                    {
                        break;
                    }
                    _displayContent = _displayContent.Substring(firstNewline + 1);
                    _currentAmountLogLines -= 1;
                }
                TextMeshProUGUI txtWindow = textWindowObject.GetComponent<TextMeshProUGUI>();
                txtWindow.text = _displayContent;
                txtWindow.ForceMeshUpdate();
                while(txtWindow.isTextOverflowing)
                {
                    int firstNewline = _displayContent.IndexOf('\n');
                    if (firstNewline == -1)
                    {
                        break;
                    }
                    _displayContent = _displayContent.Substring(firstNewline + 1);
                    _currentAmountLogLines -= 1;
                    txtWindow.text = _displayContent;
                    txtWindow.ForceMeshUpdate();
                }
                _logHasChanged = false;
            }
        }
    }
}
