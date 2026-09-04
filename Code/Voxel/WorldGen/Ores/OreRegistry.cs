using System;
using System.Collections.Generic;
using Unity.Collections;

namespace WildEarth.Voxel
{
    public sealed class OreRegistry
    {
        private readonly List<OreDefinition> definitions;

        private OreRuntimeData[] runtimeData;
        private ushort[] hostBlockIds;

        private bool initialized;

        public int Count =>
            runtimeData?.Length ?? 0;

        public bool IsInitialized =>
            initialized;

        public OreRegistry(
            IEnumerable<OreDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(
                    nameof(definitions)
                );
            }

            this.definitions =
                new List<OreDefinition>(
                    definitions
                );

            BuildLookup();
        }

        private void BuildLookup()
        {
            if (definitions.Count == 0)
            {
                throw new InvalidOperationException(
                    "El OreRegistry no contiene minerales."
                );
            }

            int totalHostBlocks = 0;

            for (int i = 0;
                 i < definitions.Count;
                 i++)
            {
                OreDefinition definition =
                    definitions[i];

                if (definition == null)
                {
                    throw new InvalidOperationException(
                        $"La definición de mineral " +
                        $"en índice {i} es null."
                    );
                }

                ValidateDefinition(
                    definition
                );

                totalHostBlocks +=
                    definition.HostBlockIds.Length;
            }

            runtimeData =
                new OreRuntimeData[
                    definitions.Count
                ];

            hostBlockIds =
                new ushort[
                    totalHostBlocks
                ];

            int hostOffset = 0;

            for (int i = 0;
                 i < definitions.Count;
                 i++)
            {
                OreDefinition definition =
                    definitions[i];

                ushort[] hosts =
                    definition.HostBlockIds;

                runtimeData[i] =
                    definition.ToRuntimeData();

                runtimeData[i].HostBlockStart =
                    hostOffset;

                runtimeData[i].HostBlockCount =
                    hosts.Length;

                for (int j = 0;
                     j < hosts.Length;
                     j++)
                {
                    hostBlockIds[
                        hostOffset + j
                    ] = hosts[j];
                }

                hostOffset += hosts.Length;
            }

            initialized = true;
        }

        private static void ValidateDefinition(
            OreDefinition definition)
        {
            if (definition.BlockId ==
                BlockIds.Air)
            {
                throw new InvalidOperationException(
                    $"El mineral '{definition.OreName}' " +
                    $"utiliza BlockId 0 (Air)."
                );
            }

            if (definition.MinY < 0)
            {
                throw new InvalidOperationException(
                    $"El mineral '{definition.OreName}' " +
                    $"tiene un MinY inválido."
                );
            }

            if (definition.MaxY <
                definition.MinY)
            {
                throw new InvalidOperationException(
                    $"El mineral '{definition.OreName}' " +
                    $"tiene un rango Y inválido."
                );
            }

            if (definition.MinVeinSize <= 0)
            {
                throw new InvalidOperationException(
                    $"El mineral '{definition.OreName}' " +
                    $"tiene un tamaño mínimo de veta inválido."
                );
            }

            if (definition.MaxVeinSize <
                definition.MinVeinSize)
            {
                throw new InvalidOperationException(
                    $"El mineral '{definition.OreName}' " +
                    $"tiene un tamaño máximo de veta inválido."
                );
            }

            if (definition.Rarity <= 0f)
            {
                throw new InvalidOperationException(
                    $"El mineral '{definition.OreName}' " +
                    $"tiene una rareza inválida."
                );
            }

            if (definition.Frequency <= 0f)
            {
                throw new InvalidOperationException(
                    $"El mineral '{definition.OreName}' " +
                    $"tiene una frecuencia inválida."
                );
            }

            ushort[] hosts =
                definition.HostBlockIds;

            if (hosts == null ||
                hosts.Length == 0)
            {
                throw new InvalidOperationException(
                    $"El mineral '{definition.OreName}' " +
                    $"no tiene bloques huésped."
                );
            }

            HashSet<ushort> uniqueHosts =
                new HashSet<ushort>();

            for (int i = 0;
                 i < hosts.Length;
                 i++)
            {
                ushort hostId = hosts[i];

                if (hostId == BlockIds.Air)
                {
                    throw new InvalidOperationException(
                        $"El mineral '{definition.OreName}' " +
                        $"intenta usar Air como roca huésped."
                    );
                }

                if (!uniqueHosts.Add(hostId))
                {
                    throw new InvalidOperationException(
                        $"El mineral '{definition.OreName}' " +
                        $"contiene el bloque huésped " +
                        $"{hostId} duplicado."
                    );
                }
            }
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            BuildLookup();
        }

        public OreRuntimeData GetRuntimeData(
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

        public OreRuntimeData GetRuntimeData(
            int index,
            NativeArray<ushort> nativeHostBlocks)
        {
            EnsureInitialized();

            if (index < 0 ||
                index >= runtimeData.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index)
                );
            }

            OreRuntimeData result =
                runtimeData[index];

            int start =
                result.HostBlockStart;

            for (int i = 0;
                 i < result.HostBlockCount;
                 i++)
            {
                nativeHostBlocks[
                    start + i
                ] = hostBlockIds[
                    start + i
                ];
            }

            return result;
        }

        public int GetTotalHostBlockCount()
        {
            EnsureInitialized();

            return hostBlockIds.Length;
        }

        public bool IsHostBlock(
            int oreIndex,
            ushort blockId)
        {
            EnsureInitialized();

            OreRuntimeData ore =
                GetRuntimeData(oreIndex);

            int start =
                ore.HostBlockStart;

            int end =
                start + ore.HostBlockCount;

            for (int i = start;
                 i < end;
                 i++)
            {
                if (hostBlockIds[i] == blockId)
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsureInitialized()
        {
            if (!initialized)
            {
                Initialize();
            }
        }

        public OreRegistry(
            OreRegistryAsset asset)
            : this(
                asset != null
                    ? asset.Definitions
                    : throw new ArgumentNullException(
                        nameof(asset)
                    )
            )
        {
        }
    }
}