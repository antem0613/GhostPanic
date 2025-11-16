using UnityEngine;
using UnityEngine.UI;
using WiimoteApi;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using TMPro;

public class WiimoteRegister : MonoBehaviour
{
    [SerializeField]
    GameObject counter;
    [SerializeField]
    GameObject counterParent;
    [SerializeField]
    RectTransform[] ir_pointer;
    [SerializeField]
    ZapUI Zap1, Zap2;
    [SerializeField]
    TMP_Text instruction1, instruction2, CoinCount, HighScore;
    [SerializeField]
    Image status1, status2;

    int requiredCoins = 1;
    List<GameObject> countImages;

    [SerializeField]
    EventSystem eventSystem;
    [SerializeField]
    GraphicRaycaster raycaster;
    PointerEventData pointer;
    private List<RaycastResult> uiRaycastResults;

    Wiimote wiimote1;
    Wiimote wiimote2;
    bool isPressedB1 = false;
    bool isPressedB2 = false;
    bool isPressedA1 = false;
    bool isPressedA2 = false;
    bool isRegistered1 = false;
    bool isRegistered2 = false;
    bool swapped = false;

    GameObject lastHoveredUI = null; // 最後にホバーしたUI
    GameObject pressedUI = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Player.Instance.titleInitialized = true;
        requiredCoins = 1;
        eventSystem  = eventSystem.GetComponent<EventSystem>();
        raycaster    = raycaster.GetComponent<GraphicRaycaster>();
        instruction1 = instruction1.GetComponent<TMP_Text>();
        instruction2 = instruction2.GetComponent<TMP_Text>();
        CoinCount   = CoinCount.GetComponent<TMP_Text>();
        HighScore   = HighScore.GetComponent<TMP_Text>();
        instruction1.text = "100円玉を1枚入れて操作するコントローラーのAボタンを押してください。";
        instruction2.text = "2人で遊ぶ場合はさらに100円玉を1枚入れてもう一方のコントローラーのAボタンを押してください。";
        Zap1.StartBlinking();
        Zap2.StartBlinking();
        foreach (var pointer in ir_pointer)
        {
            if (pointer != null)
                pointer.gameObject.SetActive(false);
        }
        WiimoteManager.FindWiimotes();
        Player.Instance.Enable2PMode(false);
        countImages = new List<GameObject>();
        pointer = new PointerEventData(eventSystem);
        uiRaycastResults = new List<RaycastResult>();
        if (PlayerPrefs.HasKey("First"))
        {
            HighScore.text = "ハイスコア: " + PlayerPrefs.GetInt("First").ToString();
        }
        else
        {
            HighScore.text = "ハイスコア: 0";
        }
    }

    private void Initialize()
    {
        requiredCoins = 1;
        eventSystem = eventSystem.GetComponent<EventSystem>();
        raycaster = raycaster.GetComponent<GraphicRaycaster>();
        instruction1 = instruction1.GetComponent<TMP_Text>();
        instruction2 = instruction2.GetComponent<TMP_Text>();
        CoinCount = CoinCount.GetComponent<TMP_Text>();
        HighScore = HighScore.GetComponent<TMP_Text>();
        instruction1.text = "100円玉を1枚入れて操作するコントローラーのAボタンを押してください。";
        instruction2.text = "2人で遊ぶ場合はさらに100円玉を1枚入れてもう一方のコントローラーのAボタンを押してください。";
        Zap1.StartBlinking();
        Zap2.StartBlinking();
        foreach (var pointer in ir_pointer)
        {
            if (pointer != null)
                pointer.gameObject.SetActive(false);
        }
        WiimoteManager.FindWiimotes();
        Player.Instance.Enable2PMode(false);
        countImages = new List<GameObject>();
        pointer = new PointerEventData(eventSystem);
        uiRaycastResults = new List<RaycastResult>();
        if (PlayerPrefs.HasKey("First"))
        {
            HighScore.text = "ハイスコア: " + PlayerPrefs.GetInt("First").ToString();
        }
        else
        {
            HighScore.text = "ハイスコア: 0";
        }
    }

    // Update is called once per frame
    void Update()
    {
        CoinCount.text = "投入金額: " + (Player.Instance.coinCount * 100).ToString() + "円";
        if (!WiimoteManager.HasWiimote())
        {
            WiimoteManager.FindWiimotes();
            return;
        }

        if(Input.GetMouseButtonDown(0))
        {
            Player.Instance.AddCoin(1);
        }

        if (WiimoteManager.Wiimotes.Count <= 2)
        {
            if (status1 == null && wiimote1 != null)
            {
                status1 = Instantiate(counter, counterParent.transform).GetComponent<Image>();
            }
            else if (status1 != null && wiimote1 == null)
            {
                Destroy(status1.gameObject);
                status1 = null;
            }

            if (status2 == null && wiimote2 != null)
            {
                status2 = Instantiate(counter, counterParent.transform).GetComponent<Image>();
            }
            else if (status2 != null && wiimote2 == null)
            {
                Destroy(status2.gameObject);
                status2 = null;
            }

            if (status1 != null && wiimote1.Status.battery_low)
            {
                status1.color = Color.red;
            }
            else if (status1 != null)
            {
                status1.color = Color.green;
            }

            if (wiimote2 != null && status2 != null)
            {
                if (wiimote2.Status.battery_low)
                {
                    status2.color = Color.red;
                }
                else
                {
                    status2.color = Color.green;
                }
            }
        }

        wiimote1 = WiimoteManager.Wiimotes[0];

        if (WiimoteManager.Wiimotes.Count > 1)
        {
            wiimote2 = WiimoteManager.Wiimotes[1];
        }

        int ret1, ret2 = 0;
        do
        {
            ret1 = wiimote1.ReadWiimoteData();
            if (wiimote2 != null)
            {
                ret2 = wiimote2.ReadWiimoteData();
            }
        } while (ret1 > 0 || ret2 > 0);

        // Handle Controller 1 registration
        if (wiimote1.Button.a && !isPressedA1 && Player.Instance.coinCount >= requiredCoins)
        {
            isPressedA1 = true;
            isRegistered1 = true;
            if (swapped)
            { 
                Zap2.SetNormalDisplay(true);
                Player.Instance.Enable2PMode(true);
                if (ir_pointer[1] != null)
                    ir_pointer[1].gameObject.SetActive(true);
                instruction2.text = "";
            }
            else
            {
                Zap1.SetNormalDisplay(true);
                if (ir_pointer[0] != null)
                    ir_pointer[0].gameObject.SetActive(true);
                requiredCoins++;
                instruction1.text = "カーソルが表示されたらトリガー(Bボタン)で中央のボタンを押してください。";
            }
        }
        else if (!wiimote1.Button.a && isPressedA1)
        {
            isPressedA1 = false;
        }

        // Only process controller 1 input if registered
        if (isRegistered1)
        {
            int index = swapped ? 1 : 0;
            float[] pointer1 = wiimote1.Ir.GetPointingPosition();
            ir_pointer[index].anchorMin = new Vector2(pointer1[0], pointer1[1]);
            ir_pointer[index].anchorMax = new Vector2(pointer1[0], pointer1[1]);
            pointer.position = new Vector2(pointer1[0] * Screen.width, pointer1[1] * Screen.height);
            uiRaycastResults.Clear();
            eventSystem.RaycastAll(pointer, uiRaycastResults);

            GameObject currentHoveredUI = (uiRaycastResults.Count > 0) ? uiRaycastResults[0].gameObject : null;

            // ホバー状態の更新
            if (currentHoveredUI != lastHoveredUI)
            {
                if (lastHoveredUI != null)
                {
                    // 以前ホバーしていたオブジェクトから離れた
                    ExecuteEvents.Execute(lastHoveredUI, pointer, ExecuteEvents.pointerExitHandler);
                }
                if (currentHoveredUI != null)
                {
                    // 新しいオブジェクトにホバーした
                    ExecuteEvents.Execute(currentHoveredUI, pointer, ExecuteEvents.pointerEnterHandler);
                }
                lastHoveredUI = currentHoveredUI;
            }

            if ( wiimote1.Button.b && !isPressedB1)
            {
                isPressedB1 = true;
                if (currentHoveredUI != null)
                {
                    // --- A: UIをクリックした場合 ---
                    pressedUI = currentHoveredUI;
                    // UIに「押された(Down)」イベントを送信
                    ExecuteEvents.Execute(pressedUI, pointer, ExecuteEvents.pointerDownHandler);
                }
            }
            else if (isPressedB1)
            {
                isPressedB1 = false;

                if (pressedUI != null)
                {
                    // --- C: UIを離した場合 ---
                    // UIに「離された(Up)」イベントを送信
                    ExecuteEvents.Execute(pressedUI, pointer, ExecuteEvents.pointerUpHandler);

                    // もし押した時と離した時が同じオブジェクト上なら、「クリック」イベントを送信
                    if (pressedUI == currentHoveredUI)
                    {
                        ExecuteEvents.Execute(pressedUI, pointer, ExecuteEvents.pointerClickHandler);
                    }
                    pressedUI = null;
                }
            }
        }

        // Handle Controller 2
        if (wiimote2 != null)
        {
            // Handle Controller 2 registration
            if (wiimote2.Button.a && !isPressedA2 && Player.Instance.coinCount >= requiredCoins)
            {
                isPressedA2 = true;
                isRegistered2 = true;
                if (!isRegistered1)
                {
                    swapped = true;                  
                    Zap1.SetNormalDisplay(true);
                    Player.Instance.SwapPlayers(swapped);
                    if (ir_pointer[0] != null)
                        ir_pointer[0].gameObject.SetActive(true);
                    requiredCoins++;
                    instruction1.text = "カーソルが表示されたらトリガー(Bボタン)で中央のボタンを押してください。";
                }
                else
                {
                    Zap2.SetNormalDisplay(true);
                    Player.Instance.Enable2PMode(true);
                    if (ir_pointer[1] != null)
                        ir_pointer[1].gameObject.SetActive(true);
                    instruction2.text = "";
                }
            }
            else if (!wiimote2.Button.a && isPressedA2)
            {
                isPressedA2 = false;
            }

            // Only process controller 2 input if registered
            if (isRegistered2)
            {
                int index = swapped ? 0 : 1;
                float[] pointer2 = wiimote2.Ir.GetPointingPosition();
                ir_pointer[index].anchorMin = new Vector2(pointer2[0], pointer2[1]);
                ir_pointer[index].anchorMax = new Vector2(pointer2[0], pointer2[1]);
                pointer.position = new Vector2(pointer2[0] * Screen.width, pointer2[1] * Screen.height);
                uiRaycastResults.Clear();
                eventSystem.RaycastAll(pointer, uiRaycastResults);

                GameObject currentHoveredUI = (uiRaycastResults.Count > 0) ? uiRaycastResults[0].gameObject : null;

                // ホバー状態の更新
                if (currentHoveredUI != lastHoveredUI)
                {
                    if (lastHoveredUI != null)
                    {
                        // 以前ホバーしていたオブジェクトから離れた
                        ExecuteEvents.Execute(lastHoveredUI, pointer, ExecuteEvents.pointerExitHandler);
                    }
                    if (currentHoveredUI != null)
                    {
                        // 新しいオブジェクトにホバーした
                        ExecuteEvents.Execute(currentHoveredUI, pointer, ExecuteEvents.pointerEnterHandler);
                    }
                    lastHoveredUI = currentHoveredUI;
                }

                if (wiimote2.Button.b && !isPressedB2)
                {
                    isPressedB2 = true;

                    if (currentHoveredUI != null)
                    {
                        // --- A: UIをクリックした場合 ---
                        pressedUI = currentHoveredUI;
                        // UIに「押された(Down)」イベントを送信
                        ExecuteEvents.Execute(pressedUI, pointer, ExecuteEvents.pointerDownHandler);
                    }
                }
                else if (isPressedB2)
                {
                    isPressedB2 = false;

                    if (pressedUI != null)
                    {
                        // --- C: UIを離した場合 ---
                        // UIに「離された(Up)」イベントを送信
                        ExecuteEvents.Execute(pressedUI, pointer, ExecuteEvents.pointerUpHandler);

                        // もし押した時と離した時が同じオブジェクト上なら、「クリック」イベントを送信
                        if (pressedUI == currentHoveredUI)
                        {
                            ExecuteEvents.Execute(pressedUI, pointer, ExecuteEvents.pointerClickHandler);
                        }
                        pressedUI = null;
                    }
                }
            }
        }
    }
}
