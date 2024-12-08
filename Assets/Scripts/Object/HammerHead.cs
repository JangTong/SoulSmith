using UnityEngine;
using System.Collections; // IEnumerator를 위한 네임스페이스

public class HammerHead : MonoBehaviour
{
    public ParticleSystem sparkEffect; // 불똥 파티클 시스템
    public float soundDelay = 0.5f;    // 소리 간 딜레이 (초)
    private bool isPlayingSound = false; // 현재 소리가 재생 중인지 확인

    private void OnTriggerEnter(Collider other)
    {
        if (ItemPickup.Instance.isSwinging && other.gameObject.CompareTag("Items") && !isPlayingSound)
        {
            StartCoroutine(PlaySoundWithDelay());
        }
    }

    private IEnumerator PlaySoundWithDelay()
    {
        isPlayingSound = true; // 소리 재생 중으로 설정

        // 파티클 효과 재생
        if (sparkEffect != null)
        {
            sparkEffect.Play();
        }

        // 소리 리스트 중 랜덤으로 하나 선택하여 재생
        string[] soundNames = { "HammerHeat_1", "HammerHeat_2", "HammerHeat_3" }; // 소리 이름 배열
        int randIndex = Random.Range(0, soundNames.Length); // 랜덤 인덱스 선택
        SoundManager.Instance.PlaySoundAtPosition(soundNames[randIndex], transform.position);

        // 딜레이
        yield return new WaitForSeconds(soundDelay);

        isPlayingSound = false; // 소리 재생 완료 후 대기 상태로 전환
    }
}
