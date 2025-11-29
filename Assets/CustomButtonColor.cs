using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CustomButtonColor : MonoBehaviour
{
    private Image buttonImage;
    [Header("Buttons")]
    public Button buttonA;
    public Button buttonB;


    public Image imageA;
    public Image imageB;

    [Header("Toggle Reference")]
    public Toggle toggle;

    [Header("Colors")]
    public Color toggleOnColor = Color.green;
    public Color toggleOffColor = Color.red;

    private void Awake()
    {
        if (buttonA != null) imageA = buttonA.GetComponent<Image>();
        if (buttonB != null) imageB = buttonB.GetComponent<Image>();
      
        // Add listeners
    
    }


    

    private void SetActiveButton(Button activeButton)
    {
        if (buttonA == null || buttonB == null) return;

        if (activeButton == buttonA)
        {
            imageA.color = toggleOnColor;
            imageB.color = toggleOffColor;
        }
        else if (activeButton == buttonB)
        {
            imageA.color = toggleOffColor;
            imageB.color = toggleOnColor;
        }
    }

    /// <summary>
    /// Call this function (or subscribe in code) to update button color
    /// based on the toggle's current state
    /// </summary>
    public void UpdateColorFromToggle()
    {
        if (toggle == null || buttonImage == null) return;

        buttonImage.color = toggle.isOn ? toggleOnColor : toggleOffColor;
    }

    private void Start()
    {
        // If toggle is assigned, listen to value changes automatically
        if (toggle != null)
            toggle.onValueChanged.AddListener(delegate { UpdateColorFromToggle(); });

        // Initialize the button’s color based on the toggle’s value
        UpdateColorFromToggle();
        if (buttonA != null)
        {
            SetActiveButton(buttonA.GetComponent<Button>());
            buttonA.GetComponent<Button>().onClick.AddListener(() => SetActiveButton(buttonA.GetComponent<Button>()));
        }
        if (buttonB != null)
        {
            buttonB.GetComponent<Button>().onClick.AddListener(() => SetActiveButton(buttonB.GetComponent<Button>()));
        }
    }
}
