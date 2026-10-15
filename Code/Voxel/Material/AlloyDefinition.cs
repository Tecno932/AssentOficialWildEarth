
using System.Collections.Generic;
using UnityEngine;

namespace WildEarth.Voxel
{
    [CreateAssetMenu(
        fileName = "AlloyDefinition",
        menuName = "WildEarth/Voxel/Alloy Definition"
    )]
    public sealed class AlloyDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        private ushort id;

        [SerializeField]
        private string alloyName;

        [Header("Composition")]
        [SerializeField]
        private List<AlloyComponent> components = new();

        [Header("Thermal Properties")]
        [SerializeField, Min(0f)]
        private float meltingTemperature;

        [SerializeField, Min(0f)]
        private float workingTemperature;

        public ushort Id => id;

        public string AlloyName => alloyName;

        public IReadOnlyList<AlloyComponent> Components =>
            components;

        public float MeltingTemperature =>
            meltingTemperature;

        public float WorkingTemperature =>
            workingTemperature;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(alloyName))
            {
                alloyName = name;
            }

            if (components == null)
            {
                components = new List<AlloyComponent>();
            }

            meltingTemperature =
                Mathf.Max(0f, meltingTemperature);

            workingTemperature =
                Mathf.Max(0f, workingTemperature);
        }
#endif
    }
}