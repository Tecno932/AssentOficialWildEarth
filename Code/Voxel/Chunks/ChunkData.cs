using Unity.Collections;

namespace WildEarth.Voxel
{
    /// <summary>
    /// Propietario de la memoria voxel de un chunk.
    ///
    /// Esta clase es responsable de crear y liberar el NativeArray.
    /// Los Jobs deben recibir únicamente la NativeArray, nunca esta clase.
    /// </summary>
    public sealed class ChunkData
    {
        public NativeArray<Voxel> Voxels { get; private set; }

        public bool IsCreated => Voxels.IsCreated;

        public ChunkData(Allocator allocator)
        {
            Voxels = new NativeArray<Voxel>(
                VoxelConstants.VoxelsPerChunk,
                allocator,
                NativeArrayOptions.ClearMemory
            );
        }

        public ChunkData(
            NativeArray<Voxel> voxels)
        {
            Voxels = voxels;
        }

        public void Dispose()
        {
            if (!Voxels.IsCreated)
                return;

            Voxels.Dispose();
            Voxels = default;
        }
    }
}