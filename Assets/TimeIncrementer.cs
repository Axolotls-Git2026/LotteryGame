using System;
using UnityEngine;
using TMPro;

public class TimeIncrementer : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text timeText;

    [Header("Start Time")]
    public string startTimeString = "2025-08-24 00:43:00";

    private DateTime currentTime;
    public TMP_Text startTimeTxt;
    private float timer = 0f;

    private void Start()
    {
    }

    public void StartHeaderCurrentTime()
    {
        startTimeString = startTimeTxt.text;
        if (DateTime.TryParse(startTimeString, out currentTime))
        {
            // Debug.Log("? Parsed start time: " + currentTime);
        }
        else
        {
            Debug.LogError("? Failed to parse time string: " + startTimeString);
        }
    }

    private void Update()
    {
        // Add the time passed since the last frame to our timer
        timer += Time.deltaTime;

        // If a full second has passed, increment the time
        if (timer >= 1f)
        {
            IncrementTime();
            timer -= 1f; // Subtract 1 second to handle any excess time
        }
    }

    private void IncrementTime()
    {
        currentTime = currentTime.AddSeconds(1);

        if (timeText != null)
            timeText.text = currentTime.ToString("dd-MM-yyyy HH:mm:ss");
    }
}