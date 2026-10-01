using UnityEngine;
using UnityEngine.InputSystem;

namespace WildEarth.Voxel
{
    public sealed class VoxelPlayerInteraction : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private VoxelWorldRuntime worldRuntime;

        [SerializeField]
        private Camera playerCamera;

        [SerializeField]
        private CharacterController playerController;

        [Header("Interaction")]
        [SerializeField]
        private float interactionDistance = 6f;

        [SerializeField]
        private ushort placeBlockId = 2;

        private VoxelPlayerMode mode =
            VoxelPlayerMode.Survival;

        public void SetMode(
            VoxelPlayerMode newMode)
        {
            mode = newMode;
        }

        private void Awake()
        {
            if (worldRuntime == null)
            {
                throw new System.InvalidOperationException(
                    "VoxelPlayerInteraction requiere " +
                    "VoxelWorldRuntime."
                );
            }

            if (playerCamera == null)
            {
                throw new System.InvalidOperationException(
                    "VoxelPlayerInteraction requiere " +
                    "una Camera."
                );
            }

            if (playerController == null)
            {
                playerController =
                    GetComponent<CharacterController>();
            }
        }

        public void Tick()
        {
            if (Cursor.lockState !=
                CursorLockMode.Locked)
            {
                return;
            }

            if (Mouse.current == null)
                return;

            if (Mouse.current.leftButton
                    .wasPressedThisFrame)
            {
                TryBreakVoxel();
            }

            if (Mouse.current.rightButton
                    .wasPressedThisFrame)
            {
                TryPlaceVoxel();
            }
        }

        private void TryBreakVoxel()
        {
            if (!TryGetVoxelHit(
                    out RaycastHit hit))
            {
                return;
            }

            Vector3 point =
                hit.point -
                hit.normal * 0.001f;

            WorldVoxelCoordinate voxel =
                ToVoxelCoordinate(point);

            worldRuntime.World.TrySetVoxel(
                voxel.X,
                voxel.Y,
                voxel.Z,
                BlockIds.Air
            );
        }

        private void TryPlaceVoxel()
        {
            if (!TryGetVoxelHit(
                    out RaycastHit hit))
            {
                return;
            }

            Vector3 point =
                hit.point +
                hit.normal * 0.001f;

            WorldVoxelCoordinate voxel =
                ToVoxelCoordinate(point);

            if (WouldIntersectPlayer(voxel))
            {
                return;
            }

            worldRuntime.World.TrySetVoxel(
                voxel.X,
                voxel.Y,
                voxel.Z,
                placeBlockId
            );
        }

        private bool TryGetVoxelHit(
            out RaycastHit hit)
        {
            return Physics.Raycast(
                playerCamera.transform.position,
                playerCamera.transform.forward,
                out hit,
                interactionDistance
            );
        }

        private bool WouldIntersectPlayer(
            WorldVoxelCoordinate voxel)
        {
            if (playerController == null ||
                !playerController.enabled)
            {
                return false;
            }

            Bounds playerBounds =
                playerController.bounds;

            Bounds voxelBounds =
                new Bounds(
                    new Vector3(
                        voxel.X + 0.5f,
                        voxel.Y + 0.5f,
                        voxel.Z + 0.5f
                    ),
                    Vector3.one
                );

            return playerBounds.Intersects(
                voxelBounds
            );
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