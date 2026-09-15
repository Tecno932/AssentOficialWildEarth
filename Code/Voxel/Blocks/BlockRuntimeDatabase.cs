using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace WildEarth.Voxel
{
    public sealed class BlockRuntimeDatabase : IDisposable
    {
        private NativeArray<BlockRuntimeData> data;

        public bool IsCreated =>
            data.IsCreated;

        public int Length =>
            data.IsCreated
                ? data.Length
                : 0;

        public BlockRuntimeDatabase(
            BlockRegistry registry,
            Allocator allocator)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(
                    nameof(registry)
                );
            }

            Debug.Log(
                "[VoxelBlockDatabase] " +
                "=================================================="
            );

            Debug.Log(
                "[VoxelBlockDatabase] " +
                "      BLOCK RUNTIME DATABASE DIAGNOSTIC"
            );

            Debug.Log(
                "[VoxelBlockDatabase] " +
                "=================================================="
            );

            // ============================================================
            // REGISTRY
            // ============================================================

            Debug.Log(
                "[VoxelBlockDatabase] " +
                "========== REGISTRY =========="
            );

            Debug.Log(
                "[VoxelBlockDatabase] " +
                $"Registry Name={registry.name}"
            );

            Debug.Log(
                "[VoxelBlockDatabase] " +
                "Registry reference is valid."
            );

            Debug.Log(
                "[VoxelBlockDatabase] " +
                $"Registry Asset Type={registry.GetType().FullName}"
            );

            Debug.Log(
                "[VoxelBlockDatabase] " +
                $"Definitions.Count={registry.Definitions.Count}"
            );

            // ============================================================
            // SERIALIZED DEFINITIONS
            // ============================================================

            Debug.Log(
                "[VoxelBlockDatabase] " +
                "========== SERIALIZED DEFINITIONS =========="
            );

            HashSet<ushort> serializedIds =
                new HashSet<ushort>();

            int nullDefinitions = 0;
            int duplicateIds = 0;

            for (
                int i = 0;
                i < registry.Definitions.Count;
                i++
            )
            {
                BlockDefinition definition =
                    registry.Definitions[i];

                if (definition == null)
                {
                    nullDefinitions++;

                    Debug.LogWarning(
                        "[VoxelBlockDatabase] " +
                        $"Definition[{i}] = NULL"
                    );

                    continue;
                }

                ushort id =
                    definition.Id;

                bool duplicate =
                    !serializedIds.Add(id);

                if (duplicate)
                {
                    duplicateIds++;

                    Debug.LogError(
                        "[VoxelBlockDatabase] " +
                        $"DUPLICATE ID DETECTED: " +
                        $"Definition[{i}] " +
                        $"Name={definition.name} " +
                        $"ID={id}"
                    );
                }

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"Definition[{i}]"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  UnityName={definition.name}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  BlockName={definition.BlockName}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  ID={id}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  IDHex=0x{id:X4}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  MeshType={definition.MeshType}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  Flags={definition.Flags}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  IsSolid={definition.IsSolid}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  IsTransparent={definition.IsTransparent}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  IsFluid={definition.IsFluid}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  EmitsLight={definition.EmitsLight}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  OccludesFaces={definition.OccludesFaces}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  Hardness={definition.Hardness}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  LightEmission={definition.LightEmission}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  RequiredTool={definition.RequiredTool}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  RequiredToolLevel={definition.RequiredToolLevel}"
                );

                // ========================================================
                // TOP TEXTURE
                // ========================================================

                Debug.Log(
                    "[VoxelBlockTexture] " +
                    $"ID={id} Name={definition.BlockName} " +
                    "TOP"
                );

                Debug.Log(
                    "[VoxelBlockTexture] " +
                    $"  Row={definition.TopTexture.Row}"
                );

                Debug.Log(
                    "[VoxelBlockTexture] " +
                    $"  Column={definition.TopTexture.Column}"
                );

                Debug.Log(
                    "[VoxelBlockTexture] " +
                    $"  Tile=(" +
                    $"{definition.TopTexture.Column}," +
                    $"{definition.TopTexture.Row})"
                );

                // ========================================================
                // BOTTOM TEXTURE
                // ========================================================

                Debug.Log(
                    "[VoxelBlockTexture] " +
                    $"ID={id} Name={definition.BlockName} " +
                    "BOTTOM"
                );

                Debug.Log(
                    "[VoxelBlockTexture] " +
                    $"  Row={definition.BottomTexture.Row}"
                );

                Debug.Log(
                    "[VoxelBlockTexture] " +
                    $"  Column={definition.BottomTexture.Column}"
                );

                Debug.Log(
                    "[VoxelBlockTexture] " +
                    $"  Tile=(" +
                    $"{definition.BottomTexture.Column}," +
                    $"{definition.BottomTexture.Row})"
                );

                // ========================================================
                // SIDE TEXTURE
                // ========================================================

                Debug.Log(
                    "[VoxelBlockTexture] " +
                    $"ID={id} Name={definition.BlockName} " +
                    "SIDE"
                );

                Debug.Log(
                    "[VoxelBlockTexture] " +
                    $"  Row={definition.SideTexture.Row}"
                );

                Debug.Log(
                    "[VoxelBlockTexture] " +
                    $"  Column={definition.SideTexture.Column}"
                );

                Debug.Log(
                    "[VoxelBlockTexture] " +
                    $"  Tile=(" +
                    $"{definition.SideTexture.Column}," +
                    $"{definition.SideTexture.Row})"
                );

                // ========================================================
                // MATERIAL
                // ========================================================

                Material material =
                    definition.Material;

                if (material == null)
                {
                    Debug.Log(
                        "[VoxelBlockMaterial] " +
                        $"ID={id} " +
                        $"Name={definition.BlockName} " +
                        "Material=NULL"
                    );
                }
                else
                {
                    Debug.Log(
                        "[VoxelBlockMaterial] " +
                        $"ID={id} " +
                        $"Name={definition.BlockName} " +
                        $"Material={material.name}"
                    );

                    Debug.Log(
                        "[VoxelBlockMaterial] " +
                        $"Shader={(material.shader != null ? material.shader.name : "NULL")}"
                    );

                    Debug.Log(
                        "[VoxelBlockMaterial] " +
                        $"HasBaseMap={material.HasProperty("_BaseMap")}"
                    );

                    Debug.Log(
                        "[VoxelBlockMaterial] " +
                        $"HasMainTex={material.HasProperty("_MainTex")}"
                    );
                }

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    "------------------------------------------"
                );
            }

            Debug.Log(
                "[VoxelBlockDatabase] " +
                $"Serialized NullDefinitions={nullDefinitions}"
            );

            Debug.Log(
                "[VoxelBlockDatabase] " +
                $"Serialized DuplicateIDs={duplicateIds}"
            );

            Debug.Log(
                "[VoxelBlockDatabase] " +
                $"Serialized UniqueIDs={serializedIds.Count}"
            );

            // ============================================================
            // INITIALIZE
            // ============================================================

            Debug.Log(
                "[VoxelBlockDatabase] " +
                "========== INITIALIZE REGISTRY =========="
            );

            registry.Initialize();

            Debug.Log(
                "[VoxelBlockDatabase] " +
                $"Registry Count AFTER Initialize=" +
                $"{registry.Count}"
            );

            Debug.Log(
                "[VoxelBlockDatabase] " +
                $"Registry Definitions.Count AFTER Initialize=" +
                $"{registry.Definitions.Count}"
            );

            // ============================================================
            // RUNTIME DATA
            // ============================================================

            Debug.Log(
                "[VoxelBlockDatabase] " +
                "========== CREATE NATIVE DATA =========="
            );

            data =
                registry.CreateNativeRuntimeData(
                    allocator
                );

            Debug.Log(
                "[VoxelBlockDatabase] " +
                $"Runtime database creada."
            );

            Debug.Log(
                "[VoxelBlockDatabase] " +
                $"Runtime Length={data.Length}"
            );

            Debug.Log(
                "[VoxelBlockDatabase] " +
                $"Runtime IsCreated={data.IsCreated}"
            );

            // ============================================================
            // RUNTIME BLOCKS
            // ============================================================

            Debug.Log(
                "[VoxelBlockDatabase] " +
                "========== RUNTIME BLOCKS =========="
            );

            int runtimeAirCount = 0;
            int runtimeUnknownCount = 0;
            int runtimeMismatchCount = 0;

            for (
                int i = 0;
                i < data.Length;
                i++
            )
            {
                BlockRuntimeData block =
                    data[i];

                ushort runtimeId =
                    block.Id;

                BlockDefinition definition =
                    registry.GetDefinition(
                        runtimeId
                    );

                string blockName;

                if (runtimeId == BlockIds.Air)
                {
                    blockName = "Air";
                    runtimeAirCount++;
                }
                else if (definition != null)
                {
                    blockName =
                        definition.BlockName;
                }
                else
                {
                    blockName = "UNKNOWN";
                    runtimeUnknownCount++;
                }

                bool idMatchesIndex =
                    runtimeId == i;

                if (!idMatchesIndex)
                {
                    runtimeMismatchCount++;

                    Debug.LogError(
                        "[VoxelBlockDatabase] " +
                        $"RUNTIME ID MISMATCH: " +
                        $"Index={i} " +
                        $"RuntimeID={runtimeId}"
                    );
                }

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"Runtime[{i}]"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  Index={i}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  ID={runtimeId}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  IDHex=0x{runtimeId:X4}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  Name={blockName}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  DefinitionExists=" +
                    $"{definition != null}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  IndexMatchesID=" +
                    $"{idMatchesIndex}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  MeshType={block.MeshType}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  Flags={block.Flags}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  IsSolid={block.IsSolid}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  IsTransparent={block.IsTransparent}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  IsFluid={block.IsFluid}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  EmitsLight={block.EmitsLight}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  OccludesFaces={block.OccludesFaces}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  IsCollidable={block.IsCollidable}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  IsReplaceable={block.IsReplaceable}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  IsCutout={block.IsCutout}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  IsCaveCarvable={block.IsCaveCarvable}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  Hardness={block.Hardness}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  LightEmission={block.LightEmission}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  RequiredTool={block.RequiredTool}"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    $"  RequiredToolLevel={block.RequiredToolLevel}"
                );

                // ========================================================
                // RUNTIME TEXTURES
                // ========================================================

                Debug.Log(
                    "[VoxelBlockTexture] " +
                    $"Runtime ID={runtimeId} " +
                    $"Name={blockName} " +
                    "TOP"
                );

                Debug.Log(
                    "[VoxelBlockTexture] " +
                    $"  Row={block.TopTexture.Row}"
                );

                Debug.Log(
                    "[VoxelBlockTexture] " +
                    $"  Column={block.TopTexture.Column}"
                );

                Debug.Log(
                    "[VoxelBlockTexture] " +
                    $"  Tile=(" +
                    $"{block.TopTexture.Column}," +
                    $"{block.TopTexture.Row})"
                );

                Debug.Log(
                    "[VoxelBlockTexture] " +
                    $"Runtime ID={runtimeId} " +
                    $"Name={blockName} " +
                    "BOTTOM"
                );

                Debug.Log(
                    "[VoxelBlockTexture] " +
                    $"  Row={block.BottomTexture.Row}"
                );

                Debug.Log(
                    "[VoxelBlockTexture] " +
                    $"  Column={block.BottomTexture.Column}"
                );

                Debug.Log(
                    "[VoxelBlockTexture] " +
                    $"  Tile=(" +
                    $"{block.BottomTexture.Column}," +
                    $"{block.BottomTexture.Row})"
                );

                Debug.Log(
                    "[VoxelBlockTexture] " +
                    $"Runtime ID={runtimeId} " +
                    $"Name={blockName} " +
                    "SIDE"
                );

                Debug.Log(
                    "[VoxelBlockTexture] " +
                    $"  Row={block.SideTexture.Row}"
                );

                Debug.Log(
                    "[VoxelBlockTexture] " +
                    $"  Column={block.SideTexture.Column}"
                );

                Debug.Log(
                    "[VoxelBlockTexture] " +
                    $"  Tile=(" +
                    $"{block.SideTexture.Column}," +
                    $"{block.SideTexture.Row})"
                );

                Debug.Log(
                    "[VoxelBlockDatabase] " +
                    "------------------------------------------"
                );
            }

            // ============================================================
            // EXPECTED ID RANGE
            // ============================================================

            Debug.Log(
                "[VoxelBlockDatabase] " +
                "========== ID RANGE VALIDATION =========="
            );

            int expectedMaxId =
                registry.Definitions.Count > 0
                    ? GetMaxDefinitionId(registry)
                    : 0;

            Debug.Log(
                "[VoxelBlockDatabase] " +
                $"ExpectedMaxDefinitionID={expectedMaxId}"
            );

            Debug.Log(
                "[VoxelBlockDatabase] " +
                $"ExpectedRuntimeLength=" +
                $"{expectedMaxId + 1}"
            );

            Debug.Log(
                "[VoxelBlockDatabase] " +
                $"ActualRuntimeLength={data.Length}"
            );

            if (data.Length != expectedMaxId + 1)
            {
                Debug.LogError(
                    "[VoxelBlockDatabase] " +
                    $"RUNTIME LENGTH MISMATCH! " +
                    $"Expected={expectedMaxId + 1} " +
                    $"Actual={data.Length}"
                );
            }

            // ============================================================
            // SUMMARY
            // ============================================================

            Debug.Log(
                "[VoxelBlockDatabase] " +
                "========== SUMMARY =========="
            );

            Debug.Log(
                "[VoxelBlockDatabase] " +
                $"Registry={registry.name}"
            );

            Debug.Log(
                "[VoxelBlockDatabase] " +
                $"Definitions={registry.Definitions.Count}"
            );

            Debug.Log(
                "[VoxelBlockDatabase] " +
                $"UniqueSerializedIDs={serializedIds.Count}"
            );

            Debug.Log(
                "[VoxelBlockDatabase] " +
                $"RuntimeLength={data.Length}"
            );

            Debug.Log(
                "[VoxelBlockDatabase] " +
                $"ExpectedMaxID={expectedMaxId}"
            );

            Debug.Log(
                "[VoxelBlockDatabase] " +
                $"AirEntries={runtimeAirCount}"
            );

            Debug.Log(
                "[VoxelBlockDatabase] " +
                $"UnknownRuntimeEntries={runtimeUnknownCount}"
            );

            Debug.Log(
                "[VoxelBlockDatabase] " +
                $"RuntimeIDMismatches={runtimeMismatchCount}"
            );

            Debug.Log(
                "[VoxelBlockDatabase] " +
                $"NullDefinitions={nullDefinitions}"
            );

            Debug.Log(
                "[VoxelBlockDatabase] " +
                $"DuplicateSerializedIDs={duplicateIds}"
            );

            bool databaseLooksValid =
                data.IsCreated &&
                data.Length > 0 &&
                data.Length == expectedMaxId + 1 &&
                runtimeUnknownCount == 0 &&
                runtimeMismatchCount == 0;

            Debug.Log(
                "[VoxelBlockDatabase] " +
                $"DATABASE VALID={databaseLooksValid}"
            );

            Debug.Log(
                "[VoxelBlockDatabase] " +
                "=================================================="
            );
        }

        private static int GetMaxDefinitionId(
            BlockRegistry registry)
        {
            int maxId = 0;

            foreach (
                BlockDefinition definition
                in registry.Definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                if (definition.Id > maxId)
                {
                    maxId = definition.Id;
                }
            }

            return maxId;
        }

        public BlockRuntimeData Get(
            ushort blockId)
        {
            ThrowIfNotCreated();

            int index = blockId;

            if (
                index < 0 ||
                index >= data.Length)
            {
                Debug.LogError(
                    "[VoxelBlockDatabase] " +
                    $"GET INVALID BLOCK ID: " +
                    $"BlockId={blockId} " +
                    $"Length={data.Length}"
                );

                throw new ArgumentOutOfRangeException(
                    nameof(blockId)
                );
            }

            return data[index];
        }

        public bool TryGet(
            ushort blockId,
            out BlockRuntimeData block)
        {
            ThrowIfNotCreated();

            int index = blockId;

            if (
                index < 0 ||
                index >= data.Length)
            {
                block = default;

                Debug.LogWarning(
                    "[VoxelBlockDatabase] " +
                    $"TryGet failed: " +
                    $"BlockId={blockId} " +
                    $"Length={data.Length}"
                );

                return false;
            }

            block = data[index];

            bool valid =
                block.Id == blockId;

            if (!valid)
            {
                Debug.LogError(
                    "[VoxelBlockDatabase] " +
                    $"TryGet ID mismatch: " +
                    $"Requested={blockId} " +
                    $"Returned={block.Id}"
                );
            }

            return valid;
        }

        public NativeArray<BlockRuntimeData>
            AsNativeArray()
        {
            ThrowIfNotCreated();

            Debug.Log(
                "[VoxelBlockDatabase] " +
                $"AsNativeArray requested. " +
                $"Length={data.Length}"
            );

            return data;
        }

        public void Dispose()
        {
            if (!data.IsCreated)
            {
                return;
            }

            Debug.Log(
                "[VoxelBlockDatabase] " +
                $"Disposing runtime database. " +
                $"Length={data.Length}"
            );

            data.Dispose();
            data = default;
        }

        private void ThrowIfNotCreated()
        {
            if (!data.IsCreated)
            {
                throw new InvalidOperationException(
                    "BlockRuntimeDatabase no está inicializada."
                );
            }
        }
    }
}