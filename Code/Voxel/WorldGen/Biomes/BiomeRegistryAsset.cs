using System.Collections.Generic;
using UnityEngine;

namespace WildEarth.Voxel
{
    [CreateAssetMenu(
        fileName = "BiomeRegistry",
        menuName = "WildEarth/Voxel/Biome Registry"
    )]
    public sealed class BiomeRegistryAsset : ScriptableObject
    {
        [Header("Biomes")]
        [SerializeField]
        private List<BiomeDefinition> definitions =
            new List<BiomeDefinition>();

        public IReadOnlyList<BiomeDefinition> Definitions =>
            definitions;

#if UNITY_EDITOR

        private void OnValidate()
        {
            ValidateDefinitions();
        }

        private void ValidateDefinitions()
        {
            HashSet<BiomeId> usedIds =
                new HashSet<BiomeId>();

            for (int i = 0; i < definitions.Count; i++)
            {
                BiomeDefinition definition =
                    definitions[i];

                if (definition == null)
                    continue;

                if (!usedIds.Add(definition.Id))
                {
                    Debug.LogError(
                        $"El BiomeRegistry '{name}' contiene " +
                        $"el ID de bioma duplicado: {definition.Id}.",
                        this
                    );
                }
            }
        }

#endif
    }
}