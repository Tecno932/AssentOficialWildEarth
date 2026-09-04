using System;
using Unity.Collections;

namespace WildEarth.Voxel
{
    public sealed class FluidRuntimeDatabase : IDisposable
    {
        private NativeArray<FluidRuntimeData> data;

        private NativeArray<FluidRuntimeData> dataByBlockId;

        public bool IsCreated =>
            data.IsCreated &&
            dataByBlockId.IsCreated;

        public int Length =>
            data.IsCreated
                ? data.Length
                : 0;

        public int BlockLookupLength =>
            dataByBlockId.IsCreated
                ? dataByBlockId.Length
                : 0;

        public FluidRuntimeDatabase(
            FluidRegistry registry,
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
                    "El FluidRegistry no contiene fluidos."
                );
            }

            FluidRuntimeData[] runtimeData =
                registry.GetRuntimeDataArray();

            ushort maxBlockId = 0;

            for (int i = 0;
                 i < runtimeData.Length;
                 i++)
            {
                FluidRuntimeData fluid =
                    runtimeData[i];

                if (!fluid.IsValid)
                {
                    throw new InvalidOperationException(
                        $"El fluido en índice {i} " +
                        "no contiene datos runtime válidos."
                    );
                }

                if (fluid.BlockId > maxBlockId)
                {
                    maxBlockId =
                        fluid.BlockId;
                }
            }

            data =
                new NativeArray<FluidRuntimeData>(
                    runtimeData,
                    allocator
                );

            dataByBlockId =
                new NativeArray<FluidRuntimeData>(
                    maxBlockId + 1,
                    allocator,
                    NativeArrayOptions.ClearMemory
                );

            for (int i = 0;
                 i < runtimeData.Length;
                 i++)
            {
                FluidRuntimeData fluid =
                    runtimeData[i];

                dataByBlockId[
                    fluid.BlockId
                ] = fluid;
            }
        }

        public FluidRuntimeData Get(
            FluidType type)
        {
            ThrowIfNotCreated();

            for (int i = 0;
                 i < data.Length;
                 i++)
            {
                FluidRuntimeData fluid =
                    data[i];

                if (fluid.Type == type)
                {
                    return fluid;
                }
            }

            throw new InvalidOperationException(
                $"No existe un fluido registrado " +
                $"para el tipo {type}."
            );
        }

        public bool TryGet(
            FluidType type,
            out FluidRuntimeData fluid)
        {
            ThrowIfNotCreated();

            for (int i = 0;
                 i < data.Length;
                 i++)
            {
                FluidRuntimeData current =
                    data[i];

                if (current.Type == type)
                {
                    fluid = current;
                    return true;
                }
            }

            fluid = default;
            return false;
        }

        public FluidRuntimeData Get(
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

        public FluidRuntimeData GetByBlockId(
            ushort blockId)
        {
            ThrowIfNotCreated();

            if (blockId == BlockIds.Air)
            {
                return default;
            }

            if (blockId >= dataByBlockId.Length)
            {
                return default;
            }

            return dataByBlockId[blockId];
        }

        public bool TryGetByBlockId(
            ushort blockId,
            out FluidRuntimeData fluid)
        {
            ThrowIfNotCreated();

            if (blockId == BlockIds.Air ||
                blockId >= dataByBlockId.Length)
            {
                fluid = default;
                return false;
            }

            fluid =
                dataByBlockId[blockId];

            return fluid.IsValid;
        }

        public NativeArray<FluidRuntimeData>
            AsNativeArray()
        {
            ThrowIfNotCreated();

            return data;
        }

        public NativeArray<FluidRuntimeData>
            AsBlockLookupNativeArray()
        {
            ThrowIfNotCreated();

            return dataByBlockId;
        }

        public void Dispose()
        {
            if (data.IsCreated)
            {
                data.Dispose();
            }

            if (dataByBlockId.IsCreated)
            {
                dataByBlockId.Dispose();
            }

            data = default;
            dataByBlockId = default;
        }

        private void ThrowIfNotCreated()
        {
            if (!IsCreated)
            {
                throw new InvalidOperationException(
                    "FluidRuntimeDatabase no está inicializada."
                );
            }
        }
    }
}