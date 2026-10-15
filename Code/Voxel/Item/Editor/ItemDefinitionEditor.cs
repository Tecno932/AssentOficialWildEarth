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

        private SerializedProperty blockDefinition;
        private SerializedProperty toolDefinition;
        private SerializedProperty materialDefinition;
        private SerializedProperty alloyDefinition;

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

            blockDefinition =
                serializedObject.FindProperty("blockDefinition");

            toolDefinition =
                serializedObject.FindProperty("toolDefinition");

            materialDefinition =
                serializedObject.FindProperty("materialDefinition");

            alloyDefinition =
                serializedObject.FindProperty("alloyDefinition");

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

            if (itemType == null)
            {
                EditorGUILayout.HelpBox(
                    "No se encontró la propiedad itemType en ItemDefinition.",
                    MessageType.Error
                );

                return;
            }

            ItemType currentType =
                (ItemType)itemType.enumValueIndex;

            DrawIdentity();

            switch (currentType)
            {
                case ItemType.Block:
                    DrawBlock();
                    break;

                case ItemType.Tool:
                    DrawTool();
                    break;

                case ItemType.Material:
                    DrawMaterial();
                    break;

                case ItemType.Weapon:
                    DrawWeapon();
                    break;
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

            DrawProperty(id, "ID");
            DrawProperty(itemName, "Item Name");
            DrawProperty(itemType, "Item Type");

            EditorGUILayout.Space(8);
        }

        private void DrawBlock()
        {
            EditorGUILayout.LabelField(
                "Block",
                EditorStyles.boldLabel
            );

            DrawProperty(
                blockDefinition,
                "Block Definition"
            );

            EditorGUILayout.Space(8);
        }

        private void DrawTool()
        {
            EditorGUILayout.LabelField(
                "Tool",
                EditorStyles.boldLabel
            );

            DrawProperty(
                toolDefinition,
                "Tool Definition"
            );

            DrawProperty(toolType, "Tool Type");
            DrawProperty(toolLevel, "Tool Level");
            DrawProperty(toolSpeed, "Legacy Tool Speed");

            EditorGUILayout.Space(8);
        }

        private void DrawMaterial()
        {
            EditorGUILayout.LabelField(
                "Material",
                EditorStyles.boldLabel
            );

            DrawProperty(
                materialDefinition,
                "Material Definition"
            );

            DrawProperty(
                alloyDefinition,
                "Alloy Definition"
            );

            EditorGUILayout.Space(8);
        }

        private void DrawWeapon()
        {
            EditorGUILayout.LabelField(
                "Weapon",
                EditorStyles.boldLabel
            );

            DrawProperty(weaponType, "Weapon Type");

            EditorGUILayout.Space(8);
        }

        private void DrawCombatStats()
        {
            EditorGUILayout.LabelField(
                "Legacy Combat Stats",
                EditorStyles.boldLabel
            );

            DrawProperty(damage, "Damage");
            DrawProperty(sharpness, "Sharpness (%)");

            EditorGUILayout.Space(8);
        }

        private void DrawDurability()
        {
            EditorGUILayout.LabelField(
                "Durability",
                EditorStyles.boldLabel
            );

            DrawProperty(maxDurability, "Max Durability");

            EditorGUILayout.Space(8);
        }

        private void DrawStacking()
        {
            EditorGUILayout.LabelField(
                "Stacking",
                EditorStyles.boldLabel
            );

            DrawProperty(maxStackSize, "Max Stack Size");

            EditorGUILayout.Space(8);
        }

        private void DrawVisual()
        {
            EditorGUILayout.LabelField(
                "Visual",
                EditorStyles.boldLabel
            );

            DrawProperty(icon, "Icon");
        }

        private static void DrawProperty(
            SerializedProperty property,
            string label)
        {
            if (property == null)
            {
                EditorGUILayout.HelpBox(
                    $"No se encontró la propiedad serializada '{label}'.",
                    MessageType.Warning
                );

                return;
            }

            EditorGUILayout.PropertyField(
                property,
                new GUIContent(label)
            );
        }
    }
}