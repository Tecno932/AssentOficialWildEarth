using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace WildEarth.Voxel
{
    [BurstCompile]
    public struct CaveGenerationJob : IJob
    {
        public ChunkGenerationContext Context;
        public CaveGenerationSettings Settings;

        public NativeArray<Voxel> Voxels;

        [ReadOnly]
        public NativeArray<BlockRuntimeData> BlockDatabase;
        [ReadOnly]
        public NativeArray<BiomeId> Biomes;

        public void Execute()
        {
            if (!Settings.Enabled)
                return;

            int chunkSize =
                VoxelConstants.ChunkSize;

            int baseSeed =
                Context.Seed +
                Settings.SeedOffset;

            float3 seedOffset0 =
                CaveNoise.CreateSeedOffset(
                    baseSeed);

            float3 seedOffset1 =
                CaveNoise.CreateSeedOffset(
                    baseSeed + 977);

            float3 seedOffset2 =
                CaveNoise.CreateSeedOffset(
                    baseSeed + 1954);

            for (int y = 0;
                 y < chunkSize;
                 y++)
            {
                int worldY =
                    Context.WorldOrigin.y + y;

                if (worldY < Settings.MinimumY ||
                    worldY > Settings.MaximumY)
                {
                    continue;
                }

                for (int z = 0;
                     z < chunkSize;
                     z++)
                {
                    for (int x = 0;
                         x < chunkSize;
                         x++)
                    {
                        int index =
                            VoxelIndex.ToIndex(
                                x,
                                y,
                                z);

                        int columnIndex =
                            x + z * chunkSize;

                        BiomeId biomeId =
                            Biomes[columnIndex];

                        Voxel voxel =
                            Voxels[index];

                        if (voxel.BlockId ==
                            BlockIds.Air)
                        {
                            continue;
                        }

                        if (!CanCarve(
                                voxel.BlockId))
                        {
                            continue;
                        }

                        int worldX =
                            Context.WorldOrigin.x + x;

                        int worldZ =
                            Context.WorldOrigin.z + z;

                        float3 position =
                            new float3(
                                worldX,
                                worldY,
                                worldZ);

                        float density =
                            CaveNoise.Fractal01Cached(
                                position,
                                Settings.Frequency,
                                Settings.Octaves,
                                Settings.Lacunarity,
                                Settings.Persistence,
                                seedOffset0,
                                seedOffset1,
                                seedOffset2);

                        float biomeMultiplier =
                            GetBiomeCaveMultiplier(
                                biomeId
                            );

                        float depthMultiplier =
                            GetDepthMultiplier(
                                worldY
                            );

                        float effectiveThreshold =
                            Settings.Threshold +
                            (
                                1f -
                                biomeMultiplier *
                                depthMultiplier
                            ) *
                            0.20f;

                        if (density >=
                            effectiveThreshold)
                        {
                            Voxels[index] =
                                new Voxel(
                                    BlockIds.Air);
                        }
                    }
                }
            }
        }

        private float GetBiomeCaveMultiplier(
            BiomeId biomeId)
        {
            switch (biomeId)
            {
                case BiomeId.Mountains:
                    return 0.75f;

                case BiomeId.Forest:
                    return 0.70f;

                case BiomeId.Tundra:
                    return 0.60f;

                case BiomeId.Plains:
                    return 0.55f;

                case BiomeId.Desert:
                    return 0.35f;

                default:
                    return 0.75f;
            }
        }

        private float GetDepthMultiplier(
            int worldY)
        {
            float minimum =
                Settings.MinimumY;

            float maximum =
                Settings.MaximumY;

            float normalized =
                math.saturate(
                    (worldY - minimum) /
                    math.max(
                        1f,
                        maximum - minimum
                    )
                );

            /*
            * Cerca de la superficie:
            * pocas cuevas.
            *
            * En profundidad:
            * máxima actividad.
            */
            return math.lerp(
                0.35f,
                1.15f,
                1f - normalized
            );
        }

        private bool CanCarve(
            ushort blockId)
        {
            int index =
                blockId;

            if (index < 0 ||
                index >= BlockDatabase.Length)
            {
                return false;
            }

            BlockRuntimeData block =
                BlockDatabase[index];

            return block.Id == blockId &&
                   block.IsCaveCarvable;
        }
    }
}