using System;
using UnityEngine;

namespace WildEarth.Voxel
{
    [CreateAssetMenu(
        fileName = "OreDefinition",
        menuName = "WildEarth/Voxel/Ore Definition"
    )]
    public sealed class OreDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        private ushort blockId;

        [SerializeField]
        private string oreName;

        [Header("Generation")]
        [SerializeField, Min(1)]
        private int minY = 5;

        [SerializeField, Min(1)]
        private int maxY = 100;

        [SerializeField, Range(0f, 1f)]
        private float rarity = 0.25f;

        [SerializeField, Min(1)]
        private int minVeinSize = 3;

        [SerializeField, Min(1)]
        private int maxVeinSize = 8;

        [SerializeField, Min(0.0001f)]
        private float frequency = 0.08f;

        [Header("Host Rock")]
        [SerializeField]
        private ushort[] hostBlockIds =
            Array.Empty<ushort>();

        public ushort BlockId => blockId;

        public string OreName => oreName;

        public int MinY => minY;

        public int MaxY => maxY;

        public float Rarity => rarity;

        public int MinVeinSize => minVeinSize;

        public int MaxVeinSize => maxVeinSize;

        public float Frequency => frequency;

        public ushort[] HostBlockIds =>
            hostBlockIds;

        public OreRuntimeData ToRuntimeData()
        {
            return new OreRuntimeData
            {
                BlockId = blockId,
                MinY = minY,
                MaxY = maxY,
                Rarity = rarity,
                MinVeinSize = minVeinSize,
                MaxVeinSize = maxVeinSize,
                Frequency = frequency
            };
        }

#if UNITY_EDITOR

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(oreName))
            {
                oreName = name;
            }

            if (maxY < minY)
            {
                maxY = minY;
            }

            if (maxVeinSize < minVeinSize)
            {
                maxVeinSize = minVeinSize;
            }

            rarity =
                Mathf.Clamp01(rarity);

            if (frequency <= 0f)
            {
                frequency = 0.0001f;
            }
        }

#endif
    }
}