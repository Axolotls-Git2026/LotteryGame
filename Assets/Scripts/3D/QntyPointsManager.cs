using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class QntyPointsManager : MonoBehaviour
{
    [System.Serializable]
    public class SpotData
    {
        public string spotName;       // STR, BOX, FP, BP, SP, AP
        public int quantity;          // bets placed
        public int points;            // total points
        public TMP_Text quantityText; // UI ref
        public TMP_Text pointsText;   // UI ref
        public int rate;
    }

    public List<SpotData> spots = new List<SpotData>();

    private Dictionary<string, SpotData> spotLookup;

    public int totalPoints;
    public int totalQuantity;

    public TMP_Text totalPointsTxt;
    public TMP_Text totalQuantityTxt;

    public OutputNumGenerator outputNumGenerator;

    private void Awake()
    {
        // Build spot lookup
        spotLookup = new Dictionary<string, SpotData>();
        foreach (var spot in spots)
        {
            if (!spotLookup.ContainsKey(spot.spotName))
                spotLookup.Add(spot.spotName, spot);
        }

        UpdateTotals(); // initialize UI
    }

    /// <summary>
    /// Add a bet to a specific spot.
    /// </summary>
    public void AddBet(string spotName, int rate,int generatedUnits)
    {
        if (spotLookup.TryGetValue(spotName, out SpotData spot))
        {
            spot.quantity += outputNumGenerator.resultSelectedCount * generatedUnits * PlayerPrefs.GetInt("selectedTimes");
            spot.points += rate * outputNumGenerator.resultSelectedCount * generatedUnits * PlayerPrefs.GetInt("selectedTimes");
            spot.rate = rate;

            UpdateUI(spot);
            UpdateTotals();
        }
        else
        {
            Debug.LogWarning($"Invalid spotName: {spotName}");
        }
    }

    /// <summary>
    /// Remove a bet from a specific spot.
    /// </summary>
    public void RemoveBet(string spotName, int rate)
    {
        if (spotLookup.TryGetValue(spotName, out SpotData spot))
        {
            if (spot.quantity > 0) spot.quantity--;
            if (spot.points >= rate) spot.points -= rate;
            else spot.points = 0; // safety

            UpdateUI(spot);
            UpdateTotals();
        }
        else
        {
            Debug.LogWarning($"Invalid spotName: {spotName}");
        }
    }

    /// <summary>
    /// Calculate winnings for all spots based on winning number.
    /// </summary>
    public void CalculateWinnings(string winningNumber, string playerNumber)
    {
        foreach (var spot in spots)
        {
            if (CheckWinCondition(spot.spotName, winningNumber, playerNumber))
            {
                int winPoints = Mathf.RoundToInt(spot.points * spot.rate);
                spot.points += winPoints;
            }
            UpdateUI(spot);
        }

        UpdateTotals();
    }

    private bool CheckWinCondition(string spotName, string winNum, string playerNum)
    {
        return spotName switch
        {
            "STR" => winNum == playerNum,
            "BOX" => IsBoxMatch(winNum, playerNum),
            "FP" => winNum[..2] == playerNum[..2],                  // first 2 digits
            "BP" => winNum[^2..] == playerNum[^2..],                // last 2 digits
            "SP" => winNum.Length >= 3 && winNum[0] == playerNum[0]
                                      && winNum[2] == playerNum[2], // split (1st & 3rd)
            "AP" => HasAnyPair(winNum),
            _ => false,
        };
    }

    private bool IsBoxMatch(string winNum, string playerNum)
    {
        char[] w = winNum.ToCharArray();
        char[] p = playerNum.ToCharArray();
        System.Array.Sort(w);
        System.Array.Sort(p);
        return new string(w) == new string(p);
    }

    private bool HasAnyPair(string num)
    {
        for (int i = 0; i < num.Length; i++)
        {
            for (int j = i + 1; j < num.Length; j++)
            {
                if (num[i] == num[j]) return true;
            }
        }
        return false;
    }

    private void UpdateUI(SpotData spot)
    {
        if (spot.quantityText)
            spot.quantityText.text = spot.quantity.ToString();

        if (spot.pointsText)
            spot.pointsText.text = spot.points.ToString();
    }

    /// <summary>
    /// Recalculate and update total quantity & points UI.
    /// </summary>
    private void UpdateTotals()
    {
        totalPoints = 0;
        totalQuantity = 0;

        foreach (var spot in spots)
        {
            totalPoints += spot.points;
            totalQuantity += spot.quantity;
        }

        if (totalPointsTxt)
            totalPointsTxt.text = totalPoints.ToString();

        if (totalQuantityTxt)
            totalQuantityTxt.text = totalQuantity.ToString();
    }

    /// <summary>
    /// Reset all spots for a new round.
    /// </summary>
    public void ResetSpots()
    {
        foreach (var spot in spots)
        {
            spot.quantity = 0;
            spot.points = 0;
            UpdateUI(spot);
        }

        UpdateTotals();
    }
}
