using System;
using System.Collections.Generic;
using UnityEngine;

namespace WildEarth.Voxel
{
    public sealed class VoxelWorldRenderer : MonoBehaviour
    {
        private const int MaxColliderUpdatesPerFrame = 4;

        [SerializeField]
        private Material defaultMaterial;

        private readonly Dictionary<
            ChunkCoordinate,
            ChunkMeshRenderer
        > renderers = new();

        private readonly List<ChunkCoordinate> coordinatesBuffer =
            new List<ChunkCoordinate>();

        private readonly Queue<ChunkCoordinate> colliderQueue =
            new Queue<ChunkCoordinate>();

        private readonly HashSet<ChunkCoordinate> pendingColliders =
            new HashSet<ChunkCoordinate>();

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

            RenderChunksNeedingMesh();
            ProcessPendingColliders();
        }

        private void RenderChunksNeedingMesh()
        {
            foreach (
                KeyValuePair<
                    ChunkCoordinate,
                    ChunkMeshRenderer
                > pair in renderers)
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

                if (!AreNeighborsReadyForMesh(
                        chunk))
                {
                    continue;
                }

                RenderChunk(chunk);
            }

            coordinatesBuffer.Clear();

            world.Chunks.GetCoordinates(
                coordinatesBuffer
            );

            for (
                int i = 0;
                i < coordinatesBuffer.Count;
                i++)
            {
                ChunkCoordinate coordinate =
                    coordinatesBuffer[i];

                if (renderers.ContainsKey(coordinate))
                    continue;

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

                if (!AreNeighborsReadyForMesh(
                        chunk))
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

                QueueColliderUpdate(
                    chunk.Coordinate
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

        private void QueueColliderUpdate(
            ChunkCoordinate coordinate)
        {
            if (!pendingColliders.Add(
                    coordinate))
            {
                return;
            }

            colliderQueue.Enqueue(
                coordinate
            );
        }

        private void ProcessPendingColliders()
        {
            int processed = 0;

            while (
                processed < MaxColliderUpdatesPerFrame &&
                colliderQueue.Count > 0)
            {
                ChunkCoordinate coordinate =
                    colliderQueue.Dequeue();

                pendingColliders.Remove(
                    coordinate
                );

                if (!renderers.TryGetValue(
                        coordinate,
                        out ChunkMeshRenderer renderer))
                {
                    processed++;
                    continue;
                }

                if (renderer == null)
                {
                    processed++;
                    continue;
                }

                if (!world.Chunks.TryGet(
                        coordinate,
                        out Chunk chunk))
                {
                    renderer.ClearCollider();
                    processed++;
                    continue;
                }

                if (chunk == null)
                {
                    renderer.ClearCollider();
                    processed++;
                    continue;
                }

                renderer.ApplyCollider();

                processed++;
            }
        }

        private bool AreNeighborsReadyForMesh(
            Chunk chunk)
        {
            ChunkCoordinate coordinate =
                chunk.Coordinate;

            if (!IsNeighborReady(
                    coordinate.X - 1,
                    coordinate.Y,
                    coordinate.Z))
            {
                return false;
            }

            if (!IsNeighborReady(
                    coordinate.X + 1,
                    coordinate.Y,
                    coordinate.Z))
            {
                return false;
            }

            if (!IsNeighborReady(
                    coordinate.X,
                    coordinate.Y,
                    coordinate.Z - 1))
            {
                return false;
            }

            if (!IsNeighborReady(
                    coordinate.X,
                    coordinate.Y,
                    coordinate.Z + 1))
            {
                return false;
            }

            if (coordinate.Y > 0)
            {
                if (!IsNeighborReady(
                        coordinate.X,
                        coordinate.Y - 1,
                        coordinate.Z))
                {
                    return false;
                }
            }

            int maxChunkY =
                VoxelConstants.WorldHeight /
                VoxelConstants.ChunkSize - 1;

            if (coordinate.Y < maxChunkY)
            {
                if (!IsNeighborReady(
                        coordinate.X,
                        coordinate.Y + 1,
                        coordinate.Z))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsNeighborReady(
            int x,
            int y,
            int z)
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    x,
                    y,
                    z
                );

            if (!world.Chunks.TryGet(
                    coordinate,
                    out Chunk neighbor))
            {
                return true;
            }

            if (neighbor == null)
                return true;

            return
                neighbor.State == ChunkState.Generated ||
                neighbor.State == ChunkState.Ready;
        }

        public void RemoveChunk(
            ChunkCoordinate coordinate)
        {
            pendingColliders.Remove(
                coordinate
            );

            if (!renderers.TryGetValue(
                    coordinate,
                    out ChunkMeshRenderer renderer))
            {
                return;
            }

            if (renderer != null)
            {
                renderer.ClearCollider();

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
            colliderQueue.Clear();
            pendingColliders.Clear();

            foreach (
                ChunkMeshRenderer renderer
                in renderers.Values)
            {
                if (renderer != null)
                {
                    renderer.ClearCollider();

                    Destroy(
                        renderer.gameObject
                    );
                }
            }

            renderers.Clear();
            coordinatesBuffer.Clear();
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