using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using WiimoteApi;

public class GameClearManager : MonoBehaviour
{
    [SerializeField]
    TMP_Text timeLimitText, First, Second, Third;
    [SerializeField]
    Image timeGage;
    [SerializeField]
    float limitTime = 30f;
    float timeLimit;
    Wiimote wiimote1, wiimote2;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        timeLimitText = timeLimitText.GetComponent<TMP_Text>();
        timeGage = timeGage.GetComponent<Image>();
        First = First.GetComponent<TMP_Text>();
        Second = Second.GetComponent<TMP_Text>();
        Third = Third.GetComponent<TMP_Text>();
        WiimoteManager.FindWiimotes();
    }

    public void Initialize()
    {
        timeGage.fillAmount = 1f;
        timeLimit = limitTime;
        First.text = PlayerPrefs.HasKey("First") ? $"1ˆÊ: {PlayerPrefs.GetInt("First")}" : "1ˆÊ: 0";
        Second.text = PlayerPrefs.HasKey("Second") ? $"2ˆÊ: {PlayerPrefs.GetInt("Second")}" : "2ˆÊ: 0";
        Third.text = PlayerPrefs.HasKey("Third") ? $"3ˆÊ: {PlayerPrefs.GetInt("Third")}" : "3ˆÊ: 0";
    }

    // Update is called once per frame
    void Update()
    {
        timeLimit -= Time.unscaledDeltaTime;
        timeGage.fillAmount = timeLimit / limitTime;

        timeLimitText.text = Mathf.CeilToInt(timeLimit).ToString();

        if (WiimoteManager.Wiimotes.Count <= 0)
        {
            WiimoteManager.FindWiimotes();
            return;
        }

        wiimote1 = WiimoteManager.Wiimotes[0];

        if (WiimoteManager.Wiimotes.Count > 1)
        {
            wiimote2 = WiimoteManager.Wiimotes[1];
        }

        bool home2 = wiimote2 != null ? wiimote2.Button.home : false;

        if (timeLimit <= 0f || wiimote1.Button.home || home2)
        {
            Player.Instance.titleInitialized = false;
            Player.Instance.AddCoin(-Player.Instance.coinCount);
            Player.Instance.LockCursor();
            Player.Instance.UpdateRanking();
            Player.Instance.ResetScore();
            Time.timeScale = 1f;
            SceneManager.LoadScene("Title");
        }
    }
}
