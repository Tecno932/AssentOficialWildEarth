using UnityEngine;

namespace WildEarth.Voxel
{
    [CreateAssetMenu(
        fileName = "FluidDefinition",
        menuName = "WildEarth/Voxel/Fluid Definition"
    )]
    public sealed class FluidDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        private FluidType type = FluidType.None;

        [SerializeField]
        private string fluidName;

        [Header("Voxel")]
        [SerializeField]
        private ushort blockId;

        [Header("Simulation")]
        [SerializeField, Range(1, 15)]
        private byte maxLevel = FluidState.MaxLevel;

        [SerializeField, Range(0, 15)]
        private byte horizontalFlowDecay = 1;

        [SerializeField, Range(0, 15)]
        private byte verticalFlowDecay = 0;

        [Header("Properties")]
        [SerializeField]
        private bool isLava;

        public FluidType Type =>
            type;

        public string FluidName =>
            fluidName;

        public ushort BlockId =>
            blockId;

        public byte MaxLevel =>
            maxLevel;

        public byte HorizontalFlowDecay =>
            horizontalFlowDecay;

        public byte VerticalFlowDecay =>
            verticalFlowDecay;

        public bool IsLava =>
            isLava;

        public FluidRuntimeData ToRuntimeData()
        {
            return new FluidRuntimeData
            {
                Type = type,
                BlockId = blockId,
                MaxLevel = maxLevel,
                HorizontalFlowDecay =
                    horizontalFlowDecay,
                VerticalFlowDecay =
                    verticalFlowDecay,
                IsLava = isLava
            };
        }

#if UNITY_EDITOR

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(fluidName))
                fluidName = name;

            if (type == FluidType.None)
            {
                blockId = BlockIds.Air;
            }

            if (maxLevel == 0)
                maxLevel = FluidState.MaxLevel;

            if (isLava)
            {
                type = FluidType.Lava;
            }
        }

#endif
    }
}