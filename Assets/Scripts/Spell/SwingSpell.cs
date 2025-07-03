using UnityEngine;
using System.Collections;

/// <summary>
/// 휘두르는 마법 - 플레이어 위치에 사용이펙트, Raycast 지점에 폭발이펙트
/// </summary>
[CreateAssetMenu(menuName = "Spell/SwingSpell")]
public class SwingSpell : MagicSpell
{
    // Debug
    private const string LOG_PREFIX = "[SwingSpell]";
    
    [Header("Effect Settings")]
    [Tooltip("플레이어 위치에 재생될 사용 이펙트 프리팹")]
    public GameObject castEffectPrefab;
    
    [Tooltip("Raycast 지점에 재생될 폭발 이펙트 프리팹")]
    public GameObject explosionEffectPrefab;
    
    [Header("Raycast Settings")]
    [Tooltip("Raycast 최대 거리")]
    public float maxDistance = 10f;
    
    [Tooltip("Raycast가 감지할 레이어")]
    public LayerMask hitLayers = -1;
    
    [Header("Timing Settings")]
    [Tooltip("사용이펙트와 폭발이펙트 사이의 딜레이 (초)")]
    public float effectDelay = 0.5f;
    
    [Tooltip("이펙트 자동 삭제 시간 (초, 0이면 자동삭제 안함)")]
    public float effectLifetime = 3f;
    
    [Header("Audio Settings")]
    [Tooltip("사용 시 재생할 오디오 클립")]
    public AudioClip castSound;
    
    [Tooltip("폭발 시 재생할 오디오 클립")]
    public AudioClip explosionSound;
    
    [Tooltip("오디오 볼륨")]
    [Range(0f, 1f)]
    public float audioVolume = 0.7f;

    public override void Fire(Transform caster)
    {
        if (caster == null)
        {
            Debug.LogWarning($"{LOG_PREFIX} Caster is null!");
            return;
        }

        Debug.Log($"{LOG_PREFIX} '{spellName}' 시전 시작!");
        
        // 사용이펙트 즉시 재생
        PlayCastEffect(caster);
        
        // MonoBehaviour 컴포넌트를 통해 코루틴 실행
        MonoBehaviour casterMono = caster.GetComponent<MonoBehaviour>();
        if (casterMono != null)
        {
            casterMono.StartCoroutine(DelayedExplosion(caster));
        }
        else
        {
            // MonoBehaviour가 없는 경우 즉시 폭발이펙트 재생
            Debug.LogWarning($"{LOG_PREFIX} No MonoBehaviour found on caster, playing explosion immediately");
            StartRaycastAndExplosion(caster);
        }
    }
    
    /// <summary>
    /// 플레이어 위치에 사용이펙트 재생
    /// </summary>
    private void PlayCastEffect(Transform caster)
    {
        if (castEffectPrefab == null)
        {
            Debug.LogWarning($"{LOG_PREFIX} Cast effect prefab is not assigned!");
            return;
        }
        
        // 플레이어 위치에 사용이펙트 생성
        Vector3 castPosition = caster.position;
        Quaternion castRotation = caster.rotation;
        
        GameObject castEffect = GameObject.Instantiate(castEffectPrefab, castPosition, castRotation);
        
        // 사용 사운드 재생
        if (castSound != null)
        {
            PlayAudioAtPosition(castPosition, castSound);
        }
        
        // 자동 삭제 설정
        if (effectLifetime > 0)
        {
            GameObject.Destroy(castEffect, effectLifetime);
        }
        
        Debug.Log($"{LOG_PREFIX} Cast effect played at {castPosition}");
    }
    
    /// <summary>
    /// 딜레이 후 Raycast 및 폭발이펙트 재생
    /// </summary>
    private IEnumerator DelayedExplosion(Transform caster)
    {
        // 딜레이 대기
        yield return new WaitForSeconds(effectDelay);
        
        // Raycast 및 폭발이펙트 실행
        StartRaycastAndExplosion(caster);
    }
    
    /// <summary>
    /// Raycast 실행 및 폭발이펙트 재생
    /// </summary>
    private void StartRaycastAndExplosion(Transform caster)
    {
        // Raycast 실행
        Vector3 rayOrigin = caster.position + Vector3.up * 1.5f; // 플레이어 가슴 높이
        Vector3 rayDirection = caster.forward;
        
        Debug.DrawRay(rayOrigin, rayDirection * maxDistance, Color.red, 2f);
        
        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, maxDistance, hitLayers))
        {
            // 충돌 지점에 폭발이펙트 재생
            PlayExplosionEffect(hit.point, hit.normal);
            Debug.Log($"{LOG_PREFIX} Raycast hit: {hit.collider.name} at {hit.point}");
        }
        else
        {
            // 최대 거리 지점에 폭발이펙트 재생
            Vector3 endPoint = rayOrigin + rayDirection * maxDistance;
            PlayExplosionEffect(endPoint, -rayDirection);
            Debug.Log($"{LOG_PREFIX} Raycast reached max distance: {endPoint}");
        }
    }
    
    /// <summary>
    /// 폭발이펙트 재생
    /// </summary>
    private void PlayExplosionEffect(Vector3 position, Vector3 normal)
    {
        if (explosionEffectPrefab == null)
        {
            Debug.LogWarning($"{LOG_PREFIX} Explosion effect prefab is not assigned!");
            return;
        }
        
        // 표면 법선에 맞춰 회전 설정
        Quaternion explosionRotation = Quaternion.LookRotation(normal);
        
        GameObject explosionEffect = GameObject.Instantiate(explosionEffectPrefab, position, explosionRotation);
        
        // 폭발 사운드 재생
        if (explosionSound != null)
        {
            PlayAudioAtPosition(position, explosionSound);
        }
        
        // 자동 삭제 설정
        if (effectLifetime > 0)
        {
            GameObject.Destroy(explosionEffect, effectLifetime);
        }
        
        Debug.Log($"{LOG_PREFIX} Explosion effect played at {position}");
    }
    
    /// <summary>
    /// 특정 위치에서 오디오 재생
    /// </summary>
    private void PlayAudioAtPosition(Vector3 position, AudioClip clip)
    {
        if (clip == null) return;
        
        // 임시 오디오 소스 생성
        GameObject tempAudioObject = new GameObject("TempAudio_" + clip.name);
        tempAudioObject.transform.position = position;
        
        AudioSource audioSource = tempAudioObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = audioVolume;
        audioSource.spatialBlend = 1f; // 3D 사운드
        audioSource.Play();
        
        // 오디오 재생 완료 후 자동 삭제
        GameObject.Destroy(tempAudioObject, clip.length + 0.1f);
    }
    
    /// <summary>
    /// Inspector에서 값 검증
    /// </summary>
    private void OnValidate()
    {
        // 거리 제한
        maxDistance = Mathf.Max(0.1f, maxDistance);
        
        // 딜레이 제한
        effectDelay = Mathf.Max(0f, effectDelay);
        
        // 수명 제한
        effectLifetime = Mathf.Max(0f, effectLifetime);
        
        // 볼륨 제한
        audioVolume = Mathf.Clamp01(audioVolume);
    }
} 