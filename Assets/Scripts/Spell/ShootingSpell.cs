using UnityEngine;

[CreateAssetMenu(menuName = "Spell/ShootingSpell")]
public class ShootingSpell : MagicSpell
{
    public GameObject projectilePrefab;
    public float launchForce = 20f;

    public override void Fire(Transform caster)
    {
        if (projectilePrefab == null || caster == null) return;

        Vector3 spawnPos = caster.position + caster.forward * 2f;
        Quaternion rotation = Quaternion.LookRotation(caster.forward, caster.up);

        GameObject projectile = GameObject.Instantiate(projectilePrefab, spawnPos, rotation);

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = caster.forward * launchForce;
        }

        Debug.Log($"'{this.name}'시전됨!");
    }
}
