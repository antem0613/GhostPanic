using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Image))]
public class DamageIndicator : MonoBehaviour
{
    [Range(0f, 1f)]
    public float maxAlpha = 0.8f;

    public float fadeDuration = 0.5f;

    private Image damageImage;
    private Coroutine fadeCoroutine;

    void Awake()
    {
        damageImage = GetComponent<Image>();
        if (damageImage == null)
        {
            return;
        }

        Color initialColor = damageImage.color;
        initialColor.a = 0f;
        damageImage.color = initialColor;
    }

    public void ShowDamageEffect()
    {
        if (damageImage == null) return;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeOutEffect());
    }

    private IEnumerator FadeOutEffect()
    {
        float timer = 0f;
        Color currentColor = damageImage.color;

        currentColor.a = maxAlpha;
        damageImage.color = currentColor;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(maxAlpha, 0f, timer / fadeDuration);
            currentColor.a = alpha;
            damageImage.color = currentColor;
            yield return null;
        }

        currentColor.a = 0f;
        damageImage.color = currentColor;

        fadeCoroutine = null;
    }
}