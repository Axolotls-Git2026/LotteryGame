using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject targetGameObject;
    private Coroutine disableCoroutine;

    private bool isMobile;

    private void Awake()
    {
        // Detect if running on Android or iOS
#if UNITY_ANDROID || UNITY_IOS
        isMobile = true;
#else
        isMobile = false;
#endif
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isMobile)
        {
            // On mobile ? just enable immediately
            if (targetGameObject != null)
                targetGameObject.SetActive(true);
            return;
        }

        // Desktop hover logic
        if (disableCoroutine != null)
        {
            StopCoroutine(disableCoroutine);
            disableCoroutine = null;
        }

        if (targetGameObject != null)
        {
            targetGameObject.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isMobile)
        {
            // On mobile ? do nothing (leave enabled until user closes it manually)
            return;
        }

        if (targetGameObject != null)
        {
            // Start coroutine to hide after delay
            disableCoroutine = StartCoroutine(DisableWithDelay());
        }
    }

    private IEnumerator DisableWithDelay()
    {
        yield return new WaitForSeconds(0.1f);

        if (!EventSystem.current.IsPointerOverGameObject())
        {
            targetGameObject.SetActive(false);
        }
        else
        {
            if (!IsPointerOverTargetObject())
            {
                targetGameObject.SetActive(false);
            }
        }
    }

    private bool IsPointerOverTargetObject()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject == targetGameObject)
                return true;
        }

        return false;
    }
}
