using System;
using UnityEngine;

namespace WildEarth.Voxel
{
    [CreateAssetMenu(
        fileName = "ToolDefinition",
        menuName = "WildEarth/Voxel/Tool Definition"
    )]
    public sealed class ToolDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        private ushort id;

        [SerializeField]
        private string toolName;

        [Header("Tool")]
        [SerializeField]
        private ToolType toolType =
            ToolType.None;

        [Header("Parts")]
        [SerializeField]
        private ToolHeadDefinition head;

        [SerializeField]
        private HandleDefinition handle;

        public ushort Id => id;
        public string ToolName => toolName;
        public ToolType ToolType => toolType;
        public ToolHeadDefinition Head => head;
        public HandleDefinition Handle => handle;

        public bool IsValid =>
            id != 0 &&
            toolType != ToolType.None &&
            head.IsValid &&
            handle.IsValid;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(toolName))
            {
                toolName = name;
            }

            if (toolType == ToolType.None)
            {
                head = default;
                handle = default;
            }
        }
#endif
    }
}