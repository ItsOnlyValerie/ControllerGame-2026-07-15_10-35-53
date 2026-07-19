using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour, IHittable
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -9.8f;
    [SerializeField] private float groundedSnapVelocity = -2f;
    [SerializeField] private float knockbackDamping = 12f;

    [Header("Hit Feedback Settings")]
    [SerializeField] private Color hitFlashColor = Color.white;
    [SerializeField, Min(0.01f)] private float hitFlashDuration = 0.12f;
    [SerializeField] private TextMeshProUGUI label;

    private CharacterController controller;
    private PlayerHealth health;
    private Renderer cachedRenderer;
    private Material cachedMaterial;
    private Vector2 moveInput;
    private Vector3 velocity;
    private Vector3 knockbackVelocity;
    private Vector3 lastMoveDirection = Vector3.forward;
    private Coroutine flashRoutine;
    private Color assignedColor = Color.white;
    private string deviceLabel = "Unknown Device";

    public Vector3 FacingDirection => lastMoveDirection.sqrMagnitude > 0.0001f ? lastMoveDirection : transform.forward;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        health = EnsureComponent<PlayerHealth>();
        cachedRenderer = GetComponent<Renderer>();

        if (cachedRenderer != null)
        {
            cachedMaterial = cachedRenderer.material;
            assignedColor = cachedMaterial.color;
        }

        Vector3 startingForward = transform.forward;
        startingForward.y = 0f;

        if (startingForward.sqrMagnitude > 0.0001f)
        {
            lastMoveDirection = startingForward.normalized;
        }

        RefreshLabel();
    }

    private void OnEnable()
    {
        if (health == null)
        {
            health = EnsureComponent<PlayerHealth>();
        }

        health.HealthChanged += OnHealthChanged;
        RefreshLabel();
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.HealthChanged -= OnHealthChanged;
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        ApplyColor(assignedColor);
    }

    public void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt (jumpHeight * -2f * gravity);
        }
    }

    public void SetLabel(string newLabel)
    {
        deviceLabel = string.IsNullOrWhiteSpace(newLabel) ? "Unknown Device" : newLabel;
        RefreshLabel();
    }

    public void SetAssignedColor(Color color)
    {
        assignedColor = color;
        ApplyColor(assignedColor);
    }

    public void ReceiveHit(in CombatHitData hitData)
    {
        health?.ApplyDamage(hitData.Damage);
        knockbackVelocity += hitData.KnockbackForce;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashRoutine());
        RefreshLabel();
    }

    private void Update()
    {
        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);

        if (move.sqrMagnitude > 0.0001f)
        {
            lastMoveDirection = move.normalized;
            transform.rotation = Quaternion.LookRotation(lastMoveDirection, Vector3.up);
        }

        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = groundedSnapVelocity;
        }
        
        velocity.y += gravity * Time.deltaTime;

        Vector3 totalVelocity = move * speed + velocity + knockbackVelocity;
        controller.Move(totalVelocity * Time.deltaTime);

        knockbackVelocity = Vector3.MoveTowards(knockbackVelocity,Vector3.zero, knockbackDamping * Time.deltaTime);
    }

    private IEnumerator FlashRoutine()
    {
        ApplyColor(hitFlashColor);
        yield return new WaitForSeconds(hitFlashDuration);
        ApplyColor(assignedColor);
        flashRoutine = null;
    }

    private void OnHealthChanged(float currentHealth, float maxHealth)
    {
        RefreshLabel();
    }

    private void RefreshLabel()
    {
        if (label != null)
        {
            string healthText = health != null ? $"{health.CurrentHealth:0}/{health.MaxHealth:0}" : "N/A";
            label.text = $"{deviceLabel}\n{healthText}";
        }
    }

    private void ApplyColor(Color color)
    {
        if (cachedMaterial != null)
        {
            cachedMaterial.color = color;
        }
    }

    private T EnsureComponent<T>() where T : Component
    {
        if (!TryGetComponent<T>(out T comp))
        {
            comp = gameObject.AddComponent<T>();
        }
        return comp;
    }
}