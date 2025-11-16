using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using WiimoteApi;

public class GameOverManager : MonoBehaviour
{
    [SerializeField]
    TMP_Text instruction1, instruction2, coinCount, timeLimitText, First,Second,Third;
    [SerializeField]
    Image timeGage;
    [SerializeField]
    float limitTime = 30f;
    float timeLimit;
    int requiredCoins = 1;
    Wiimote wiimote1, wiimote2;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        instruction1 = instruction1.GetComponent<TMP_Text>();
        instruction2 = instruction2.GetComponent<TMP_Text>();
        coinCount = coinCount.GetComponent<TMP_Text>();
        timeLimitText = timeLimitText.GetComponent<TMP_Text>();
        timeGage = timeGage.GetComponent<Image>();
        First = First.GetComponent<TMP_Text>();
        Second = Second.GetComponent<TMP_Text>();
        Third = Third.GetComponent<TMP_Text>();
        WiimoteManager.FindWiimotes();
    }

    public void Initialize()
    {
        requiredCoins = Player.Instance.is2P ? 2 : 1;

        if(Player.Instance.coinCount >= requiredCoins)
        {
            instruction1.text = "トリガー(Bボタン)を押してリトライ!";
            instruction2.text = "ホームを押すと終了します。差額はスタッフに返金を申し出て下さい。";
            coinCount.text = $"投入金額: {Player.Instance.coinCount*100}円";
            timeGage.fillAmount = 1f;
            timeLimit = limitTime;
            timeLimitText.text = Mathf.CeilToInt(timeLimit).ToString();
        }
        else
        {
            instruction1.text = "リトライするには100円玉を入れてください。";
            instruction2.text = $"残り {(requiredCoins - Player.Instance.coinCount) * 100}円";
            coinCount.text = $"投入金額: {Player.Instance.coinCount*100}円";
            timeGage.fillAmount = 1f;
            timeLimit = limitTime;
            timeLimitText.text = Mathf.CeilToInt(timeLimit).ToString();
        }

        First.text = PlayerPrefs.HasKey("First") ? $"1位: {PlayerPrefs.GetInt("First")}" : "1位: 0";
        Second.text = PlayerPrefs.HasKey("Second") ?  $"2位: {PlayerPrefs.GetInt("Second")}" : "2位: 0";
        Third.text = PlayerPrefs.HasKey("Third") ? $"3位: {PlayerPrefs.GetInt("Third")}" : "3位: 0";
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Player.Instance.AddCoin(1);
        }

        wiimote1 = WiimoteManager.Wiimotes[0];

        if (WiimoteManager.Wiimotes.Count > 1)
        {
            wiimote2 = WiimoteManager.Wiimotes[1];
        }

        coinCount.text = $"投入金額: {Player.Instance.coinCount * 100}円";

        if (Player.Instance.coinCount < requiredCoins)
        {
            instruction2.text = $"残り {(requiredCoins - Player.Instance.coinCount) * 100}円";
            timeLimit -= Time.unscaledDeltaTime;
            timeGage.fillAmount = timeLimit / limitTime;
        }
        else
        {
            instruction1.text = "トリガー(Bボタン)を押してリトライ!";
            instruction2.text = "ホームを押すと終了します。差額はスタッフに返金を申し出て下さい。";
        }

        timeLimitText.text = Mathf.CeilToInt(timeLimit).ToString();
        bool b2 = wiimote2 != null ? wiimote2.Button.b : false;

        if (Player.Instance.coinCount >= requiredCoins && (wiimote1.Button.b || b2))
        {
            Player.Instance.Revive();
            Player.Instance.AddCoin(-requiredCoins);
            Time.timeScale = 1f;
            gameObject.SetActive(false);
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
