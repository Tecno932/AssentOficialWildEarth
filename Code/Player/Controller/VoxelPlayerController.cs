using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WildEarth.Voxel
{
    [RequireComponent(typeof(VoxelPlayerMovement))]
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

        [Header("Voxel Interaction")]
        [SerializeField]
        private float interactionDistance = 6f;

        [SerializeField]
        private ushort placeBlockId = 2;

        [Header("Mode")]
        [SerializeField]
        private VoxelPlayerMode startMode =
            VoxelPlayerMode.Survival;

        private VoxelPlayerMovement movement;

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

            movement.SetMode(
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

            HandleVoxelInteraction();
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

        private void HandleVoxelInteraction()
        {
            if (Cursor.lockState !=
                CursorLockMode.Locked)
            {
                return;
            }

            if (Mouse.current == null)
                return;

            if (!Physics.Raycast(
                    playerCamera.transform.position,
                    playerCamera.transform.forward,
                    out RaycastHit hit,
                    interactionDistance))
            {
                return;
            }

            if (Mouse.current.leftButton
                    .wasPressedThisFrame)
            {
                BreakVoxel(hit);
            }

            if (Mouse.current.rightButton
                    .wasPressedThisFrame)
            {
                PlaceVoxel(hit);
            }
        }

        private void BreakVoxel(
            RaycastHit hit)
        {
            Vector3 point =
                hit.point -
                hit.normal * 0.001f;

            WorldVoxelCoordinate voxel =
                ToVoxelCoordinate(point);

            worldRuntime.World.TrySetVoxel(
                voxel.X,
                voxel.Y,
                voxel.Z,
                0
            );
        }

        private void PlaceVoxel(
            RaycastHit hit)
        {
            Vector3 point =
                hit.point +
                hit.normal * 0.001f;

            WorldVoxelCoordinate voxel =
                ToVoxelCoordinate(point);

            worldRuntime.World.TrySetVoxel(
                voxel.X,
                voxel.Y,
                voxel.Z,
                placeBlockId
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

        private static WorldVoxelCoordinate
            ToVoxelCoordinate(
                Vector3 worldPosition)
        {
            return new WorldVoxelCoordinate(
                Mathf.FloorToInt(
                    worldPosition.x
                ),
                Mathf.FloorToInt(
                    worldPosition.y
                ),
                Mathf.FloorToInt(
                    worldPosition.z
                )
            );
        }
    }
}