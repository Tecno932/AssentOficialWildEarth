using System;
using Unity.Jobs;

namespace WildEarth.Voxel
{
    public sealed class ChunkGenerationPipeline
    {
        private readonly ChunkGenerationSettings settings;
        private readonly BiomeRuntimeDatabase biomeDatabase;
        private readonly BlockRuntimeDatabase blockDatabase;
        private readonly OreRuntimeDatabase oreDatabase;

        public ChunkGenerationPipeline(
            ChunkGenerationSettings settings,
            BiomeRuntimeDatabase biomeDatabase,
            BlockRuntimeDatabase blockDatabase,
            OreRuntimeDatabase oreDatabase)
        {
            this.settings = settings;

            this.biomeDatabase =
                biomeDatabase ??
                throw new ArgumentNullException(
                    nameof(biomeDatabase)
                );

            this.blockDatabase =
                blockDatabase ??
                throw new ArgumentNullException(
                    nameof(blockDatabase)
                );

            this.oreDatabase =
                oreDatabase ??
                throw new ArgumentNullException(
                    nameof(oreDatabase)
                );
        }

        public JobHandle Schedule(
            Chunk chunk,
            JobHandle dependency = default)
        {
            if (chunk == null)
            {
                throw new ArgumentNullException(
                    nameof(chunk)
                );
            }

            if (!chunk.Data.IsCreated)
            {
                throw new InvalidOperationException(
                    $"El chunk {chunk.Coordinate} no tiene datos válidos."
                );
            }

            if (!chunk.BiomeData.IsCreated)
            {
                throw new InvalidOperationException(
                    $"El chunk {chunk.Coordinate} no tiene datos de bioma válidos."
                );
            }

            ChunkGenerationContext context =
                new ChunkGenerationContext(
                    settings.Seed,
                    chunk.Coordinate.ToInt3()
                );

            JobHandle biomeHandle =
                ScheduleBiome(
                    chunk,
                    context,
                    dependency
                );

            JobHandle terrainHandle =
                ScheduleTerrain(
                    chunk,
                    context,
                    biomeHandle
                );

            JobHandle caveHandle =
                ScheduleCaves(
                    chunk,
                    context,
                    terrainHandle
                );

            JobHandle oreHandle =
                ScheduleOres(
                    chunk,
                    context,
                    caveHandle
                );

            return oreHandle;
        }

        private JobHandle ScheduleBiome(
            Chunk chunk,
            ChunkGenerationContext context,
            JobHandle dependency)
        {
            BiomeGenerationJob job =
                new BiomeGenerationJob
                {
                    Context = context,
                    Settings = settings.Biome,

                    BiomeDatabase =
                        biomeDatabase.AsNativeArray(),

                    Output =
                        chunk.BiomeData.Biomes
                };

            return job.Schedule(
                dependency
            );
        }

        private JobHandle ScheduleCaves(
            Chunk chunk,
            ChunkGenerationContext context,
            JobHandle dependency)
        {
            CaveGenerationJob job =
                new CaveGenerationJob
                {
                    Context = context,
                    Settings = settings.Caves,

                    Voxels =
                        chunk.Data.Voxels,

                    BlockDatabase =
                        blockDatabase.AsNativeArray()
                };

            return job.Schedule(
                dependency
            );
        }

        private JobHandle ScheduleOres(
            Chunk chunk,
            ChunkGenerationContext context,
            JobHandle dependency)
        {
            OreGenerationJob job =
                new OreGenerationJob
                {
                    Context = context,
                    Settings = settings.Ores,

                    Voxels =
                        chunk.Data.Voxels,

                    OreDatabase =
                        oreDatabase.AsNativeArray(),

                    HostBlockIds =
                        oreDatabase.AsHostBlockArray()
                };

            return job.Schedule(
                dependency
            );
        }

        private JobHandle ScheduleTerrain(
            Chunk chunk,
            ChunkGenerationContext context,
            JobHandle dependency)
        {
            TerrainGenerationJob job =
                new TerrainGenerationJob
                {
                    Context = context,
                    Settings = settings.Terrain,

                    Voxels =
                        chunk.Data.Voxels,

                    Biomes =
                        chunk.BiomeData.Biomes,

                    BiomeDatabase =
                        biomeDatabase.AsNativeArray()
                };

            return job.Schedule(
                dependency
            );
        }
    }
}