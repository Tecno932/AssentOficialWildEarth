using UnityEngine;
using UnityEngine.InputSystem;

namespace WildEarth.Voxel
{
    public sealed class VoxelPlayerInteraction : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private VoxelWorldRuntime worldRuntime;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private CharacterController playerController;
        [SerializeField] private VoxelPlayerHotbar hotbar;
        [SerializeField] private float creativeBreakCooldown = 0.15f;

        [Header("Interaction")]
        [SerializeField] private float interactionDistance = 6f;

        [Header("Breaking")]
        [SerializeField] private float minimumBreakTime = 0.05f;

        private VoxelPlayerMode mode =
            VoxelPlayerMode.Survival;

        private bool isBreaking;
        private WorldVoxelCoordinate breakingVoxel;
        private float breakingProgress;
        private float breakingDuration;
        private float breakCooldownTimer;

        public bool IsBreaking => isBreaking;

        public float BreakingProgress
        {
            get
            {
                if (!isBreaking ||
                    breakingDuration <= 0f)
                {
                    return 0f;
                }

                return Mathf.Clamp01(
                    breakingProgress / breakingDuration
                );
            }
        }

        public void SetMode(
            VoxelPlayerMode newMode)
        {
            mode = newMode;

            ResetBreaking();
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

            if (hotbar == null)
            {
                hotbar =
                    GetComponent<VoxelPlayerHotbar>();
            }

            if (hotbar == null)
            {
                throw new System.InvalidOperationException(
                    "VoxelPlayerInteraction requiere " +
                    "VoxelPlayerHotbar."
                );
            }
        }

        public void Tick()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                ResetBreaking();
                return;
            }

            if (breakCooldownTimer > 0f)
            {
                breakCooldownTimer -= Time.deltaTime;
            }

            if (Mouse.current == null)
            {
                ResetBreaking();
                return;
            }

            if (Mouse.current.leftButton.isPressed)
            {
                UpdateBreaking();
            }
            else
            {
                ResetBreaking();
            }

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                TryPlaceVoxel();
            }
        }

        private void UpdateBreaking()
        {
            if (!TryGetVoxelHit(
                out RaycastHit hit))
            {
                ResetBreaking();
                return;
            }

            Vector3 point =
                hit.point -
                hit.normal * 0.001f;

            WorldVoxelCoordinate voxel =
                ToVoxelCoordinate(point);

            if (!worldRuntime.World.TryGetVoxel(
                voxel.X,
                voxel.Y,
                voxel.Z,
                out Voxel currentVoxel))
            {
                ResetBreaking();
                return;
            }

            if (currentVoxel.BlockId ==
                BlockIds.Air)
            {
                ResetBreaking();
                return;
            }

            if (!worldRuntime.World.Blocks.TryGetDefinition(
                currentVoxel.BlockId,
                out BlockDefinition definition))
            {
                ResetBreaking();
                return;
            }

            if (mode == VoxelPlayerMode.Creative)
            {
                if (breakCooldownTimer > 0f)
                {
                    return;
                }

                BreakVoxel(voxel);

                breakCooldownTimer =
                    creativeBreakCooldown;

                ResetBreaking();
                return;
            }

            if (!isBreaking ||
                !IsSameVoxel(
                    breakingVoxel,
                    voxel))
            {
                StartBreaking(
                    voxel,
                    definition
                );
            }

            breakingProgress +=
                Time.deltaTime;

            if (breakingProgress >=
                breakingDuration)
            {
                BreakVoxel(
                    voxel
                );

                ResetBreaking();
            }
        }

        private void StartBreaking(
            WorldVoxelCoordinate voxel,
            BlockDefinition definition)
        {
            breakingVoxel = voxel;
            breakingProgress = 0f;

            breakingDuration =
                Mathf.Max(
                    minimumBreakTime,
                    definition.Hardness
                );

            isBreaking = true;
        }

        private void BreakVoxel(
            WorldVoxelCoordinate voxel)
        {
            worldRuntime.World.TrySetVoxel(
                voxel.X,
                voxel.Y,
                voxel.Z,
                BlockIds.Air
            );
        }

        private void ResetBreaking()
        {
            isBreaking = false;
            breakingProgress = 0f;
            breakingDuration = 0f;
        }

        private static bool IsSameVoxel(
            WorldVoxelCoordinate a,
            WorldVoxelCoordinate b)
        {
            return a.X == b.X &&
                   a.Y == b.Y &&
                   a.Z == b.Z;
        }

        private void TryPlaceVoxel()
        {
            if (!hotbar.TryGetSelectedRuntimeItem(
                    out ItemRuntimeData item))
            {

                return;
            }

            if (!item.RepresentsBlock)
            {

                return;
            }

            ushort blockId =
                item.BlockId;

            if (blockId == BlockIds.Air)
            {
                return;
            }

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

            bool placed =
                worldRuntime.World.TrySetVoxel(
                    voxel.X,
                    voxel.Y,
                    voxel.Z,
                    blockId
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
                Mathf.FloorToInt(worldPosition.x),
                Mathf.FloorToInt(worldPosition.y),
                Mathf.FloorToInt(worldPosition.z)
            );
        }
    }
}