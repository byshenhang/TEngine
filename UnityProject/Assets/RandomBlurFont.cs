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
    public float finalFadeDuration = 0.5f;              // ������ʧʱ��
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
        // ��һ�֣�0, 2, 4...
        yield return new WaitForSeconds(1f);
        for (int i = 0; i < TextInfo.Length && i < textMeshProUGUIs.Length; i += 2)
        {
            StartCoroutine(ActivateAndFade(i));
            yield return StartCoroutine(WaitForBlurBelowThreshold(blurFilters[i]));
        }

        // �ڶ��֣�1, 3, 5...
        for (int i = 1; i < TextInfo.Length && i < textMeshProUGUIs.Length; i += 2)
        {
            StartCoroutine(ActivateAndFade(i));
            yield return StartCoroutine(WaitForBlurBelowThreshold(blurFilters[i]));
        }

        // �����ַ���ʾ��󣬿�ʼ���彥��
        yield return new WaitForSeconds(0.5f);  // ��ѡ�ӳ�
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

        // �����ʼ��ɫ
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

        // ������Ϊ͸��
        for (int i = 0; i < textMeshProUGUIs.Length; i++)
        {
            Color c = textMeshProUGUIs[i].color;
            c.a = 0f;
            textMeshProUGUIs[i].color = c;
        }
    }
}