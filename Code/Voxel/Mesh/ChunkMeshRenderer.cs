using UnityEngine;

namespace WildEarth.Voxel
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public sealed class ChunkMeshRenderer : MonoBehaviour
    {
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;
        private Mesh mesh;

        private Vector3[] verticesBuffer;
        private Vector2[] uvsBuffer;
        private Vector2[] tiledUVsBuffer;
        private Vector3[] normalsBuffer;
        private Color32[] colorsBuffer;

        public Mesh Mesh => mesh;

        private void Awake()
        {
            meshFilter =
                GetComponent<MeshFilter>();

            meshRenderer =
                GetComponent<MeshRenderer>();

            meshCollider =
                GetComponent<MeshCollider>();

            mesh =
                new Mesh
                {
                    name = "ChunkMesh"
                };

            mesh.MarkDynamic();

            meshFilter.sharedMesh = mesh;

            // El collider se asigna únicamente cuando
            // VoxelWorldRenderer lo solicita.
            meshCollider.sharedMesh = null;
        }

        public void Apply(
            ChunkMeshData meshData,
            Material material)
        {
            if (meshData == null)
            {
                throw new System.ArgumentNullException(
                    nameof(meshData)
                );
            }

            EnsureMesh();

            mesh.Clear();

            if (meshData.VertexCount > 0)
            {
                int vertexCount =
                    meshData.VertexCount;

                EnsureBuffers(vertexCount);

                for (
                    int i = 0;
                    i < vertexCount;
                    i++)
                {
                    verticesBuffer[i] =
                        new Vector3(
                            meshData.Vertices[i].x,
                            meshData.Vertices[i].y,
                            meshData.Vertices[i].z
                        );

                    uvsBuffer[i] =
                        new Vector2(
                            meshData.UVs[i].x,
                            meshData.UVs[i].y
                        );

                    tiledUVsBuffer[i] =
                        new Vector2(
                            meshData.TiledUVs[i].x,
                            meshData.TiledUVs[i].y
                        );

                    normalsBuffer[i] =
                        new Vector3(
                            meshData.Normals[i].x,
                            meshData.Normals[i].y,
                            meshData.Normals[i].z
                        );

                    colorsBuffer[i] =
                        meshData.Colors[i];
                }

                mesh.SetVertices(
                    verticesBuffer,
                    0,
                    vertexCount
                );

                mesh.SetTriangles(
                    meshData.Triangles,
                    0
                );

                mesh.SetUVs(
                    0,
                    uvsBuffer,
                    0,
                    vertexCount
                );

                mesh.SetUVs(
                    1,
                    tiledUVsBuffer,
                    0,
                    vertexCount
                );

                mesh.SetNormals(
                    normalsBuffer,
                    0,
                    vertexCount
                );

                mesh.SetColors(
                    colorsBuffer,
                    0,
                    vertexCount
                );

                mesh.RecalculateBounds();
            }

            meshRenderer.sharedMaterial =
                material;
        }

        public void ApplyCollider()
        {
            EnsureMesh();

            meshCollider.sharedMesh = null;

            if (mesh.vertexCount > 0)
            {
                meshCollider.sharedMesh = mesh;
            }
        }

        public void ClearCollider()
        {
            if (meshCollider == null)
            {
                meshCollider =
                    GetComponent<MeshCollider>();
            }

            meshCollider.sharedMesh = null;
        }

        public void Clear()
        {
            EnsureMesh();

            mesh.Clear();

            meshCollider.sharedMesh = null;
        }

        private void EnsureBuffers(
            int requiredVertexCount)
        {
            if (requiredVertexCount <= 0)
                return;

            if (verticesBuffer == null ||
                verticesBuffer.Length < requiredVertexCount)
            {
                verticesBuffer =
                    new Vector3[
                        requiredVertexCount
                    ];
            }

            if (uvsBuffer == null ||
                uvsBuffer.Length < requiredVertexCount)
            {
                uvsBuffer =
                    new Vector2[
                        requiredVertexCount
                    ];
            }

            if (tiledUVsBuffer == null ||
                tiledUVsBuffer.Length < requiredVertexCount)
            {
                tiledUVsBuffer =
                    new Vector2[
                        requiredVertexCount
                    ];
            }

            if (normalsBuffer == null ||
                normalsBuffer.Length < requiredVertexCount)
            {
                normalsBuffer =
                    new Vector3[
                        requiredVertexCount
                    ];
            }

            if (colorsBuffer == null ||
                colorsBuffer.Length < requiredVertexCount)
            {
                colorsBuffer =
                    new Color32[
                        requiredVertexCount
                    ];
            }
        }

        private void EnsureMesh()
        {
            if (meshFilter == null)
            {
                meshFilter =
                    GetComponent<MeshFilter>();
            }

            if (meshRenderer == null)
            {
                meshRenderer =
                    GetComponent<MeshRenderer>();
            }

            if (meshCollider == null)
            {
                meshCollider =
                    GetComponent<MeshCollider>();
            }

            if (mesh != null)
                return;

            mesh =
                new Mesh
                {
                    name = "ChunkMesh"
                };

            mesh.MarkDynamic();

            meshFilter.sharedMesh = mesh;

            meshCollider.sharedMesh = null;
        }

        private void OnDestroy()
        {
            if (meshCollider != null)
            {
                meshCollider.sharedMesh = null;
            }

            if (mesh == null)
                return;

            Destroy(mesh);

            mesh = null;

            verticesBuffer = null;
            uvsBuffer = null;
            tiledUVsBuffer = null;
            normalsBuffer = null;
            colorsBuffer = null;
        }
    }
}