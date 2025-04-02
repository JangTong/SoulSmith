using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class WeaponColliderHandler : MonoBehaviour
{
    private Material mat;

    public float maxEmission = 3f;
    public Color baseColor = Color.red;

    void Awake()
    {
        Renderer renderer = GetComponent<Renderer>();
        mat = renderer.material;

        if (mat.HasProperty("_EmissionColor"))
        {
            baseColor = mat.GetColor("_EmissionColor");
        }
    }

    public void SetEmissionLevel(int collisionCount)
    {
        float normalized = Mathf.Clamp01(collisionCount / 10f); // 10회 이상이면 최대
        float emission = normalized * maxEmission;

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.SetColor("_EmissionColor", baseColor * emission);
        }
    }
}
