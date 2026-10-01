using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace WildEarth.Voxel
{
    [CreateAssetMenu(
        fileName = "ItemRegistry",
        menuName = "WildEarth/Voxel/Item Registry"
    )]
    public sealed class ItemRegistry : ScriptableObject
    {
        [SerializeField]
        private List<ItemDefinition> definitions = new();

        private Dictionary<ushort, ItemDefinition>
            definitionLookup;

        private ItemRuntimeData[] runtimeData;

        public int Count =>
            runtimeData?.Length ?? 0;

        public IReadOnlyList<ItemDefinition>
            Definitions =>
            definitions;

        public void Initialize()
        {
            BuildLookup();
            BuildRuntimeData();
        }

        private void BuildLookup()
        {
            definitionLookup =
                new Dictionary<ushort, ItemDefinition>(
                    definitions.Count
                );

            foreach (ItemDefinition definition
                     in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                if (definition.Id == 0)
                {
                    continue;
                }

                if (definitionLookup.ContainsKey(
                    definition.Id))
                {
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

            int maxId = 0;

            foreach (ItemDefinition definition
                     in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                int id = definition.Id;

                if (id < 0)
                {
                    throw new InvalidOperationException(
                        $"ItemRegistry: ID inválido {id}."
                    );
                }

                if (id > maxId)
                {
                    maxId = id;
                }
            }

            runtimeData =
                new ItemRuntimeData[maxId + 1];

            runtimeData[0] =
                default;

            foreach (ItemDefinition definition
                     in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                ushort id =
                    definition.Id;

                if (id == 0)
                {
                    continue;
                }

                ItemRuntimeData data =
                    new ItemRuntimeData
                    {
                        Id = id,

                        ItemType =
                            definition.ItemType,

                        BlockId =
                            definition.BlockId,

                        MaxStackSize =
                            definition.MaxStackSize,

                        ToolType =
                            definition.ToolType,

                        ToolLevel =
                            definition.ToolLevel,

                        ToolSpeedMultiplier =
                            definition.ToolSpeedMultiplier,

                        HasDurability =
                            definition.HasDurability,

                        MaxDurability =
                            definition.MaxDurability,

                        Icon =
                            definition.Icon
                    };

                if (id >= runtimeData.Length)
                {
                    throw new InvalidOperationException(
                        $"ItemRegistry: ID {id} " +
                        $"fuera del runtimeData."
                    );
                }

                if (runtimeData[id].Id != 0)
                {
                    throw new InvalidOperationException(
                        $"ItemRegistry: ID duplicado {id}."
                    );
                }

                runtimeData[id] =
                    data;
            }

            for (int i = 1;
                 i < runtimeData.Length;
                 i++)
            {
                ItemRuntimeData data =
                    runtimeData[i];

                if (data.Id != i)
                {
                    throw new InvalidOperationException(
                        $"ItemRegistry: falta una " +
                        $"definición para el ID {i}, " +
                        $"o el runtime data está desalineado."
                    );
                }
            }
        }

        public ItemDefinition GetDefinition(
            ushort itemId)
        {
            if (itemId == 0)
            {
                return null;
            }

            if (definitionLookup == null)
            {
                Initialize();
            }

            return definitionLookup.TryGetValue(
                itemId,
                out ItemDefinition definition
            )
                ? definition
                : null;
        }

        public bool TryGetDefinition(
            ushort itemId,
            out ItemDefinition definition)
        {
            if (itemId == 0)
            {
                definition = null;
                return false;
            }

            if (definitionLookup == null)
            {
                Initialize();
            }

            return definitionLookup.TryGetValue(
                itemId,
                out definition
            );
        }

        public ItemRuntimeData GetRuntimeData(
            ushort itemId)
        {
            if (itemId == 0)
            {
                return default;
            }

            if (runtimeData == null)
            {
                Initialize();
            }

            if (itemId >= runtimeData.Length)
            {
                return default;
            }

            return runtimeData[itemId];
        }

        public bool TryGetRuntimeData(
            ushort itemId,
            out ItemRuntimeData data)
        {
            if (itemId == 0)
            {
                data = default;
                return false;
            }

            if (runtimeData == null)
            {
                Initialize();
            }

            if (itemId >= runtimeData.Length)
            {
                data = default;
                return false;
            }

            data =
                runtimeData[itemId];

            return true;
        }

        public NativeArray<ItemRuntimeData>
            CreateNativeRuntimeData(
                Allocator allocator)
        {
            if (runtimeData == null)
            {
                Initialize();
            }

            return new NativeArray<ItemRuntimeData>(
                runtimeData,
                allocator
            );
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

            foreach (ItemDefinition definition
                     in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                if (definition.Id == 0)
                {
                    Debug.LogError(
                        $"Item '{definition.name}' " +
                        "utiliza ID 0. " +
                        "ID 0 está reservado para None.",
                        this
                    );
                }

                if (!usedIds.Add(definition.Id))
                {
                    Debug.LogError(
                        $"ItemRegistry contiene ID duplicado: " +
                        $"{definition.Id}.",
                        this
                    );
                }
            }
        }

#endif
    }
}