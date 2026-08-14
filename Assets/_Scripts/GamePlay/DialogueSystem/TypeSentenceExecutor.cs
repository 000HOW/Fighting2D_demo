using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TypeSentenceExecutor : MonoBehaviour
{
    public Action FinishTyping;
    public Action CallFinish;

    // 是否正在逐字打印（供 Dialogue 判断"继续键"行为）
    public bool IsTyping { get; private set; }

    Text curLine;
    string curSentence;

    public IEnumerator TypeSentence(Text line, string sentence, float delay)
    {
        IsTyping = true;
        line.text = "";
        foreach (char letter in sentence.ToCharArray())
        {
            line.text += letter;
            yield return new WaitForSeconds(delay);  // 可配置速度
        }
        IsTyping = false;
        FinishTyping?.Invoke();
        CallFinish?.Invoke();
    }

    public void Actutor(Text line, string sentence, float delay)
    {
        curLine = line;
        curSentence = sentence;
        StopAllCoroutines();
        StartCoroutine(TypeSentence(line, sentence, delay));
    }

    /// <summary>
    /// 立即显示完整文本并结束打字（打印中按"继续"时调用）。
    /// </summary>
    public void FinishInstantly()
    {
        StopAllCoroutines();
        IsTyping = false;
        if (curLine != null && curSentence != null)
        {
            curLine.text = curSentence;
        }
        FinishTyping?.Invoke();
        CallFinish?.Invoke();
    }

    public void StopType()
    {
        StopAllCoroutines();
        IsTyping = false;
    }
}
