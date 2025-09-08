using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameAPIs
{

    public static string baseUrl = "https://akshay.axolotls.in/lottery_v2/api/";


    public static string loginAPi =
        baseUrl + "auth/login"; //param  id,password 

    public static string getUserDataAPi = baseUrl + "fetch"; //param  id
    public static string submitBetAPi = baseUrl + "fetch/submit_bet"; //param  id
    public static string getResultsAPi = baseUrl + "fetch/results"; //param  id
    public static string getTimerAPi = baseUrl + "fetch/get_time"; //param  id
    public static string advanceTimeAPi = baseUrl + "fetch/slots"; //param  id
    public static string fetchResultAPi = baseUrl + "fetch/result_history"; //param  id
    public static string fetchHistoryAPi = baseUrl + "fetch/bet_history"; //param  id
    public static string fetchHistoryDetailsAPi = baseUrl + "fetch/show_bets"; //param  id
    public static string cancelBetAPi = baseUrl + "fetch/cancel_bet"; //param  id



    //for Manual Payment Bank Details
    public static string bankName;
    public static string bankAcountNumber;
    public static string bankIFSEID;
    public static readonly string login_Done = "login_done";

    public static string user_id = "User_Id";
    public static readonly string user_Password = "User_password";
    public static readonly string user_name = "User_Name";






    public static string GetUserPassword()
    {
        return PlayerPrefs.GetString(user_Password);
    }

    public static void SetUserName(string user)
    {
        PlayerPrefs.SetString(user_name, user);
    }
    public static string GetUserName()
    {
        return PlayerPrefs.GetString(user_name);
    }

    public static int GetUserID()
    {
        return PlayerPrefs.GetInt(user_id);
    }


}
