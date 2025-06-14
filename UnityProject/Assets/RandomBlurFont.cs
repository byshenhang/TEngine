using ChocDino.UIFX;
using Newtonsoft.Json.Bson;
using System.Collections;
using TMPro;
using UnityEngine;

public class RandomBlurFont : MonoBehaviour
{
    public BlurFilter[] blurFilters;
    public TextMeshProUGUI[] textMeshProUGUIs;
    public string TextInfo = "I saw you";
    public float blurStart = 30.0f;
    public float blurThreshold = 10f;
    public float blurFadeDuration = 1.0f;
    public float finalFadeDuration = 0.5f;              // 匀速消失时间
    public AnimationCurve blurCurve;

    private void OnEnable()
    {
        PlayFontAnimation();
    }

    private void PlayFontAnimation()
    {
        for (int i = 0; i < textMeshProUGUIs.Length; i++)
        {
            textMeshProUGUIs[i].gameObject.SetActive(false);
        }

        StartCoroutine(PlayBlurInTwoPasses());
    }

    IEnumerator PlayBlurInTwoPasses()
    {
        // 第一轮：0, 2, 4...
        yield return new WaitForSeconds(1f);
        for (int i = 0; i < TextInfo.Length && i < textMeshProUGUIs.Length; i += 2)
        {
            StartCoroutine(ActivateAndFade(i));
            yield return StartCoroutine(WaitForBlurBelowThreshold(blurFilters[i]));
        }

        // 第二轮：1, 3, 5...
        for (int i = 1; i < TextInfo.Length && i < textMeshProUGUIs.Length; i += 2)
        {
            StartCoroutine(ActivateAndFade(i));
            yield return StartCoroutine(WaitForBlurBelowThreshold(blurFilters[i]));
        }

        // 所有字符显示完后，开始整体渐隐
        yield return new WaitForSeconds(0.5f);  // 可选延迟
        StartCoroutine(FadeOutAllText());
    }

    IEnumerator ActivateAndFade(int index)
    {
        textMeshProUGUIs[index].gameObject.SetActive(true);
        textMeshProUGUIs[index].text = TextInfo[index].ToString();
        blurFilters[index].Blur = blurStart;

        float elapsed = 0f;

        while (elapsed < blurFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / blurFadeDuration);
            float curveValue = blurCurve.Evaluate(t);
            blurFilters[index].Blur = Mathf.Lerp(blurStart, 0f, curveValue);
            yield return null;
        }

        blurFilters[index].Blur = 0f;
    }

    IEnumerator WaitForBlurBelowThreshold(BlurFilter filter)
    {
        while (filter.Blur > blurThreshold)
        {
            yield return null;
        }
    }

    IEnumerator FadeOutAllText()
    {
        float elapsed = 0f;
        Color[] originalColors = new Color[textMeshProUGUIs.Length];

        // 保存初始颜色
        for (int i = 0; i < textMeshProUGUIs.Length; i++)
        {
            originalColors[i] = textMeshProUGUIs[i].color;
        }

        while (elapsed < finalFadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / finalFadeDuration);

            for (int i = 0; i < textMeshProUGUIs.Length; i++)
            {
                Color c = originalColors[i];
                c.a = alpha;
                textMeshProUGUIs[i].color = c;
            }

            yield return null;
        }

        // 最终设为透明
        for (int i = 0; i < textMeshProUGUIs.Length; i++)
        {
            Color c = textMeshProUGUIs[i].color;
            c.a = 0f;
            textMeshProUGUIs[i].color = c;
        }
    }
}