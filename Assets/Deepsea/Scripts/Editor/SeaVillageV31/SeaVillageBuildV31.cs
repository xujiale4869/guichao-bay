using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Deepsea.Editor {
 public static class SeaVillageBuildV31 {
  const string Scene="Assets/Deepsea/Scenes/CoastSeaVillageV31.unity", Root="Assets/Deepsea/Resources/SeaVillageV31/";
  [Serializable] class Dependencies {public string[] paths;}
  [Serializable] class Part {public string role;public int[] triangles;}
  [Serializable] class Source {public string name;public float[] vertices,normals,uv;public Part[] parts;}
  public static void PrepareAndBuild(){Prepare();Build();}
  public static void Prepare(){
   AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
   Directory.CreateDirectory(Root+"Baked");int count=0,total=0;
   foreach(var f in Directory.GetFiles(Root+"SourceMeshes","*.json")){
    var d=JsonUtility.FromJson<Source>(File.ReadAllText(f));if(d.vertices==null||d.vertices.Length%3!=0||d.parts.Length==0)throw new Exception("Invalid authored geometry "+f);
    var vertices=new Vector3[d.vertices.Length/3];var normals=new Vector3[vertices.Length];var uv=new Vector2[vertices.Length];
    for(int i=0;i<vertices.Length;i++){vertices[i]=new Vector3(d.vertices[i*3],d.vertices[i*3+1],d.vertices[i*3+2]);normals[i]=new Vector3(d.normals[i*3],d.normals[i*3+1],d.normals[i*3+2]);uv[i]=new Vector2(d.uv[i*2],d.uv[i*2+1]);}
    var mesh=new Mesh{name=d.name,indexFormat=vertices.Length>65535?IndexFormat.UInt32:IndexFormat.UInt16};mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;mesh.subMeshCount=d.parts.Length;
    for(int k=0;k<d.parts.Length;k++){mesh.SetTriangles(d.parts[k].triangles,k);total+=d.parts[k].triangles.Length/3;}
    mesh.RecalculateBounds();if(mesh.bounds.size.y<.05f||mesh.bounds.size.y>15f)throw new Exception("Incorrect metre/Y-up bounds "+d.name);
    string path=Root+"Baked/"+d.name+".asset";var prior=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(prior!=null){EditorUtility.CopySerialized(mesh,prior);UnityEngine.Object.DestroyImmediate(mesh);}else AssetDatabase.CreateAsset(mesh,path);count++;
   }
   if(count!=11)throw new Exception("Expected 11 authored models, found "+count);
   foreach(var f in Directory.GetFiles(Root+"Models","*.fbx")){var importer=AssetImporter.GetAtPath(f) as ModelImporter;if(importer!=null){importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.globalScale=1;importer.SaveAndReimport();}}
   AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Deepsea/Scenes/CoastSeaUnifiedV30.unity");var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.SaveScene(scene,Scene);
   Directory.CreateDirectory("Tools/SeaVillageV31");File.WriteAllText("Tools/SeaVillageV31/bake.json","{\"models\":"+count+",\"triangles\":"+total+",\"basis\":\"Y-up metres; identical Blender source triangles\"}");Debug.Log("VILLAGE_V31_PREPARED models="+count+" triangles="+total);
  }
  public static void Build() => BuildVillage(false);
  public static void BuildCoralContrast() => BuildVillage(true);
  public static void BuildCraft() => BuildVillage(true,true);
  public static void BuildLush() => BuildVillage(true,true,true);

  public static void BuildGameplay() => BuildVillage(true,true,true,true);

  public static void BuildMistCape() => BuildVillage(true,true,true,true,true);

  public static void BuildMistCapePhysical() => BuildVillage(true,true,true,true,true,true);

  public static void BuildMistCapeOrganic() => BuildVillage(true,true,true,true,true,true,true);

  public static void BuildSunnyAudio() => BuildSunnyAudio(false);
  public static void BuildAtmosphere() => BuildSunnyAudio(true);

  static void BuildSunnyAudio(bool atmosphere) {
   const string musicRoot="Assets/Deepsea/Resources/SeaAudioV36";
   foreach(string clip in new[]{"BGM_0_SunnyBay_GuitarMarimba","BGM_1_CozyTavern_HarborAccordion","BGM_2_StarlitCampfire_Kalimba","BGM_3_LivelyCatch_MarimbaGroove","SFX_WarmMarimbaSplash"}){
    var asset=AssetDatabase.LoadAssetAtPath<AudioClip>(musicRoot+"/"+clip+".wav");
    int expected=clip.StartsWith("BGM_")?846720:97020;
    if(asset==null||asset.frequency!=44100||asset.channels!=2||asset.samples!=expected)
     throw new InvalidOperationException("Missing or mismatched v36 score asset "+clip);
   }
   if(atmosphere){
    foreach(string name in new[]{"BeamKeep","ContactKeep"}){
     var material=Resources.Load<Material>("SeaAtmosphereV37/"+name);
     if(material==null||material.shader==null||ShaderUtil.ShaderHasError(material.shader))
      throw new InvalidOperationException("Invalid v37 atmosphere shader "+name);
    }
    var shore=Shader.Find("Deepsea/V24CoastShowcaseSurface");
    if(shore==null||ShaderUtil.ShaderHasError(shore))throw new InvalidOperationException("Invalid v37 shore shader");
   }
   BuildVillage(true,true,true,true,adaptiveAudio:true,atmosphere:atmosphere);
  }

  static void BuildVillage(bool contrast, bool craft = false, bool lush = false, bool gameplay = false, bool mistCape = false, bool physicalWater = false, bool organicWater = false, bool adaptiveAudio = false, bool atmosphere = false){
   string output = atmosphere ? "Builds/SeaAtmosphereV37" : adaptiveAudio ? "Builds/SeaSunnyAudioV36_1" : organicWater ? "Builds/SeaMistCapeV35_2" : physicalWater ? "Builds/SeaMistCapeV35_1" : mistCape ? "Builds/SeaMistCapeV35" : gameplay ? "Builds/SeaGameplayV34" : lush ? "Builds/SeaLushV33" : craft ? "Builds/SeaCraftV32" : contrast ? "Builds/SeaVillageV31.1" : "Builds/SeaVillageV31";
   string executable = atmosphere ? "Guichao-SeaAtmosphereV37.exe" : adaptiveAudio ? "Guichao-SeaSunnyAudioV36_1.exe" : organicWater ? "Guichao-SeaMistCapeV35_2.exe" : physicalWater ? "Guichao-SeaMistCapeV35_1.exe" : mistCape ? "Guichao-SeaMistCapeV35.exe" : gameplay ? "Guichao-SeaGameplayV34.exe" : lush ? "Guichao-SeaLushV33.exe" : craft ? "Guichao-SeaCraftV32.exe" : contrast ? "Guichao-SeaVillageV31.1.exe" : "Guichao-SeaVillageV31.exe";
   string reportPath = atmosphere ? "Tools/SeaAtmosphereV37/build.json" : adaptiveAudio ? "Tools/SeaAudioV36/build-v36_1.json" : organicWater ? "Tools/SeaMistCapeV35_2/build.json" : physicalWater ? "Tools/SeaMistCapeV35_1/build.json" : mistCape ? "Tools/SeaMistCapeV35/build.json" : gameplay ? "Tools/SeaGameplayV34/build.json" : lush ? "Tools/SeaLushWorldV33/build.json" : craft ? "Tools/SeaCraftV32/build.json" : contrast ? "Tools/SeaVillageV311/build.json" : "Tools/SeaVillageV31/build.json";
   string[] defines = adaptiveAudio ? new[]{"DEEPSEA_V31_1","DEEPSEA_V32","DEEPSEA_V33","DEEPSEA_V34","DEEPSEA_V36"} : organicWater ? new[]{"DEEPSEA_V31_1","DEEPSEA_V32","DEEPSEA_V33","DEEPSEA_V34","DEEPSEA_V35","DEEPSEA_V35_1","DEEPSEA_V35_2"} : physicalWater ? new[]{"DEEPSEA_V31_1","DEEPSEA_V32","DEEPSEA_V33","DEEPSEA_V34","DEEPSEA_V35","DEEPSEA_V35_1"} : mistCape ? new[]{"DEEPSEA_V31_1","DEEPSEA_V32","DEEPSEA_V33","DEEPSEA_V34","DEEPSEA_V35"} : gameplay ? new[]{"DEEPSEA_V31_1","DEEPSEA_V32","DEEPSEA_V33","DEEPSEA_V34"} : lush ? new[]{"DEEPSEA_V31_1","DEEPSEA_V32","DEEPSEA_V33"} : craft ? new[]{"DEEPSEA_V31_1","DEEPSEA_V32"} : contrast ? new[]{"DEEPSEA_V31_1"} : Array.Empty<string>();
   EditorSceneManager.OpenScene(Scene);var files=new[]{"ProjectSettings/ProjectSettings.asset","ProjectSettings/GraphicsSettings.asset","ProjectSettings/QualitySettings.asset","ProjectSettings/EditorBuildSettings.asset"};var bytes=files.Select(File.ReadAllBytes).ToArray();var name=PlayerSettings.productName;bool timing=PlayerSettings.enableFrameTimingStats;var gp=GraphicsSettings.renderPipelineAsset;var qp=QualitySettings.renderPipeline;BuildReport report=null;
   try{PlayerSettings.productName="深海灯塔 · 海湾生活 v21";PlayerSettings.enableFrameTimingStats=true;GraphicsSettings.renderPipelineAsset=Resources.Load<UniversalRenderPipelineAsset>("SeaSurfaceV24/Pipeline");QualitySettings.renderPipeline=GraphicsSettings.renderPipelineAsset;Directory.CreateDirectory(output);report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName=output+"/"+executable,extraScriptingDefines=defines,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});}
   finally{PlayerSettings.productName=name;PlayerSettings.enableFrameTimingStats=timing;GraphicsSettings.renderPipelineAsset=gp;QualitySettings.renderPipeline=qp;AssetDatabase.SaveAssets();for(int i=0;i<files.Length;i++)File.WriteAllBytes(files[i],bytes[i]);}
   if(report==null||report.summary.result!=BuildResult.Succeeded)throw new Exception("v31 build failed");foreach(var f in Directory.GetFiles("Builds/SeaUnifiedV30/Licenses","*",SearchOption.AllDirectories)){var dst=Path.Combine(output+"/Licenses",f.Substring("Builds/SeaUnifiedV30/Licenses".Length+1));Directory.CreateDirectory(Path.GetDirectoryName(dst));File.Copy(f,dst,true);}Directory.CreateDirectory(Path.GetDirectoryName(reportPath));File.WriteAllText(reportPath,"{\"passed\":true,\"warnings\":"+report.summary.totalWarnings+"}");if(atmosphere){Debug.Log("SEA_ATMOSPHERE_V37_BUILD_OK");return;}if(adaptiveAudio){Debug.Log("SEA_AUDIO_V36_BUILD_OK");return;}if(organicWater){Debug.Log("MIST_CAPE_V352_BUILD_OK");return;}if(physicalWater){Debug.Log("MIST_CAPE_V351_BUILD_OK");return;}if(mistCape){Debug.Log("MIST_CAPE_V35_BUILD_OK");return;}if(gameplay){Debug.Log("GAMEPLAY_V34_BUILD_OK");return;}if(lush){Debug.Log("LUSH_V33_BUILD_OK");return;}if(craft){Debug.Log("CRAFT_V32_BUILD_OK");return;}if(contrast){Debug.Log("CORAL_CONTRAST_V311_BUILD_OK");return;}var dependencies=AssetDatabase.GetDependencies(new[]{"Assets/Deepsea/Resources/SeaLifeV25/Meshes/Fish_0_SilverBream.asset","Assets/Deepsea/Resources/SeaLifeV25/Materials/Fish0.mat"},true);File.WriteAllText("Tools/SeaVillageV31/asset-dependencies.json",JsonUtility.ToJson(new Dependencies{paths=dependencies},true));Debug.Log("VILLAGE_V31_BUILD_OK");
  }
 }
}
