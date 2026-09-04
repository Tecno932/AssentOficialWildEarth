using Unity.Jobs;

namespace WildEarth.Voxel
{
    public interface IChunkGenerator
    {
        JobHandle Schedule(
            Chunk chunk,
            JobHandle dependency = default
        );
    }
}