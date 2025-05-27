using UnityEngine;

[CreateAssetMenu(menuName = "GameEvent/Action/Reset Camera")]
public class ResetCameraAction : ScriptableObject, IEventAction
{
   public void Execute()
    {
        PlayerController.Instance.cam.ResetToDefault(0.5f, true);

        Debug.Log($"카메라 리셋");
    }
}
