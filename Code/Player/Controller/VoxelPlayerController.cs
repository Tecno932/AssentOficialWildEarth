using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WildEarth.Voxel
{
    [RequireComponent(typeof(VoxelPlayerMovement))]
    [RequireComponent(typeof(VoxelPlayerInteraction))]
    [RequireComponent(typeof(CharacterController))]
    public sealed class VoxelPlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private VoxelWorldRuntime worldRuntime;

        [SerializeField]
        private Camera playerCamera;

        [Header("Look")]
        [SerializeField]
        private float mouseSensitivity = 0.1f;

        [Header("Mode")]
        [SerializeField]
        private VoxelPlayerMode startMode =
            VoxelPlayerMode.Survival;

        private VoxelPlayerMovement movement;
        private VoxelPlayerInteraction interaction;

        private float cameraPitch;

        private void Awake()
        {
            if (worldRuntime == null)
            {
                throw new InvalidOperationException(
                    "VoxelPlayerController requiere " +
                    "VoxelWorldRuntime."
                );
            }

            if (playerCamera == null)
            {
                throw new InvalidOperationException(
                    "VoxelPlayerController requiere " +
                    "una Camera."
                );
            }

            movement =
                GetComponent<VoxelPlayerMovement>();

            interaction =
                GetComponent<VoxelPlayerInteraction>();

            movement.SetMode(
                startMode
            );

            interaction.SetMode(
                startMode
            );
        }

        private void Start()
        {
            LockCursor();
        }

        private void Update()
        {
            HandleModeInput();
            HandleLook();

            movement.Tick();
            interaction.Tick();

            HandleCursorInput();
        }

        private void HandleModeInput()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current.f1Key.wasPressedThisFrame)
            {
                SetMode(
                    VoxelPlayerMode.Survival
                );
            }

            if (Keyboard.current.f2Key.wasPressedThisFrame)
            {
                SetMode(
                    VoxelPlayerMode.Creative
                );
            }

            if (Keyboard.current.f3Key.wasPressedThisFrame)
            {
                SetMode(
                    VoxelPlayerMode.Spectator
                );
            }
        }

        private void SetMode(
            VoxelPlayerMode mode)
        {
            movement.SetMode(mode);
            interaction.SetMode(mode);
        }

        private void HandleLook()
        {
            if (Cursor.lockState !=
                CursorLockMode.Locked)
            {
                return;
            }

            if (Mouse.current == null)
                return;

            Vector2 mouseDelta =
                Mouse.current.delta.ReadValue();

            float mouseX =
                mouseDelta.x *
                mouseSensitivity;

            float mouseY =
                mouseDelta.y *
                mouseSensitivity;

            transform.Rotate(
                Vector3.up,
                mouseX
            );

            cameraPitch -= mouseY;

            cameraPitch =
                Mathf.Clamp(
                    cameraPitch,
                    -89f,
                    89f
                );

            playerCamera.transform.localRotation =
                Quaternion.Euler(
                    cameraPitch,
                    0f,
                    0f
                );
        }

        private void HandleCursorInput()
        {
            if (Keyboard.current != null &&
                Keyboard.current.escapeKey
                    .wasPressedThisFrame)
            {
                UnlockCursor();
            }

            if (Mouse.current != null &&
                Mouse.current.leftButton
                    .wasPressedThisFrame)
            {
                LockCursor();
            }
        }

        private void LockCursor()
        {
            Cursor.lockState =
                CursorLockMode.Locked;

            Cursor.visible = false;
        }

        private void UnlockCursor()
        {
            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible = true;
        }
    }
}