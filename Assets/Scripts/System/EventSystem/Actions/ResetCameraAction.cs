using UnityEngine;

[CreateAssetMenu(menuName = "GameEvent/Action/Reset Camera")]
public class ResetCameraAction : ScriptableObject, IEventAction
{
   public void Execute()
    {
        PlayerController.Instance.ResetCameraToLocalDefault(0.5f, true);

        Debug.Log($"카메라 리셋");
    }
}
