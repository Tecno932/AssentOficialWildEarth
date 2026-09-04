using System;
using System.Collections.Generic;
using Unity.Collections;

namespace WildEarth.Voxel
{
    public sealed class BiomeRegistry
    {
        private readonly List<BiomeDefinition> definitions;

        private BiomeRuntimeData[] runtimeData;

        private bool initialized;

        public int Count =>
            runtimeData?.Length ?? 0;

        public bool IsInitialized =>
            initialized;

        public BiomeRegistry(
            IEnumerable<BiomeDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(
                    nameof(definitions)
                );
            }

            this.definitions =
                new List<BiomeDefinition>(
                    definitions
                );

            BuildLookup();
        }

        public BiomeRegistry(
            BiomeRegistryAsset asset)
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
                    "El BiomeRegistry no contiene biomas."
                );
            }

            int maximumId = 0;

            for (int i = 0; i < definitions.Count; i++)
            {
                BiomeDefinition definition =
                    definitions[i];

                if (definition == null)
                {
                    throw new InvalidOperationException(
                        $"La definición de bioma en índice {i} es null."
                    );
                }

                int id =
                    (int)definition.Id;

                if (id < 0)
                {
                    throw new InvalidOperationException(
                        $"El ID de bioma {id} no es válido."
                    );
                }

                if (id > maximumId)
                    maximumId = id;
            }

            runtimeData =
                new BiomeRuntimeData[
                    maximumId + 1
                ];

            bool[] used =
                new bool[
                    maximumId + 1
                ];

            for (int i = 0; i < definitions.Count; i++)
            {
                BiomeDefinition definition =
                    definitions[i];

                int id =
                    (int)definition.Id;

                if (used[id])
                {
                    throw new InvalidOperationException(
                        $"El ID de bioma {id} está duplicado."
                    );
                }

                used[id] = true;

                runtimeData[id] =
                    definition.ToRuntimeData();
            }

            ValidateRuntimeData();

            initialized = true;
        }

        private void ValidateRuntimeData()
        {
            if (runtimeData == null ||
                runtimeData.Length == 0)
            {
                throw new InvalidOperationException(
                    "El BiomeRegistry no contiene datos de runtime."
                );
            }

            for (int i = 0; i < runtimeData.Length; i++)
            {
                BiomeRuntimeData data =
                    runtimeData[i];

                if ((int)data.Id != i)
                {
                    throw new InvalidOperationException(
                        $"El BiomeRegistry tiene un hueco " +
                        $"en el ID de bioma {i}."
                    );
                }
            }
        }

        public void Initialize()
        {
            if (initialized)
                return;

            BuildLookup();
        }

        public BiomeDefinition GetDefinition(
            BiomeId id)
        {
            EnsureInitialized();

            int index =
                (int)id;

            if (index < 0 ||
                index >= runtimeData.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(id),
                    id,
                    "El ID de bioma está fuera del rango del registro."
                );
            }

            BiomeDefinition definition =
                GetDefinitionSafe(id);

            if (definition == null)
            {
                throw new InvalidOperationException(
                    $"No existe una definición para el bioma {id}."
                );
            }

            return definition;
        }

        public bool TryGetDefinition(
            BiomeId id,
            out BiomeDefinition definition)
        {
            EnsureInitialized();

            definition =
                GetDefinitionSafe(id);

            return definition != null;
        }

        private BiomeDefinition GetDefinitionSafe(
            BiomeId id)
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                BiomeDefinition definition =
                    definitions[i];

                if (definition != null &&
                    definition.Id == id)
                {
                    return definition;
                }
            }

            return null;
        }

        public BiomeRuntimeData GetRuntimeData(
            BiomeId id)
        {
            EnsureInitialized();

            int index =
                (int)id;

            if (index < 0 ||
                index >= runtimeData.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(id),
                    id,
                    "El ID de bioma está fuera del rango del registro."
                );
            }

            return runtimeData[index];
        }

        public bool TryGetRuntimeData(
            BiomeId id,
            out BiomeRuntimeData data)
        {
            EnsureInitialized();

            int index =
                (int)id;

            if (index < 0 ||
                index >= runtimeData.Length)
            {
                data = default;
                return false;
            }

            data =
                runtimeData[index];

            return data.Id == id;
        }

        public NativeArray<BiomeRuntimeData>
            CreateNativeRuntimeData(
                Allocator allocator)
        {
            EnsureInitialized();

            NativeArray<BiomeRuntimeData> nativeData =
                new NativeArray<BiomeRuntimeData>(
                    runtimeData.Length,
                    allocator,
                    NativeArrayOptions.UninitializedMemory
                );

            nativeData.CopyFrom(
                runtimeData
            );

            return nativeData;
        }

        private void EnsureInitialized()
        {
            if (initialized)
                return;

            Initialize();
        }
    }
}