using System;
using System.Collections.Generic;
using UnityEngine;

namespace WildEarth.Voxel
{
    public sealed class VoxelWorldRenderer : MonoBehaviour
    {
        [SerializeField]
        private Material defaultMaterial;

        /*
         * ============================================================
         * DEBUG
         * ============================================================
         *
         * Mantener en true mientras diagnosticamos el atlas.
         *
         * Una vez solucionado:
         *
         * DebugRendering = false;
         */
        [SerializeField]
        private bool debugRendering = true;

        /*
         * Evita llenar la Console con el mismo diagnóstico
         * en cada frame.
         */
        private bool debugMaterialPrinted;
        private readonly HashSet<ChunkCoordinate>
            debuggedChunks = new();

        private readonly Dictionary<
            ChunkCoordinate,
            ChunkMeshRenderer
        > renderers = new();

        private VoxelWorld world;
        private VoxelMeshBuilder meshBuilder;

        public int RenderedChunkCount =>
            renderers.Count;

        public void Initialize(
            VoxelWorld voxelWorld,
            VoxelMeshBuilder voxelMeshBuilder)
        {
            if (voxelWorld == null)
                throw new ArgumentNullException(
                    nameof(voxelWorld)
                );

            if (voxelMeshBuilder == null)
                throw new ArgumentNullException(
                    nameof(voxelMeshBuilder)
                );

            world = voxelWorld;
            meshBuilder = voxelMeshBuilder;

            if (debugRendering)
            {
                Debug.Log(
                    "[VoxelRendererDebug] " +
                    "VoxelWorldRenderer inicializado."
                );

                Debug.Log(
                    "[VoxelRendererDebug] " +
                    $"GameObject={gameObject.name}"
                );

                Debug.Log(
                    "[VoxelRendererDebug] " +
                    $"DefaultMaterial=" +
                    DescribeMaterial(defaultMaterial)
                );
            }
        }

        public void RenderCompletedChunks()
        {
            if (world == null)
                throw new InvalidOperationException(
                    "VoxelWorldRenderer no está inicializado."
                );

            IReadOnlyList<Chunk> completedChunks =
                world.Generator.CompletedChunks;

            if (debugRendering &&
                completedChunks.Count > 0)
            {
                Debug.Log(
                    "[VoxelRendererDebug] " +
                    $"Chunks completados recibidos: " +
                    $"{completedChunks.Count}"
                );
            }

            for (int i = 0; i < completedChunks.Count; i++)
            {
                Chunk chunk =
                    completedChunks[i];

                if (chunk == null)
                    continue;

                RenderChunk(chunk);
            }
        }

        public void RenderChunk(
            Chunk chunk)
        {
            if (chunk == null)
                throw new ArgumentNullException(
                    nameof(chunk)
                );

            if (world == null)
                throw new InvalidOperationException(
                    "VoxelWorldRenderer no está inicializado."
                );

            if (meshBuilder == null)
                throw new InvalidOperationException(
                    "VoxelWorldRenderer no tiene VoxelMeshBuilder."
                );

            /*
             * ========================================================
             * MATERIAL CHECK
             * ========================================================
             */

            ValidateDefaultMaterial(
                chunk
            );

            /*
             * ========================================================
             * BUILD MESH
             * ========================================================
             */

            ChunkMeshData meshData =
                meshBuilder.Build(
                    chunk,
                    world.Chunks
                );

            try
            {
                if (meshData == null)
                {
                    throw new InvalidOperationException(
                        "[VoxelRendererDebug] " +
                        $"VoxelMeshBuilder devolvió null " +
                        $"para {chunk.Coordinate}."
                    );
                }

                if (debugRendering &&
                    !debuggedChunks.Contains(
                        chunk.Coordinate))
                {
                    Debug.Log(
                        "[VoxelRendererDebug] " +
                        $"MeshData construido para " +
                        $"{chunk.Coordinate}: " +
                        $"Vertices={meshData.VertexCount}, " +
                        $"UVs={meshData.UVs.Count}, " +
                        $"Triangles={meshData.Triangles.Count}"
                    );

                    Debug.Log(
                        "[VoxelRendererDebug] " +
                        $"MeshData UV consistency: " +
                        $"{meshData.VertexCount == meshData.UVs.Count}"
                    );

                    DebugLogMeshDataUVs(
                        meshData
                    );
                }

                /*
                 * ====================================================
                 * CREATE / GET CHUNK RENDERER
                 * ====================================================
                 */

                ChunkMeshRenderer renderer =
                    GetOrCreateRenderer(
                        chunk
                    );

                if (renderer == null)
                {
                    throw new InvalidOperationException(
                        "[VoxelRendererDebug] " +
                        $"No se pudo crear ChunkMeshRenderer " +
                        $"para {chunk.Coordinate}."
                    );
                }

                /*
                 * ====================================================
                 * APPLY
                 * ====================================================
                 */

                renderer.Apply(
                    meshData,
                    defaultMaterial
                );

                /*
                 * ====================================================
                 * POST-APPLY DIAGNOSTICS
                 * ====================================================
                 */

                if (debugRendering &&
                    !debuggedChunks.Contains(
                        chunk.Coordinate))
                {
                    DebugLogRendererState(
                        chunk,
                        renderer
                    );

                    debuggedChunks.Add(
                        chunk.Coordinate
                    );
                }

                chunk.ClearNeedsMesh();

                chunk.SetState(
                    ChunkState.Ready
                );
            }
            finally
            {
                meshData.Dispose();
            }
        }

        public void RemoveChunk(
            ChunkCoordinate coordinate)
        {
            if (!renderers.TryGetValue(
                    coordinate,
                    out ChunkMeshRenderer renderer))
            {
                return;
            }

            if (renderer != null)
            {
                Destroy(
                    renderer.gameObject
                );
            }

            renderers.Remove(
                coordinate
            );

            debuggedChunks.Remove(
                coordinate
            );
        }

        public void Clear()
        {
            foreach (
                ChunkMeshRenderer renderer
                in renderers.Values)
            {
                if (renderer != null)
                {
                    Destroy(
                        renderer.gameObject
                    );
                }
            }

            renderers.Clear();

            debuggedChunks.Clear();
        }

        private ChunkMeshRenderer GetOrCreateRenderer(
            Chunk chunk)
        {
            if (renderers.TryGetValue(
                    chunk.Coordinate,
                    out ChunkMeshRenderer existing))
            {
                if (existing != null)
                    return existing;

                renderers.Remove(
                    chunk.Coordinate
                );
            }

            GameObject gameObject =
                new GameObject(
                    $"Chunk_{chunk.Coordinate}"
                );

            gameObject.transform.SetParent(
                transform,
                false
            );

            gameObject.transform.localPosition =
                new Vector3(
                    chunk.Coordinate.X *
                        VoxelConstants.ChunkSize,

                    chunk.Coordinate.Y *
                        VoxelConstants.ChunkSize,

                    chunk.Coordinate.Z *
                        VoxelConstants.ChunkSize
                );

            gameObject.transform.localRotation =
                Quaternion.identity;

            gameObject.transform.localScale =
                Vector3.one;

            ChunkMeshRenderer renderer =
                gameObject.AddComponent<ChunkMeshRenderer>();

            renderers.Add(
                chunk.Coordinate,
                renderer
            );

            if (debugRendering)
            {
                Debug.Log(
                    "[VoxelRendererDebug] " +
                    $"Renderer creado para " +
                    $"{chunk.Coordinate}. " +
                    $"LocalPosition=" +
                    $"{gameObject.transform.localPosition} " +
                    $"WorldPosition=" +
                    $"{gameObject.transform.position} " +
                    $"Scale=" +
                    $"{gameObject.transform.localScale}"
                );
            }

            return renderer;
        }

        private void ValidateDefaultMaterial(
            Chunk chunk)
        {
            if (defaultMaterial == null)
            {
                throw new InvalidOperationException(
                    "[VoxelRendererDebug] " +
                    $"Default Material es NULL. " +
                    $"No se puede renderizar " +
                    $"{chunk.Coordinate}."
                );
            }

            Shader shader =
                defaultMaterial.shader;

            if (shader == null)
            {
                throw new InvalidOperationException(
                    "[VoxelRendererDebug] " +
                    $"El material " +
                    $"'{defaultMaterial.name}' " +
                    "no tiene Shader."
                );
            }

            if (debugRendering &&
                !debugMaterialPrinted)
            {
                Debug.Log(
                    "[VoxelRendererDebug] " +
                    "========== MATERIAL =========="
                );

                Debug.Log(
                    "[VoxelRendererDebug] " +
                    $"Material: " +
                    $"{defaultMaterial.name}"
                );

                Debug.Log(
                    "[VoxelRendererDebug] " +
                    $"Shader: " +
                    $"{shader.name}"
                );

                Debug.Log(
                    "[VoxelRendererDebug] " +
                    $"Shader supported: " +
                    $"{shader.isSupported}"
                );

                Debug.Log(
                    "[VoxelRendererDebug] " +
                    $"Has _BaseMap: " +
                    $"{defaultMaterial.HasProperty("_BaseMap")}"
                );

                Debug.Log(
                    "[VoxelRendererDebug] " +
                    $"Has _MainTex: " +
                    $"{defaultMaterial.HasProperty("_MainTex")}"
                );

                Texture baseMap =
                    defaultMaterial.HasProperty("_BaseMap")
                        ? defaultMaterial.GetTexture(
                            "_BaseMap"
                        )
                        : null;

                Texture mainTex =
                    defaultMaterial.HasProperty("_MainTex")
                        ? defaultMaterial.GetTexture(
                            "_MainTex"
                        )
                        : null;

                Debug.Log(
                    "[VoxelRendererDebug] " +
                    $"_BaseMap: " +
                    DescribeTexture(baseMap)
                );

                Debug.Log(
                    "[VoxelRendererDebug] " +
                    $"_MainTex: " +
                    DescribeTexture(mainTex)
                );

                Debug.Log(
                    "[VoxelRendererDebug] " +
                    $"mainTexture: " +
                    DescribeTexture(
                        defaultMaterial.mainTexture
                    )
                );

                if (baseMap == null &&
                    mainTex == null &&
                    defaultMaterial.mainTexture == null)
                {
                    Debug.LogError(
                        "[VoxelRendererDebug] " +
                        "EL MATERIAL NO TIENE NINGUNA " +
                        "TEXTURA ASIGNADA."
                    );
                }

                debugMaterialPrinted = true;

                Debug.Log(
                    "[VoxelRendererDebug] " +
                    "=============================="
                );
            }
        }

private static void DebugLogMeshDataUVs(
    ChunkMeshData meshData)
{
    if (meshData.UVs.Count == 0)
    {
        /*
         * Un ChunkMeshData vacío es válido.
         *
         * Si no existen vértices tampoco deben existir UVs.
         * No debe registrarse como Error porque los chunks
         * completamente vacíos producen exactamente este estado.
         */
        if (meshData.VertexCount == 0 &&
            meshData.Triangles.Count == 0)
        {
            Debug.Log(
                "[VoxelRendererDebug] " +
                "ChunkMeshData vacío: " +
                "no contiene vértices, triángulos ni UVs."
            );

            return;
        }

        Debug.LogError(
            "[VoxelRendererDebug] " +
            "ChunkMeshData tiene vértices/triángulos " +
            "pero NO contiene UVs."
        );

        return;
    }

    if (meshData.VertexCount !=
        meshData.UVs.Count)
    {
        Debug.LogError(
            "[VoxelRendererDebug] " +
            $"Cantidad de UVs inconsistente: " +
            $"Vertices={meshData.VertexCount}, " +
            $"UVs={meshData.UVs.Count}."
        );
    }

    int count =
        Mathf.Min(
            meshData.UVs.Count,
            12
        );

    for (int i = 0; i < count; i++)
    {
        Vector2 uv =
            new Vector2(
                meshData.UVs[i].x,
                meshData.UVs[i].y
            );

        bool valid =
            uv.x >= 0f &&
            uv.x <= 1f &&
            uv.y >= 0f &&
            uv.y <= 1f;

        Debug.Log(
            "[VoxelRendererDebug] " +
            $"UV[{i}]={uv} " +
            $"Valid01={valid}"
        );
    }

    /*
     * Busca cualquier UV fuera de 0..1.
     */
    for (int i = 0;
         i < meshData.UVs.Count;
         i++)
    {
        Vector2 uv =
            new Vector2(
                meshData.UVs[i].x,
                meshData.UVs[i].y
            );

        if (uv.x < 0f ||
            uv.x > 1f ||
            uv.y < 0f ||
            uv.y > 1f)
        {
            Debug.LogError(
                "[VoxelRendererDebug] " +
                $"UV FUERA DE RANGO: " +
                $"index={i}, UV={uv}"
            );

            break;
        }
    }
}

        private static void DebugLogRendererState(
            Chunk chunk,
            ChunkMeshRenderer renderer)
        {
            Debug.Log(
                "[VoxelRendererDebug] " +
                "========== CHUNK RENDER =========="
            );

            Debug.Log(
                "[VoxelRendererDebug] " +
                $"Chunk: {chunk.Coordinate}"
            );

            GameObject go =
                renderer.gameObject;

            Debug.Log(
                "[VoxelRendererDebug] " +
                $"GameObject: {go.name}"
            );

            Debug.Log(
                "[VoxelRendererDebug] " +
                $"ActiveSelf: {go.activeSelf}"
            );

            Debug.Log(
                "[VoxelRendererDebug] " +
                $"ActiveInHierarchy: " +
                $"{go.activeInHierarchy}"
            );

            Debug.Log(
                "[VoxelRendererDebug] " +
                $"Renderer enabled: " +
                $"{renderer.enabled}"
            );

            MeshFilter meshFilter =
                renderer.GetComponent<MeshFilter>();

            MeshRenderer meshRenderer =
                renderer.GetComponent<MeshRenderer>();

            if (meshFilter == null)
            {
                Debug.LogError(
                    "[VoxelRendererDebug] " +
                    "MeshFilter NO EXISTE."
                );
            }
            else
            {
                Mesh mesh =
                    meshFilter.sharedMesh;

                if (mesh == null)
                {
                    Debug.LogError(
                        "[VoxelRendererDebug] " +
                        "MeshFilter.sharedMesh es NULL."
                    );
                }
                else
                {
                    Debug.Log(
                        "[VoxelRendererDebug] " +
                        $"Mesh: {mesh.name}"
                    );

                    Debug.Log(
                        "[VoxelRendererDebug] " +
                        $"Mesh vertices: " +
                        $"{mesh.vertexCount}"
                    );

                    Debug.Log(
                        "[VoxelRendererDebug] " +
                        $"Mesh triangles: " +
                        $"{mesh.triangles.Length}"
                    );

                    Debug.Log(
                        "[VoxelRendererDebug] " +
                        $"Mesh UV count: " +
                        $"{mesh.uv.Length}"
                    );

                    Debug.Log(
                        "[VoxelRendererDebug] " +
                        $"Mesh bounds: " +
                        $"{mesh.bounds}"
                    );

                    DebugLogUnityMeshUVs(
                        mesh
                    );
                }
            }

            if (meshRenderer == null)
            {
                Debug.LogError(
                    "[VoxelRendererDebug] " +
                    "MeshRenderer NO EXISTE."
                );
            }
            else
            {
                Material appliedMaterial =
                    meshRenderer.sharedMaterial;

                Debug.Log(
                    "[VoxelRendererDebug] " +
                    $"MeshRenderer.sharedMaterial: " +
                    $"{DescribeMaterial(appliedMaterial)}"
                );

                if (appliedMaterial == null)
                {
                    Debug.LogError(
                        "[VoxelRendererDebug] " +
                        "MeshRenderer.sharedMaterial " +
                        "ES NULL."
                    );
                }
                else
                {
                    Debug.Log(
                        "[VoxelRendererDebug] " +
                        $"Applied shader: " +
                        $"{appliedMaterial.shader?.name}"
                    );

                    Debug.Log(
                        "[VoxelRendererDebug] " +
                        $"Applied texture: " +
                        $"{DescribeTexture(appliedMaterial.mainTexture)}"
                    );
                }
            }

            Debug.Log(
                "[VoxelRendererDebug] " +
                $"LocalPosition: " +
                $"{go.transform.localPosition}"
            );

            Debug.Log(
                "[VoxelRendererDebug] " +
                $"WorldPosition: " +
                $"{go.transform.position}"
            );

            Debug.Log(
                "[VoxelRendererDebug] " +
                $"Rotation: " +
                $"{go.transform.rotation.eulerAngles}"
            );

            Debug.Log(
                "[VoxelRendererDebug] " +
                $"Scale: " +
                $"{go.transform.lossyScale}"
            );

            Debug.Log(
                "[VoxelRendererDebug] " +
                "=================================="
            );
        }

        private static void DebugLogUnityMeshUVs(
            Mesh mesh)
        {
            if (mesh == null)
            {
                Debug.LogError(
                    "[VoxelRendererDebug] " +
                    "El Mesh de Unity es NULL."
                );

                return;
            }

            Vector2[] uvs =
                mesh.uv;

            /*
            * Un mesh vacío es válido.
            *
            * Si no tiene vértices, no necesita UVs.
            */
            if (mesh.vertexCount == 0 &&
                mesh.triangles.Length == 0 &&
                (uvs == null || uvs.Length == 0))
            {
                Debug.Log(
                    "[VoxelRendererDebug] " +
                    "Unity Mesh vacío: " +
                    "no contiene vértices, triángulos ni UVs."
                );

                return;
            }

            /*
            * Un mesh con geometría sí debe tener UVs.
            */
            if (uvs == null ||
                uvs.Length == 0)
            {
                Debug.LogError(
                    "[VoxelRendererDebug] " +
                    "El Mesh de Unity tiene geometría " +
                    "pero NO tiene UVs."
                );

                return;
            }

            if (mesh.vertexCount != uvs.Length)
            {
                Debug.LogError(
                    "[VoxelRendererDebug] " +
                    $"Cantidad de UVs inconsistente: " +
                    $"Vertices={mesh.vertexCount}, " +
                    $"UVs={uvs.Length}."
                );
            }

            int count =
                Mathf.Min(
                    uvs.Length,
                    12
                );

            for (int i = 0; i < count; i++)
            {
                Debug.Log(
                    "[VoxelRendererDebug] " +
                    $"UnityMesh UV[{i}]=" +
                    $"{uvs[i]}"
                );
            }
        }

        private static string DescribeMaterial(
            Material material)
        {
            if (material == null)
                return "NULL";

            Shader shader =
                material.shader;

            return
                $"'{material.name}' " +
                $"Shader='{shader?.name ?? "NULL"}'";
        }

        private static string DescribeTexture(
            Texture texture)
        {
            if (texture == null)
                return "NULL";

            return
                $"'{texture.name}' " +
                $"Type={texture.GetType().Name} " +
                $"Size={texture.width}x{texture.height}";
        }

        private void OnDestroy()
        {
            Clear();

            world = null;
            meshBuilder = null;
        }
    }
}