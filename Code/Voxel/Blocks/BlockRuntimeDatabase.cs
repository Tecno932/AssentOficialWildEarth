using System;
using Unity.Collections;

namespace WildEarth.Voxel
{
    public sealed class BlockRuntimeDatabase : IDisposable
    {
        private NativeArray<BlockRuntimeData> data;

        public bool IsCreated =>
            data.IsCreated;

        public int Length =>
            data.IsCreated
                ? data.Length
                : 0;

        public BlockRuntimeDatabase(
            BlockRegistry registry,
            Allocator allocator)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(
                    nameof(registry)
                );
            }

            data =
                registry.CreateNativeRuntimeData(
                    allocator
                );
        }

        public BlockRuntimeData Get(
            ushort blockId)
        {
            ThrowIfNotCreated();

            int index = blockId;

            if (index < 0 ||
                index >= data.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(blockId)
                );
            }

            return data[index];
        }

        public bool TryGet(
            ushort blockId,
            out BlockRuntimeData block)
        {
            ThrowIfNotCreated();

            int index = blockId;

            if (index < 0 ||
                index >= data.Length)
            {
                block = default;
                return false;
            }

            block = data[index];

            return block.Id == blockId;
        }

        public NativeArray<BlockRuntimeData>
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
                    "BlockRuntimeDatabase no está inicializada."
                );
            }
        }
    }
}