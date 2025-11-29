using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(TMP_InputField))]
public class InputFieldColor : MonoBehaviour
{
    public Color normalColor = Color.white;
    public Color selectedColor = Color.green;

    private TMP_InputField inputField;
    private Image backgroundImage;

    void Awake()
    {
        inputField = GetComponent<TMP_InputField>();
        backgroundImage = GetComponent<Image>(); // assumes the same object has an Image

        if (backgroundImage == null)
        {
            Debug.LogWarning("No Image component found on input field object.");
        }
    }

    void Start()
    {
        // Subscribe to events
        inputField.onSelect.AddListener(OnSelected);
        inputField.onDeselect.AddListener(OnDeselected);

        // Set initial color
        if (backgroundImage != null)
            backgroundImage.color = normalColor;
    }

    private void OnSelected(string text)
    {
        if (backgroundImage != null)
            backgroundImage.color = selectedColor;
    }

    private void OnDeselected(string text)
    {
        if (backgroundImage != null)
            backgroundImage.color = normalColor;
    }
}
