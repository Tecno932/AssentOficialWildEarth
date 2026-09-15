using System;
using Unity.Collections;
using UnityEngine;

namespace WildEarth.Voxel
{
    public sealed class BlockRuntimeDatabase : IDisposable
    {
        private NativeArray<BlockRuntimeData> data;

        public bool IsCreated =>
            data.IsCreated;

        public int Length =>
            data.IsCreated ? data.Length : 0;

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

            if (allocator == Allocator.Invalid)
            {
                throw new ArgumentException(
                    "Allocator inválido.",
                    nameof(allocator)
                );
            }

            registry.Initialize();

            ValidateRegistry(registry);

            data = registry.CreateNativeRuntimeData(
                allocator
            );

            ValidateRuntimeData();
        }

        public BlockRuntimeData Get(
            ushort blockId)
        {
            ThrowIfNotCreated();

            int index = blockId;

            if (index < 0 || index >= data.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(blockId),
                    blockId,
                    $"Block ID fuera de rango. " +
                    $"Runtime database length={data.Length}."
                );
            }

            BlockRuntimeData block = data[index];

            if (block.Id != blockId)
            {
                throw new InvalidOperationException(
                    $"BlockRuntimeDatabase inconsistente. " +
                    $"Índice={index}, " +
                    $"ID solicitado={blockId}, " +
                    $"ID almacenado={block.Id}."
                );
            }

            return block;
        }

        public bool TryGet(
            ushort blockId,
            out BlockRuntimeData block)
        {
            ThrowIfNotCreated();

            int index = blockId;

            if (index < 0 || index >= data.Length)
            {
                block = default;
                return false;
            }

            block = data[index];

            if (block.Id != blockId)
            {
                block = default;
                return false;
            }

            return true;
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
            {
                return;
            }

            data.Dispose();
            data = default;
        }

        private static void ValidateRegistry(
            BlockRegistry registry)
        {
            if (registry.Definitions == null)
            {
                throw new InvalidOperationException(
                    "BlockRegistry.Definitions es NULL."
                );
            }

            if (registry.Definitions.Count == 0)
            {
                throw new InvalidOperationException(
                    "BlockRegistry no contiene definiciones."
                );
            }

            if (registry.Definitions[0] != null)
            {
                throw new InvalidOperationException(
                    "BlockRegistry[0] debe ser NULL " +
                    "porque el ID 0 está reservado para Air."
                );
            }

            for (
                int i = 1;
                i < registry.Definitions.Count;
                i++)
            {
                BlockDefinition definition =
                    registry.Definitions[i];

                if (definition == null)
                {
                    throw new InvalidOperationException(
                        $"BlockRegistry.Definitions[{i}] " +
                        "es NULL. " +
                        "Los IDs deben ser continuos."
                    );
                }

                if (definition.Id != i)
                {
                    throw new InvalidOperationException(
                        $"BlockRegistry inconsistente. " +
                        $"Índice={i}, " +
                        $"Definition.Id={definition.Id}."
                    );
                }
            }
        }

        private void ValidateRuntimeData()
        {
            if (!data.IsCreated)
            {
                throw new InvalidOperationException(
                    "No se pudo crear BlockRuntimeDatabase."
                );
            }

            if (data.Length == 0)
            {
                throw new InvalidOperationException(
                    "BlockRuntimeDatabase está vacía."
                );
            }

            BlockRuntimeData air = data[0];

            if (air.Id != BlockIds.Air)
            {
                throw new InvalidOperationException(
                    $"Runtime block 0 inválido. " +
                    $"Expected={BlockIds.Air}, " +
                    $"Actual={air.Id}."
                );
            }

            for (int i = 1; i < data.Length; i++)
            {
                BlockRuntimeData block = data[i];

                if (block.Id != i)
                {
                    throw new InvalidOperationException(
                        $"Runtime block inválido. " +
                        $"Index={i}, " +
                        $"BlockId={block.Id}."
                    );
                }
            }
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