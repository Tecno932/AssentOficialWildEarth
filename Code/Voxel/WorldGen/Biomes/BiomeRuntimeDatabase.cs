using System;
using Unity.Collections;

namespace WildEarth.Voxel
{
    public sealed class BiomeRuntimeDatabase : IDisposable
    {
        private NativeArray<BiomeRuntimeData> data;

        public bool IsCreated =>
            data.IsCreated;

        public int Length =>
            data.IsCreated ? data.Length : 0;

        public BiomeRuntimeDatabase(
            BiomeRegistry registry,
            Allocator allocator)
        {
            if (registry == null)
                throw new ArgumentNullException(
                    nameof(registry)
                );

            data =
                registry.CreateNativeRuntimeData(
                    allocator
                );
        }

        public BiomeRuntimeData Get(
            BiomeId id)
        {
            ThrowIfNotCreated();

            int index = (int)id;

            if (index < 0 ||
                index >= data.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(id)
                );
            }

            return data[index];
        }

        public NativeArray<BiomeRuntimeData>
            AsNativeArray()
        {
            ThrowIfNotCreated();

            return data;
        }

        public void Dispose()
        {
            if (!data.IsCreated)
                return;

            data.Dispose();
            data = default;
        }

        private void ThrowIfNotCreated()
        {
            if (!data.IsCreated)
            {
                throw new InvalidOperationException(
                    "BiomeRuntimeDatabase no está inicializada."
                );
            }
        }
    }
}