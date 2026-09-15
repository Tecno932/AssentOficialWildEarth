using System;
using Unity.Mathematics;

namespace WildEarth.Voxel
{
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

        public AtlasTileCoordinate TopTexture;

        public AtlasTileCoordinate BottomTexture;

        public AtlasTileCoordinate SideTexture;

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