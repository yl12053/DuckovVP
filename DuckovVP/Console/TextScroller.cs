using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace DuckovVP.Console;

public class TextScroller: MonoBehaviour
{
    public float maxWidth = 240f;
    private TextMeshPro tmpText;

    public string _text = "";

    public string text
    {
        get => _text;
        set
        {
            _text = value;
            CalculateText();
        }
    }
    
    private List<string>? TextWindow;

    void Awake()
    {
        tmpText = GetComponent<TextMeshPro>();
        tmpText.enableWordWrapping = false;
    }

    public List<string> SplitTextIntoWindows(string fullText)
    {
        List<string> resultWindows = new();
        if (string.IsNullOrWhiteSpace(fullText)) return resultWindows;

        int startIndex = 0;
        int lastTime = -1;
        while (startIndex < fullText.Length)
        {
            int bestEndIndex = FindBestFitLength(fullText, startIndex, maxWidth);
            if (lastTime == bestEndIndex)
            {
                startIndex++;
                continue;
            }
            string chunk = fullText.Substring(startIndex, bestEndIndex - startIndex);
            resultWindows.Add(chunk);

            if (bestEndIndex >= fullText.Length) break;
            startIndex++;
        }

        return resultWindows;
    }
    
    private int FindBestFitLength(string text, int startIndex, float targetWidth)
    {
        int low = startIndex + 1;
        int high = text.Length;
        int bestFit = low;
        
        while (low <= high)
        {
            int mid = (low + high) / 2;
            string subString = text.Substring(startIndex, mid - startIndex);
            
            Vector2 size = tmpText.GetPreferredValues(subString);

            if (size.x <= targetWidth)
            {
                bestFit = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return bestFit;
    }

    private float TimeCnt = 0f;
    private int index = 0;
    private CancellationTokenSource? _cts;

    private void CalculateText()
    {
        _cts?.Cancel();
        TextWindow = SplitTextIntoWindows(text);
    }

    public void Restart()
    {
        if (TextWindow == null)
        {
            CalculateText();
        }
        StartCoroutine(Scroll(this.GetCancellationTokenOnDestroy()).ToCoroutine());
    }

    private async UniTask Scroll(CancellationToken ct)
    {
        _cts?.Cancel();
        if (TextWindow == null) return;
        if (TextWindow.Count == 0)
        {
            tmpText.text = "";
            return;
        }
        if (TextWindow.Count == 1)
        {
            tmpText.text = TextWindow[0];
            tmpText.ForceMeshUpdate();
            return;
        }
        _cts = new CancellationTokenSource();
        using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, ct))
        {
            while (true)
            {
                for (int i = 0; i < TextWindow.Count; i++)
                {
                    tmpText.text = TextWindow[i];
                    tmpText.ForceMeshUpdate();
                    await UniTask.WaitForSeconds((i == 0 || i == TextWindow.Count - 1) ? 3f : 1f,
                        cancellationToken: linkedCts.Token);
                }
            }
        }
    }
}