using UnityEngine;
using System.Collections; // IEnumerator를 위한 네임스페이스

public class HammerHead : MonoBehaviour
{
    public ParticleSystem sparkEffect; // 불똥 파티클 시스템
    public float soundDelay = 0.3f;    // 소리 간 딜레이 (초)
    private bool isPlayingSound = false; // 현재 소리가 재생 중인지 확인

    private void OnTriggerEnter(Collider other)
    {
        if (ItemPickup.Instance.currentState == ItemPickupState.Swinging && other.gameObject.CompareTag("Items") && !isPlayingSound)
        {
            StartCoroutine(PlaySoundWithDelay());
        }
    }

    private IEnumerator PlaySoundWithDelay()
    {
        isPlayingSound = true;

        // 파티클 효과 재생
        if (sparkEffect != null)
        {
            sparkEffect.Play();
        }

        string[] soundNames = { "HammerHeat_1", "HammerHeat_3"};
        int randIndex = Random.Range(0, soundNames.Length);
        SoundManager.Instance.PlaySoundAtPosition(soundNames[randIndex], transform.position);

        yield return new WaitForSeconds(soundDelay);

        isPlayingSound = false;
    }
}
