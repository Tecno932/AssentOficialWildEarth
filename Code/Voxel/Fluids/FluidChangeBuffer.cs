using System;
using Unity.Collections;

namespace WildEarth.Voxel
{
    public sealed class FluidChangeBuffer : IDisposable
    {
        public const int MaxChangesPerVoxel = 5;

        private NativeArray<FluidChange> changes;
        private NativeArray<byte> changeCounts;

        public bool IsCreated =>
            changes.IsCreated &&
            changeCounts.IsCreated;

        public int Capacity =>
            changes.IsCreated
                ? changes.Length
                : 0;

        public FluidChangeBuffer(
            int voxelCount,
            Allocator allocator)
        {
            if (voxelCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(voxelCount)
                );
            }

            changes =
                new NativeArray<FluidChange>(
                    voxelCount * MaxChangesPerVoxel,
                    allocator,
                    NativeArrayOptions.UninitializedMemory
                );

            changeCounts =
                new NativeArray<byte>(
                    voxelCount,
                    allocator,
                    NativeArrayOptions.ClearMemory
                );
        }

        public NativeArray<FluidChange>
            AsNativeArray()
        {
            ThrowIfNotCreated();

            return changes;
        }

        public NativeArray<byte>
            AsCountArray()
        {
            ThrowIfNotCreated();

            return changeCounts;
        }

        public void Clear()
        {
            ThrowIfNotCreated();

            for (int i = 0; i < changeCounts.Length; i++)
            {
                changeCounts[i] = 0;
            }
        }

        public int GetChangeCount(
            int voxelIndex)
        {
            ThrowIfNotCreated();

            if (voxelIndex < 0 ||
                voxelIndex >= changeCounts.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(voxelIndex)
                );
            }

            return changeCounts[voxelIndex];
        }

        public FluidChange GetChange(
            int voxelIndex,
            int changeIndex)
        {
            ThrowIfNotCreated();

            if (voxelIndex < 0 ||
                voxelIndex >= changeCounts.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(voxelIndex)
                );
            }

            int count =
                changeCounts[voxelIndex];

            if (changeIndex < 0 ||
                changeIndex >= count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(changeIndex)
                );
            }

            int index =
                voxelIndex *
                MaxChangesPerVoxel +
                changeIndex;

            return changes[index];
        }

        public void Dispose()
        {
            if (changes.IsCreated)
                changes.Dispose();

            if (changeCounts.IsCreated)
                changeCounts.Dispose();

            changes = default;
            changeCounts = default;
        }

        private void ThrowIfNotCreated()
        {
            if (!IsCreated)
            {
                throw new InvalidOperationException(
                    "FluidChangeBuffer no está inicializado."
                );
            }
        }
    }
}