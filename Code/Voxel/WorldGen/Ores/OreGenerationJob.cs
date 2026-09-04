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

        public NativeArray<OreRuntimeData> OreDatabase;
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
                    ore
                );
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

            /*
             * Las vetas pueden desplazarse en las seis
             * direcciones, por lo que necesitamos margen
             * en los tres ejes.
             */
            int margin =
                math.max(
                    1,
                    ore.MaxVeinSize
                );

            /*
             * Si el rango vertical del mineral no intersecta
             * el área extendida del chunk, no hay nada que generar.
             */
            if (ore.MaxY < chunkMinY - margin ||
                ore.MinY > chunkMaxY + margin)
            {
                return;
            }

            int minWorldY =
                math.max(
                    ore.MinY,
                    chunkMinY - margin
                );

            int maxWorldY =
                math.min(
                    ore.MaxY,
                    chunkMaxY + margin
                );

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
                            oreIndex,
                            ore,
                            localX,
                            localY,
                            localZ
                        );
                    }
                }
            }
        }

        private void TryCreateVein(
            int oreIndex,
            OreRuntimeData ore,
            int localX,
            int localY,
            int localZ)
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
                OreNoise.Fractal01(
                    new float3(
                        worldX,
                        worldY,
                        worldZ
                    ),
                    ore.Frequency,
                    Settings.Octaves,
                    Settings.Lacunarity,
                    Settings.Persistence,
                    Context.Seed +
                    Settings.SeedOffset +
                    oreIndex * 7919
                );

            float threshold =
                1f - ore.Rarity;

            if (density < threshold)
                return;

            uint positionSeed =
                Hash(
                    worldX,
                    worldY,
                    worldZ,
                    Context.Seed +
                    Settings.SeedOffset +
                    oreIndex * 7919
                );

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
                        (int)positionSeed
                    );

                veinSize +=
                    (int)(
                        HashTo01(sizeSeed) *
                        veinRange
                    );

                veinSize =
                    math.min(
                        veinSize,
                        ore.MaxVeinSize
                    );
            }

            GenerateVein(
                ore,
                worldX,
                worldY,
                worldZ,
                veinSize,
                positionSeed
            );
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
                    startZ
                );

            for (int i = 0;
                 i < veinSize;
                 i++)
            {
                TryPlaceOre(
                    ore,
                    current.x,
                    current.y,
                    current.z
                );

                uint stepSeed =
                    Hash(
                        current.x,
                        current.y,
                        current.z,
                        (int)seed +
                        i * 1543
                    );

                int direction =
                    (int)(
                        HashTo01(stepSeed) *
                        6f
                    );

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

                /*
                 * La veta puede cruzar el límite del chunk.
                 *
                 * NO detenemos la generación por salir del chunk.
                 * TryPlaceOre() se encargará de ignorar posiciones
                 * que no pertenezcan al chunk actual.
                 *
                 * Solo detenemos si abandonamos el rango Y válido
                 * del propio mineral.
                 */
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

            /*
             * La posición pertenece a otro chunk.
             * Este Job únicamente modifica el chunk actual.
             */
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
                    localZ
                );

            Voxel voxel =
                Voxels[index];

            /*
             * Solo reemplazamos bloques que estén definidos
             * como roca huésped para este mineral.
             */
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
                    voxel.State
                );
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

            h ^= (uint)x *
                 374761393u;

            h ^= (uint)y *
                 668265263u;

            h ^= (uint)z *
                 2147483647u;

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