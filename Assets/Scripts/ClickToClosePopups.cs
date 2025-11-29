using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class ClickToClosePopups : MonoBehaviour
{
    // A list of the pop-up/UI objects you want to close
    public List<GameObject> popupsToClose;

    // This method is called once per frame
    void Update()
    {
        // Check if the left mouse button was clicked
        if (Input.GetMouseButtonDown(0))
        {
            // Check if the pointer is over ANY UI element first
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                // If the click is not on any UI, check if it's on a pop-up
                bool clickedOnPopup = false;
                foreach (GameObject popup in popupsToClose)
                {
                    // Ensure the pop-up is active before checking its bounds
                    if (popup.activeInHierarchy)
                    {
                        RectTransform rectTransform = popup.GetComponent<RectTransform>();
                        if (rectTransform != null && RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition))
                        {
                            clickedOnPopup = true;
                            break;
                        }
                    }
                }

                // If the click was not on any of the pop-ups, close them all
                if (!clickedOnPopup)
                {
                    CloseAllPopups();
                }
            }
        }
    }

    // A public method to close all pop-ups in the list
    private void CloseAllPopups()
    {
        foreach (GameObject popup in popupsToClose)
        {
            popup.SetActive(false);
        }
    }
}