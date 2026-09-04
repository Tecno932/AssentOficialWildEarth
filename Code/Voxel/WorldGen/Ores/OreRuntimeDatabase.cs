using System;
using Unity.Collections;

namespace WildEarth.Voxel
{
    public sealed class OreRuntimeDatabase : IDisposable
    {
        private NativeArray<OreRuntimeData> data;
        private NativeArray<ushort> hostBlockIds;

        public bool IsCreated =>
            data.IsCreated;

        public int Length =>
            data.IsCreated
                ? data.Length
                : 0;

        public NativeArray<OreRuntimeData>
            AsNativeArray()
        {
            ThrowIfNotCreated();

            return data;
        }

        public NativeArray<ushort>
            AsHostBlockArray()
        {
            ThrowIfNotCreated();

            return hostBlockIds;
        }

        public OreRuntimeDatabase(
            OreRegistry registry,
            Allocator allocator)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(
                    nameof(registry)
                );
            }

            if (registry.Count <= 0)
            {
                throw new InvalidOperationException(
                    "El OreRegistry no contiene minerales."
                );
            }

            int totalHostBlocks =
                registry.GetTotalHostBlockCount();

            if (totalHostBlocks <= 0)
            {
                throw new InvalidOperationException(
                    "El OreRegistry no contiene bloques huésped."
                );
            }

            data =
                new NativeArray<OreRuntimeData>(
                    registry.Count,
                    allocator,
                    NativeArrayOptions.UninitializedMemory
                );

            hostBlockIds =
                new NativeArray<ushort>(
                    totalHostBlocks,
                    allocator,
                    NativeArrayOptions.UninitializedMemory
                );

            for (int i = 0;
                 i < registry.Count;
                 i++)
            {
                data[i] =
                    registry.GetRuntimeData(
                        i,
                        hostBlockIds
                    );
            }
        }

        public OreRuntimeData Get(
            int index)
        {
            ThrowIfNotCreated();

            if (index < 0 ||
                index >= data.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index)
                );
            }

            return data[index];
        }

        public void Dispose()
        {
            if (data.IsCreated)
            {
                data.Dispose();
            }

            if (hostBlockIds.IsCreated)
            {
                hostBlockIds.Dispose();
            }

            data = default;
            hostBlockIds = default;
        }

        private void ThrowIfNotCreated()
        {
            if (!data.IsCreated)
            {
                throw new InvalidOperationException(
                    "OreRuntimeDatabase no está inicializada."
                );
            }
        }
    }
}