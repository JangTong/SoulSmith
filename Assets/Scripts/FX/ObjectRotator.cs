using UnityEngine;

public class InfiniteRotator : MonoBehaviour
{
    public float rotationSpeed = 90f; // 초당 회전 각도

    private void Update()
    {
        // 일정한 속도로 Y축 기준으로 회전
        transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
    }
}
