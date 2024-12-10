using UnityEngine;
using System.Collections;

public class RotateGrindingWheel : MonoBehaviour
{
    public ParticleSystem sparkEffect;
    public float rotationSpeed = 100f; // 회전 속도
    private bool isPlayerInside = false; // 플레이어가 트리거 안에 있는지 확인
    private Coroutine polishingCoroutine = null; // 현재 연마 작업 코루틴 참조

    private void OnTriggerEnter(Collider other)
    {
        // ItemComponent 가져오기
        ItemComponent polishingItem = other.GetComponent<ItemComponent>();

        if (other.CompareTag("Items") && polishingItem != null)
        {
            isPlayerInside = true;

            // 연마가 완료되지 않은 아이템일 경우 코루틴 시작
            if (!polishingItem.isPolished)
            {
                // 기존 코루틴이 있으면 종료
                if (polishingCoroutine != null)
                {
                    StopCoroutine(polishingCoroutine);
                }
                // 새로운 코루틴 시작
                polishingCoroutine = StartCoroutine(PolishItemAfterDelay(polishingItem, 5f)); // 5초 후 연마
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // ItemComponent 가져오기
        ItemComponent polishingItem = other.GetComponent<ItemComponent>();

        if (other.CompareTag("Items") && polishingItem != null)
        {
            isPlayerInside = false;

            // 아이템이 트리거를 벗어날 경우 연마 코루틴 종료
            if (polishingCoroutine != null)
            {
                StopCoroutine(polishingCoroutine);
                polishingCoroutine = null;
            }
        }
    }

    private void Update()
    {
        // 플레이어가 트리거 안에 있을 때만 x축으로 회전
        if (isPlayerInside)
        {
            transform.Rotate(Vector3.right * rotationSpeed * Time.deltaTime);
        }
    }

    private IEnumerator PolishItemAfterDelay(ItemComponent item, float delay)
    {
        yield return new WaitForSeconds(delay); // delay 만큼 대기

        // 연마 상태 설정
        if (!item.isPolished)
        {
            item.isPolished = true;
            item.atkPower *= 1.1f;
            item.defPower *= 1.1f;
            Debug.Log($"연마 완료: {item.itemName}, 공격력: {item.atkPower}");
            sparkEffect.Play();
        }

        polishingCoroutine = null; // 코루틴 참조 초기화
    }
}
