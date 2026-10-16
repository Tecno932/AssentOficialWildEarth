using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace WildEarth.Voxel
{
    public sealed class VoxelHotbarBlockPreview
    {
        private const int PreviewLayer = 31;
        private const int PixelsPerSlot = 256;
        private const float SlotSpacing = 1.6f;

        private readonly int previewSlotCount;
        private readonly GameObject root;
        private readonly Camera previewCamera;
        private readonly RenderTexture renderTexture;
        private readonly Material previewMaterial;
        private readonly MeshFilter[] meshFilters;
        private readonly Mesh[] meshes;

        public Texture Texture => renderTexture;

        public VoxelHotbarBlockPreview(Texture2D atlasTexture)
            : this(atlasTexture, VoxelPlayerHotbar.SlotCount)
        {
        }

        public VoxelHotbarBlockPreview(Texture2D atlasTexture, int slotCount)
        {
            if (atlasTexture == null)
                throw new System.ArgumentNullException(nameof(atlasTexture));

            if (slotCount < 1)
                throw new System.ArgumentOutOfRangeException(nameof(slotCount));

            previewSlotCount = slotCount;

            Shader shader = Shader.Find("WildEarth/VoxelAtlasBlock");

            if (shader == null)
                throw new System.InvalidOperationException(
                    "No se encontró el shader WildEarth/VoxelAtlasBlock.");

            root = new GameObject("VoxelHotbar3DPreview");

            // Mantiene cada vista aislada, con coordenadas moderadas.
            int previewId = System.Guid.NewGuid().GetHashCode() & 0x7FFF;
            root.transform.position = new Vector3(
                previewId * 25f,
                10000f,
                0f
            );

            previewMaterial = new Material(shader);
            previewMaterial.name = "VoxelHotbar3DPreviewMaterial";
            previewMaterial.SetTexture("_BaseMap", atlasTexture);
            previewMaterial.SetColor("_BaseColor", Color.white);

            renderTexture = new RenderTexture(
                previewSlotCount * PixelsPerSlot,
                PixelsPerSlot,
                24,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default
            );

            renderTexture.name = "VoxelHotbar3DPreviewTexture";
            renderTexture.filterMode = FilterMode.Point;
            renderTexture.wrapMode = TextureWrapMode.Clamp;
            renderTexture.Create();

            meshFilters = new MeshFilter[previewSlotCount];
            meshes = new Mesh[previewSlotCount];

            for (int i = 0; i < meshFilters.Length; i++)
            {
                GameObject blockObject = new GameObject(
                    $"PreviewBlock_{i + 1}");

                blockObject.layer = PreviewLayer;
                blockObject.transform.SetParent(root.transform, false);
                blockObject.transform.localPosition = new Vector3(
                    (i - (meshFilters.Length - 1) * 0.5f)
                    * SlotSpacing,
                    0f,
                    0f);

                blockObject.transform.localRotation =
                    Quaternion.Euler(0f, 35f, 0f);

                MeshFilter filter =
                    blockObject.AddComponent<MeshFilter>();

                MeshRenderer renderer =
                    blockObject.AddComponent<MeshRenderer>();

                renderer.sharedMaterial = previewMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                meshFilters[i] = filter;
            }

            GameObject cameraObject = new GameObject(
                "VoxelHotbar3DPreviewCamera");

            cameraObject.layer = PreviewLayer;
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.localPosition =
                new Vector3(0f, 3.4f, -7.4f);

            previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.enabled = true;
            previewCamera.orthographic = true;
            previewCamera.orthographicSize = 0.8f;
            previewCamera.nearClipPlane = 0.1f;
            previewCamera.farClipPlane = 20f;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            previewCamera.cullingMask = 1 << PreviewLayer;
            previewCamera.targetTexture = renderTexture;
            previewCamera.aspect = previewSlotCount;
            previewCamera.allowHDR = false;
            previewCamera.allowMSAA = false;

            cameraObject.transform.LookAt(root.transform.position);
        }

        public Rect GetUVRect(int slot)
        {
            if (slot < 0 || slot >= previewSlotCount)
                return new Rect(0f, 0f, 0f, 0f);

            float width = 1f / previewSlotCount;
            return new Rect(slot * width, 0f, width, 1f);
        }

        public bool SetBlock(int slot, BlockDefinition definition,
            VoxelAtlasSettings atlasSettings)
        {
            if (slot < 0 || slot >= meshFilters.Length)
                return false;

            ClearBlock(slot);

            if (definition == null ||
                definition.Id == BlockIds.Air ||
                (definition.MeshType != BlockMeshType.Cube &&
                 definition.MeshType != BlockMeshType.Fluid))
            {
                return false;
            }

            meshes[slot] = CreateCubeMesh(definition, atlasSettings);
            meshFilters[slot].sharedMesh = meshes[slot];

            return true;
        }

        public void ClearBlock(int slot)
        {
            if (slot < 0 || slot >= meshFilters.Length)
                return;

            meshFilters[slot].sharedMesh = null;

            if (meshes[slot] != null)
            {
                Object.Destroy(meshes[slot]);
                meshes[slot] = null;
            }
        }

        private static Mesh CreateCubeMesh(
            BlockDefinition definition,
            VoxelAtlasSettings atlasSettings)
        {
            Mesh mesh = new Mesh
            {
                name = $"HotbarPreview_{definition.BlockName}"
            };

            var vertices = new List<Vector3>(24);
            var normals = new List<Vector3>(24);
            var atlasUVs = new List<Vector2>(24);
            var tiledUVs = new List<Vector2>(24);
            var colors = new List<Color>(24);
            var triangles = new List<int>(36);

            AddFace(vertices, normals, atlasUVs, tiledUVs, colors,
                triangles, atlasSettings, definition.SideTexture,
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f,  0.5f, -0.5f),
                new Vector3( 0.5f,  0.5f, -0.5f),
                new Vector3( 0.5f, -0.5f, -0.5f),
                Vector3.back);

            AddFace(vertices, normals, atlasUVs, tiledUVs, colors,
                triangles, atlasSettings, definition.SideTexture,
                new Vector3( 0.5f, -0.5f,  0.5f),
                new Vector3( 0.5f,  0.5f,  0.5f),
                new Vector3(-0.5f,  0.5f,  0.5f),
                new Vector3(-0.5f, -0.5f,  0.5f),
                Vector3.forward);

            AddFace(vertices, normals, atlasUVs, tiledUVs, colors,
                triangles, atlasSettings, definition.SideTexture,
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f,  0.5f, -0.5f),
                new Vector3(0.5f,  0.5f,  0.5f),
                new Vector3(0.5f, -0.5f,  0.5f),
                Vector3.right);

            AddFace(vertices, normals, atlasUVs, tiledUVs, colors,
                triangles, atlasSettings, definition.SideTexture,
                new Vector3(-0.5f, -0.5f,  0.5f),
                new Vector3(-0.5f,  0.5f,  0.5f),
                new Vector3(-0.5f,  0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, -0.5f),
                Vector3.left);

            AddFace(vertices, normals, atlasUVs, tiledUVs, colors,
                triangles, atlasSettings, definition.TopTexture,
                new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f,  0.5f),
                new Vector3( 0.5f, 0.5f,  0.5f),
                new Vector3( 0.5f, 0.5f, -0.5f),
                Vector3.up);

            AddFace(vertices, normals, atlasUVs, tiledUVs, colors,
                triangles, atlasSettings, definition.BottomTexture,
                new Vector3(-0.5f, -0.5f,  0.5f),
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3( 0.5f, -0.5f, -0.5f),
                new Vector3( 0.5f, -0.5f,  0.5f),
                Vector3.down);

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, atlasUVs);
            mesh.SetUVs(1, tiledUVs);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();

            return mesh;
        }

        private static void AddFace(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Vector2> atlasUVs,
            List<Vector2> tiledUVs,
            List<Color> colors,
            List<int> triangles,
            VoxelAtlasSettings settings,
            AtlasTileCoordinate tile,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector3 d,
            Vector3 normal)
        {
            int start = vertices.Count;
            Vector2 tileMin = settings.GetTileMinUV(tile);

            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(d);

            for (int i = 0; i < 4; i++)
            {
                normals.Add(normal);
                atlasUVs.Add(tileMin);
                colors.Add(Color.white);
            }

            tiledUVs.Add(new Vector2(0f, 0f));
            tiledUVs.Add(new Vector2(0f, 1f));
            tiledUVs.Add(new Vector2(1f, 1f));
            tiledUVs.Add(new Vector2(1f, 0f));

            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        public void Dispose()
        {
            if (previewCamera != null)
                previewCamera.targetTexture = null;

            if (root != null)
                Object.Destroy(root);

            foreach (Mesh mesh in meshes)
            {
                if (mesh != null)
                    Object.Destroy(mesh);
            }

            if (previewMaterial != null)
                Object.Destroy(previewMaterial);

            if (renderTexture != null)
            {
                renderTexture.Release();
                Object.Destroy(renderTexture);
            }
        }
    }
}
