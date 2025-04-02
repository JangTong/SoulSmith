using UnityEngine;

public abstract class Tool : MonoBehaviour
{
    public string toolName;

    // 사용 행동은 상속받은 클래스가 정의함
    public abstract void Use();
}
