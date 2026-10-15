using System;
using UnityEngine;

namespace WildEarth.Voxel
{
    [Serializable]
    public struct AlloyComponent
    {
        [SerializeField]
        private ushort materialId;

        [SerializeField]
        [Min(0.0001f)]
        private float amount;

        public ushort MaterialId =>
            materialId;

        public float Amount =>
            amount;

        public bool IsValid =>
            materialId != 0 &&
            amount > 0f;
    }
}