using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Deepsea.Editor
{
    public static class SeaLushBuildV33
    {
        const string Root = "Assets/Deepsea/Resources/SeaLushWorldV33/";
        const string Sources = "Tools/SeaLushWorldV33/SourceMeshes/";
        const string ShaderPath = "Assets/Deepsea/Shaders/CoastalEcologyV33.shader";

        public static void PrepareAndBuild()
        {
            Prepare();
            SeaVillageBuildV31.BuildLush();
        }

        public static void Prepare()
        {
            Directory.CreateDirectory(Root+"Baked");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null) throw new InvalidDataException("Missing v33 coastal ecology shader.");
            if (ShaderUtil.ShaderHasError(shader))
            {
                foreach (var message in ShaderUtil.GetShaderMessages(shader))
                    Debug.LogError("V33 ecology shader line "+message.line+": "+message.message);
                throw new InvalidDataException("V33 coastal ecology shader did not compile.");
            }
            string path = Root+"EcologyKeep.mat";
            var keep = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (keep == null) AssetDatabase.CreateAsset(new Material(shader) {name="EcologyKeep",enableInstancing=true},path);
            else if (keep.shader != shader) throw new InvalidDataException("Unexpected v33 shader retention material.");

            int count = 0, triangles = 0;
            foreach (string file in Directory.GetFiles(Sources,"*.json").OrderBy(f => f,StringComparer.Ordinal))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                string model = Root+"Models/"+name+".fbx";
                var importer = AssetImporter.GetAtPath(model) as ModelImporter;
                if (importer == null) throw new InvalidDataException("Missing authored model "+model);
                if (importer.materialImportMode != ModelImporterMaterialImportMode.None || importer.importAnimation)
                {
                    importer.materialImportMode = ModelImporterMaterialImportMode.None;
                    importer.importAnimation = false;
                    importer.SaveAndReimport();
                }
                if (AssetDatabase.LoadAssetAtPath<GameObject>(model)?.GetComponentInChildren<MeshFilter>() == null)
                    throw new InvalidDataException("FBX import has no mesh "+model);
                triangles += SeaCraftBuildV32.BakeMesh(name,file,Root+"Baked/"+name+".asset");
                count++;
            }
            if (count != 10) throw new InvalidDataException("Expected ten v33 meshes, found "+count);
            AssetDatabase.SaveAssets();
            Debug.Log("LUSH_V33_BAKED models="+count+" triangles="+triangles);
        }
    }
}
