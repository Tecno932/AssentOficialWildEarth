using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace WildEarth.Voxel
{
    /// <summary>
    /// Registro global de definiciones de bloques.
    ///
    /// Convierte los BlockDefinition de Unity en datos runtime
    /// compactos que pueden ser utilizados por sistemas de alto
    /// rendimiento.
    /// </summary>
    [CreateAssetMenu(
        fileName = "BlockRegistry",
        menuName = "WildEarth/Voxel/Block Registry"
    )]
    public sealed class BlockRegistry : ScriptableObject
    {
        [SerializeField]
        private List<BlockDefinition> definitions = new();

        private Dictionary<ushort, BlockDefinition> definitionLookup;

        private BlockRuntimeData[] runtimeData;

        public int Count =>
            runtimeData?.Length ?? 0;

        public IReadOnlyList<BlockDefinition> Definitions =>
            definitions;

        public void Initialize()
        {
            BuildLookup();
            BuildRuntimeData();
        }

        private void BuildLookup()
        {
            definitionLookup =
                new Dictionary<ushort, BlockDefinition>(
                    definitions.Count
                );

            foreach (BlockDefinition definition in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                if (definition.Id == BlockIds.Air)
                {
                    Debug.LogError(
                        "BlockRegistry: ID 0 está reservado para Air."
                    );

                    continue;
                }

                if (definitionLookup.ContainsKey(definition.Id))
                {
                    Debug.LogError(
                        $"BlockRegistry: ID duplicado " +
                        $"{definition.Id} " +
                        $"({definition.BlockName})."
                    );

                    continue;
                }

                definitionLookup.Add(
                    definition.Id,
                    definition
                );
            }
        }

        private void BuildRuntimeData()
        {
            if (definitionLookup == null)
            {
                BuildLookup();
            }

            ushort maxId = 0;

            foreach (BlockDefinition definition in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                maxId = Math.Max(
                    maxId,
                    definition.Id
                );
            }

            runtimeData =
                new BlockRuntimeData[maxId + 1];

            foreach (BlockDefinition definition in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                runtimeData[definition.Id] =
                    definition.ToRuntimeData();
            }
        }

        public BlockDefinition GetDefinition(
            ushort blockId)
        {
            if (blockId == BlockIds.Air)
            {
                return null;
            }

            if (definitionLookup == null)
            {
                Initialize();
            }

            return definitionLookup.TryGetValue(
                blockId,
                out BlockDefinition definition
            )
                ? definition
                : null;
        }

        public bool TryGetDefinition(
            ushort blockId,
            out BlockDefinition definition)
        {
            if (blockId == BlockIds.Air)
            {
                definition = null;
                return false;
            }

            if (definitionLookup == null)
            {
                Initialize();
            }

            return definitionLookup.TryGetValue(
                blockId,
                out definition
            );
        }

        public BlockRuntimeData GetRuntimeData(
            ushort blockId)
        {
            if (blockId == BlockIds.Air)
            {
                return default;
            }

            if (runtimeData == null)
            {
                Initialize();
            }

            if (blockId >= runtimeData.Length)
            {
                return default;
            }

            return runtimeData[blockId];
        }

        public bool TryGetRuntimeData(
            ushort blockId,
            out BlockRuntimeData data)
        {
            if (blockId == BlockIds.Air)
            {
                data = default;
                return false;
            }

            if (runtimeData == null)
            {
                Initialize();
            }

            if (blockId >= runtimeData.Length)
            {
                data = default;
                return false;
            }

            data = runtimeData[blockId];

            return true;
        }

        /// <summary>
        /// Copia los datos runtime a memoria nativa.
        ///
        /// Esta memoria será utilizada posteriormente por Jobs/Burst.
        /// El llamador es responsable de liberar el NativeArray.
        /// </summary>
        public NativeArray<BlockRuntimeData>
            CreateNativeRuntimeData(
                Allocator allocator)
        {
            if (runtimeData == null)
            {
                Initialize();
            }

            NativeArray<BlockRuntimeData> nativeData =
                new NativeArray<BlockRuntimeData>(
                    runtimeData,
                    allocator
                );

            return nativeData;
        }

#if UNITY_EDITOR

        private void OnValidate()
        {
            ValidateDefinitions();
        }

        private void ValidateDefinitions()
        {
            HashSet<ushort> usedIds =
                new HashSet<ushort>();

            foreach (BlockDefinition definition in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                if (definition.Id == BlockIds.Air)
                {
                    Debug.LogError(
                        $"Block '{definition.name}' " +
                        "utiliza ID 0. " +
                        "ID 0 está reservado para Air.",
                        this
                    );
                }

                if (!usedIds.Add(definition.Id))
                {
                    Debug.LogError(
                        $"BlockRegistry contiene ID duplicado: " +
                        $"{definition.Id}.",
                        this
                    );
                }
            }
        }

#endif
    }
}