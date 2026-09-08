using NUnit.Framework;
using UnityEngine;
using Unity.Mathematics;
using WildEarth.Voxel;

namespace WildEarth.Tests.Voxel
{
    public sealed class ChunkMeshUnityTests
    {
        [Test]
        public void EmptyMesh_ProducesEmptyUnityMesh()
        {
            ChunkMeshData data =
                new ChunkMeshData();

            Mesh mesh =
                CreateUnityMesh(data);

            Assert.That(mesh.vertexCount, Is.EqualTo(0));
            Assert.That(mesh.triangles.Length, Is.EqualTo(0));

            Object.DestroyImmediate(mesh);
            data.Dispose();
        }

        [Test]
        public void SingleQuad_ProducesFourVerticesAndTwoTriangles()
        {
            ChunkMeshData data =
                new ChunkMeshData();

            data.AddQuad(
                new float3(0, 0, 0),
                new float3(1, 0, 0),
                new float3(1, 1, 0),
                new float3(0, 1, 0),
                new float2(0, 0),
                new float2(1, 0),
                new float2(1, 1),
                new float2(0, 1)
            );

            Mesh mesh =
                CreateUnityMesh(data);

            Assert.That(mesh.vertexCount, Is.EqualTo(4));
            Assert.That(mesh.triangles.Length, Is.EqualTo(6));
            Assert.That(mesh.uv.Length, Is.EqualTo(4));

            Object.DestroyImmediate(mesh);
            data.Dispose();
        }

        [Test]
        public void SingleQuad_PreservesVertexPositions()
        {
            ChunkMeshData data =
                new ChunkMeshData();

            data.AddQuad(
                new float3(0, 0, 0),
                new float3(1, 0, 0),
                new float3(1, 1, 0),
                new float3(0, 1, 0),
                new float2(0, 0),
                new float2(1, 0),
                new float2(1, 1),
                new float2(0, 1)
            );

            Mesh mesh =
                CreateUnityMesh(data);

            Vector3[] vertices =
                mesh.vertices;

            Assert.That(vertices[0], Is.EqualTo(new Vector3(0, 0, 0)));
            Assert.That(vertices[1], Is.EqualTo(new Vector3(1, 0, 0)));
            Assert.That(vertices[2], Is.EqualTo(new Vector3(1, 1, 0)));
            Assert.That(vertices[3], Is.EqualTo(new Vector3(0, 1, 0)));

            Object.DestroyImmediate(mesh);
            data.Dispose();
        }

        private static Mesh CreateUnityMesh(
            ChunkMeshData data)
        {
            Mesh mesh =
                new Mesh();

            Vector3[] vertices =
                new Vector3[data.Vertices.Count];

            Vector2[] uvs =
                new Vector2[data.UVs.Count];

            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] =
                    new Vector3(
                        data.Vertices[i].x,
                        data.Vertices[i].y,
                        data.Vertices[i].z
                    );
            }

            for (int i = 0; i < uvs.Length; i++)
            {
                uvs[i] =
                    new Vector2(
                        data.UVs[i].x,
                        data.UVs[i].y
                    );
            }

            mesh.vertices = vertices;
            mesh.triangles = data.Triangles.ToArray();
            mesh.uv = uvs;

            return mesh;
        }
    }
}