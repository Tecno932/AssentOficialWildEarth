using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace WildEarth.Voxel
{
    [BurstCompile]
    public struct OreGenerationJob : IJob
    {
        public ChunkGenerationContext Context;
        public OreGenerationSettings Settings;

        public NativeArray<Voxel> Voxels;

        [ReadOnly]
        public NativeArray<OreRuntimeData> OreDatabase;

        [ReadOnly]
        public NativeArray<ushort> HostBlockIds;

        public void Execute()
        {
            if (!Settings.Enabled)
                return;

            if (!OreDatabase.IsCreated ||
                OreDatabase.Length == 0)
            {
                return;
            }

            for (int oreIndex = 0;
                 oreIndex < OreDatabase.Length;
                 oreIndex++)
            {
                OreRuntimeData ore =
                    OreDatabase[oreIndex];

                if (!ore.IsValid)
                    continue;

                GenerateOre(
                    oreIndex,
                    ore);
            }
        }

        private void GenerateOre(
            int oreIndex,
            OreRuntimeData ore)
        {
            int chunkSize =
                VoxelConstants.ChunkSize;

            int chunkMinX =
                Context.WorldOrigin.x;

            int chunkMinY =
                Context.WorldOrigin.y;

            int chunkMinZ =
                Context.WorldOrigin.z;

            int chunkMaxX =
                chunkMinX +
                chunkSize -
                1;

            int chunkMaxY =
                chunkMinY +
                chunkSize -
                1;

            int chunkMaxZ =
                chunkMinZ +
                chunkSize -
                1;

            int margin =
                math.max(
                    1,
                    ore.MaxVeinSize);

            if (ore.MaxY < chunkMinY - margin ||
                ore.MinY > chunkMaxY + margin)
            {
                return;
            }

            int minWorldY =
                math.max(
                    ore.MinY,
                    chunkMinY - margin);

            int maxWorldY =
                math.min(
                    ore.MaxY,
                    chunkMaxY + margin);

            int minLocalY =
                minWorldY -
                chunkMinY;

            int maxLocalY =
                maxWorldY -
                chunkMinY;

            int minX = -margin;
            int maxX = chunkSize + margin - 1;

            int minZ = -margin;
            int maxZ = chunkSize + margin - 1;

            int baseSeed =
                Context.Seed +
                Settings.SeedOffset +
                oreIndex * 7919;

            float3 seedOffset0 =
                OreNoise.CreateSeedOffset(
                    baseSeed);

            float3 seedOffset1 =
                OreNoise.CreateSeedOffset(
                    baseSeed + 1297);

            float3 seedOffset2 =
                OreNoise.CreateSeedOffset(
                    baseSeed + 2594);

            for (int localY = minLocalY;
                 localY <= maxLocalY;
                 localY++)
            {
                for (int localZ = minZ;
                     localZ <= maxZ;
                     localZ++)
                {
                    for (int localX = minX;
                         localX <= maxX;
                         localX++)
                    {
                        TryCreateVein(
                            ore,
                            localX,
                            localY,
                            localZ,
                            baseSeed,
                            seedOffset0,
                            seedOffset1,
                            seedOffset2);
                    }
                }
            }
        }

        private void TryCreateVein(
            OreRuntimeData ore,
            int localX,
            int localY,
            int localZ,
            int baseSeed,
            float3 seedOffset0,
            float3 seedOffset1,
            float3 seedOffset2)
        {
            int worldX =
                Context.WorldOrigin.x +
                localX;

            int worldY =
                Context.WorldOrigin.y +
                localY;

            int worldZ =
                Context.WorldOrigin.z +
                localZ;

            if (worldY < ore.MinY ||
                worldY > ore.MaxY)
            {
                return;
            }

            float density =
                OreNoise.Fractal01Cached(
                    new float3(
                        worldX,
                        worldY,
                        worldZ),
                    ore.Frequency,
                    Settings.Octaves,
                    Settings.Lacunarity,
                    Settings.Persistence,
                    seedOffset0,
                    seedOffset1,
                    seedOffset2);

            float threshold =
                1f - ore.Rarity;

            if (density < threshold)
                return;

            uint positionSeed =
                Hash(
                    worldX,
                    worldY,
                    worldZ,
                    baseSeed);

            int veinRange =
                ore.MaxVeinSize -
                ore.MinVeinSize +
                1;

            int veinSize =
                ore.MinVeinSize;

            if (veinRange > 1)
            {
                uint sizeSeed =
                    Hash(
                        worldX + 17,
                        worldY + 31,
                        worldZ + 47,
                        (int)positionSeed);

                veinSize +=
                    (int)(
                        HashTo01(sizeSeed) *
                        veinRange);

                veinSize =
                    math.min(
                        veinSize,
                        ore.MaxVeinSize);
            }

            GenerateVein(
                ore,
                worldX,
                worldY,
                worldZ,
                veinSize,
                positionSeed);
        }

        private void GenerateVein(
            OreRuntimeData ore,
            int startX,
            int startY,
            int startZ,
            int veinSize,
            uint seed)
        {
            int3 current =
                new int3(
                    startX,
                    startY,
                    startZ);

            for (int i = 0;
                 i < veinSize;
                 i++)
            {
                TryPlaceOre(
                    ore,
                    current.x,
                    current.y,
                    current.z);

                uint stepSeed =
                    Hash(
                        current.x,
                        current.y,
                        current.z,
                        (int)seed +
                        i * 1543);

                int direction =
                    (int)(
                        HashTo01(stepSeed) *
                        6f);

                switch (direction)
                {
                    case 0:
                        current.x++;
                        break;

                    case 1:
                        current.x--;
                        break;

                    case 2:
                        current.y++;
                        break;

                    case 3:
                        current.y--;
                        break;

                    case 4:
                        current.z++;
                        break;

                    default:
                        current.z--;
                        break;
                }

                if (current.y < ore.MinY ||
                    current.y > ore.MaxY)
                {
                    break;
                }
            }
        }

        private void TryPlaceOre(
            OreRuntimeData ore,
            int worldX,
            int worldY,
            int worldZ)
        {
            if (worldY < ore.MinY ||
                worldY > ore.MaxY)
            {
                return;
            }

            int localX =
                worldX -
                Context.WorldOrigin.x;

            int localY =
                worldY -
                Context.WorldOrigin.y;

            int localZ =
                worldZ -
                Context.WorldOrigin.z;

            if (!VoxelIndex.IsValidLocalCoordinate(
                    localX,
                    localY,
                    localZ))
            {
                return;
            }

            int index =
                VoxelIndex.ToIndex(
                    localX,
                    localY,
                    localZ);

            Voxel voxel =
                Voxels[index];

            if (!IsHostBlock(
                    ore,
                    voxel.BlockId))
            {
                return;
            }

            Voxels[index] =
                new Voxel(
                    ore.BlockId,
                    voxel.Light,
                    voxel.State);
        }

        private bool IsHostBlock(
            OreRuntimeData ore,
            ushort blockId)
        {
            int start =
                ore.HostBlockStart;

            int end =
                start +
                ore.HostBlockCount;

            for (int i = start;
                 i < end;
                 i++)
            {
                if (HostBlockIds[i] == blockId)
                    return true;
            }

            return false;
        }

        private static uint Hash(
            int x,
            int y,
            int z,
            int seed)
        {
            uint h =
                (uint)seed;

            h ^= (uint)x * 374761393u;
            h ^= (uint)y * 668265263u;
            h ^= (uint)z * 2147483647u;
            h ^= h >> 13;
            h *= 1274126177u;
            h ^= h >> 16;

            return h;
        }

        private static float HashTo01(
            uint value)
        {
            return
                (value & 0x00FFFFFFu) /
                16777215f;
        }
    }
}