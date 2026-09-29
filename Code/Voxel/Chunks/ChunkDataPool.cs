using System;
using System.Collections.Generic;
using Unity.Collections;

namespace WildEarth.Voxel
{
    /// <summary>
    /// Pool de ChunkData.
    ///
    /// Reutiliza NativeArray<Voxel> para evitar allocaciones y
    /// liberaciones constantes durante el streaming del mundo.
    ///
    /// Cada ChunkData adquirido se entrega completamente limpio.
    /// </summary>
    public sealed class ChunkDataPool : IDisposable
    {
        private readonly Stack<ChunkData> pool;

        private readonly Allocator allocator;

        private readonly int maxCapacity;

        private int totalCreated;

        public int AvailableCount => pool.Count;

        public int TotalCreated => totalCreated;

        public ChunkDataPool(
            Allocator allocator,
            int initialCapacity,
            int maxCapacity)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(initialCapacity));

            if (maxCapacity < initialCapacity)
                throw new ArgumentOutOfRangeException(
                    nameof(maxCapacity));

            this.allocator = allocator;
            this.maxCapacity = maxCapacity;

            pool = new Stack<ChunkData>(
                Math.Max(initialCapacity, 1)
            );

            Prewarm(initialCapacity);
        }

        private void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                ChunkData data = CreateData();
                pool.Push(data);
            }
        }

        private ChunkData CreateData()
        {
            ChunkData data = new ChunkData(allocator);
            totalCreated++;
            return data;
        }

        public ChunkData Acquire()
        {
            ChunkData data;

            if (pool.Count > 0)
            {
                data = pool.Pop();
            }
            else
            {
                if (totalCreated >= maxCapacity)
                {
                    throw new InvalidOperationException(
                        "ChunkDataPool alcanzó su capacidad máxima."
                    );
                }

                data = CreateData();
            }

            Clear(data);

            return data;
        }

        public void Release(ChunkData data)
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

        private static void Clear(ChunkData data)
        {
            if (data == null || !data.IsCreated)
                return;

            Unity.Collections.NativeArrayOptions options =
                Unity.Collections.NativeArrayOptions.ClearMemory;

            data.Voxels.CopyFrom(
                new NativeArray<Voxel>(
                    VoxelConstants.VoxelsPerChunk,
                    Allocator.Temp,
                    options
                )
            );
        }

        public void Dispose()
        {
            while (pool.Count > 0)
            {
                ChunkData data = pool.Pop();
                data.Dispose();
            }

            totalCreated = 0;
        }
    }
}