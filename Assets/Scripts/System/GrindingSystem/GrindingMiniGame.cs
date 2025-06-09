using System.Collections.Generic;
using UnityEngine;

public class GrindingMiniGame
{
    public enum JudgmentType { Perfect, Good, Fail }
    
    [System.Serializable]
    public class GrindingResult
    {
        public JudgmentType judgment;
        public float smoothIncrease;
        public float accuracy;
        public float cursorPosition;
        
        public GrindingResult(JudgmentType judgment, float smoothIncrease, float accuracy, float cursorPosition)
        {
            this.judgment = judgment;
            this.smoothIncrease = smoothIncrease;
            this.accuracy = accuracy;
            this.cursorPosition = cursorPosition;
        }
    }
    
    private GrindingWheel wheel;
    
    public GrindingMiniGame(GrindingWheel wheel)
    {
        this.wheel = wheel;
    }
    
    /// <summary>
    /// 공격력 기반 판정 계산
    /// </summary>
    public GrindingResult CalculateResult(float cursorPosition, float weaponAttack)
    {
        float center = 0.5f;
        float distance = Mathf.Abs(cursorPosition - center);
        
        // 공격력에 따라 판정 범위 조정
        float adjustedPerfectRange = wheel.GetAdjustedRange(wheel.PerfectRange, weaponAttack);
        float adjustedGoodRange = wheel.GetAdjustedRange(wheel.GoodRange, weaponAttack);
        
        JudgmentType judgment;
        float smoothIncrease;
        
        if (distance <= adjustedPerfectRange)
        {
            judgment = JudgmentType.Perfect;
            smoothIncrease = wheel.PerfectIncrease;
        }
        else if (distance <= adjustedGoodRange)
        {
            judgment = JudgmentType.Good;
            smoothIncrease = wheel.GoodIncrease;
        }
        else
        {
            judgment = JudgmentType.Fail;
            smoothIncrease = wheel.FailIncrease;
        }
        
        // 정확도 계산 (0~1, 중앙에 가까울수록 높음)
        float accuracy = 1f - (distance / 0.5f);
        accuracy = Mathf.Clamp01(accuracy);
        
        return new GrindingResult(judgment, smoothIncrease, accuracy, cursorPosition);
    }
    
    /// <summary>
    /// 3회 결과 평균 계산
    /// </summary>
    public float CalculateFinalSmooth(List<GrindingResult> results)
    {
        if (results.Count == 0) return 0f;
        
        float total = 0f;
        foreach (var result in results)
        {
            total += result.smoothIncrease;
        }
        
        return total / results.Count;
    }
    
    /// <summary>
    /// 난이도 표시용 텍스트
    /// </summary>
    public string GetDifficultyText(float weaponAttack)
    {
        if (weaponAttack < 10f) return "쉬움";
        else if (weaponAttack < 25f) return "보통";
        else if (weaponAttack < 50f) return "어려움";
        else return "매우 어려움";
    }
    
    /// <summary>
    /// 공격력에 따른 세부 난이도 정보
    /// </summary>
    public string GetDifficultyDetails(float weaponAttack)
    {
        float speedMultiplier = wheel.CalculateCursorSpeed(weaponAttack) / 1f; // 기본 속도 대비
        float perfectRange = wheel.GetAdjustedRange(wheel.PerfectRange, weaponAttack);
        float goodRange = wheel.GetAdjustedRange(wheel.GoodRange, weaponAttack);
        
        return $"커서 속도: {speedMultiplier:F1}x, Perfect 범위: {perfectRange * 100:F1}%, Good 범위: {goodRange * 100:F1}%";
    }
    
    /// <summary>
    /// 판정 결과에 따른 색상 반환
    /// </summary>
    public Color GetJudgmentColor(JudgmentType judgment)
    {
        switch (judgment)
        {
            case JudgmentType.Perfect:
                return Color.yellow;
            case JudgmentType.Good:
                return Color.green;
            case JudgmentType.Fail:
                return Color.red;
            default:
                return Color.white;
        }
    }
    
    /// <summary>
    /// 판정 결과에 따른 텍스트 반환
    /// </summary>
    public string GetJudgmentText(JudgmentType judgment)
    {
        switch (judgment)
        {
            case JudgmentType.Perfect:
                return "Perfect!";
            case JudgmentType.Good:
                return "Good!";
            case JudgmentType.Fail:
                return "Fail...";
            default:
                return "";
        }
    }
} 