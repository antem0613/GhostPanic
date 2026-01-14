using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Image))]
public class ZapUI : MonoBehaviour
{
    [Header("フェード設定")]
    [Tooltip("フェードインにかかる時間（秒）")]
    public float fadeInTime = 0.5f;

    [Tooltip("フェードアウトにかかる時間（秒）")]
    public float fadeOutTime = 0.5f;

    [Tooltip("点滅時の最大アルファ値")]
    [Range(0f, 1f)]
    public float maxAlpha = 1.0f;

    [Tooltip("点滅時の最小アルファ値")]
    [Range(0f, 1f)]
    public float minAlpha = 0.0f;

    [Tooltip("点滅と点滅の間の待機時間")]
    public float blinkInterval = 0.1f;

    private Image uiImage;
    private Coroutine runningFadeCoroutine;
    private bool isBlinking = false;

    void Start()
    {
        uiImage = GetComponent<Image>();
        if (uiImage == null)
        {
            Debug.LogError("Image コンポーネントが見つかりません。", this);
        }
    }

    public void StartBlinking()
    {
        Debug.Log("StartBlinking called");
        uiImage = GetComponent<Image>();

        if (isBlinking) return;

        isBlinking = true;

        if (runningFadeCoroutine != null)
        {
            StopCoroutine(runningFadeCoroutine);
        }

        runningFadeCoroutine = StartCoroutine(FadeBlinkRoutine());
    }

    public void SetNormalDisplay(bool isVisible)
    {
        isBlinking = false;

        if (runningFadeCoroutine != null)
        {
            StopCoroutine(runningFadeCoroutine);
            runningFadeCoroutine = null;
        }

        SetAlpha(isVisible ? maxAlpha : 0f);
    }

    private IEnumerator FadeBlinkRoutine()
    {
        SetAlpha(maxAlpha);

        Debug.Log(isBlinking ? "Blinking started" : "Blinking not started");

        while (isBlinking)
        {
            float timer = 0f;
            while (timer < fadeOutTime)
            {
                if (!isBlinking) yield break;
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(maxAlpha, minAlpha, timer / fadeOutTime);
                SetAlpha(alpha);
                yield return null;
            }
            SetAlpha(minAlpha);

            if (blinkInterval > 0)
            {
                yield return new WaitForSeconds(blinkInterval);
            }
            if (!isBlinking) yield break;

            timer = 0f;
            while (timer < fadeInTime)
            {
                if (!isBlinking) yield break;
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(minAlpha, maxAlpha, timer / fadeInTime);
                SetAlpha(alpha);
                yield return null;
            }
            SetAlpha(maxAlpha);

            if (blinkInterval > 0)
            {
                yield return new WaitForSeconds(blinkInterval);
            }
        }

        runningFadeCoroutine = null;
    }

    private void SetAlpha(float alpha)
    {
        if (uiImage == null) return;
        Color currentColor = uiImage.color;
        currentColor.a = alpha;
        uiImage.color = currentColor;
    }

    void OnDisable()
    {
        isBlinking = false;
        if (runningFadeCoroutine != null)
        {
            StopCoroutine(runningFadeCoroutine);
            runningFadeCoroutine = null;
        }
    }
}