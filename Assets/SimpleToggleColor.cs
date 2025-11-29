using UnityEngine;
using UnityEngine.UI;

public class SimpleToggleColor : MonoBehaviour
{
    public Toggle toggle;
    public Color normalColor = Color.white;
    public Color selectedColor = Color.green;

    private Image btnImage;

    private void Awake()
    {
        btnImage = GetComponent<Image>();
        if (toggle != null)
            toggle.onValueChanged.AddListener(UpdateColor);
    }

    private void Start()
    {
        UpdateColor(toggle != null && toggle.isOn);
    }

    private void UpdateColor(bool isOn)
    {
        if (btnImage != null)
            btnImage.color = isOn ? selectedColor : normalColor;
    }

    // optional: call this from button’s OnClick to flip toggle
    public void OnButtonClick()
    {
        if (toggle != null)
            toggle.isOn = !toggle.isOn;
    }
}
