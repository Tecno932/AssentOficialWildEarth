using NUnit.Framework;
using Unity.Mathematics;
using WildEarth.Voxel;

namespace WildEarth.Tests.Voxel
{
    public sealed class ChunkMeshDataTests
    {
        [Test]
        public void NewMeshData_IsEmpty()
        {
            using ChunkMeshData mesh =
                new ChunkMeshData();

            Assert.That(
                mesh.VertexCount,
                Is.EqualTo(0)
            );

            Assert.That(
                mesh.TriangleIndexCount,
                Is.EqualTo(0)
            );

            Assert.That(
                mesh.FaceCount,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void AddQuad_AddsFourVertices()
        {
            using ChunkMeshData mesh =
                new ChunkMeshData();

            mesh.AddQuad(
                new float3(0, 0, 0),
                new float3(1, 0, 0),
                new float3(1, 1, 0),
                new float3(0, 1, 0),
                new float2(0, 0),
                new float2(1, 0),
                new float2(1, 1),
                new float2(0, 1)
            );

            Assert.That(
                mesh.VertexCount,
                Is.EqualTo(4)
            );

            Assert.That(
                mesh.TriangleIndexCount,
                Is.EqualTo(6)
            );

            Assert.That(
                mesh.FaceCount,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void Clear_RemovesAllGeometry()
        {
            using ChunkMeshData mesh =
                new ChunkMeshData();

            mesh.AddQuad(
                float3.zero,
                new float3(1, 0, 0),
                new float3(1, 1, 0),
                new float3(0, 1, 0),
                float2.zero,
                new float2(1, 0),
                new float2(1, 1),
                new float2(0, 1)
            );

            mesh.Clear();

            Assert.That(
                mesh.VertexCount,
                Is.EqualTo(0)
            );

            Assert.That(
                mesh.TriangleIndexCount,
                Is.EqualTo(0)
            );

            Assert.That(
                mesh.FaceCount,
                Is.EqualTo(0)
            );
        }
    }
}