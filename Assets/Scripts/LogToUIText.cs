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
            _displayContent += logMsg + "\n";
            _logHasChanged = true;
        }

        void Update()
        {
            if (_logHasChanged)
            {
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
                    txtWindow.text = _displayContent;
                    txtWindow.ForceMeshUpdate();
                }
                _logHasChanged = false;
            }
        }
    }
}
