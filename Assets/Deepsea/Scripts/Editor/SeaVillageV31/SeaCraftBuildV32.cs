using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Deepsea.Editor
{
    public static class SeaCraftBuildV32
    {
        const string Assets = "Assets/Deepsea/Resources/SeaCraftV32/";
        const string Sources = "Tools/SeaCraftV32/SourceMeshes/";
        const string ShaderPath = "Assets/Deepsea/Shaders/StylizedCoastalCraftV32.shader";
        static readonly string[] Models = { "Boatwright", "Tavern", "Observatory", "Slipway" };

        [Serializable] sealed class Part { public string role; public int[] triangles; }
        [Serializable] sealed class Source
        {
            public string name;
            public float[] vertices, normals, uv, rgba;
            public Part[] parts;
        }

        public static void PrepareAndBuild()
        {
            Prepare();
            SeaVillageBuildV31.BuildCraft();
        }

        public static void Prepare()
        {
            Directory.CreateDirectory(Assets + "Baked");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
                throw new InvalidOperationException("Coastal craft shader asset is missing");
            if (ShaderUtil.ShaderHasError(shader))
            {
                foreach (var message in ShaderUtil.GetShaderMessages(shader))
                    Debug.LogError("Craft shader line " + message.line + ": " + message.message);
                throw new InvalidOperationException("Coastal craft shader import failed");
            }
            string materialPath = Assets + "CraftKeep.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
                AssetDatabase.CreateAsset(new Material(shader) { name = "CraftKeep" }, materialPath);
            else if (material.shader != shader)
                throw new InvalidOperationException("CraftKeep references a different shader");

            int total = 0;
            foreach (string model in Models)
            {
                string name = "Village_V32_" + model;
                total += BakeMesh(name,Sources+name+".json",Assets+"Baked/"+name+".asset");
            }
            AssetDatabase.SaveAssets();
            Debug.Log("CRAFT_V32_BAKED models=" + Models.Length + " triangles=" + total);
        }

        internal static int BakeMesh(string name, string sourcePath, string assetPath)
        {
            var source = JsonUtility.FromJson<Source>(File.ReadAllText(sourcePath));
            int count = source?.vertices?.Length / 3 ?? 0;
            if (count == 0 || source.name != name || source.vertices.Length != 3*count
                || source.normals?.Length != 3*count || source.uv?.Length != 2*count
                || source.rgba?.Length != 4*count || source.parts == null || source.parts.Length == 0)
                throw new InvalidDataException("Invalid metre/Y-up vertex channels for " + name);
            var positions = new Vector3[count];
            var normals = new Vector3[count];
            var uv = new Vector2[count];
            var colors = new Color[count];
            for (int i = 0; i < count; i++)
            {
                positions[i] = new Vector3(source.vertices[3*i],source.vertices[3*i+1],source.vertices[3*i+2]);
                normals[i] = new Vector3(source.normals[3*i],source.normals[3*i+1],source.normals[3*i+2]);
                uv[i] = new Vector2(source.uv[2*i],source.uv[2*i+1]);
                colors[i] = new Color(source.rgba[4*i],source.rgba[4*i+1],source.rgba[4*i+2],source.rgba[4*i+3]);
            }
            var mesh = new Mesh
            {
                name = name,
                indexFormat = count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16,
                vertices = positions,
                normals = normals,
                uv = uv,
                colors = colors,
                subMeshCount = source.parts.Length
            };
            int total = 0;
            for (int i = 0; i < source.parts.Length; i++)
            {
                int[] triangles = source.parts[i].triangles;
                if (triangles == null || triangles.Length == 0 || triangles.Length % 3 != 0
                    || Array.Exists(triangles, index => index < 0 || index >= count))
                    throw new InvalidDataException("Invalid triangle list for " + name + "/" + source.parts[i].role);
                mesh.SetTriangles(triangles,i,false);
                total += triangles.Length / 3;
            }
            mesh.RecalculateBounds();
            if (mesh.bounds.size.y < .05f || mesh.bounds.size.y > 12f)
                throw new InvalidDataException("Invalid bounds for " + name);
            var prior = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (prior == null)
                AssetDatabase.CreateAsset(mesh,assetPath);
            else
            {
                EditorUtility.CopySerialized(mesh,prior);
                UnityEngine.Object.DestroyImmediate(mesh);
            }
            return total;
        }
    }
}
