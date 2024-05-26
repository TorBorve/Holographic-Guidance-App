using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointOfReferenceHandle : MonoBehaviour
{
    [SerializeField]
    private GameObject handle;

    [SerializeField]
    private GameObject debugPanel;

    // Start is called before the first frame update
    void Start()
    {
        handle.SetActive(false);
    }

    public void ToggleHandle()
    {
        debugPanel.SetActive(!debugPanel.activeSelf);
        return;
        //handle.SetActive(!handle.activeSelf);
    }
}
