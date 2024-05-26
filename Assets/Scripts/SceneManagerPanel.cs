using UnityEngine;

/// <summary>
/// Handles functionalities provided by a scene manager panel
/// </summary>
public class SceneManagerPanel : MonoBehaviour
{
    [SerializeField]
    private GameObject panel;
    [SerializeField]
    private GameObject debugPanel;

    private void Start()
    {
        panel.SetActive(false);
        debugPanel.SetActive(false);
    }

    public void ToggleDebugPanel()
    {
        debugPanel.SetActive(!debugPanel.active);
    }

    /// <summary>
    /// Toggles a manager panel to control specific aspects of the scene
    /// </summary>
    public void ToggleSceneManager()
    {
        bool debugShown = debugPanel.active;
        panel.SetActive(!panel.activeSelf);
        debugPanel.SetActive(debugShown);
    }
}
