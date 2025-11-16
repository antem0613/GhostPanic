using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SceneInitializer : MonoBehaviour
{
    [SerializeField] Transform chargeTarget;
    [SerializeField] Slider HPbar_1, HPbar_2;
    [SerializeField] Transform target1, target2;
    [SerializeField] RectTransform[] ir_pointer;
    [SerializeField] DamageIndicator damageIndicator;
    [SerializeField] GameObject PlayerPrefab;
    [SerializeField] GameObject GameOverPanel;
    [SerializeField] TMP_Text scoreText;

    void Awake()
    {
        InitializeScene();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        scoreText = scoreText.GetComponent<TMP_Text>();
    }

    // Update is called once per frame
    void Update()
    {
        scoreText.text = $"ÉXÉRÉA: {Player.Instance.score}";
    }

    void InitializeScene()
    {
        Player player;
        bool isNotExistPlayer = Player.Instance == null;
        GameOverPanel.SetActive(false);

        if (isNotExistPlayer)
        {
            player = Instantiate(PlayerPrefab).GetComponent<Player>();
        }
        else
        {
            player = Player.Instance;
        }

        player.SetChargeTarget(chargeTarget);
        player.SetHPbars(HPbar_1, HPbar_2);
        player.SetTargets(target1, target2);
        player.SetIRPointers(ir_pointer);
        player.SetDamageIndicator(damageIndicator);
        player.SetGameOverPanel(GameOverPanel);

        if (player.cursorLocked)
        {
            player.Initialize();
        }

        if (!isNotExistPlayer)
        {
            player.SetUI();
        }
    }
}
