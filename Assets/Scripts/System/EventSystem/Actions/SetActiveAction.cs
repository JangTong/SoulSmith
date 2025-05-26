using UnityEngine;

//오브젝트 활성화 함수
[CreateAssetMenu(menuName = "GameEvent/Action/Set Active")]
public class SetActiveAction : ScriptableObject, IEventAction
{
    public GameObject target;
    public bool setActive = true;

    public void Execute()
    {
        if (target != null)
        {
            target.SetActive(setActive);
            Debug.Log($"[SetActiveAction] {target.name} 활성 상태: {setActive}");
        }
        else
        {
            Debug.LogWarning("[SetActiveAction] 타겟이 비어 있습니다.");
        }
    }
}