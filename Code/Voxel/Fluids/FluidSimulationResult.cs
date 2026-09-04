using System;
using Unity.Collections;

namespace WildEarth.Voxel
{
    public sealed class FluidSimulationResult : IDisposable
    {
        private NativeArray<FluidChange> changes;
        private NativeArray<byte> changeCounts;

        public bool IsCreated =>
            changes.IsCreated &&
            changeCounts.IsCreated;

        public int VoxelCount =>
            changeCounts.IsCreated
                ? changeCounts.Length
                : 0;

        public int ChangeCapacity =>
            changes.IsCreated
                ? changes.Length
                : 0;

        public FluidSimulationResult(
            int voxelCount,
            Allocator allocator)
        {
            if (voxelCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(voxelCount)
                );
            }

            int changeCapacity =
                voxelCount *
                FluidChangeBuffer.MaxChangesPerVoxel;

            changes =
                new NativeArray<FluidChange>(
                    changeCapacity,
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

        public NativeArray<FluidChange> Changes
        {
            get
            {
                ThrowIfNotCreated();
                return changes;
            }
        }

        public NativeArray<byte> ChangeCounts
        {
            get
            {
                ThrowIfNotCreated();
                return changeCounts;
            }
        }

        public int GetChangeCount(
            int voxelIndex)
        {
            ThrowIfNotCreated();

            if (
                voxelIndex < 0 ||
                voxelIndex >= changeCounts.Length
            )
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

            if (
                voxelIndex < 0 ||
                voxelIndex >= changeCounts.Length
            )
            {
                throw new ArgumentOutOfRangeException(
                    nameof(voxelIndex)
                );
            }

            int count =
                changeCounts[voxelIndex];

            if (
                changeIndex < 0 ||
                changeIndex >= count
            )
            {
                throw new ArgumentOutOfRangeException(
                    nameof(changeIndex)
                );
            }

            int index =
                voxelIndex *
                FluidChangeBuffer.MaxChangesPerVoxel +
                changeIndex;

            return changes[index];
        }

        public void Clear()
        {
            ThrowIfNotCreated();

            for (
                int i = 0;
                i < changeCounts.Length;
                i++)
            {
                changeCounts[i] = 0;
            }
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
                    "FluidSimulationResult no está inicializado."
                );
            }
        }
    }
}