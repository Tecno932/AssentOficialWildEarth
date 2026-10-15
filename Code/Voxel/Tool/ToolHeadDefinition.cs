using System;
using UnityEngine;

namespace WildEarth.Voxel
{
    [Serializable]
    public struct ToolHeadDefinition
    {
        [SerializeField]
        private ushort materialId;

        [SerializeField]
        private ushort alloyId;

        [SerializeField]
        private ToolQuality quality;

        [Header("Physical Properties")]
        [SerializeField, Min(0f)]
        private float volumeCm3;

        [SerializeField, Range(0f, 1f)]
        private float speedReduction;

        public ushort MaterialId => materialId;
        public ushort AlloyId => alloyId;
        public ToolQuality Quality => quality;

        public float VolumeCm3 => volumeCm3;
        public float SpeedReduction => speedReduction;

        public bool UsesMaterial => materialId != 0;
        public bool UsesAlloy => alloyId != 0;

        public bool IsValid => UsesMaterial ^ UsesAlloy;

        public ToolPartDefinition ToToolPart()
        {
            return ToolPartDefinition.Create(
                ToolPartType.Head,
                materialId,
                alloyId,
                quality
            );
        }
    }
}