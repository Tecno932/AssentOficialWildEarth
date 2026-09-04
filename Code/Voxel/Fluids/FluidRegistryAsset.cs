using System.Collections.Generic;
using UnityEngine;

namespace WildEarth.Voxel
{
    [CreateAssetMenu(
        fileName = "FluidRegistry",
        menuName = "WildEarth/Voxel/Fluid Registry"
    )]
    public sealed class FluidRegistryAsset : ScriptableObject
    {
        [Header("Fluids")]
        [SerializeField]
        private List<FluidDefinition> definitions =
            new List<FluidDefinition>();

        public IReadOnlyList<FluidDefinition> Definitions =>
            definitions;

#if UNITY_EDITOR

        private void OnValidate()
        {
            ValidateDefinitions();
        }

        private void ValidateDefinitions()
        {
            HashSet<FluidType> usedTypes =
                new HashSet<FluidType>();

            for (int i = 0;
                 i < definitions.Count;
                 i++)
            {
                FluidDefinition definition =
                    definitions[i];

                if (definition == null)
                    continue;

                if (!usedTypes.Add(definition.Type))
                {
                    Debug.LogError(
                        $"El FluidRegistry '{name}' " +
                        $"contiene el tipo de fluido duplicado: " +
                        $"{definition.Type}.",
                        this
                    );
                }
            }
        }

#endif
    }
}