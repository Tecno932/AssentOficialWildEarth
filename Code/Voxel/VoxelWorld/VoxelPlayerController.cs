using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WildEarth.Voxel
{
    public sealed class VoxelPlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private VoxelWorldRuntime worldRuntime;

        [SerializeField]
        private Camera playerCamera;

        [Header("Movement")]
        [SerializeField]
        private float moveSpeed = 6f;

        [SerializeField]
        private float mouseSensitivity = 0.1f;

        [Header("Voxel Interaction")]
        [SerializeField]
        private float interactionDistance = 6f;

        [SerializeField]
        private ushort placeBlockId = 2;

        private float cameraPitch;

        private void Awake()
        {
            if (worldRuntime == null)
                throw new InvalidOperationException(
                    "VoxelPlayerController requiere VoxelWorldRuntime."
                );

            if (playerCamera == null)
                throw new InvalidOperationException(
                    "VoxelPlayerController requiere una Camera."
                );
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            HandleLook();
            HandleMovement();
            HandleVoxelInteraction();

            if (Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void HandleLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
                return;

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

            cameraPitch = Mathf.Clamp(
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

        private void HandleMovement()
        {
            if (Keyboard.current == null)
                return;

            Vector2 input =
                Vector2.zero;

            if (Keyboard.current.aKey.isPressed)
                input.x -= 1f;

            if (Keyboard.current.dKey.isPressed)
                input.x += 1f;

            if (Keyboard.current.sKey.isPressed)
                input.y -= 1f;

            if (Keyboard.current.wKey.isPressed)
                input.y += 1f;

            Vector3 movement =
                transform.right * input.x +
                transform.forward * input.y;

            if (movement.sqrMagnitude > 1f)
                movement.Normalize();

            transform.position +=
                movement *
                moveSpeed *
                Time.deltaTime;

            if (Keyboard.current.spaceKey.isPressed)
            {
                transform.position +=
                    Vector3.up *
                    moveSpeed *
                    Time.deltaTime;
            }

            if (Keyboard.current.leftCtrlKey.isPressed)
            {
                transform.position +=
                    Vector3.down *
                    moveSpeed *
                    Time.deltaTime;
            }
        }

        private void HandleVoxelInteraction()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
                return;

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

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                BreakVoxel(hit);
            }

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                PlaceVoxel(hit);
            }
        }

        private void BreakVoxel(RaycastHit hit)
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

        private void PlaceVoxel(RaycastHit hit)
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

        private static WorldVoxelCoordinate ToVoxelCoordinate(
            Vector3 worldPosition)
        {
            return new WorldVoxelCoordinate(
                Mathf.FloorToInt(worldPosition.x),
                Mathf.FloorToInt(worldPosition.y),
                Mathf.FloorToInt(worldPosition.z)
            );
        }
    }
}