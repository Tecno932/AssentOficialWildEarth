using System;
using Unity.Mathematics;

namespace WildEarth.Voxel
{
    /// <summary>
    /// Datos compactos de un bloque utilizados durante runtime.
    ///
    /// No contiene referencias administradas ni objetos de Unity.
    /// Esto permite utilizar estos datos con Jobs y Burst.
    /// </summary>
    [Serializable]
    public struct BlockRuntimeData
    {
        public ushort Id;

        public BlockMeshType MeshType;

        public BlockFlags Flags;

        public float Hardness;

        public byte LightEmission;

        public ToolType RequiredTool;

        public byte RequiredToolLevel;

        public int TopTexture;

        public int BottomTexture;

        public int SideTexture;

        public bool IsSolid =>
            (Flags & BlockFlags.Solid) != 0;

        public bool IsTransparent =>
            (Flags & BlockFlags.Transparent) != 0;

        public bool IsFluid =>
            (Flags & BlockFlags.Fluid) != 0;

        public bool EmitsLight =>
            (Flags & BlockFlags.EmitsLight) != 0;

        public bool OccludesFaces =>
            (Flags & BlockFlags.OccludesFaces) != 0;

        public bool IsCollidable =>
            (Flags & BlockFlags.Collidable) != 0;

        public bool IsReplaceable =>
            (Flags & BlockFlags.Replaceable) != 0;

        public bool IsCutout =>
            (Flags & BlockFlags.Cutout) != 0;

        public bool IsCaveCarvable =>
            (Flags & BlockFlags.CaveCarvable) != 0;
    }
}