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
            {
                throw new ArgumentNullException(
                    nameof(voxelWorld)
                );
            }

            if (voxelMeshBuilder == null)
            {
                throw new ArgumentNullException(
                    nameof(voxelMeshBuilder)
                );
            }

            if (defaultMaterial == null)
            {
                throw new InvalidOperationException(
                    "VoxelWorldRenderer requiere un " +
                    "Default Material."
                );
            }

            world = voxelWorld;
            meshBuilder = voxelMeshBuilder;
        }

        public void RenderCompletedChunks()
        {
            if (world == null)
            {
                throw new InvalidOperationException(
                    "VoxelWorldRenderer no está inicializado."
                );
            }

            if (meshBuilder == null)
            {
                throw new InvalidOperationException(
                    "VoxelWorldRenderer no tiene VoxelMeshBuilder."
                );
            }

            IReadOnlyList<Chunk> completedChunks =
                world.Generator.CompletedChunks;

            for (int i = 0; i < completedChunks.Count; i++)
            {
                Chunk chunk = completedChunks[i];

                if (chunk == null)
                    continue;

                RenderChunk(chunk);
            }

            RenderChunksNeedingMesh();
        }

        private void RenderChunksNeedingMesh()
        {
            foreach (KeyValuePair<
                ChunkCoordinate,
                ChunkMeshRenderer> pair in renderers)
            {
                ChunkCoordinate coordinate =
                    pair.Key;

                if (!world.Chunks.TryGet(
                        coordinate,
                        out Chunk chunk))
                {
                    continue;
                }

                if (chunk == null)
                    continue;

                if (!chunk.NeedsMesh)
                    continue;

                if (chunk.State != ChunkState.Generated &&
                    chunk.State != ChunkState.Ready)
                {
                    continue;
                }

                RenderChunk(chunk);
            }
        }

        public void RenderChunk(Chunk chunk)
        {
            if (chunk == null)
            {
                throw new ArgumentNullException(
                    nameof(chunk)
                );
            }

            if (world == null)
            {
                throw new InvalidOperationException(
                    "VoxelWorldRenderer no está inicializado."
                );
            }

            if (meshBuilder == null)
            {
                throw new InvalidOperationException(
                    "VoxelWorldRenderer no tiene VoxelMeshBuilder."
                );
            }

            if (defaultMaterial == null)
            {
                throw new InvalidOperationException(
                    "VoxelWorldRenderer requiere un " +
                    "Default Material."
                );
            }

            ChunkMeshData meshData =
                meshBuilder.Build(
                    chunk,
                    world.Chunks
                );

            if (meshData == null)
            {
                throw new InvalidOperationException(
                    $"VoxelMeshBuilder devolvió null " +
                    $"para {chunk.Coordinate}."
                );
            }

            try
            {
                ChunkMeshRenderer renderer =
                    GetOrCreateRenderer(chunk);

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
            ChunkCoordinate coordinate =
                chunk.Coordinate;

            if (renderers.TryGetValue(
                    coordinate,
                    out ChunkMeshRenderer existing))
            {
                if (existing != null)
                    return existing;

                renderers.Remove(
                    coordinate
                );
            }

            GameObject chunkObject =
                new GameObject(
                    $"Chunk_{coordinate}"
                );

            chunkObject.transform.SetParent(
                transform,
                false
            );

            chunkObject.transform.localPosition =
                new Vector3(
                    coordinate.X *
                        VoxelConstants.ChunkSize,

                    coordinate.Y *
                        VoxelConstants.ChunkSize,

                    coordinate.Z *
                        VoxelConstants.ChunkSize
                );

            chunkObject.transform.localRotation =
                Quaternion.identity;

            chunkObject.transform.localScale =
                Vector3.one;

            ChunkMeshRenderer renderer =
                chunkObject.AddComponent<
                    ChunkMeshRenderer
                >();

            renderers.Add(
                coordinate,
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