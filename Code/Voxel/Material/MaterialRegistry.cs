using System;
using System.Collections.Generic;
using UnityEngine;

namespace WildEarth.Voxel
{
    [CreateAssetMenu(
        fileName = "MaterialRegistry",
        menuName = "WildEarth/Voxel/Material Registry"
    )]
    public sealed class MaterialRegistry : ScriptableObject
    {
        [SerializeField]
        private List<MaterialDefinition> definitions = new();

        private Dictionary<ushort, MaterialDefinition>
            definitionLookup;

        private MaterialRuntimeData[] runtimeData;

        public int Count =>
            runtimeData?.Length ?? 0;

        public IReadOnlyList<MaterialDefinition> Definitions =>
            definitions;

        public void Initialize()
        {
            BuildLookup();
            BuildRuntimeData();
        }

        private void BuildLookup()
        {
            definitionLookup =
                new Dictionary<ushort, MaterialDefinition>(
                    definitions.Count
                );

            foreach (
                MaterialDefinition definition
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

            foreach (
                MaterialDefinition definition
                in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                if (definition.Id > maxId)
                {
                    maxId = definition.Id;
                }
            }

            runtimeData =
                new MaterialRuntimeData[maxId + 1];

            foreach (
                MaterialDefinition definition
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

                if (runtimeData[id].Id != 0)
                {
                    throw new InvalidOperationException(
                        $"MaterialRegistry: ID duplicado {id}."
                    );
                }

                runtimeData[id] =
                    new MaterialRuntimeData
                    {
                        Id = id,
                        Stats = definition.Stats
                    };
            }

            for (
                int i = 1;
                i < runtimeData.Length;
                i++)
            {
                if (runtimeData[i].Id != i)
                {
                    throw new InvalidOperationException(
                        $"MaterialRegistry: falta una definición " +
                        $"para el ID {i}."
                    );
                }
            }
        }

        public MaterialDefinition GetDefinition(
            ushort materialId)
        {
            if (materialId == 0)
            {
                return null;
            }

            if (definitionLookup == null)
            {
                Initialize();
            }

            return definitionLookup.TryGetValue(
                materialId,
                out MaterialDefinition definition)
                ? definition
                : null;
        }

        public bool TryGetDefinition(
            ushort materialId,
            out MaterialDefinition definition)
        {
            if (materialId == 0)
            {
                definition = null;
                return false;
            }

            if (definitionLookup == null)
            {
                Initialize();
            }

            return definitionLookup.TryGetValue(
                materialId,
                out definition
            );
        }

        public MaterialRuntimeData GetRuntimeData(
            ushort materialId)
        {
            if (materialId == 0)
            {
                return default;
            }

            if (runtimeData == null)
            {
                Initialize();
            }

            if (materialId >= runtimeData.Length)
            {
                return default;
            }

            return runtimeData[materialId];
        }

        public bool TryGetRuntimeData(
            ushort materialId,
            out MaterialRuntimeData data)
        {
            if (materialId == 0)
            {
                data = default;
                return false;
            }

            if (runtimeData == null)
            {
                Initialize();
            }

            if (materialId >= runtimeData.Length)
            {
                data = default;
                return false;
            }

            data =
                runtimeData[materialId];

            return data.Id != 0;
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

            foreach (
                MaterialDefinition definition
                in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                if (definition.Id == 0)
                {
                    Debug.LogError(
                        $"Material '{definition.name}' " +
                        "utiliza ID 0. " +
                        "ID 0 está reservado.",
                        this
                    );
                }

                if (!usedIds.Add(definition.Id))
                {
                    Debug.LogError(
                        $"MaterialRegistry contiene " +
                        $"ID duplicado: {definition.Id}.",
                        this
                    );
                }
            }
        }

#endif
    }
}