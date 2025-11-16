using UnityEngine;
using UnityEngine.UI; // Image を操作するために必要
using System.Collections; // コルーチンを使用するために必要

[RequireComponent(typeof(Image))] // このスクリプトには Image コンポーネントが必須
public class ZapUI : MonoBehaviour
{
    [Header("フェード設定")]
    [Tooltip("フェードイン（不透明になる）にかかる時間（秒）")]
    public float fadeInTime = 0.5f;

    [Tooltip("フェードアウト（透明になる）にかかる時間（秒）")]
    public float fadeOutTime = 0.5f;

    [Tooltip("点滅時の最大アルファ値（濃さ） 0.0 ～ 1.0")]
    [Range(0f, 1f)]
    public float maxAlpha = 1.0f;

    [Tooltip("点滅時の最小アルファ値（薄さ） 0.0 ～ 1.0")]
    [Range(0f, 1f)]
    public float minAlpha = 0.0f;

    [Tooltip("点滅と点滅の間の待機時間（秒）")]
    public float blinkInterval = 0.1f;

    private Image uiImage;
    private Coroutine runningFadeCoroutine; // 実行中のコルーチンを保持する変数
    private bool isBlinking = false;

    void Start()
    {
        uiImage = GetComponent<Image>();
        if (uiImage == null)
        {
            Debug.LogError("Image コンポーネントが見つかりません。", this);
        }
    }

    /// <summary>
    /// UI画像のフェード点滅を開始します。
    /// </summary>
    [ContextMenu("Start Blinking")] // Inspectorの右クリックメニューからも実行可能
    public void StartBlinking()
    {
        Debug.Log("StartBlinking called");
        uiImage = GetComponent<Image>();
        // 既に点滅中であれば何もしない
        if (isBlinking) return;

        isBlinking = true;

        // もし古いコルーチンが残っていれば停止
        if (runningFadeCoroutine != null)
        {
            StopCoroutine(runningFadeCoroutine);
        }

        // 点滅コルーチンを開始
        runningFadeCoroutine = StartCoroutine(FadeBlinkRoutine());
    }

    /// <summary>
    /// UI画像の点滅を停止し、通常の表示状態に切り替えます。
    /// </summary>
    /// <param name="isVisible">通常表示の際に、画像を表示するか (true) 非表示にするか (false)</param>
    [ContextMenu("Set Normal (Visible)")]
    public void SetNormalDisplay(bool isVisible)
    {
        isBlinking = false;

        // 実行中の点滅コルーチンがあれば停止
        if (runningFadeCoroutine != null)
        {
            StopCoroutine(runningFadeCoroutine);
            runningFadeCoroutine = null;
        }

        // 画像のアルファ値を指定された状態（表示または非表示）に設定
        // 表示する場合は maxAlpha を、非表示にする場合は 0 を設定
        SetAlpha(isVisible ? maxAlpha : 0f);
    }

    // --- デバッグ用 ---
    // [ContextMenu("Set Normal (Invisible)")]
    // private void SetNormalInvisible()
    // {
    //     SetNormalDisplay(false);
    // }

    /// <summary>
    /// フェード点滅を繰り返すコルーチン本体
    /// </summary>
    private IEnumerator FadeBlinkRoutine()
    {
        // 常に maxAlpha から開始する
        SetAlpha(maxAlpha);

        Debug.Log(isBlinking ? "Blinking started" : "Blinking not started");

        // isBlinking が true の間、無限にループ
        while (isBlinking)
        {
            // --- 1. フェードアウト (maxAlpha -> minAlpha) ---
            float timer = 0f;
            while (timer < fadeOutTime)
            {
                if (!isBlinking) yield break; // 中断チェック
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(maxAlpha, minAlpha, timer / fadeOutTime);
                SetAlpha(alpha);
                yield return null; // 1フレーム待機
            }
            SetAlpha(minAlpha); // 確実に minAlpha にする

            // --- 2. 待機 ---
            if (blinkInterval > 0)
            {
                yield return new WaitForSeconds(blinkInterval);
            }
            if (!isBlinking) yield break; // 中断チェック

            // --- 3. フェードイン (minAlpha -> maxAlpha) ---
            timer = 0f;
            while (timer < fadeInTime)
            {
                if (!isBlinking) yield break; // 中断チェック
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(minAlpha, maxAlpha, timer / fadeInTime);
                SetAlpha(alpha);
                yield return null;
            }
            SetAlpha(maxAlpha); // 確実に maxAlpha にする

            // --- 4. 待機 ---
            if (blinkInterval > 0)
            {
                yield return new WaitForSeconds(blinkInterval);
            }
        }

        // ループが終了（isBlinking が false になった）
        runningFadeCoroutine = null;
    }

    /// <summary>
    /// Image のアルファ値を安全に設定するヘルパー関数
    /// </summary>
    private void SetAlpha(float alpha)
    {
        if (uiImage == null) return;
        Color currentColor = uiImage.color;
        currentColor.a = alpha;
        uiImage.color = currentColor;
    }

    /// <summary>
    /// オブジェクトが非アクティブになった時にコルーチンを停止（安全対策）
    /// </summary>
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