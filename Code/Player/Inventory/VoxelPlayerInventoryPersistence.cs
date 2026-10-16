
using System;
using System.IO;
using UnityEngine;

namespace WildEarth.Voxel
{
    [DisallowMultipleComponent]
    public sealed class VoxelPlayerInventoryPersistence
        : MonoBehaviour
    {
        private const int FileMagic = 0x57454956;
        private const int FileVersion = 1;

        [Header("References")]
        [SerializeField]
        private VoxelPlayerHotbar hotbar;

        private ItemStack[] buffer;
        private string savePath;
        private int lastSavedConfigurationVersion;

        private void Awake()
        {
            if (hotbar == null)
                hotbar = GetComponent<VoxelPlayerHotbar>();

            buffer = new ItemStack[
                VoxelPlayerHotbar.TotalSlotCount
            ];

            savePath = Path.Combine(
                Application.persistentDataPath,
                "World",
                "player_inventory.bin"
            );
        }

        private void Start()
        {
            if (hotbar == null)
            {
                Debug.LogError(
                    "InventoryPersistence: falta " +
                    "VoxelPlayerHotbar.",
                    this
                );
                enabled = false;
                return;
            }

            LoadInventory();

            lastSavedConfigurationVersion =
                hotbar.ConfigurationVersion;
        }

        private void Update()
        {
            if (hotbar == null)
                return;

            if (hotbar.ConfigurationVersion ==
                lastSavedConfigurationVersion)
            {
                return;
            }

            SaveInventory();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                SaveInventory();
        }

        private void OnApplicationQuit()
        {
            SaveInventory();
        }

        private void OnDestroy()
        {
            SaveInventory();
        }

        private void LoadInventory()
        {
            if (!File.Exists(savePath))
                return;

            try
            {
                using (FileStream stream = new FileStream(
                    savePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read))
                using (BinaryReader reader =
                    new BinaryReader(stream))
                {
                    int magic = reader.ReadInt32();
                    int version = reader.ReadInt32();
                    int slotCount = reader.ReadInt32();
                    int selectedSlot = reader.ReadInt32();

                    if (magic != FileMagic ||
                        version != FileVersion ||
                        slotCount !=
                            VoxelPlayerHotbar.TotalSlotCount)
                    {
                        Debug.LogWarning(
                            "InventoryPersistence: archivo " +
                            "incompatible. Se conserva el " +
                            "inventario inicial.",
                            this
                        );
                        return;
                    }

                    for (int i = 0; i < slotCount; i++)
                    {
                        ushort itemId = reader.ReadUInt16();
                        ushort quantity = reader.ReadUInt16();
                        ushort durability = reader.ReadUInt16();

                        buffer[i] = new ItemStack(
                            itemId,
                            quantity,
                            durability
                        );
                    }

                    if (stream.Position != stream.Length)
                    {
                        Debug.LogWarning(
                            "InventoryPersistence: el archivo " +
                            "contiene datos adicionales.",
                            this
                        );
                    }

                    if (hotbar.RestoreInventory(
                        buffer,
                        selectedSlot))
                    {
                        Debug.Log(
                            "InventoryPersistence: inventario " +
                            "restaurado correctamente.",
                            this
                        );
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "InventoryPersistence: no se pudo cargar " +
                    "el inventario. " + exception.Message,
                    this
                );
            }
        }

        private void SaveInventory()
        {
            if (hotbar == null ||
                buffer == null ||
                string.IsNullOrEmpty(savePath))
            {
                return;
            }

            string temporaryPath = savePath + ".tmp";

            try
            {
                hotbar.CopyInventoryTo(buffer);

                string directory =
                    Path.GetDirectoryName(savePath);

                Directory.CreateDirectory(directory);

                using (FileStream stream = new FileStream(
                    temporaryPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None))
                using (BinaryWriter writer =
                    new BinaryWriter(stream))
                {
                    writer.Write(FileMagic);
                    writer.Write(FileVersion);
                    writer.Write(buffer.Length);
                    writer.Write(hotbar.SelectedSlot);

                    for (int i = 0; i < buffer.Length; i++)
                    {
                        writer.Write(buffer[i].ItemId);
                        writer.Write(buffer[i].Quantity);
                        writer.Write(buffer[i].Durability);
                    }

                    writer.Flush();
                    stream.Flush();
                }

                if (File.Exists(savePath))
                    File.Delete(savePath);

                File.Move(temporaryPath, savePath);

                lastSavedConfigurationVersion =
                    hotbar.ConfigurationVersion;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "InventoryPersistence: no se pudo guardar " +
                    "el inventario. " + exception.Message,
                    this
                );
            }
        }
    }
}
