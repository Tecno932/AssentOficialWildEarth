using System;
using System.Collections.Generic;

namespace WildEarth.Voxel
{
    public sealed class FluidRegistry
    {
        private readonly List<FluidDefinition> definitions;

        private FluidRuntimeData[] runtimeData;
        private Dictionary<FluidType, int> lookup;

        private bool initialized;

        public int Count =>
            runtimeData?.Length ?? 0;

        public bool IsInitialized =>
            initialized;

        public FluidRegistry(
            IEnumerable<FluidDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(
                    nameof(definitions)
                );
            }

            this.definitions =
                new List<FluidDefinition>(
                    definitions
                );

            BuildLookup();
        }

        public FluidRegistry(
            FluidRegistryAsset asset)
            : this(
                asset != null
                    ? asset.Definitions
                    : throw new ArgumentNullException(
                        nameof(asset)
                    )
            )
        {
        }

        private void BuildLookup()
        {
            if (definitions.Count == 0)
            {
                throw new InvalidOperationException(
                    "El FluidRegistry no contiene fluidos."
                );
            }

            lookup =
                new Dictionary<FluidType, int>(
                    definitions.Count
                );

            runtimeData =
                new FluidRuntimeData[
                    definitions.Count
                ];

            for (int i = 0;
                 i < definitions.Count;
                 i++)
            {
                FluidDefinition definition =
                    definitions[i];

                if (definition == null)
                {
                    throw new InvalidOperationException(
                        $"La definición de fluido " +
                        $"en índice {i} es null."
                    );
                }

                ValidateDefinition(
                    definition
                );

                if (lookup.ContainsKey(
                        definition.Type))
                {
                    throw new InvalidOperationException(
                        $"El FluidRegistry contiene " +
                        $"el tipo duplicado: " +
                        $"{definition.Type}."
                    );
                }

                runtimeData[i] =
                    definition.ToRuntimeData();

                lookup.Add(
                    definition.Type,
                    i
                );
            }

            initialized = true;
        }

        private static void ValidateDefinition(
            FluidDefinition definition)
        {
            if (definition.Type ==
                FluidType.None)
            {
                throw new InvalidOperationException(
                    $"El fluido '{definition.FluidName}' " +
                    "no puede utilizar FluidType.None."
                );
            }

            if (definition.BlockId ==
                BlockIds.Air)
            {
                throw new InvalidOperationException(
                    $"El fluido '{definition.FluidName}' " +
                    "utiliza BlockId 0 (Air)."
                );
            }

            if (definition.MaxLevel == 0)
            {
                throw new InvalidOperationException(
                    $"El fluido '{definition.FluidName}' " +
                    "tiene MaxLevel inválido."
                );
            }

            if (definition.MaxLevel >
                FluidState.MaxLevel)
            {
                throw new InvalidOperationException(
                    $"El fluido '{definition.FluidName}' " +
                    "supera el nivel máximo permitido."
                );
            }
        }

        public void Initialize()
        {
            if (initialized)
                return;

            BuildLookup();
        }

        public FluidRuntimeData GetRuntimeData(
            FluidType type)
        {
            EnsureInitialized();

            if (!lookup.TryGetValue(
                    type,
                    out int index))
            {
                throw new KeyNotFoundException(
                    $"No existe un fluido registrado " +
                    $"para el tipo {type}."
                );
            }

            return runtimeData[index];
        }

        public bool TryGetRuntimeData(
            FluidType type,
            out FluidRuntimeData data)
        {
            EnsureInitialized();

            if (!lookup.TryGetValue(
                    type,
                    out int index))
            {
                data = default;
                return false;
            }

            data = runtimeData[index];

            return true;
        }

        public FluidRuntimeData GetRuntimeData(
            int index)
        {
            EnsureInitialized();

            if (index < 0 ||
                index >= runtimeData.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index)
                );
            }

            return runtimeData[index];
        }

        public FluidRuntimeData[] GetRuntimeDataArray()
        {
            EnsureInitialized();

            FluidRuntimeData[] result =
                new FluidRuntimeData[
                    runtimeData.Length
                ];

            Array.Copy(
                runtimeData,
                result,
                runtimeData.Length
            );

            return result;
        }

        private void EnsureInitialized()
        {
            if (!initialized)
                Initialize();
        }
    }
}