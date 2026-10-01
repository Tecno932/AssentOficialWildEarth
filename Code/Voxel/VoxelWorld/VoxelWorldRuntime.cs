using System;
using System.Collections.Generic;
using UnityEngine;

namespace WildEarth.Voxel
{
    public sealed class VoxelWorldRuntime : MonoBehaviour
    {
        [SerializeField]
        private BiomeRegistryAsset biomeRegistryAsset;

        [SerializeField]
        private BlockRegistry blockRegistry;

        [SerializeField]
        private ItemRegistry itemRegistry;

        [SerializeField]
        private VoxelAtlasSettings atlasSettings;

        [SerializeField]
        private OreRegistryAsset oreRegistryAsset;

        [SerializeField]
        private FluidRegistryAsset fluidRegistryAsset;

        private readonly List<ChunkCoordinate> chunksToUnload = new();

        [SerializeField]
        private VoxelWorldRenderer worldRenderer;

        [Header("World Streaming")]
        [SerializeField]
        [Min(0)]
        private int renderDistance = 2;

        [SerializeField]
        private Transform streamingTarget;

        private VoxelWorld world;
        private VoxelMeshBuilder meshBuilder;

        private bool debugVoxelCountsLogged;
        private bool hasStreamingCoordinate;
        private ChunkCoordinate lastStreamingCoordinate;

        private bool hasLoggedBiome;
        private BiomeId lastLoggedBiome;

        public VoxelWorld World =>
            world;

        public BlockRegistry BlockRegistry =>
            blockRegistry;

        public ItemRegistry ItemRegistry =>
            itemRegistry;

        public VoxelAtlasSettings AtlasSettings =>
            atlasSettings;

        private void Awake()
        {
            if (biomeRegistryAsset == null)
                throw new InvalidOperationException(
                    "VoxelWorldRuntime: falta BiomeRegistryAsset."
                );

            if (blockRegistry == null)
                throw new InvalidOperationException(
                    "VoxelWorldRuntime: falta BlockRegistry."
                );

            if (itemRegistry == null)
                throw new InvalidOperationException(
                    "VoxelWorldRuntime: falta ItemRegistry."
                );

            if (atlasSettings == null)
                throw new InvalidOperationException(
                    "VoxelWorldRuntime: falta VoxelAtlasSettings."
                );

            if (oreRegistryAsset == null)
                throw new InvalidOperationException(
                    "VoxelWorldRuntime: falta OreRegistryAsset."
                );

            if (fluidRegistryAsset == null)
                throw new InvalidOperationException(
                    "VoxelWorldRuntime: falta FluidRegistryAsset."
                );

            if (worldRenderer == null)
                throw new InvalidOperationException(
                    "VoxelWorldRuntime: falta VoxelWorldRenderer."
                );

            if (streamingTarget == null)
                throw new InvalidOperationException(
                    "VoxelWorldRuntime: falta Streaming Target."
                );

            world = new VoxelWorld(
                VoxelWorldSettings.Default,
                ChunkGenerationSettings.Default,
                biomeRegistryAsset,
                blockRegistry,
                oreRegistryAsset,
                fluidRegistryAsset
            );

            world.Initialize();

            meshBuilder =
                new VoxelMeshBuilder(
                    world.BlockDatabase,
                    atlasSettings
                );

            worldRenderer.Initialize(
                world,
                meshBuilder
            );

            GenerateInitialRing();

            lastStreamingCoordinate =
                WorldPositionToChunkCoordinate(
                    streamingTarget.position
                );

            hasStreamingCoordinate = true;
        }

        private void GenerateInitialRing()
        {
            ChunkCoordinate center =
                WorldPositionToChunkCoordinate(
                    streamingTarget.position
                );

            world.SetGenerationPriorityCenter(
                center
            );

            int chunksPerColumn =
                VoxelConstants.WorldHeight /
                VoxelConstants.ChunkSize;

            int horizontalChunkCount =
                renderDistance * 2 + 1;

            int totalColumns =
                horizontalChunkCount *
                horizontalChunkCount;

            int totalChunks =
                totalColumns *
                chunksPerColumn;

            for (int z = -renderDistance;
                z <= renderDistance;
                z++)
            {
                for (int x = -renderDistance;
                    x <= renderDistance;
                    x++)
                {
                    for (int y = 0;
                        y < chunksPerColumn;
                        y++)
                    {
                        ChunkCoordinate coordinate =
                            new ChunkCoordinate(
                                center.X + x,
                                y,
                                center.Z + z
                            );

                        world.LoadAndGenerateChunk(
                            coordinate
                        );
                    }
                }
            }

            Debug.Log(
                $"[VoxelWorldRuntime] " +
                $"Columnas solicitadas: {totalColumns}, " +
                $"chunks solicitados: {totalChunks}"
            );
        }

        private void Update()
        {
            if (world == null)
                return;

            UpdateStreaming();

            world.Update();

            UpdateBiomeDebug();

            worldRenderer.RenderCompletedChunks();

            if (!debugVoxelCountsLogged)
            {
                ChunkCoordinate debugCoordinate =
                    new ChunkCoordinate(0, 3, 0);

                if (world.TryGetChunk(
                        debugCoordinate,
                        out Chunk chunk) &&
                    chunk != null &&
                    (chunk.State == ChunkState.Generated ||
                     chunk.State == ChunkState.Ready))
                {
                    world.DebugLogChunkBlockCounts(
                        debugCoordinate
                    );

                    debugVoxelCountsLogged = true;
                }
            }
        }

        private void UpdateStreaming()
        {
            if (streamingTarget == null)
                return;

            ChunkCoordinate currentCoordinate =
                WorldPositionToChunkCoordinate(
                    streamingTarget.position
                );

            if (hasStreamingCoordinate &&
                currentCoordinate == lastStreamingCoordinate)
            {
                return;
            }

            lastStreamingCoordinate =
                currentCoordinate;

            hasStreamingCoordinate = true;

            LoadRequiredChunks(
                currentCoordinate
            );

            UnloadDistantChunks(
                currentCoordinate
            );
        }

        private void LoadRequiredChunks(
            ChunkCoordinate center)
        {
            world.SetGenerationPriorityCenter(
                center
            );

            int chunksPerColumn =
                VoxelConstants.WorldHeight /
                VoxelConstants.ChunkSize;

            for (int z = -renderDistance;
                z <= renderDistance;
                z++)
            {
                for (int x = -renderDistance;
                    x <= renderDistance;
                    x++)
                {
                    for (int y = 0;
                        y < chunksPerColumn;
                        y++)
                    {
                        ChunkCoordinate coordinate =
                            new ChunkCoordinate(
                                center.X + x,
                                y,
                                center.Z + z
                            );

                        world.LoadAndGenerateChunk(
                            coordinate
                        );
                    }
                }
            }
        }

        private void UnloadDistantChunks(
            ChunkCoordinate center)
        {
            chunksToUnload.Clear();

            world.Chunks.GetCoordinates(
                chunksToUnload
            );

            for (int i = 0;
                i < chunksToUnload.Count;
                i++)
            {
                ChunkCoordinate coordinate =
                    chunksToUnload[i];

                int distanceX =
                    Mathf.Abs(
                        coordinate.X - center.X
                    );

                int distanceZ =
                    Mathf.Abs(
                        coordinate.Z - center.Z
                    );

                if (distanceX <= renderDistance &&
                    distanceZ <= renderDistance)
                {
                    continue;
                }

                if (world.UnloadChunk(coordinate))
                {
                    worldRenderer.RemoveChunk(
                        coordinate
                    );
                }
            }
        }

        private static ChunkCoordinate
            WorldPositionToChunkCoordinate(
                Vector3 position)
        {
            int x =
                Mathf.FloorToInt(
                    position.x /
                    VoxelConstants.ChunkSize
                );

            int y =
                Mathf.FloorToInt(
                    position.y /
                    VoxelConstants.ChunkSize
                );

            int z =
                Mathf.FloorToInt(
                    position.z /
                    VoxelConstants.ChunkSize
                );

            return new ChunkCoordinate(
                x,
                y,
                z
            );
        }

        private void UpdateBiomeDebug()
        {
            if (world == null ||
                streamingTarget == null)
            {
                return;
            }

            Vector3 position =
                streamingTarget.position;

            int worldX =
                Mathf.FloorToInt(position.x);

            int worldZ =
                Mathf.FloorToInt(position.z);

            if (!world.TryGetBiomeAt(
                    worldX,
                    worldZ,
                    out BiomeId biomeId))
            {
                return;
            }

            if (hasLoggedBiome &&
                biomeId == lastLoggedBiome)
            {
                return;
            }

            lastLoggedBiome =
                biomeId;

            hasLoggedBiome = true;

            Debug.Log(
                $"[VoxelBiome] Bioma actual: {biomeId} " +
                $"| Posición: ({worldX}, {worldZ})"
            );
        }

        private void OnDestroy()
        {
            if (worldRenderer != null)
                worldRenderer.Clear();

            world?.Dispose();

            world = null;
            meshBuilder = null;
        }
    }
}