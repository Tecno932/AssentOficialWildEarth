using UnityEngine;

namespace WildEarth.Voxel
{
    [CreateAssetMenu(
        fileName = "MaterialDefinition",
        menuName = "WildEarth/Voxel/Material Definition"
    )]
    public sealed class MaterialDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        private ushort id;

        [SerializeField]
        private string materialName;

        [Header("Physical Properties")]
        [SerializeField]
        private MaterialStats stats;

        public ushort Id => id;

        public string MaterialName => materialName;

        public MaterialStats Stats => stats;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(materialName))
            {
                materialName = name;
            }

            stats.Density = Mathf.Max(0f, stats.Density);
            stats.Hardness = Mathf.Max(0f, stats.Hardness);
            stats.Resistance = Mathf.Max(0f, stats.Resistance);
            stats.Toughness = Mathf.Max(0f, stats.Toughness);
            stats.MeltingTemperature =
                Mathf.Max(0f, stats.MeltingTemperature);
            stats.WorkingTemperature =
                Mathf.Max(0f, stats.WorkingTemperature);
            stats.EdgeRetention =
                Mathf.Max(0f, stats.EdgeRetention);
            stats.EdgeAptitude =
                Mathf.Max(0f, stats.EdgeAptitude);
        }
#endif
    }
}