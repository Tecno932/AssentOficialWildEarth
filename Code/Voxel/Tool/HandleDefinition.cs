using System;
using UnityEngine;

namespace WildEarth.Voxel
{
    [Serializable]
    public struct HandleDefinition
    {
        [SerializeField]
        private ushort materialId;

        [SerializeField]
        private ToolQuality quality;

        [Header("Physical Properties")]
        [SerializeField, Min(0f)]
        private float volumeCm3;

        [SerializeField, Range(0f, 1f)]
        private float speedReduction;

        public ushort MaterialId => materialId;
        public ToolQuality Quality => quality;

        public float VolumeCm3 => volumeCm3;
        public float SpeedReduction => speedReduction;

        public bool IsValid => materialId != 0;

        public ToolPartDefinition ToToolPart()
        {
            return ToolPartDefinition.Create(
                ToolPartType.Handle,
                materialId,
                0,
                quality
            );
        }
    }
}