using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace WildEarth.Voxel
{
    public sealed class ChunkMeshData : IDisposable
    {
        public List<float3> Vertices { get; }
        public List<int> Triangles { get; }
        public List<float2> UVs { get; }

        public int VertexCount =>
            Vertices.Count;

        public int TriangleIndexCount =>
            Triangles.Count;

        public int FaceCount =>
            Triangles.Count / 6;

        public ChunkMeshData(
            int initialCapacity = 1024)
        {
            if (initialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialCapacity)
                );
            }

            Vertices =
                new List<float3>(
                    initialCapacity
                );

            Triangles =
                new List<int>(
                    initialCapacity * 3
            );

            UVs =
                new List<float2>(
                    initialCapacity
                );
        }

        public void Clear()
        {
            Vertices.Clear();
            Triangles.Clear();
            UVs.Clear();
        }

        public void AddQuad(
            float3 v0,
            float3 v1,
            float3 v2,
            float3 v3,
            float2 uv0,
            float2 uv1,
            float2 uv2,
            float2 uv3)
        {
            int startIndex =
                Vertices.Count;

            Vertices.Add(v0);
            Vertices.Add(v1);
            Vertices.Add(v2);
            Vertices.Add(v3);

            UVs.Add(uv0);
            UVs.Add(uv1);
            UVs.Add(uv2);
            UVs.Add(uv3);

            Triangles.Add(startIndex);
            Triangles.Add(startIndex + 1);
            Triangles.Add(startIndex + 2);

            Triangles.Add(startIndex);
            Triangles.Add(startIndex + 2);
            Triangles.Add(startIndex + 3);
        }

        public void Dispose()
        {
            Clear();
        }
    }
}