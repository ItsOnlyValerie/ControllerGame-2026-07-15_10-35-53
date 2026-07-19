using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField, Min(0f)] private float windup = 0.08f;
    [SerializeField, Min(0f)] private float recovery = 0.18f;
    [SerializeField, Min(0f)] private float damage = 10f;
    [SerializeField, Min(0f)] private float knockback = 6f;
    [SerializeField] private string attackName = "Punch";
    [SerializeField] private float hitHeight = 1f;
    [SerializeField] private float forwardDistance = 0.85f;
    [SerializeField, Min(0f)] private float hitRadius = 0.85f;
    [SerializeField] private LayerMask targetLayers = ~0;
    [SerializeField, Min(4)] private int maxOverlapCount = 8;
    [FormerlySerializedAs("cameraShakeAmount")]
    [SerializeField, Min(0f)] private float hitImpulseForce = 0.18f;
    [FormerlySerializedAs("cameraShakeDurtaion")]
    [SerializeField, Min(0.01f)] private float hitImpulseDuration = 0.12f;

    private readonly HashSet<Transform> hitRoots = new HashSet<Transform>();

    private Collider[] overlapResults;
    private CinemachineImpulseSource impulseSource;
    private InputAction attackAction;
    private Coroutine attackRoutine;
    private Vector3 lastHitOrigin;

    private void Awake()
    {
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }

        EnsureBuffer();
        EnsureImpulseSource();
    }

    private void OnEnable()
    {
        BindAttackAction();
    }

    private void OnDisable()
    {
        UnbindAttackAction();

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
    }

    private void OnValidate()
    {
        maxOverlapCount = Mathf.Max(4, maxOverlapCount);
        EnsureBuffer();
    }

    private void BindAttackAction()
    {
        if (playerInput == null || playerInput.actions == null)
        {
            return;
        }

        attackAction = playerInput.actions["Attack"];

        if (attackAction != null)
        {
            attackAction.performed += OnAttackPerformed;
        }
    }

    private void UnbindAttackAction()
    {
        if (attackAction == null)
        {
            return;
        }

        attackAction.performed -= OnAttackPerformed;
        attackAction = null;
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            TryAttack();
        }
    }

    private void TryAttack()
    {
        if (attackRoutine != null)
        {
            return;
        }

        attackRoutine = StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        if (windup > 0f)
        {
            yield return new WaitForSeconds(windup);
        }

        PerformHitScan();

        if (recovery > 0f)
        {
            yield return new WaitForSeconds(recovery);
        }

        attackRoutine = null;
    }

    private void PerformHitScan()
    {
        EnsureBuffer();
        hitRoots.Clear();

        Vector3 attackDirection = playerController != null ? playerController.FacingDirection : transform.forward;
        attackDirection.y = 0f;

        if (attackDirection.sqrMagnitude <= 0.0001f)
        {
            attackDirection = transform.forward;
            attackDirection.y = 0f;
        }

        attackDirection.Normalize();
        lastHitOrigin = transform.position + Vector3.up * hitHeight + attackDirection * forwardDistance;

        int hitCount = Physics.OverlapSphereNonAlloc(lastHitOrigin, hitRadius, overlapResults, targetLayers, QueryTriggerInteraction.Ignore);

        bool landedHit = false;

        for (int i=0; i < hitCount; i++)
        {
            Collider result = overlapResults[i];
            overlapResults[i] = null;

            if (result == null)
            {
                continue;
            }

            Transform targetRoot = result.transform.root;

            if (targetRoot == transform.root || !hitRoots.Add(targetRoot))
            {
                continue;
            }

            IHittable hittable = result.GetComponentInParent<IHittable>();

            if (hittable == null)
            {
                continue;
            }

            CombatHitData hitData = new CombatHitData(transform.root, targetRoot, lastHitOrigin, result.ClosestPoint(lastHitOrigin), attackDirection, damage, knockback, attackName);
            hittable.ReceiveHit(hitData);
            landedHit = true;
        }

        if (landedHit)
        {
            PlayHitImpulse();
        }
    }

    private void PlayHitImpulse()
    {
        if (hitImpulseForce <= 0f || hitImpulseDuration <= 0f)
        {
            return;
        }

        EnsureImpulseSource();
        EnsureCameraListener();

        impulseSource.ImpulseDefinition.ImpulseChannel = 1;
        impulseSource.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
        impulseSource.ImpulseDefinition.ImpulseDuration = hitImpulseDuration;
        impulseSource.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
        impulseSource.ImpulseDefinition.DissipationDistance = 100f;
        impulseSource.ImpulseDefinition.DissipationRate = 0.25f;
        impulseSource.ImpulseDefinition.PropagationSpeed = 343f;
        impulseSource.ImpulseDefinition.OnValidate();

        impulseSource.GenerateImpulseWithVelocity(Vector3.down * hitImpulseForce);
    }

    private void EnsureBuffer()
    {
        if (overlapResults == null || overlapResults.Length != maxOverlapCount)
        {
            overlapResults = new Collider[maxOverlapCount];
        }
    }

    private void EnsureImpulseSource()
    {
        if (impulseSource == null)
        {
            impulseSource = GetComponent<CinemachineImpulseSource>();
        }

        if (impulseSource == null)
        {
            impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
        }
    }

    private static void EnsureCameraListener()
    {
        if (Camera.main == null)
        {
            return;
        }

        if (Camera.main.GetComponent<CinemachineImpulseListener>() != null)
        {
            return;
        }

        Camera.main.gameObject.AddComponent<CinemachineImpulseListener>();
    }
}