using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class QuantityPointsManager : MonoBehaviour
{

    public List<GameObject> quantity;
    public List<GameObject> points;
    public float multiplier;

    public SeriesManager seriesMgr;

    public TMP_Text quantityTotalTxt;
    public TMP_Text PointsTotalTxt;

    public AdvanceTime advanceTime;

    private void OnEnable()
    {
        SeriesManager.OnQuantityAdded += OnQuantityAdded;
    }

    private void OnDisable()
    {
        SeriesManager.OnQuantityAdded -= OnQuantityAdded;
    }

    private void Start()
    {
        for (int i = 0; i <= quantity.Count - 1; i++)
        {
            quantity[i].transform.GetChild(0).GetComponent<TMP_Text>().text = "0";
        }
        for (int i = 0; i <= points.Count - 1; i++)
        {
            points[i].transform.GetChild(0).GetComponent<TMP_Text>().text = "0";
        }
    }
    void OnDestroy()
    {
        SeriesManager.OnQuantityAdded -= OnQuantityAdded;
    }



    void OnQuantityAdded(int amount, List<int> series, List<int> range)
    {
        // If a specific range is provided, update only that range
        if (range != null && range.Count > 0)
        {
            foreach (var rangeIndex in range)
            {
                if (rangeIndex >= 0 && rangeIndex < quantity.Count)
                {
                    // Update the specific range's quantity
                    quantity[rangeIndex].transform.GetChild(0).GetComponent<TMP_Text>().text = (amount * advanceTime.selectedTimes.Count).ToString();

                    // Update the specific range's points
                    float pointsValue = amount * multiplier;
                    points[rangeIndex].transform.GetChild(0).GetComponent<TMP_Text>().text = (pointsValue * advanceTime.selectedTimes.Count).ToString();

                   // Debug.Log($"[OnQuantityAdded] Range {rangeIndex}: Qty={amount}, Points={pointsValue}");
                }
            }

            UpdateFinalTotal();
        }
        else
        {
            // Fallback: if no range specified, update all ranges with the same value
            // (This might be your old logic, but should rarely be called now)
            foreach (var indx in Enumerable.Range(0, quantity.Count))
            {
                quantity[indx].transform.GetChild(0).GetComponent<TMP_Text>().text = (amount*series.Count * advanceTime.selectedTimes.Count).ToString();
                points[indx].transform.GetChild(0).GetComponent<TMP_Text>().text = (amount * multiplier * advanceTime.selectedTimes.Count).ToString();
            }
            UpdateFinalTotal();
        }
    }

    void OnPointsAdded(float amount, List<int> series, List<int> range)
    {
        float aftercalculation;
        foreach (var indx in range)
        {
            aftercalculation = amount * multiplier;
            points[indx].transform.GetChild(0).GetComponent<TMP_Text>().text = aftercalculation.ToString();
        }
        UpdateFinalTotal();

    }

    void UpdateFinalTotal()
    {
        int total = 0;
        for (int i = 0; i <= quantity.Count - 1; i++)
        {
            total += int.Parse(quantity[i].transform.GetChild(0).GetComponent<TMP_Text>().text);
        }
        quantityTotalTxt.text = total.ToString();

        int totalpoints = 0;
        for (int i = 0; i <= points.Count - 1; i++)
        {
            totalpoints += int.Parse(points[i].transform.GetChild(0).GetComponent<TMP_Text>().text);
        }
        PointsTotalTxt.text = (total * multiplier).ToString();
    }

    public void ClearData()
    {
        for (int i = 0; i <= quantity.Count - 1; i++)
        {
            quantity[i].transform.GetChild(0).GetComponent<TMP_Text>().text = "0";
        }
        for (int i = 0; i <= points.Count - 1; i++)
        {
            points[i].transform.GetChild(0).GetComponent<TMP_Text>().text = "0";
        }


        UpdateFinalTotal();

    }

}
