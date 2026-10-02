#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace WildEarth.Voxel
{
    [CustomEditor(typeof(ItemDefinition))]
    public sealed class ItemDefinitionEditor : Editor
    {
        private SerializedProperty id;
        private SerializedProperty itemName;
        private SerializedProperty itemType;

        private SerializedProperty blockId;

        private SerializedProperty toolType;
        private SerializedProperty toolLevel;
        private SerializedProperty toolSpeed;

        private SerializedProperty weaponType;

        private SerializedProperty damage;
        private SerializedProperty sharpness;

        private SerializedProperty maxDurability;
        private SerializedProperty maxStackSize;

        private SerializedProperty icon;

        private void OnEnable()
        {
            id = serializedObject.FindProperty("id");
            itemName = serializedObject.FindProperty("itemName");
            itemType = serializedObject.FindProperty("itemType");

            blockId = serializedObject.FindProperty("blockId");

            toolType = serializedObject.FindProperty("toolType");
            toolLevel = serializedObject.FindProperty("toolLevel");
            toolSpeed = serializedObject.FindProperty("toolSpeed");

            weaponType = serializedObject.FindProperty("weaponType");

            damage = serializedObject.FindProperty("damage");
            sharpness = serializedObject.FindProperty("sharpness");

            maxDurability =
                serializedObject.FindProperty("maxDurability");

            maxStackSize =
                serializedObject.FindProperty("maxStackSize");

            icon = serializedObject.FindProperty("icon");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            ItemType currentType =
                (ItemType)itemType.enumValueIndex;

            DrawIdentity();

            if (currentType == ItemType.Block)
            {
                DrawBlock();
            }

            if (currentType == ItemType.Tool)
            {
                DrawTool();
            }

            if (currentType == ItemType.Weapon)
            {
                DrawWeapon();
            }

            if (currentType == ItemType.Tool ||
                currentType == ItemType.Weapon)
            {
                DrawCombatStats();
                DrawDurability();
            }

            DrawStacking();
            DrawVisual();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawIdentity()
        {
            EditorGUILayout.LabelField(
                "Identity",
                EditorStyles.boldLabel
            );

            EditorGUILayout.PropertyField(id);
            EditorGUILayout.PropertyField(itemName);
            EditorGUILayout.PropertyField(itemType);

            EditorGUILayout.Space(8);
        }

        private void DrawBlock()
        {
            EditorGUILayout.LabelField(
                "Block",
                EditorStyles.boldLabel
            );

            EditorGUILayout.PropertyField(
                blockId,
                new GUIContent("Block ID")
            );

            EditorGUILayout.Space(8);
        }

        private void DrawTool()
        {
            EditorGUILayout.LabelField(
                "Tool",
                EditorStyles.boldLabel
            );

            EditorGUILayout.PropertyField(
                toolType,
                new GUIContent("Tool Type")
            );

            EditorGUILayout.PropertyField(
                toolLevel,
                new GUIContent("Tool Level")
            );

            EditorGUILayout.PropertyField(
                toolSpeed,
                new GUIContent("Tool Speed")
            );

            EditorGUILayout.Space(8);
        }

        private void DrawWeapon()
        {
            EditorGUILayout.LabelField(
                "Weapon",
                EditorStyles.boldLabel
            );

            EditorGUILayout.PropertyField(
                weaponType,
                new GUIContent("Weapon Type")
            );

            EditorGUILayout.Space(8);
        }

        private void DrawCombatStats()
        {
            EditorGUILayout.LabelField(
                "Combat",
                EditorStyles.boldLabel
            );

            EditorGUILayout.PropertyField(
                damage,
                new GUIContent("Damage")
            );

            EditorGUILayout.PropertyField(
                sharpness,
                new GUIContent("Sharpness (%)")
            );

            EditorGUILayout.Space(8);
        }

        private void DrawDurability()
        {
            EditorGUILayout.LabelField(
                "Durability",
                EditorStyles.boldLabel
            );

            EditorGUILayout.PropertyField(
                maxDurability,
                new GUIContent("Max Durability")
            );

            EditorGUILayout.Space(8);
        }

        private void DrawStacking()
        {
            EditorGUILayout.LabelField(
                "Stacking",
                EditorStyles.boldLabel
            );

            EditorGUILayout.PropertyField(
                maxStackSize,
                new GUIContent("Max Stack Size")
            );

            EditorGUILayout.Space(8);
        }

        private void DrawVisual()
        {
            EditorGUILayout.LabelField(
                "Visual",
                EditorStyles.boldLabel
            );

            EditorGUILayout.PropertyField(
                icon,
                new GUIContent("Icon")
            );
        }
    }
}

#endif