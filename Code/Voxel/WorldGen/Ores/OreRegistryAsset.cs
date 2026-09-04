using System.Collections.Generic;
using UnityEngine;

namespace WildEarth.Voxel
{
    [CreateAssetMenu(
        fileName = "OreRegistry",
        menuName = "WildEarth/Voxel/Ore Registry"
    )]
    public sealed class OreRegistryAsset : ScriptableObject
    {
        [Header("Ore Definitions")]
        [SerializeField]
        private List<OreDefinition> definitions =
            new List<OreDefinition>();

        public IReadOnlyList<OreDefinition> Definitions =>
            definitions;

#if UNITY_EDITOR

        private void OnValidate()
        {
            ValidateDefinitions();
        }

        private void ValidateDefinitions()
        {
            HashSet<ushort> usedBlockIds =
                new HashSet<ushort>();

            for (int i = 0;
                 i < definitions.Count;
                 i++)
            {
                OreDefinition definition =
                    definitions[i];

                if (definition == null)
                {
                    continue;
                }

                if (!usedBlockIds.Add(
                        definition.BlockId))
                {
                    Debug.LogError(
                        $"El OreRegistry '{name}' " +
                        $"contiene múltiples minerales " +
                        $"con BlockId {definition.BlockId}.",
                        this
                    );
                }
            }
        }

#endif
    }
}