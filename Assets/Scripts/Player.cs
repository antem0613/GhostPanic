using JetBrains.Annotations;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;
using WiimoteApi;

public class Player : Singleton<Player>
{
    [SerializeField]
    int attackDamage = 1;
    [SerializeField]
    int maxHP = 10;

    public int coinCount { get; private set; } = 0;

    public int HP1 { get; private set; }
    public int HP2 { get; private set; }

    public bool is2P { get; private set; }

    public Transform chargeTarget { get; private set; }
    public Slider HPbar_1 { get; private set; }
    public Slider HPbar_2 { get; private set; }
    public Transform target1 { get; private set; }
    public Transform target2 { get; private set; }

    public int score { get; private set; } = 0;

    RectTransform[] ir_pointer;

    [SerializeField]
    GameObject[] hitEffectPrefab;

    [SerializeField]
    float shootInterval = 0.5f;
    [SerializeField]
    float shootInterval2P = 1f;
    float interval1, interval2 = 0f;

    [SerializeField]
    float beamMaxDistance = 100f;

    [SerializeField]
    LayerMask beamHitMask;
    [SerializeField]
    LayerMask orbMask;

    public GameObject GameClearPanel { get; private set; }
    public GameObject GameOverPanel { get; private set; }

    DamageIndicator damageIndicator;

    Wiimote wiimote1;
    Wiimote wiimote2;
    public bool cursorLocked { get; private set; } = true;
    bool swapped = false;
    bool isGameOver = false;
    public bool titleInitialized = false;
    public bool iscoinBlocking = false;

    // Update is called once per frame
    void Update()
    {
        if (cursorLocked)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            Shot(Input.mousePosition, target1.parent, 0);
        }

        if (!WiimoteManager.HasWiimote())
        {
            WiimoteManager.FindWiimotes();
            return;
        }

        wiimote1 = WiimoteManager.Wiimotes[0];

        if (is2P)
        {
            wiimote2 = WiimoteManager.Wiimotes[1];

            if (HP2 == -100)
            {
                HP2 = maxHP;
            }

            if (!HPbar_2.gameObject.activeSelf)
                HPbar_2.gameObject.SetActive(true);
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

        float[] pointer1 = wiimote1.Ir.GetPointingPosition();
        int index1 = swapped ? 1 : 0;
        ir_pointer[index1].anchorMin = new Vector2(pointer1[0], pointer1[1]);
        ir_pointer[index1].anchorMax = new Vector2(pointer1[0], pointer1[1]);
        HPbar_1.value = (float)HP1 / (float)maxHP;

        if (wiimote1.Button.b && interval1 <= 0 && HP1 > 0)
        {
            Shot(new Vector2(pointer1[0] * Screen.width, pointer1[1] * Screen.height), target2, 0);
        }
        else if (interval1 > 0)
        {
            interval1 -= Time.deltaTime;
        }

        // Handle Controller 2
        if (is2P)
        {
            float[] pointer2 = wiimote2.Ir.GetPointingPosition();
            int index2 = swapped ? 0 : 1;
            ir_pointer[index2].anchorMin = new Vector2(pointer2[0], pointer2[1]);
            ir_pointer[index2].anchorMax = new Vector2(pointer2[0], pointer2[1]);
            HPbar_2.value = (float)HP2 / (float)maxHP;

            if (wiimote2.Button.b && interval2 <= 0 && HP2 > 0)
            {
                Shot(new Vector2(pointer2[0] * Screen.width, pointer2[1] * Screen.height), target2, 1);
            }
            else if (interval2 > 0)
            {
                interval2 -= Time.deltaTime;
            }
        }
    }

    void Shot(Vector2 pos, Transform beamSpawnPoint, int id)
    {
        if (hitEffectPrefab == null || beamSpawnPoint == null)
        {
            Debug.LogError("ビームのプレハブまたは発生場所が設定されていません！");
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(pos);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, beamMaxDistance, beamHitMask))
        {
            Enemy enemy = hit.collider.GetComponent<Enemy>();
            enemy?.TakeDamage(attackDamage);

            if (enemy != null)
            {
                if ( id == 0 )
                {
                    interval1 = shootInterval;
                }
                else
                {
                    interval2 = shootInterval;
                }
                Instantiate(hitEffectPrefab[id], hit.point, Quaternion.LookRotation(hit.normal));
            }
        }
        else if (Physics.Raycast(ray, out hit, beamMaxDistance, orbMask))
        {
            Projectile orb = hit.collider.GetComponent<Projectile>();
            orb?.DestroyProjectile();
        }
    }
    public void TakeDamage(int playerNumber, int damage)
    {
        if (playerNumber == 1)
        {
            HP1 -= damage;
            if (HP1 < 0) HP1 = 0;
            HPbar_1.value = (float)HP1 / (float)maxHP;
        }
        else if (playerNumber == 2)
        {
            HP2 -= damage;
            if (HP2 < 0) HP2 = 0;
            HPbar_2.value = (float)HP2 / (float)maxHP;
        }

        if (HP1 <= 0 && HP2 <= 0)
        {
            GameOver();
        }

        if (damageIndicator != null)
        {
            damageIndicator.ShowDamageEffect();
        }
    }

    public void Initialize()
    {
        WiimoteManager.FindWiimotes();
        HPbar_2.gameObject.SetActive(false);
        target2.gameObject.SetActive(false);
        cursorLocked = false;
        HP1 = maxHP;
        HP2 = -100;

        // Initially hide all pointers
        if (ir_pointer != null)
        {
            foreach (var pointer in ir_pointer)
            {
                if (pointer != null)
                    pointer.gameObject.SetActive(false);
            }
        }
    }

    public void GameClear()
    {
        isGameOver = false;
        GameClearPanel.SetActive(true);
    }

    void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        GameOverManager manager = GameOverPanel.GetComponent<GameOverManager>();
        manager.Initialize();
        Time.timeScale = 0f;
        GameOverPanel.SetActive(true);
    }

    public void SetUI()
    {
        Debug.Log("Setting UI");
        WiimoteManager.FindWiimotes();
        if (ir_pointer != null)
        {
            foreach (var pointer in ir_pointer)
            {
                if (pointer != null)
                    pointer.gameObject.SetActive(false);
            }

            target2.gameObject.SetActive(false);
        }

        int index1 = swapped ? 1 : 0;
        ir_pointer[index1].gameObject.SetActive(true);
        HPbar_2.gameObject.SetActive(false);
        HPbar_1.value = (float)HP1 / (float)maxHP;

        interval1 = 0f;
        interval2 = 0f;

        target2.gameObject.SetActive(false);

        Debug.Log("is2P: " + WiimoteManager.Wiimotes.Count);
        if (is2P)
        {
            shootInterval = shootInterval2P;
            int index2 = swapped ? 0 : 1;
            HPbar_2.gameObject.SetActive(true);
            ir_pointer[index2].gameObject.SetActive(true);
            HPbar_2.value = (float)HP2 / (float)maxHP;
            target2.gameObject.SetActive(true);
        }
    }

    public void SetChargeTarget(Transform target)
    {
        chargeTarget = target;
    }

    public void SetHPbars(Slider bar1, Slider bar2)
    {
        HPbar_1 = bar1;
        HPbar_2 = bar2;
    }

    public void SetIRPointers(RectTransform[] pointers)
    {
        ir_pointer = new RectTransform[] { pointers[0], pointers[1] };
    }

    public void SetDamageIndicator(DamageIndicator indicator)
    {
        damageIndicator = indicator;
    }

    public void SetTargets(Transform t1, Transform t2)
    {
        target1 = t1;
        target2 = t2;
    }

    public void Revive()
    {
        isGameOver = false;
        HP1 = maxHP;
        HP2 = is2P ? maxHP : -100;
        HPbar_1.value = (float)HP1 / (float)maxHP;
        if (is2P)
        {
            HPbar_2.value = (float)HP2 / (float)maxHP;
        }
    }

    public void LockCursor()
    {
        cursorLocked = true;
    }

    public void SetGameOverPanel(GameObject panel)
    {
        GameOverPanel = panel;
    }

    public void Enable2PMode(bool condition)
    {
        is2P = condition;
    }

    public void SwapPlayers(bool condition)
    {
        swapped = condition;
    }

    public void AddCoin(int amount)
    {
        coinCount += amount;
    }

    public void AddScore(int amount)
    {
        score += amount;
    }

    public void ResetScore()
    {
        score = 0;
    }

    public void UpdateRanking()
    {
        bool has1st, has2nd, has3rd = false;

        has1st = PlayerPrefs.HasKey("First");
        has2nd = PlayerPrefs.HasKey("Second");
        has3rd = PlayerPrefs.HasKey("Third");

        if (!has1st) { 
            PlayerPrefs.SetInt("First", score);
            PlayerPrefs.Save();
            return;
        }
        
        if (score > PlayerPrefs.GetInt("First"))
        {
            if (has2nd)
            {
                PlayerPrefs.SetInt("Third", PlayerPrefs.GetInt("Second"));
            }

            PlayerPrefs.SetInt("Second", PlayerPrefs.GetInt("First"));
            PlayerPrefs.SetInt("First", score);
            PlayerPrefs.Save();
            return;
        }

        if (!has2nd)
        {
            PlayerPrefs.SetInt("Second", score);
            PlayerPrefs.Save();
            return;
        }

        if (score > PlayerPrefs.GetInt("Second"))
        {
            PlayerPrefs.SetInt("Third", PlayerPrefs.GetInt("Second"));
            PlayerPrefs.SetInt("Second", score);
            PlayerPrefs.Save();
            return;
        }

        if (!has3rd)
        {
            PlayerPrefs.SetInt("Third", score);
            PlayerPrefs.Save();
            return;
        }
    }
}
