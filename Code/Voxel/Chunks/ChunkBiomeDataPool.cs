using System;
using System.Collections.Generic;
using Unity.Collections;

namespace WildEarth.Voxel
{
    public sealed class ChunkBiomeDataPool : IDisposable
    {
        private readonly Stack<ChunkBiomeData> pool;
        private readonly Allocator allocator;
        private readonly int maxCapacity;

        private int totalCreated;

        public int AvailableCount =>
            pool.Count;

        public int TotalCreated =>
            totalCreated;

        public ChunkBiomeDataPool(
            Allocator allocator,
            int initialCapacity,
            int maxCapacity)
        {
            if (initialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialCapacity)
                );
            }

            if (maxCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxCapacity)
                );
            }

            if (initialCapacity > maxCapacity)
            {
                throw new ArgumentException(
                    "El pool inicial no puede superar el máximo."
                );
            }

            this.allocator = allocator;
            this.maxCapacity = maxCapacity;

            pool =
                new Stack<ChunkBiomeData>(
                    Math.Max(initialCapacity, 1)
                );

            Prewarm(initialCapacity);
        }

        private void Prewarm(
            int count)
        {
            for (int i = 0; i < count; i++)
            {
                ChunkBiomeData data =
                    CreateData();

                pool.Push(data);
            }
        }

        private ChunkBiomeData CreateData()
        {
            ChunkBiomeData data =
                new ChunkBiomeData(
                    allocator
                );

            totalCreated++;

            return data;
        }

        public ChunkBiomeData Acquire()
        {
            if (pool.Count > 0)
            {
                return pool.Pop();
            }

            if (totalCreated < maxCapacity)
            {
                return CreateData();
            }

            throw new InvalidOperationException(
                "ChunkBiomeDataPool alcanzó su capacidad máxima."
            );
        }

        public void Release(
            ChunkBiomeData data)
        {
            if (data == null)
                return;

            if (!data.IsCreated)
                return;

            if (pool.Count >= maxCapacity)
            {
                data.Dispose();
                totalCreated--;

                return;
            }

            pool.Push(data);
        }

        public void Dispose()
        {
            while (pool.Count > 0)
            {
                ChunkBiomeData data =
                    pool.Pop();

                data.Dispose();
            }

            totalCreated = 0;
        }
    }
}