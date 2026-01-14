using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void LoadStage1()
    {
        if (Player.Instance.is2P)
        {
            Player.Instance.AddCoin(-2);
        }
        else
        {
            Player.Instance.AddCoin(-1);
        }

        Player.Instance.ResetScore();

        Player.Instance.CheckDebugMode();

        SceneManager.LoadScene("Stage1");
    }

    public void LoadStage2()
    {
        SceneManager.LoadScene("Stage2");
    }
}
