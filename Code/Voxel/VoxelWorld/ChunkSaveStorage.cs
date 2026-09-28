using System;
using System.IO;
using Unity.Collections;

namespace WildEarth.Voxel
{
    public sealed class ChunkSaveStorage
    {
        private const int FileVersion = 1;

        private readonly string rootPath;

        public ChunkSaveStorage(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
                throw new ArgumentException(
                    "La ruta de guardado no puede estar vacía.",
                    nameof(rootPath)
                );

            this.rootPath = rootPath;

            Directory.CreateDirectory(rootPath);
        }

        public bool Exists(ChunkCoordinate coordinate)
        {
            return File.Exists(
                GetChunkPath(coordinate)
            );
        }

        public void Save(
            ChunkCoordinate coordinate,
            NativeArray<Voxel> voxels)
        {
            if (!voxels.IsCreated)
                throw new InvalidOperationException(
                    "No se puede guardar un NativeArray no creado."
                );

            if (voxels.Length != VoxelConstants.VoxelsPerChunk)
                throw new InvalidOperationException(
                    $"Cantidad de voxels inválida: {voxels.Length}."
                );

            string path =
                GetChunkPath(coordinate);

            string temporaryPath =
                path + ".tmp";

            try
            {
                using (FileStream stream =
                       new FileStream(
                           temporaryPath,
                           FileMode.Create,
                           FileAccess.Write,
                           FileShare.None))
                using (BinaryWriter writer =
                       new BinaryWriter(stream))
                {
                    writer.Write(FileVersion);
                    writer.Write(voxels.Length);

                    for (int i = 0; i < voxels.Length; i++)
                    {
                        Voxel voxel = voxels[i];

                        writer.Write(voxel.BlockId);
                        writer.Write(voxel.Light);
                        writer.Write(voxel.State);
                    }
                }

                if (File.Exists(path))
                    File.Delete(path);

                File.Move(
                    temporaryPath,
                    path
                );
            }
            catch
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);

                throw;
            }
        }

        public bool TryLoad(
            ChunkCoordinate coordinate,
            NativeArray<Voxel> voxels)
        {
            if (!voxels.IsCreated)
                throw new InvalidOperationException(
                    "No se puede cargar en un NativeArray no creado."
                );

            if (voxels.Length != VoxelConstants.VoxelsPerChunk)
                throw new InvalidOperationException(
                    $"Cantidad de voxels inválida: {voxels.Length}."
                );

            string path =
                GetChunkPath(coordinate);

            if (!File.Exists(path))
                return false;

            using (FileStream stream =
                   new FileStream(
                       path,
                       FileMode.Open,
                       FileAccess.Read,
                       FileShare.Read))
            using (BinaryReader reader =
                   new BinaryReader(stream))
            {
                int version =
                    reader.ReadInt32();

                if (version != FileVersion)
                    throw new InvalidDataException(
                        $"Versión de chunk no compatible: {version}."
                    );

                int voxelCount =
                    reader.ReadInt32();

                if (voxelCount != voxels.Length)
                    throw new InvalidDataException(
                        $"Cantidad de voxels guardada inválida: {voxelCount}."
                    );

                for (int i = 0; i < voxels.Length; i++)
                {
                    ushort blockId =
                        reader.ReadUInt16();

                    byte light =
                        reader.ReadByte();

                    byte state =
                        reader.ReadByte();

                    voxels[i] =
                        new Voxel(
                            blockId,
                            light,
                            state
                        );
                }
            }

            return true;
        }

        public bool Delete(ChunkCoordinate coordinate)
        {
            string path =
                GetChunkPath(coordinate);

            if (!File.Exists(path))
                return false;

            File.Delete(path);
            return true;
        }

        private string GetChunkPath(
            ChunkCoordinate coordinate)
        {
            return Path.Combine(
                rootPath,
                $"chunk_{coordinate.X}_{coordinate.Y}_{coordinate.Z}.bin"
            );
        }
    }
}