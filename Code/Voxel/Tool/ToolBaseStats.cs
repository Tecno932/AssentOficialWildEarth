
using System;
using UnityEngine;

namespace WildEarth.Voxel
{
    [Serializable]
    public struct ToolBaseStats
    {
        [Header("Base Performance")]
        [Min(0f)] public float Damage;
        [Min(0f)] public float Speed;
        [Min(0f)] public float Efficiency;

        [Header("Base Durability")]
        [Min(0f)] public float HeadDurability;
        [Min(0f)] public float HandleDurability;

        public static ToolBaseStats Default => new ToolBaseStats
        {
            Damage = 10f,
            Speed = 1f,
            Efficiency = 1f,
            HeadDurability = 100f,
            HandleDurability = 60f
        };
    }
}