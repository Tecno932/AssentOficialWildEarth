using System;

namespace WildEarth.Voxel
{
    public sealed class Chunk : IDisposable
    {
        public ChunkCoordinate Coordinate { get; }

        public ChunkData Data { get; }

        public ChunkBiomeData BiomeData { get; }

        public ChunkState State { get; private set; }

        public ChunkFlags Flags { get; private set; }

        public uint DataRevision { get; private set; }

        public uint MeshRevision { get; private set; }

        public uint SaveRevision { get; private set; }

        public bool IsDirty =>
            (Flags & ChunkFlags.Dirty) != 0;

        public bool NeedsMesh =>
            (Flags & ChunkFlags.NeedsMesh) != 0;

        public bool NeedsSave =>
            (Flags & ChunkFlags.NeedsSave) != 0;

        public Chunk(
            ChunkCoordinate coordinate,
            ChunkData data,
            ChunkBiomeData biomeData)
        {
            if (data == null)
            {
                throw new ArgumentNullException(
                    nameof(data)
                );
            }

            if (biomeData == null)
            {
                throw new ArgumentNullException(
                    nameof(biomeData)
                );
            }

            Coordinate = coordinate;

            Data = data;

            BiomeData = biomeData;

            State =
                ChunkState.Unloaded;

            Flags =
                ChunkFlags.None;

            DataRevision = 0;
            MeshRevision = 0;
            SaveRevision = 0;
        }

        public void SetState(
            ChunkState state)
        {
            State = state;
        }

        public void MarkGenerated()
        {
            State =
                ChunkState.Generated;

            Flags |=
                ChunkFlags.Generated;

            Flags &=
                ~ChunkFlags.NeedsSave;
        }

        public void MarkVoxelDataChanged()
        {
            DataRevision++;

            Flags |=
                ChunkFlags.Dirty |
                ChunkFlags.NeedsMesh |
                ChunkFlags.NeedsSave;

            State =
                ChunkState.Generated;
        }

        public void MarkNeedsMesh()
        {
            Flags |=
                ChunkFlags.NeedsMesh;
        }

        public void ClearNeedsMesh()
        {
            Flags &=
                ~ChunkFlags.NeedsMesh;

            MeshRevision =
                DataRevision;
        }

        public void MarkSaved()
        {
            SaveRevision =
                DataRevision;

            Flags &=
                ~ChunkFlags.Dirty;

            Flags &=
                ~ChunkFlags.NeedsSave;
        }

        public void SetLoadedFromDisk()
        {
            Flags |=
                ChunkFlags.LoadedFromDisk;
        }

        public void SetHasEntities(
            bool value)
        {
            if (value)
            {
                Flags |=
                    ChunkFlags.HasEntities;
            }
            else
            {
                Flags &=
                    ~ChunkFlags.HasEntities;
            }
        }

        public void SetHasBlockEntities(
            bool value)
        {
            if (value)
            {
                Flags |=
                    ChunkFlags.HasBlockEntities;
            }
            else
            {
                Flags &=
                    ~ChunkFlags.HasBlockEntities;
            }
        }

        public void MarkFailed()
        {
            State =
                ChunkState.Failed;
        }

        public void Dispose()
        {
            Data.Dispose();

            BiomeData.Dispose();

            State =
                ChunkState.Unloaded;

            Flags =
                ChunkFlags.None;
        }
    }
}