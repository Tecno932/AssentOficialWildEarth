using System;
using Unity.Mathematics;

namespace WildEarth.Voxel
{
    /// <summary>
    /// Representación compacta de un voxel almacenado dentro de un chunk.
    ///
    /// IMPORTANTE:
    /// Este struct contiene únicamente datos.
    /// No contiene referencias a UnityEngine.Object.
    /// </summary>
    [Serializable]
    public struct Voxel : IEquatable<Voxel>
    {
        /// <summary>
        /// ID de la definición del bloque.
        ///
        /// 0 = Air
        /// 1+ = bloques registrados.
        /// </summary>
        public ushort BlockId;

        /// <summary>
        /// Estado de iluminación del voxel.
        ///
        /// Los 4 bits inferiores representan luz del bloque.
        /// Los 4 bits superiores representan luz del cielo.
        ///
        /// 0..15 por canal.
        /// </summary>
        public byte Light;

        /// <summary>
        /// Estado adicional del voxel.
        ///
        /// Su interpretación depende del sistema:
        /// - fluid level
        /// - orientación
        /// - crecimiento
        /// - estado de bloque
        /// - etc.
        /// </summary>
        public byte State;

        public Voxel(ushort blockId)
        {
            BlockId = blockId;
            Light = 0;
            State = 0;
        }

        public Voxel(
            ushort blockId,
            byte light,
            byte state)
        {
            BlockId = blockId;
            Light = light;
            State = state;
        }

        public bool IsAir => BlockId == BlockIds.Air;

        public byte BlockLight
        {
            readonly get => (byte)(Light & 0x0F);
            set => Light = (byte)((Light & 0xF0) | (value & 0x0F));
        }

        public byte SkyLight
        {
            readonly get => (byte)((Light >> 4) & 0x0F);
            set => Light = (byte)((Light & 0x0F) | ((value & 0x0F) << 4));
        }

        public bool Equals(Voxel other)
        {
            return BlockId == other.BlockId &&
                   Light == other.Light &&
                   State == other.State;
        }

        public override bool Equals(object obj)
        {
            return obj is Voxel other &&
                   Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                BlockId,
                Light,
                State
            );
        }

        public static bool operator ==(
            Voxel left,
            Voxel right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            Voxel left,
            Voxel right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// IDs reservados del sistema voxel.
    /// </summary>
    public static class BlockIds
    {
        /// <summary>
        /// Aire.
        /// Nunca necesita un BlockDefinition.asset.
        /// </summary>
        public const ushort Air = 0;
    }
}