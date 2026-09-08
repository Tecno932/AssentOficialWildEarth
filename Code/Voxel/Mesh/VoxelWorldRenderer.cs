using System;
using System.Collections.Generic;
using UnityEngine;

namespace WildEarth.Voxel
{
    public sealed class VoxelWorldRenderer : MonoBehaviour
    {
        [SerializeField]
        private Material defaultMaterial;

        private readonly Dictionary<
            ChunkCoordinate,
            ChunkMeshRenderer
        > renderers = new();

        private VoxelWorld world;
        private VoxelMeshBuilder meshBuilder;

        public int RenderedChunkCount =>
            renderers.Count;

        public void Initialize(
            VoxelWorld voxelWorld,
            VoxelMeshBuilder voxelMeshBuilder)
        {
            if (voxelWorld == null)
                throw new ArgumentNullException(
                    nameof(voxelWorld)
                );

            if (voxelMeshBuilder == null)
                throw new ArgumentNullException(
                    nameof(voxelMeshBuilder)
                );

            world = voxelWorld;
            meshBuilder = voxelMeshBuilder;
        }

        public void RenderCompletedChunks()
        {
            if (world == null)
                throw new InvalidOperationException(
                    "VoxelWorldRenderer no está inicializado."
                );

            IReadOnlyList<Chunk> completedChunks =
                world.Generator.CompletedChunks;

            for (int i = 0; i < completedChunks.Count; i++)
            {
                Chunk chunk =
                    completedChunks[i];

                if (chunk == null)
                    continue;

                RenderChunk(chunk);
            }
        }

        public void RenderChunk(
            Chunk chunk)
        {
            if (chunk == null)
                throw new ArgumentNullException(
                    nameof(chunk)
                );

            if (world == null)
                throw new InvalidOperationException(
                    "VoxelWorldRenderer no está inicializado."
                );

            ChunkMeshData meshData =
                meshBuilder.Build(
                    chunk,
                    world.Chunks
                );

            try
            {
                ChunkMeshRenderer renderer =
                    GetOrCreateRenderer(
                        chunk
                    );

                renderer.Apply(
                    meshData,
                    defaultMaterial
                );

                chunk.ClearNeedsMesh();
                chunk.SetState(
                    ChunkState.Ready
                );
            }
            finally
            {
                meshData.Dispose();
            }
        }

        public void RemoveChunk(
            ChunkCoordinate coordinate)
        {
            if (!renderers.TryGetValue(
                    coordinate,
                    out ChunkMeshRenderer renderer))
            {
                return;
            }

            if (renderer != null)
            {
                Destroy(
                    renderer.gameObject
                );
            }

            renderers.Remove(
                coordinate
            );
        }

        public void Clear()
        {
            foreach (
                ChunkMeshRenderer renderer
                in renderers.Values)
            {
                if (renderer != null)
                {
                    Destroy(
                        renderer.gameObject
                    );
                }
            }

            renderers.Clear();
        }

        private ChunkMeshRenderer GetOrCreateRenderer(
            Chunk chunk)
        {
            if (renderers.TryGetValue(
                    chunk.Coordinate,
                    out ChunkMeshRenderer existing))
            {
                if (existing != null)
                    return existing;

                renderers.Remove(
                    chunk.Coordinate
                );
            }

            GameObject gameObject =
                new GameObject(
                    $"Chunk_{chunk.Coordinate}"
                );

            gameObject.transform.SetParent(
                transform,
                false
            );

            gameObject.transform.localPosition =
                new Vector3(
                    chunk.Coordinate.X *
                        VoxelConstants.ChunkSize,

                    chunk.Coordinate.Y *
                        VoxelConstants.ChunkSize,

                    chunk.Coordinate.Z *
                        VoxelConstants.ChunkSize
                );

            ChunkMeshRenderer renderer =
                gameObject.AddComponent<ChunkMeshRenderer>();

            renderers.Add(
                chunk.Coordinate,
                renderer
            );

            return renderer;
        }

        private void OnDestroy()
        {
            Clear();

            world = null;
            meshBuilder = null;
        }
    }
}