using System;
using UnityEngine;

namespace WildEarth.Voxel
{
    public enum ToolPartType : byte
    {
        Head = 0,
        Handle = 1
    }

    [Serializable]
    public struct ToolPartDefinition
    {
        [SerializeField]
        private ToolPartType partType;

        [SerializeField]
        private ushort materialId;

        [SerializeField]
        private ushort alloyId;

        [SerializeField]
        private ToolQuality quality;

        public ToolPartType PartType =>
            partType;

        public ushort MaterialId =>
            materialId;

        public ushort AlloyId =>
            alloyId;

        public ToolQuality Quality =>
            quality;

        public bool UsesMaterial =>
            materialId != 0;

        public bool UsesAlloy =>
            alloyId != 0;

        public bool IsValid =>
            UsesMaterial ^ UsesAlloy;

        public MaterialStats ApplyQuality(
            MaterialStats stats)
        {
            return ToolQualityUtility.Apply(
                stats,
                quality
            );
        }

        public static ToolPartDefinition Create(
            ToolPartType partType,
            ushort materialId,
            ushort alloyId,
            ToolQuality quality)
        {
            return new ToolPartDefinition
            {
                partType = partType,
                materialId = materialId,
                alloyId = alloyId,
                quality = quality
            };
        }
    }
}