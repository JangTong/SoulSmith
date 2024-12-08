using UnityEngine;

public class Bellows : InteractiveObject
{
    private Forge forge; // 부모 오브젝트에 연결된 Forge 스크립트 참조

    private void Start()
    {
        // 부모 오브젝트에서 Forge 컴포넌트 찾기
        forge = GetComponentInParent<Forge>();

        if (forge == null)
        {
            Debug.LogError("Blower의 부모 오브젝트에 Forge 컴포넌트가 없습니다!");
        }
    }

    public override void Interaction()
    {
        if (forge != null)
        {
            Debug.Log("Blower와 상호작용 중: Forging 실행");
            string[] soundNames = { "Bellow_1", "Bellow_2" }; // 소리 이름 배열
            int randIndex = Random.Range(0, soundNames.Length); // 랜덤 인덱스 선택
            SoundManager.Instance.PlaySoundAtPosition(soundNames[randIndex], transform.position);
            
            forge.StartForging(); // Forge 클래스의 Forging 함수 호출
        }
        else
        {
            Debug.LogWarning("Forge 컴포넌트가 연결되지 않아 Forging을 실행할 수 없습니다.");
        }
    }
}
