using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueLine
{
    [TextArea(2, 5)]
    public string text;       // 대사 텍스트
    public string speaker;    // 화자 이름
    public Sprite portrait;   // 화자 초상화
}

[CreateAssetMenu(
    fileName = "DialogueData",
    menuName = "Dialogue/DialogueData",
    order = 1)]
public class DialogueData : ScriptableObject
{
    [Tooltip("대사 리스트 (각 항목에 텍스트/화자/초상화를 설정)")]
    public List<DialogueLine> lines = new List<DialogueLine>();

    private void OnValidate()
    {
        if (lines == null || lines.Count == 0)
        {
            Debug.LogWarning($"{name}: 대사 리스트가 비어 있습니다.");
            return;
        }

        for (int i = lines.Count - 1; i >= 0; i--)
        {
            var line = lines[i];
            if (line == null)
            {
                Debug.LogWarning($"{name}: lines[{i}] 가 null 입니다. 자동 제거합니다.");
                lines.RemoveAt(i);
            }
            else if (string.IsNullOrWhiteSpace(line.speaker) && string.IsNullOrWhiteSpace(line.text))
            {
                Debug.LogWarning($"{name}: lines[{i}] 가 speaker/text 모두 비어있습니다. 자동 제거합니다.");
                lines.RemoveAt(i);
            }
            else if (string.IsNullOrEmpty(line.text))
            {
                Debug.LogWarning($"{name}: lines[{i}].text 가 비어 있습니다.");
            }
        }
    }

    public void CleanupEmptyLines()
    {
        if (lines == null) return;

        List<DialogueLine> cleaned = new List<DialogueLine>();
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line.speaker) || !string.IsNullOrWhiteSpace(line.text))
            {
                cleaned.Add(line);
            }
        }

        lines = cleaned;
        Debug.Log($"[DialogueData] CleanupEmptyLines: {cleaned.Count} valid lines retained.");
    }
}
