using System;
using UnityEngine;

namespace WildEarth.Voxel
{
    public enum BlockMeshType : byte
    {
        Cube = 0,
        Cross = 1,
        Slab = 2,
        Stairs = 3,
        Custom = 4,
        Fluid = 5
    }

    [Flags]
    public enum BlockFlags : ushort
    {
        None = 0,

        Solid = 1 << 0,
        Transparent = 1 << 1,
        Cutout = 1 << 2,
        EmitsLight = 1 << 3,
        Replaceable = 1 << 4,
        Fluid = 1 << 5,
        Collidable = 1 << 6,
        Flammable = 1 << 7,
        OccludesFaces = 1 << 8,

        CaveCarvable = 1 << 9
    }

    public enum ToolType : byte
    {
        None = 0,
        Hand = 1,
        Pickaxe = 2,
        Axe = 3,
        Shovel = 4,
        Hoe = 5,
        Sword = 6
    }

    [CreateAssetMenu(
        fileName = "BlockDefinition",
        menuName = "WildEarth/Voxel/Block Definition"
    )]
    public sealed class BlockDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        private ushort id;

        [SerializeField]
        private string blockName;

        [Header("Rendering")]
        [SerializeField]
        private BlockMeshType meshType = BlockMeshType.Cube;

        [SerializeField]
        private BlockFlags flags = BlockFlags.Solid |
                                    BlockFlags.Collidable |
                                    BlockFlags.OccludesFaces;

        [Header("Physical Properties")]
        [SerializeField]
        [Min(0f)]
        private float hardness = 1f;

        [SerializeField]
        [Range(0, 15)]
        private byte lightEmission;

        [Header("Texture Atlas")]
        [SerializeField]
        private int topTexture;

        [SerializeField]
        private int bottomTexture;

        [SerializeField]
        private int sideTexture;

        [Header("Breaking")]
        [SerializeField]
        private ToolType requiredTool = ToolType.None;

        [SerializeField]
        private byte requiredToolLevel;

        [Header("Rendering Material")]
        [SerializeField]
        private Material material;

        public ushort Id => id;

        public string BlockName => blockName;

        public BlockMeshType MeshType => meshType;

        public BlockFlags Flags => flags;

        public float Hardness => hardness;

        public byte LightEmission => lightEmission;

        public int TopTexture => topTexture;

        public int BottomTexture => bottomTexture;

        public int SideTexture => sideTexture;

        public ToolType RequiredTool => requiredTool;

        public byte RequiredToolLevel => requiredToolLevel;

        public Material Material => material;

        public bool IsSolid =>
            (flags & BlockFlags.Solid) != 0;

        public bool IsTransparent =>
            (flags & BlockFlags.Transparent) != 0;

        public bool IsFluid =>
            (flags & BlockFlags.Fluid) != 0;

        public bool EmitsLight =>
            (flags & BlockFlags.EmitsLight) != 0;

        public bool OccludesFaces =>
            (flags & BlockFlags.OccludesFaces) != 0;

        public BlockRuntimeData ToRuntimeData()
        {
            return new BlockRuntimeData
            {
                Id = id,
                MeshType = meshType,
                Flags = flags,
                Hardness = hardness,
                LightEmission = lightEmission,
                RequiredTool = requiredTool,
                RequiredToolLevel = requiredToolLevel,
                TopTexture = topTexture,
                BottomTexture = bottomTexture,
                SideTexture = sideTexture
            };
        }

#if UNITY_EDITOR

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(blockName))
            {
                blockName = name;
            }

            if (lightEmission > 0)
            {
                flags |= BlockFlags.EmitsLight;
            }
            else
            {
                flags &= ~BlockFlags.EmitsLight;
            }

            if (IsSolid)
            {
                flags |= BlockFlags.Collidable;
            }
        }

#endif
    }
}