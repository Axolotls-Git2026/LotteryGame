using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ShowRandomNums : MonoBehaviour
{
    public GameObject[] list1;
    public GameObject[] list2;
    public GameObject[] list3;

    public Coroutine animCoroutine;

    private int[] prefixes1;
    private int[] prefixes2;
    private int[] prefixes3;

    void Start()
    {
        // Assign prefixes at the beginning
        prefixes1 = AssignPrefixes(list1, 10); // 10, 11, 12...
        prefixes2 = AssignPrefixes(list2, 30); // 30, 31, 32...
        prefixes3 = AssignPrefixes(list3, 50); // 50, 51, 52...

      //  animCoroutine = StartCoroutine(AnimateNumbers());
    }

    private int[] AssignPrefixes(GameObject[] list, int startPrefix)
    {
        int[] prefixes = new int[list.Length];
        for (int i = 0; i < list.Length; i++)
        {
            prefixes[i] = startPrefix + i; // sequential prefixes
        }
        return prefixes;
    }
    private IEnumerator StartGameFlow()
    {
        yield return StartCoroutine(GameManager.instance.FetchResults(list1, list2, list3));
        yield return StartCoroutine(AnimateNumbers());
    }


    public IEnumerator AnimateNumbers()
    {
        StartCoroutine(GameManager.instance.FetchResults(list1, list2, list3));
        float duration = 2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            UpdateListWithRandom(list1, prefixes1);
            UpdateListWithRandom(list2, prefixes2);
            UpdateListWithRandom(list3, prefixes3);

            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

       GameManager.instance.ApplyStoredResults(list1, list2, list3);
        StartCoroutine(DisableDelay());
        animCoroutine = null;
    }

    IEnumerator DisableDelay()
    {
        yield return new WaitForSeconds(2f);
        this.gameObject.SetActive(false);
       GameManager.instance.buybtn.interactable = true;
        GameManager.instance.GetTimer();
        StartCoroutine(GameManager.instance.historyFetcher.FetchHistory());
      
    }

    private void UpdateListWithRandom(GameObject[] list, int[] prefixes)
    {
        // Check if the main list is null first.
        if (list == null)
        {
            Debug.LogError("The GameObject list passed to UpdateListWithRandom is null.");
            return;
        }

        // Now, check for the length and prefix array.
        if (prefixes == null || list.Length != prefixes.Length)
        {
            Debug.LogError("Array mismatch in UpdateListWithRandom: `prefixes` is null or lengths are different.");
            return;
        }

        for (int i = 0; i < list.Length; i++)
        {
            // Check if the current GameObject or its child is null
            if (list[i] != null && list[i].transform.childCount > 0 && list[i].transform.GetChild(0).TryGetComponent(out TMPro.TMP_Text tmp))
            {
                int lastTwo = Random.Range(0, 100);
                string suffix = lastTwo.ToString("00");

                tmp.text = prefixes[i].ToString("00") + suffix;
            }
            else
            {
                Debug.LogWarning($"Skipping element at index {i} because it or its child TMP_Text component is null.");
            }
        }
    }
}
