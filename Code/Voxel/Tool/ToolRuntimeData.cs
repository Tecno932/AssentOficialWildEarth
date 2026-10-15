using System;

namespace WildEarth.Voxel
{
[Serializable]
public struct ToolRuntimeData
    {
    public ushort Id;
    public ToolType ToolType;

        public ToolPartDefinition Head;
        public ToolPartDefinition Handle;

        public MaterialStats HeadStats;
        public MaterialStats HandleStats;

        public float BaseDamage;
        public float BaseSpeed;
        public float BaseEfficiency;
        public float BaseHeadDurability;
        public float BaseHandleDurability;

        public float HeadVolume;
        public float HandleVolume;

        public float HeadWeight;
        public float HandleWeight;
        public float TotalWeight;

        public float HeadSpeedReduction;
        public float HandleSpeedReduction;
        public float TotalSpeedReduction;

        public bool IsValid =>
            Id != 0 &&
            ToolType != ToolType.None &&
            Head.IsValid &&
            Handle.IsValid;

        public float HeadHardness =>
            HeadStats.Hardness;

        public float HeadResistance =>
            HeadStats.Resistance;

        public float HeadToughness =>
            HeadStats.Toughness;

        public float EdgeRetention =>
            HeadStats.EdgeRetention;

        public float EdgeAptitude =>
            HeadStats.EdgeAptitude;

        public float HandleHardness =>
            HandleStats.Hardness;

        public float HandleResistance =>
            HandleStats.Resistance;

        public float HandleToughness =>
            HandleStats.Toughness;
    }
}
