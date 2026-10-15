using System;

namespace WildEarth.Voxel
{
    [Serializable]
    public struct MaterialStats
    {
        // Propiedades físicas
        public float Density;
        public float Hardness;
        public float Resistance;
        public float Toughness;
        public float MeltingTemperature;
        public float WorkingTemperature;

        // Propiedades relevantes para herramientas
        public float EdgeRetention;
        public float EdgeAptitude;

        // Constructor compatible con el código existente.
        public MaterialStats(
            float hardness,
            float resistance,
            float edgeRetention)
        {
            Density = 0f;
            Hardness = hardness;
            Resistance = resistance;
            Toughness = 0f;
            MeltingTemperature = 0f;
            WorkingTemperature = 0f;
            EdgeRetention = edgeRetention;
            EdgeAptitude = 0f;
        }

        public MaterialStats(
            float density,
            float hardness,
            float resistance,
            float toughness,
            float meltingTemperature,
            float workingTemperature,
            float edgeRetention,
            float edgeAptitude)
        {
            Density = density;
            Hardness = hardness;
            Resistance = resistance;
            Toughness = toughness;
            MeltingTemperature = meltingTemperature;
            WorkingTemperature = workingTemperature;
            EdgeRetention = edgeRetention;
            EdgeAptitude = edgeAptitude;
        }
    }
}