using System;
using System.Collections.Generic;

namespace WildEarth.Voxel
{
    public sealed class ChunkStorage : IDisposable
    {
        private readonly Dictionary<ChunkCoordinate, Chunk> chunks;

        private readonly ChunkDataPool dataPool;

        private readonly ChunkBiomeDataPool biomeDataPool;

        public int Count =>
            chunks.Count;

        public ChunkStorage(
            ChunkDataPool dataPool,
            ChunkBiomeDataPool biomeDataPool,
            int initialCapacity = 1024)
        {
            if (dataPool == null)
            {
                throw new ArgumentNullException(
                    nameof(dataPool)
                );
            }

            if (biomeDataPool == null)
            {
                throw new ArgumentNullException(
                    nameof(biomeDataPool)
                );
            }

            this.dataPool =
                dataPool;

            this.biomeDataPool =
                biomeDataPool;

            chunks =
                new Dictionary<ChunkCoordinate, Chunk>(
                    initialCapacity
                );
        }

        public bool Contains(
            ChunkCoordinate coordinate)
        {
            return chunks.ContainsKey(
                coordinate
            );
        }

        public bool TryGet(
            ChunkCoordinate coordinate,
            out Chunk chunk)
        {
            return chunks.TryGetValue(
                coordinate,
                out chunk
            );
        }

        public Chunk Create(
            ChunkCoordinate coordinate)
        {
            if (chunks.ContainsKey(
                    coordinate))
            {
                throw new InvalidOperationException(
                    $"El chunk {coordinate} ya existe."
                );
            }

            ChunkData data =
                dataPool.Acquire();

            ChunkBiomeData biomeData =
                biomeDataPool.Acquire();

            Chunk chunk;

            try
            {
                chunk =
                    new Chunk(
                        coordinate,
                        data,
                        biomeData
                    );
            }
            catch
            {
                dataPool.Release(data);

                biomeDataPool.Release(
                    biomeData
                );

                throw;
            }

            chunk.SetState(
                ChunkState.Loading
            );

            chunks.Add(
                coordinate,
                chunk
            );

            return chunk;
        }

        public bool Remove(
            ChunkCoordinate coordinate)
        {
            if (!chunks.TryGetValue(
                    coordinate,
                    out Chunk chunk))
            {
                return false;
            }

            chunks.Remove(
                coordinate
            );

            dataPool.Release(
                chunk.Data
            );

            biomeDataPool.Release(
                chunk.BiomeData
            );

            return true;
        }

        public void Clear()
        {
            foreach (Chunk chunk in chunks.Values)
            {
                dataPool.Release(
                    chunk.Data
                );

                biomeDataPool.Release(
                    chunk.BiomeData
                );
            }

            chunks.Clear();
        }

        public void Dispose()
        {
            Clear();

            dataPool.Dispose();

            biomeDataPool.Dispose();
        }
    }
}