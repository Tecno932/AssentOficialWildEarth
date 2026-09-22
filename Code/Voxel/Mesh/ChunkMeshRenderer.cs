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

        public Mesh Mesh => mesh;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            meshCollider = GetComponent<MeshCollider>();

            mesh = new Mesh
            {
                name = "ChunkMesh"
            };

            mesh.MarkDynamic();

            meshFilter.sharedMesh = mesh;
            meshCollider.sharedMesh = mesh;
        }

        public void Apply(
            ChunkMeshData meshData,
            Material material)
        {
            if (meshData == null)
                throw new System.ArgumentNullException(
                    nameof(meshData)
                );

            EnsureMesh();

            mesh.Clear();

            if (meshData.VertexCount > 0)
            {
                var vertices =
                    new Vector3[meshData.Vertices.Count];

                var uvs =
                    new Vector2[meshData.UVs.Count];

                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] =
                        new Vector3(
                            meshData.Vertices[i].x,
                            meshData.Vertices[i].y,
                            meshData.Vertices[i].z
                        );
                }

                for (int i = 0; i < uvs.Length; i++)
                {
                    uvs[i] =
                        new Vector2(
                            meshData.UVs[i].x,
                            meshData.UVs[i].y
                        );
                }

                mesh.SetVertices(vertices);

                mesh.SetTriangles(
                    meshData.Triangles,
                    0
                );

                mesh.SetUVs(
                    0,
                    uvs
                );

                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
            }

            meshRenderer.sharedMaterial = material;

            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;
        }

        public void Clear()
        {
            EnsureMesh();

            mesh.Clear();

            meshCollider.sharedMesh = null;
        }

        private void EnsureMesh()
        {
            if (meshFilter == null)
                meshFilter = GetComponent<MeshFilter>();

            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();

            if (meshCollider == null)
                meshCollider = GetComponent<MeshCollider>();

            if (mesh != null)
                return;

            mesh = new Mesh
            {
                name = "ChunkMesh"
            };

            mesh.MarkDynamic();

            meshFilter.sharedMesh = mesh;
            meshCollider.sharedMesh = mesh;
        }

        private void OnDestroy()
        {
            if (meshCollider != null)
                meshCollider.sharedMesh = null;

            if (mesh == null)
                return;

            Destroy(mesh);
            mesh = null;
        }
    }
}