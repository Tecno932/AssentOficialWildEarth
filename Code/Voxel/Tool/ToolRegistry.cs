using System;
using System.Collections.Generic;
using UnityEngine;

namespace WildEarth.Voxel
{
[CreateAssetMenu(
fileName = "ToolRegistry",
menuName = "WildEarth/Voxel/Tool Registry"
)]
    public sealed class ToolRegistry : ScriptableObject
    {
    [Serializable]
    private struct ToolTypeBaseStats
    {
    public ToolType ToolType;
    public ToolBaseStats Stats;
    }

    [SerializeField]
    private List<ToolDefinition> definitions =
        new List<ToolDefinition>();

    [Header("Base Stats By Tool Type")]
    [SerializeField]
    private List<ToolTypeBaseStats> baseStatsByType =
        new List<ToolTypeBaseStats>();

    [Header("Dependencies")]
    [SerializeField]
    private MaterialRegistry materialRegistry;

    [SerializeField]
    private AlloyRegistry alloyRegistry;

    private Dictionary<ushort, ToolDefinition> definitionLookup;
    private Dictionary<ToolType, ToolBaseStats> baseStatsLookup;
    private ToolRuntimeData[] runtimeData;

    public int Count =>
        definitions != null ? definitions.Count : 0;

    public void Initialize()
    {
        if (materialRegistry == null)
        {
            throw new InvalidOperationException(
                "[ToolRegistry] MaterialRegistry is not assigned."
            );
        }

        if (alloyRegistry == null)
        {
            throw new InvalidOperationException(
                "[ToolRegistry] AlloyRegistry is not assigned."
            );
        }

        materialRegistry.Initialize();
        alloyRegistry.Initialize();

        BuildBaseStatsLookup();
        BuildLookup();
        BuildRuntimeData();
    }

    public ToolDefinition GetDefinition(ushort id)
    {
        if (definitionLookup == null)
            Initialize();

        if (!definitionLookup.TryGetValue(
                id,
                out ToolDefinition definition))
        {
            throw new KeyNotFoundException(
                $"[ToolRegistry] Tool ID {id} was not found."
            );
        }

        return definition;
    }

    public bool TryGetDefinition(
        ushort id,
        out ToolDefinition definition)
    {
        if (definitionLookup == null)
            Initialize();

        return definitionLookup.TryGetValue(
            id,
            out definition
        );
    }

    public ToolRuntimeData GetRuntimeData(ushort id)
    {
        if (runtimeData == null)
            Initialize();

        if (id >= runtimeData.Length ||
            !runtimeData[id].IsValid)
        {
            throw new KeyNotFoundException(
                $"[ToolRegistry] Runtime data for tool ID {id} was not found."
            );
        }

        return runtimeData[id];
    }

    public bool TryGetRuntimeData(
        ushort id,
        out ToolRuntimeData data)
    {
        if (runtimeData == null)
            Initialize();

        if (id >= runtimeData.Length)
        {
            data = default;
            return false;
        }

        data = runtimeData[id];
        return data.IsValid;
    }

    public ToolRuntimeData[] CreateNativeRuntimeData()
    {
        if (runtimeData == null)
            Initialize();

        return runtimeData;
    }

    private void BuildBaseStatsLookup()
    {
        baseStatsLookup =
            new Dictionary<ToolType, ToolBaseStats>();

        if (baseStatsByType == null)
            return;

        foreach (ToolTypeBaseStats entry in baseStatsByType)
        {
            if (entry.ToolType == ToolType.None)
                continue;

            if (baseStatsLookup.ContainsKey(entry.ToolType))
            {
                throw new InvalidOperationException(
                    $"[ToolRegistry] Duplicate base stats for tool type {entry.ToolType}."
                );
            }

            baseStatsLookup.Add(
                entry.ToolType,
                entry.Stats
            );
        }
    }

    private ToolBaseStats GetBaseStats(ToolType toolType)
    {
        if (baseStatsLookup != null &&
            baseStatsLookup.TryGetValue(
                toolType,
                out ToolBaseStats stats))
        {
            return stats;
        }

        return default;
    }

    private void BuildLookup()
    {
        definitionLookup =
            new Dictionary<ushort, ToolDefinition>();

        if (definitions == null)
            return;

        foreach (ToolDefinition definition in definitions)
        {
            if (definition == null || definition.Id == 0)
                continue;

            if (definitionLookup.ContainsKey(definition.Id))
            {
                throw new InvalidOperationException(
                    $"[ToolRegistry] Duplicate tool ID {definition.Id}."
                );
            }

            definitionLookup.Add(
                definition.Id,
                definition
            );
        }
    }

    private void BuildRuntimeData()
    {
        ushort maxId = 0;

        if (definitions != null)
        {
            foreach (ToolDefinition definition in definitions)
            {
                if (definition != null &&
                    definition.Id > maxId)
                {
                    maxId = definition.Id;
                }
            }
        }

        runtimeData =
            new ToolRuntimeData[maxId + 1];

        if (definitions == null)
            return;

        foreach (ToolDefinition definition in definitions)
        {
            if (definition == null ||
                definition.Id == 0)
            {
                continue;
            }

            if (!definition.IsValid)
            {
                throw new InvalidOperationException(
                    $"[ToolRegistry] La herramienta " +
                    $"'{definition.ToolName}' (ID {definition.Id}) " +
                    "tiene una definición inválida. " +
                    "Revisa el tipo, la cabeza y el mango."
                );
            }

            ToolHeadDefinition headDefinition =
                definition.Head;

            HandleDefinition handleDefinition =
                definition.Handle;

            ToolPartDefinition head =
                headDefinition.ToToolPart();

            ToolPartDefinition handle =
                handleDefinition.ToToolPart();

            MaterialStats headStats =
                ResolveStats(head);

            MaterialStats handleStats =
                ResolveStats(handle);

            ToolBaseStats baseStats =
                GetBaseStats(definition.ToolType);

            float headVolume =
                headDefinition.VolumeCm3;

            float handleVolume =
                handleDefinition.VolumeCm3;

            float headWeight =
                CalculateWeightKg(
                    headStats.Density,
                    headVolume
                );

            float handleWeight =
                CalculateWeightKg(
                    handleStats.Density,
                    handleVolume
                );

            float headSpeedReduction =
                headDefinition.SpeedReduction;

            float handleSpeedReduction =
                handleDefinition.SpeedReduction;

            runtimeData[definition.Id] =
                new ToolRuntimeData
                {
                    Id = definition.Id,
                    ToolType = definition.ToolType,
                    Head = head,
                    Handle = handle,
                    HeadStats = headStats,
                    HandleStats = handleStats,

                    BaseDamage = baseStats.Damage,
                    BaseSpeed = baseStats.Speed,
                    BaseEfficiency = baseStats.Efficiency,
                    BaseHeadDurability = baseStats.HeadDurability,
                    BaseHandleDurability = baseStats.HandleDurability,

                    HeadVolume = headVolume,
                    HandleVolume = handleVolume,

                    HeadWeight = headWeight,
                    HandleWeight = handleWeight,
                    TotalWeight = headWeight + handleWeight,

                    HeadSpeedReduction = headSpeedReduction,
                    HandleSpeedReduction = handleSpeedReduction,
                    TotalSpeedReduction =
                        headSpeedReduction + handleSpeedReduction
                };
        }
    }

    private static float CalculateWeightKg(
        float densityKgPerM3,
        float volumeCm3)
    {
        if (densityKgPerM3 <= 0f || volumeCm3 <= 0f)
            return 0f;

        return densityKgPerM3 * volumeCm3 / 1000000f;
    }

    private MaterialStats ResolveStats(
        ToolPartDefinition part)
    {
        if (!part.IsValid)
        {
            throw new InvalidOperationException(
                "[ToolRegistry] Invalid tool part."
            );
        }

        MaterialStats baseStats;

        if (part.UsesMaterial)
        {
            MaterialRuntimeData material =
                materialRegistry.GetRuntimeData(
                    part.MaterialId
                );

            baseStats = material.Stats;
        }
        else
        {
            AlloyRuntimeData alloy =
                alloyRegistry.GetRuntimeData(
                    part.AlloyId
                );

            baseStats = alloy.Stats;
        }

        return part.ApplyQuality(baseStats);
    }

    private void OnValidate()
    {
    if (definitions != null)
    {
        HashSet<ushort> ids =
        new HashSet<ushort>();
            foreach (ToolDefinition definition in definitions)
            {
                if (definition == null ||
                    definition.Id == 0)
                {
                    continue;
                }

                if (!ids.Add(definition.Id))
                {
                    Debug.LogError(
                        $"[ToolRegistry] Duplicate tool ID {definition.Id}.",
                        this
                    );
                }
            }
        }

        if (baseStatsByType != null)
        {
            HashSet<ToolType> types =
                new HashSet<ToolType>();

            foreach (ToolTypeBaseStats entry in baseStatsByType)
            {
                if (entry.ToolType == ToolType.None)
                    continue;

                if (!types.Add(entry.ToolType))
                {
                    Debug.LogError(
                        $"[ToolRegistry] Duplicate base stats for tool type {entry.ToolType}.",
                        this
                    );
                }
            }
        }
    }
    }
}