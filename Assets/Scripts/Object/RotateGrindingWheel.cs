using UnityEngine;
using System.Collections;

public class RotateGrindingWheel : MonoBehaviour
{
    public ParticleSystem sparkEffect;
    public float rotationSpeed = 100f; // 회전 속도
    private bool isPlayerInside = false; // 플레이어가 트리거 안에 있는지 확인
    private Coroutine polishingCoroutine = null; // 현재 연마 작업 코루틴 참조
    public int minTime; // 최소 연마 시간
    public int maxTime; // 최대 연마 시간
    private Material sparkMaterial;

    private void Start()
    {
        
    }

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
                int polishTime = Random.Range(minTime, maxTime + 1);
                polishingCoroutine = StartCoroutine(PolishItemAfterDelay(polishingItem, polishTime)); // 무작위 초 후 연마
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

    private IEnumerator PolishItemAfterDelay(ItemComponent item, int delay)
    {
        sparkMaterial = sparkEffect.GetComponent<ParticleSystemRenderer>().material;
        Color EmissionColor;
        for(int i = 0; i < delay; i++) // 매초 파티클 색상 변경 및 출력
        {
            EmissionColor = new Color((float)(i) / (delay - 1), 0.2f, (float)(delay - i - 1) / (delay - 1)) * 5f;
            sparkMaterial.SetColor("_EmissionColor", EmissionColor);
            sparkEffect.Stop();
            sparkEffect.Play();
            yield return new WaitForSeconds(1f);
        }

        // 연마 상태 설정
        if (!item.isPolished)
        {
            item.isPolished = true;
            item.atkPower *= 1.1f;
            item.defPower *= 1.1f;
            item.sellPrice += 5;
            Debug.Log($"연마 완료: {item.itemName}, 공격력: {item.atkPower}");
        }

        for(int i = 0; i < 10; i++) // 과열
        {
            EmissionColor = new Color(1f, 1f, 1f) * 10f;
            sparkMaterial.SetColor("_EmissionColor", EmissionColor);
            sparkEffect.Stop();
            sparkEffect.Play();
            yield return new WaitForSeconds(0.5f);
        }
        
        for(int i = 0; i < 5; i++) // 파손
        {
            EmissionColor = new Color(0.5f, 0.5f, 0.5f);
            sparkMaterial.SetColor("_EmissionColor", EmissionColor);
            sparkEffect.Stop();
            sparkEffect.Play();
            yield return new WaitForSeconds(1f);
        }
        item.isPolished = false;
        item.atkPower *= 0.8f;
        item.defPower *= 0.8f;
        item.sellPrice -= 6;
        Debug.Log($"과도한 연마로 장비가 손상되었습니다");

        polishingCoroutine = null; // 코루틴 참조 초기화
    }
}