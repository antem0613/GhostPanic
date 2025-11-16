using UnityEngine;
using UnityEngine.UI; // UI要素を扱うために必要
using System.Collections; // Coroutineのために必要

[RequireComponent(typeof(Image))] // Imageコンポーネントが必須
public class DamageIndicator : MonoBehaviour
{
    [Header("エフェクト設定")]
    [Tooltip("ダメージエフェクトの最大アルファ値 (0-1)")]
    [Range(0f, 1f)]
    public float maxAlpha = 0.8f; // 完全に不透明にしない方が良い場合が多い

    [Tooltip("フェードアウトにかかる時間（秒）")]
    public float fadeDuration = 0.5f;

    private Image damageImage;
    private Coroutine fadeCoroutine; // 実行中のコルーチンを保持

    void Awake()
    {
        damageImage = GetComponent<Image>();
        if (damageImage == null)
        {
            Debug.LogError("Image コンポーネントが見つかりません！");
            return;
        }

        // 初期状態では透明にする
        Color initialColor = damageImage.color;
        initialColor.a = 0f;
        damageImage.color = initialColor;
    }

    /// <summary>
    /// ダメージエフェクトを表示・フェードアウトさせる
    /// </summary>
    public void ShowDamageEffect()
    {
        if (damageImage == null) return;

        // 既にフェード中の場合は、一旦停止して最初からやり直す
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // フェードアウト処理を開始
        fadeCoroutine = StartCoroutine(FadeOutEffect());
    }

    private IEnumerator FadeOutEffect()
    {
        float timer = 0f;
        Color currentColor = damageImage.color;

        // まず最大アルファ値にする
        currentColor.a = maxAlpha;
        damageImage.color = currentColor;

        // 徐々にアルファ値を下げる
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(maxAlpha, 0f, timer / fadeDuration); // 線形補間
            currentColor.a = alpha;
            damageImage.color = currentColor;
            yield return null; // 次のフレームまで待機
        }

        // 確実にアルファ値を0にする
        currentColor.a = 0f;
        damageImage.color = currentColor;

        fadeCoroutine = null; // コルーチン終了
    }

    // --- テスト用 ---
    // void Update()
    // {
    //     if (Input.GetKeyDown(KeyCode.Space)) // スペースキーでテスト
    //     {
    //         ShowDamageEffect();
    //     }
    // }
}