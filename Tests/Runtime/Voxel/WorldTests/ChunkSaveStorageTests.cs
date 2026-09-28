using System.IO;
using NUnit.Framework;
using Unity.Collections;
using WildEarth.Voxel;
using VoxelData = WildEarth.Voxel.Voxel;

namespace WildEarth.Tests.Voxel
{
    public sealed class ChunkSaveStorageTests
    {
        private string savePath;
        private ChunkSaveStorage saveStorage;
        private NativeArray<VoxelData> sourceVoxels;
        private NativeArray<VoxelData> loadedVoxels;

        [SetUp]
        public void SetUp()
        {
            savePath =
                Path.Combine(
                    Path.GetTempPath(),
                    "WildEarth_ChunkSaveStorageTests"
                );

            if (Directory.Exists(savePath))
                Directory.Delete(savePath, true);

            Directory.CreateDirectory(savePath);

            saveStorage =
                new ChunkSaveStorage(savePath);

            sourceVoxels =
                new NativeArray<VoxelData>(
                    VoxelConstants.VoxelsPerChunk,
                    Allocator.Persistent,
                    NativeArrayOptions.ClearMemory
                );

            loadedVoxels =
                new NativeArray<VoxelData>(
                    VoxelConstants.VoxelsPerChunk,
                    Allocator.Persistent,
                    NativeArrayOptions.ClearMemory
                );
        }

        [TearDown]
        public void TearDown()
        {
            if (sourceVoxels.IsCreated)
                sourceVoxels.Dispose();

            if (loadedVoxels.IsCreated)
                loadedVoxels.Dispose();

            if (Directory.Exists(savePath))
                Directory.Delete(savePath, true);
        }

        [Test]
        public void Save_CreatesChunkFile()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    10,
                    2,
                    -4
                );

            saveStorage.Save(
                coordinate,
                sourceVoxels
            );

            Assert.That(
                saveStorage.Exists(coordinate),
                Is.True
            );
        }

        [Test]
        public void SaveAndLoad_PreservesVoxelData()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    12,
                    3,
                    -7
                );

            int firstIndex =
                VoxelIndex.ToIndex(
                    1,
                    2,
                    3
                );

            int secondIndex =
                VoxelIndex.ToIndex(
                    5,
                    6,
                    7
                );

            int thirdIndex =
                VoxelIndex.ToIndex(
                    15,
                    15,
                    15
                );

            sourceVoxels[firstIndex] =
                new VoxelData(
                    1,
                    0xA5,
                    3
                );

            sourceVoxels[secondIndex] =
                new VoxelData(
                    7,
                    0xF2,
                    15
                );

            sourceVoxels[thirdIndex] =
                new VoxelData(
                    6,
                    0x4C,
                    9
                );

            saveStorage.Save(
                coordinate,
                sourceVoxels
            );

            bool loaded =
                saveStorage.TryLoad(
                    coordinate,
                    loadedVoxels
                );

            Assert.That(
                loaded,
                Is.True
            );

            Assert.That(
                loadedVoxels[firstIndex],
                Is.EqualTo(
                    sourceVoxels[firstIndex]
                )
            );

            Assert.That(
                loadedVoxels[secondIndex],
                Is.EqualTo(
                    sourceVoxels[secondIndex]
                )
            );

            Assert.That(
                loadedVoxels[thirdIndex],
                Is.EqualTo(
                    sourceVoxels[thirdIndex]
                )
            );
        }

        [Test]
        public void TryLoad_MissingChunkReturnsFalse()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    100,
                    5,
                    -20
                );

            bool loaded =
                saveStorage.TryLoad(
                    coordinate,
                    loadedVoxels
                );

            Assert.That(
                loaded,
                Is.False
            );
        }

        [Test]
        public void Delete_RemovesSavedChunk()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    -3,
                    4,
                    8
                );

            saveStorage.Save(
                coordinate,
                sourceVoxels
            );

            Assert.That(
                saveStorage.Exists(coordinate),
                Is.True
            );

            bool deleted =
                saveStorage.Delete(
                    coordinate
                );

            Assert.That(
                deleted,
                Is.True
            );

            Assert.That(
                saveStorage.Exists(coordinate),
                Is.False
            );
        }

        [Test]
        public void Save_OverwritesExistingChunk()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    20,
                    1,
                    30
                );

            int index =
                VoxelIndex.ToIndex(
                    2,
                    4,
                    6
                );

            sourceVoxels[index] =
                new VoxelData(
                    1,
                    0x12,
                    4
                );

            saveStorage.Save(
                coordinate,
                sourceVoxels
            );

            sourceVoxels[index] =
                new VoxelData(
                    2,
                    0xE3,
                    8
                );

            saveStorage.Save(
                coordinate,
                sourceVoxels
            );

            bool loaded =
                saveStorage.TryLoad(
                    coordinate,
                    loadedVoxels
                );

            Assert.That(
                loaded,
                Is.True
            );

            Assert.That(
                loadedVoxels[index],
                Is.EqualTo(
                    sourceVoxels[index]
                )
            );
        }
    }
}