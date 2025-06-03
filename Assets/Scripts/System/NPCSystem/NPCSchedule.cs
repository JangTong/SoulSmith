using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ScheduleEntry
{
    [Header("시간 설정")]
    [Range(0, 23)]
    public int hour = 9; // 24시간 형식
    [Range(0, 59)]
    public int minute = 0;
    
    [Header("위치 설정 (우선순위: Transform > Tag > Name > Position)")]
    [Tooltip("Transform이 할당되면 최우선으로 사용됩니다 (씬에서만 설정 가능)")]
    public Transform targetLocation; // 런타임에서만 사용 가능
    
    [Tooltip("Transform이 없을 때 태그로 오브젝트를 찾습니다")]
    public string locationTag = ""; // 태그로 위치 찾기
    
    [Tooltip("Transform이 없을 때 이름으로 오브젝트를 찾습니다")]
    public string locationName = ""; // 오브젝트 이름으로 찾기
    
    [Tooltip("Transform을 찾을 수 없을 때 사용할 직접 좌표")]
    public Vector3 fallbackPosition = Vector3.zero; // 대체 위치
    
    [Tooltip("현재 위치에서 상대적인 오프셋")]
    public Vector3 positionOffset = Vector3.zero; // 위치 오프셋
    
    [Header("행동 타입")]
    public NPCBehaviorType behaviorType = NPCBehaviorType.Idle;
    
    [Header("애니메이션")]
    public string animationState = "Idle"; // 애니메이터 상태 이름
    public bool loopAnimation = true;
    
    [Header("이동 설정")]
    public float moveSpeed = 1.5f; // 이 목적지로 이동할 때의 속도
    public float stoppingDistance = 0.5f;
    
    [Header("대기 시간")]
    public float waitTimeBeforeNext = 0f; // 다음 스케줄로 넘어가기 전 대기 시간
    
    [Header("특별 행동")]
    public bool shouldLookAround = false; // 주변을 둘러볼지 여부
    public float lookAroundInterval = 3f; // 둘러보는 간격
    
    /// <summary>
    /// 실제 목적지 위치를 반환 (우선순위에 따라)
    /// </summary>
    public Vector3 GetTargetPosition()
    {
        Vector3 basePosition = Vector3.zero;
        bool foundTarget = false;
        
        // 1순위: Transform이 할당된 경우
        if (targetLocation != null)
        {
            basePosition = targetLocation.position;
            foundTarget = true;
        }
        // 2순위: 태그로 찾기
        else if (!string.IsNullOrEmpty(locationTag))
        {
            GameObject taggedObject = GameObject.FindGameObjectWithTag(locationTag);
            if (taggedObject != null)
            {
                basePosition = taggedObject.transform.position;
                foundTarget = true;
                
                // 런타임에서 찾은 Transform을 캐시 (성능 최적화)
                targetLocation = taggedObject.transform;
            }
        }
        // 3순위: 이름으로 찾기
        else if (!string.IsNullOrEmpty(locationName))
        {
            GameObject namedObject = GameObject.Find(locationName);
            if (namedObject != null)
            {
                basePosition = namedObject.transform.position;
                foundTarget = true;
                
                // 런타임에서 찾은 Transform을 캐시
                targetLocation = namedObject.transform;
            }
        }
        
        // 4순위: 대체 위치 사용
        if (!foundTarget)
        {
            basePosition = fallbackPosition;
        }
        
        // 오프셋 적용
        return basePosition + positionOffset;
    }
    
    /// <summary>
    /// 목적지 이름 반환 (디버그용)
    /// </summary>
    public string GetLocationDisplayName()
    {
        if (targetLocation != null)
            return targetLocation.name;
        
        if (!string.IsNullOrEmpty(locationTag))
            return $"Tag:{locationTag}";
            
        if (!string.IsNullOrEmpty(locationName))
            return locationName;
            
        return $"Position{fallbackPosition}";
    }
    
    /// <summary>
    /// 유효한 목적지가 있는지 확인
    /// </summary>
    public bool HasValidTarget()
    {
        return targetLocation != null || 
               !string.IsNullOrEmpty(locationTag) || 
               !string.IsNullOrEmpty(locationName) || 
               fallbackPosition != Vector3.zero;
    }
    
    // 스케줄 시간을 총 분으로 변환
    public int GetTotalMinutes()
    {
        return hour * 60 + minute;
    }
    
    // 시간을 문자열로 반환
    public string GetTimeString()
    {
        return $"{hour:00}:{minute:00}";
    }
}

public enum NPCBehaviorType
{
    Idle,           // 가만히 서 있기
    Walking,        // 걷기
    Working,        // 작업하기
    Talking,        // 대화하기
    Sitting,        // 앉아있기
    Eating,         // 먹기
    Shopping,       // 쇼핑하기
    Sleeping,       // 잠자기
    Reading,        // 읽기
    Custom          // 커스텀 행동
}

[CreateAssetMenu(fileName = "NPCSchedule", menuName = "NPC System/NPC Schedule")]
public class NPCSchedule : ScriptableObject
{
    [Header("스케줄 정보")]
    public string scheduleName = "Default Schedule";
    [TextArea(2, 4)]
    public string description = "NPC의 하루 일정을 정의합니다.";
    
    [Header("스케줄 엔트리")]
    public List<ScheduleEntry> scheduleEntries = new List<ScheduleEntry>();
    
    [Header("기본 설정")]
    public bool enableSchedule = true; // 스케줄 활성화 여부
    public bool loopSchedule = true; // 스케줄 반복 여부
    public ScheduleEntry defaultEntry; // 스케줄이 없을 때 기본 행동
    
    private void OnValidate()
    {
        // 스케줄 엔트리를 시간순으로 정렬
        scheduleEntries.Sort((a, b) => a.GetTotalMinutes().CompareTo(b.GetTotalMinutes()));
        
        // 위치 이름 자동 설정 및 유효성 검사
        foreach (var entry in scheduleEntries)
        {
            if (entry.targetLocation != null && string.IsNullOrEmpty(entry.locationName))
            {
                entry.locationName = entry.targetLocation.name;
            }
            
            // 유효하지 않은 엔트리 경고
            if (!entry.HasValidTarget())
            {
                Debug.LogWarning($"[NPCSchedule] {scheduleName}: {entry.GetTimeString()} 엔트리에 유효한 목적지가 설정되지 않았습니다!");
            }
        }
    }
    
    /// <summary>
    /// 현재 시간에 맞는 스케줄 엔트리를 반환
    /// </summary>
    public ScheduleEntry GetCurrentScheduleEntry(int currentHour, int currentMinute)
    {
        if (!enableSchedule || scheduleEntries.Count == 0)
        {
            return defaultEntry;
        }
        
        int currentTotalMinutes = currentHour * 60 + currentMinute;
        
        // 현재 시간 이전의 가장 가까운 스케줄 찾기
        ScheduleEntry currentEntry = defaultEntry;
        
        for (int i = 0; i < scheduleEntries.Count; i++)
        {
            var entry = scheduleEntries[i];
            if (entry.GetTotalMinutes() <= currentTotalMinutes)
            {
                currentEntry = entry;
            }
            else
            {
                break;
            }
        }
        
        // 마지막 스케줄보다 늦은 시간이면 첫 번째 스케줄로 (다음날 준비)
        if (loopSchedule && currentEntry == null && scheduleEntries.Count > 0)
        {
            currentEntry = scheduleEntries[scheduleEntries.Count - 1];
        }
        
        return currentEntry ?? defaultEntry;
    }
    
    /// <summary>
    /// 다음 스케줄 엔트리를 반환
    /// </summary>
    public ScheduleEntry GetNextScheduleEntry(int currentHour, int currentMinute)
    {
        if (!enableSchedule || scheduleEntries.Count == 0)
        {
            return defaultEntry;
        }
        
        int currentTotalMinutes = currentHour * 60 + currentMinute;
        
        // 현재 시간 이후의 가장 가까운 스케줄 찾기
        for (int i = 0; i < scheduleEntries.Count; i++)
        {
            var entry = scheduleEntries[i];
            if (entry.GetTotalMinutes() > currentTotalMinutes)
            {
                return entry;
            }
        }
        
        // 다음 스케줄이 없으면 첫 번째 스케줄 반환 (다음날)
        if (loopSchedule && scheduleEntries.Count > 0)
        {
            return scheduleEntries[0];
        }
        
        return defaultEntry;
    }
    
    /// <summary>
    /// 디버그용 스케줄 정보 출력
    /// </summary>
    public string GetScheduleDebugInfo()
    {
        var info = $"Schedule: {scheduleName}\n";
        info += $"Enabled: {enableSchedule}, Loop: {loopSchedule}\n";
        info += $"Entries: {scheduleEntries.Count}\n";
        
        foreach (var entry in scheduleEntries)
        {
            string locationInfo = entry.GetLocationDisplayName();
            string validIcon = entry.HasValidTarget() ? "✓" : "✗";
            info += $"- {entry.GetTimeString()}: {locationInfo} ({entry.behaviorType}) {validIcon}\n";
        }
        
        return info;
    }
    
    /// <summary>
    /// 특정 위치 이름을 가진 엔트리들을 찾아서 Transform 업데이트
    /// </summary>
    [ContextMenu("Update Location References")]
    public void UpdateLocationReferences()
    {
        int updatedCount = 0;
        
        foreach (var entry in scheduleEntries)
        {
            // 이름 기반으로 Transform 찾기 시도
            if (entry.targetLocation == null && !string.IsNullOrEmpty(entry.locationName))
            {
                GameObject found = GameObject.Find(entry.locationName);
                if (found != null)
                {
                    entry.targetLocation = found.transform;
                    updatedCount++;
                    Debug.Log($"[NPCSchedule] {entry.locationName} Transform 업데이트됨");
                }
            }
            
            // 태그 기반으로 Transform 찾기 시도  
            if (entry.targetLocation == null && !string.IsNullOrEmpty(entry.locationTag))
            {
                GameObject found = GameObject.FindGameObjectWithTag(entry.locationTag);
                if (found != null)
                {
                    entry.targetLocation = found.transform;
                    updatedCount++;
                    Debug.Log($"[NPCSchedule] Tag '{entry.locationTag}' Transform 업데이트됨");
                }
            }
        }
        
        Debug.Log($"[NPCSchedule] {updatedCount}개 위치 참조가 업데이트되었습니다.");
    }
} 