using System;
using TMPro;
using UnityEngine;
using DG.Tweening;
using System.Collections;

public class CharacterSpeech : MonoBehaviour
{
    [SerializeField] private TMP_Text m_text;
    public Action OnSpeak;

    private WaitForSeconds m_waitForSeconds = new WaitForSeconds(1.5f);
    private Coroutine m_routine = null;
    public void Say(string speech, float typeSpeed = 0.05f)
    {
        m_text.text = "";
        int len = speech.Length;

        // Tween a counter from 0 → len
        DOTween.To(() => 0, i =>
        {
            m_text.text = speech.Substring(0, i);
        }, len, len * typeSpeed)
        .SetEase(Ease.Linear)
        .OnComplete(() => {
            OnSpeak?.Invoke();

            if (m_routine != null)
            {
                StopCoroutine(m_routine);
                m_routine = null;
            }

            if (m_routine == null)
                m_routine = StartCoroutine(ClearSpeech());
        });
    }

    IEnumerator ClearSpeech()
    {
        yield return m_waitForSeconds;
        m_text.text = "";
    }
}
