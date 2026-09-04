using System;
using Unity.Collections;

namespace WildEarth.Voxel
{
    public sealed class ChunkBiomeData : IDisposable
    {
        public const int Size =
            VoxelConstants.ChunkSize *
            VoxelConstants.ChunkSize;

        public NativeArray<BiomeId> Biomes { get; private set; }

        public bool IsCreated =>
            Biomes.IsCreated;

        public ChunkBiomeData(
            Allocator allocator)
        {
            Biomes =
                new NativeArray<BiomeId>(
                    Size,
                    allocator,
                    NativeArrayOptions.ClearMemory
                );
        }

        public BiomeId Get(
            int x,
            int z)
        {
            if (x < 0 ||
                x >= VoxelConstants.ChunkSize ||
                z < 0 ||
                z >= VoxelConstants.ChunkSize)
            {
                return BiomeId.Plains;
            }

            int index =
                x +
                z * VoxelConstants.ChunkSize;

            return Biomes[index];
        }

        public void Set(
            int x,
            int z,
            BiomeId biome)
        {
            if (x < 0 ||
                x >= VoxelConstants.ChunkSize ||
                z < 0 ||
                z >= VoxelConstants.ChunkSize)
            {
                return;
            }

            int index =
                x +
                z * VoxelConstants.ChunkSize;

            SetAtIndex(
                Biomes,
                index,
                biome
            );
        }

        private static void SetAtIndex(
            NativeArray<BiomeId> array,
            int index,
            BiomeId biome)
        {
            array[index] = biome;
        }

        public void Dispose()
        {
            if (!Biomes.IsCreated)
                return;

            Biomes.Dispose();
            Biomes = default;
        }
    }
}