using UnityEngine;

public class CombatHitData
{
    public readonly Transform Attacker;
    public readonly Transform Target;
    public readonly Vector3 OriginPoint;
    public readonly Vector3 ImpactPoint;
    public readonly Vector3 Direction;
    public readonly float Damage;
    public readonly float Knockback;
    public readonly string AttackName;

    public Vector3 KnockbackForce => Direction * Knockback;

    public CombatHitData
        (Transform attacker, Transform target, Vector3 originPoint, Vector3 impactPoint, Vector3 direction, float damage, float knockback, string attackName)
    {
        Attacker = attacker;
        Target = target;
        OriginPoint = originPoint;
        ImpactPoint = impactPoint;
        Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        Damage = damage;
        Knockback = knockback;
        AttackName = attackName;
    }
}

public interface IHittable
{
    void ReceiveHit(in CombatHitData hitData);
}