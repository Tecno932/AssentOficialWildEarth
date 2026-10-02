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

        [Header("Tool Breaking")]
        [SerializeField] private float defaultToolSpeedMultiplier = 1f;

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
                    breakingProgress /
                    breakingDuration
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
            if (Cursor.lockState !=
                CursorLockMode.Locked)
            {
                ResetBreaking();
                return;
            }

            if (breakCooldownTimer > 0f)
            {
                breakCooldownTimer -=
                    Time.deltaTime;
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

            if (Mouse.current.rightButton
                .wasPressedThisFrame)
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

            if (!worldRuntime.World.Blocks
                .TryGetDefinition(
                    currentVoxel.BlockId,
                    out BlockDefinition definition))
            {
                ResetBreaking();
                return;
            }

            if (mode ==
                VoxelPlayerMode.Creative)
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

            if (!TryGetBreakingSpeed(
                    definition,
                    out float speedMultiplier))
            {
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
                    definition,
                    speedMultiplier
                );
            }

            breakingProgress +=
                Time.deltaTime;

            if (breakingProgress >=
                breakingDuration)
            {
                BreakVoxel(voxel);

                ResetBreaking();
            }
        }

        private bool TryGetBreakingSpeed(
            BlockDefinition definition,
            out float speedMultiplier)
        {
            speedMultiplier =
                Mathf.Max(
                    0.01f,
                    defaultToolSpeedMultiplier
                );

            if (definition.RequiredTool ==
                ToolType.None)
            {
                if (hotbar.TryGetSelectedRuntimeItem(
                        out ItemRuntimeData selectedItem) &&
                    selectedItem.IsTool)
                {
                    speedMultiplier =
                        CalculateToolSpeed(
                            selectedItem
                        );
                }

                return true;
            }

            if (!hotbar.TryGetSelectedRuntimeItem(
                    out ItemRuntimeData item))
            {
                return false;
            }

            if (!item.IsTool)
            {
                return false;
            }

            if (item.ToolType !=
                definition.RequiredTool)
            {
                return false;
            }

            if (item.ToolLevel <
                definition.RequiredToolLevel)
            {
                return false;
            }

            speedMultiplier =
                CalculateToolSpeed(
                    item
                );

            return true;
        }

        private static float CalculateToolSpeed(
            ItemRuntimeData item)
        {
            float sharpnessMultiplier =
                Mathf.Max(
                    0.01f,
                    item.Sharpness / 50f
                );

            return Mathf.Max(
                0.01f,
                item.ToolSpeed *
                sharpnessMultiplier
            );
        }

        private void StartBreaking(
            WorldVoxelCoordinate voxel,
            BlockDefinition definition,
            float speedMultiplier)
        {
            breakingVoxel = voxel;
            breakingProgress = 0f;

            float baseDuration =
                Mathf.Max(
                    minimumBreakTime,
                    definition.Hardness
                );

            breakingDuration =
                Mathf.Max(
                    minimumBreakTime,
                    baseDuration /
                    speedMultiplier
                );

            isBreaking = true;

            Debug.Log(
                $"[VoxelBreakStart] " +
                $"ID={definition.Id} | " +
                $"Name={definition.BlockName} | " +
                $"Hardness={definition.Hardness} | " +
                $"SpeedMultiplier={speedMultiplier} | " +
                $"Duration={breakingDuration}s | " +
                $"RequiredTool={definition.RequiredTool} | " +
                $"RequiredToolLevel={definition.RequiredToolLevel}"
            );
        }

        private void BreakVoxel(
            WorldVoxelCoordinate voxel)
        {
            if (!worldRuntime.World.TryGetVoxel(
                    voxel.X,
                    voxel.Y,
                    voxel.Z,
                    out Voxel currentVoxel))
            {
                return;
            }

            ushort blockId =
                currentVoxel.BlockId;

            if (blockId == BlockIds.Air)
            {
                return;
            }

            if (!hotbar.TryAddItemForBlock(
                blockId,
                1))
            {
                Debug.LogWarning(
                    $"[VoxelBreak] No se pudo recoger " +
                    $"el bloque {blockId}. " +
                    $"El bloque no será destruido."
                );

                return;
            }

            bool removed =
                worldRuntime.World.TrySetVoxel(
                    voxel.X,
                    voxel.Y,
                    voxel.Z,
                    BlockIds.Air
                );

            if (!removed)
            {
                Debug.LogWarning(
                    $"[VoxelBreak] El bloque {blockId} " +
                    $"fue agregado al inventario pero " +
                    $"no pudo eliminarse del mundo."
                );

                return;
            }

            if (mode != VoxelPlayerMode.Creative &&
                hotbar.TryGetSelectedRuntimeItem(
                    out ItemRuntimeData selectedItem) &&
                selectedItem.IsTool &&
                selectedItem.HasDurability)
            {
                hotbar.TryDamageSelectedItem(1);
            }
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
            if (!hotbar.TryGetSelectedBlockId(
                    out ushort blockId))
            {
                return;
            }

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

            if (!worldRuntime.World.TrySetVoxel(
                    voxel.X,
                    voxel.Y,
                    voxel.Z,
                    blockId))
            {
                return;
            }

            if (!hotbar.TryConsumeSelectedItem())
            {
                Debug.LogError(
                    "[VoxelPlace] El bloque fue colocado " +
                    "pero no se pudo consumir el ItemStack."
                );
            }
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