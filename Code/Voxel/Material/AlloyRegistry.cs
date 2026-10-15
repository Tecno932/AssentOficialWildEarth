
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WildEarth.Voxel
{
    [CreateAssetMenu(
        fileName = "AlloyRegistry",
        menuName = "WildEarth/Voxel/Alloy Registry"
    )]
    public sealed class AlloyRegistry : ScriptableObject
    {
        [SerializeField]
        private List<AlloyDefinition> definitions = new();

        [Header("Dependencies")]
        [SerializeField]
        private MaterialRegistry materialRegistry;

        private Dictionary<ushort, AlloyDefinition>
            definitionLookup;

        private AlloyRuntimeData[] runtimeData;

        public int Count =>
            runtimeData?.Length ?? 0;

        public IReadOnlyList<AlloyDefinition> Definitions =>
            definitions;

        public void Initialize()
        {
            if (materialRegistry == null)
            {
                throw new InvalidOperationException(
                    "[AlloyRegistry] MaterialRegistry no está asignado."
                );
            }

            materialRegistry.Initialize();

            BuildLookup();
            BuildRuntimeData();
        }

        private void BuildLookup()
        {
            definitionLookup =
                new Dictionary<ushort, AlloyDefinition>(
                    definitions.Count
                );

            foreach (AlloyDefinition definition in definitions)
            {
                if (definition == null || definition.Id == 0)
                {
                    continue;
                }

                if (definitionLookup.ContainsKey(definition.Id))
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

            foreach (AlloyDefinition definition in definitions)
            {
                if (definition != null &&
                    definition.Id > maxId)
                {
                    maxId = definition.Id;
                }
            }

            runtimeData = new AlloyRuntimeData[maxId + 1];

            foreach (AlloyDefinition definition in definitions)
            {
                if (definition == null || definition.Id == 0)
                {
                    continue;
                }

                ushort id = definition.Id;

                if (runtimeData[id].Id != 0)
                {
                    throw new InvalidOperationException(
                        $"AlloyRegistry: ID duplicado {id}."
                    );
                }

                MaterialStats stats = CalculateStats(definition);

                runtimeData[id] = new AlloyRuntimeData
                {
                    Id = id,
                    Stats = stats
                };
            }

            for (int i = 1; i < runtimeData.Length; i++)
            {
                if (runtimeData[i].Id != i)
                {
                    throw new InvalidOperationException(
                        $"AlloyRegistry: falta una definición " +
                        $"para el ID {i}."
                    );
                }
            }
        }

        private MaterialStats CalculateStats(
            AlloyDefinition definition)
        {
            IReadOnlyList<AlloyComponent> components =
                definition.Components;

            if (components == null || components.Count == 0)
            {
                throw new InvalidOperationException(
                    $"AlloyRegistry: la aleación " +
                    $"'{definition.AlloyName}' no tiene componentes."
                );
            }

            float totalAmount = 0f;

            float hardness = 0f;
            float resistance = 0f;
            float toughness = 0f;
            float edgeRetention = 0f;
            float edgeAptitude = 0f;

            float densityDenominator = 0f;
            bool canCalculateDensity = true;

            foreach (AlloyComponent component in components)
            {
                if (!component.IsValid)
                {
                    throw new InvalidOperationException(
                        $"AlloyRegistry: la aleación " +
                        $"'{definition.AlloyName}' " +
                        "contiene un componente inválido."
                    );
                }

                MaterialRuntimeData material =
                    materialRegistry.GetRuntimeData(
                        component.MaterialId
                    );

                if (!material.IsValid)
                {
                    throw new InvalidOperationException(
                        $"AlloyRegistry: la aleación " +
                        $"'{definition.AlloyName}' " +
                        $"referencia un material inexistente " +
                        $"con ID {component.MaterialId}."
                    );
                }

                float amount = component.Amount;
                MaterialStats materialStats = material.Stats;

                totalAmount += amount;

                // Aproximaciones ponderadas para gameplay.
                hardness += materialStats.Hardness * amount;
                resistance += materialStats.Resistance * amount;
                toughness += materialStats.Toughness * amount;
                edgeRetention += materialStats.EdgeRetention * amount;
                edgeAptitude += materialStats.EdgeAptitude * amount;

                // Mezcla ideal basada en fracciones de masa:
                // densidad = 1 / sum(fracciónMásica / densidad).
                if (materialStats.Density <= 0f)
                {
                    canCalculateDensity = false;
                }
                else
                {
                    densityDenominator +=
                        (amount / materialStats.Density);
                }
            }

            if (totalAmount <= 0f)
            {
                throw new InvalidOperationException(
                    $"AlloyRegistry: la aleación " +
                    $"'{definition.AlloyName}' " +
                    "tiene una cantidad total inválida."
                );
            }

            float density = 0f;

            if (canCalculateDensity &&
                densityDenominator > 0f)
            {
                density = totalAmount / densityDenominator;
            }

            return new MaterialStats(
                density,
                hardness / totalAmount,
                resistance / totalAmount,
                toughness / totalAmount,
                definition.MeltingTemperature,
                definition.WorkingTemperature,
                edgeRetention / totalAmount,
                edgeAptitude / totalAmount
            );
        }

        public AlloyDefinition GetDefinition(ushort alloyId)
        {
            if (alloyId == 0)
            {
                return null;
            }

            if (definitionLookup == null)
            {
                Initialize();
            }

            return definitionLookup.TryGetValue(
                alloyId,
                out AlloyDefinition definition)
                ? definition
                : null;
        }

        public bool TryGetDefinition(
            ushort alloyId,
            out AlloyDefinition definition)
        {
            if (alloyId == 0)
            {
                definition = null;
                return false;
            }

            if (definitionLookup == null)
            {
                Initialize();
            }

            return definitionLookup.TryGetValue(
                alloyId,
                out definition
            );
        }

        public AlloyRuntimeData GetRuntimeData(ushort alloyId)
        {
            if (alloyId == 0)
            {
                return default;
            }

            if (runtimeData == null)
            {
                Initialize();
            }

            if (alloyId >= runtimeData.Length)
            {
                return default;
            }

            return runtimeData[alloyId];
        }

        public bool TryGetRuntimeData(
            ushort alloyId,
            out AlloyRuntimeData data)
        {
            if (alloyId == 0)
            {
                data = default;
                return false;
            }

            if (runtimeData == null)
            {
                Initialize();
            }

            if (alloyId >= runtimeData.Length)
            {
                data = default;
                return false;
            }

            data = runtimeData[alloyId];

            return data.Id != 0;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ValidateDefinitions();
        }

        private void ValidateDefinitions()
        {
            HashSet<ushort> usedIds = new HashSet<ushort>();

            foreach (AlloyDefinition definition in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                if (definition.Id == 0)
                {
                    Debug.LogError(
                        $"Alloy '{definition.name}' utiliza ID 0. " +
                        "ID 0 está reservado.",
                        this
                    );
                }

                if (!usedIds.Add(definition.Id))
                {
                    Debug.LogError(
                        $"AlloyRegistry contiene ID duplicado: " +
                        $"{definition.Id}.",
                        this
                    );
                }
            }
        }
#endif
    }
}