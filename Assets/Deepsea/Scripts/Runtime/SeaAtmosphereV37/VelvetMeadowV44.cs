using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static Deepsea.SeaSeamlessWorldAndNauticalUIV29;

namespace Deepsea
{
    public sealed partial class SeaCinematicLightingAndAtmosphereV37
    {
        internal const int VelvetShellCountV44 = 14;
        const float MeadowMinX = -48f, MeadowMinZ = -42f, MeadowStep = .72f;
        const int MeadowColumns = 76, MeadowRows = 118;

        internal int ProtectedWoodSamplesV44 { get; private set; }

        void BuildVelvetMeadowV44(List<Bounds> exclusions, List<Vector3> trees, MeshFilter harborWood)
        {
            var material = Resources.Load<Material>("SeaAtmosphereV37/VelvetShellGrassV44");
            if (material == null || !material.shader.isSupported)
            {
                Debug.LogError("V44 velvet grass material is unavailable");
                return;
            }

            var wood = new bool[MeadowColumns * MeadowRows];
            MarkWoodClearanceV44(harborWood,wood);
            var roots = new Vector3[wood.Length];
            var normals = new Vector3[wood.Length];
            var biomes = new Color32[wood.Length];
            var growth = new float[wood.Length];
            int grassSamples = 0, flowerSamples = 0;
            float sea = SeaLifeRuntime.Sea;
            for (int row = 0; row < MeadowRows; row++)
            for (int column = 0; column < MeadowColumns; column++)
            {
                int index = row*MeadowColumns+column;
                float x = MeadowMinX+column*MeadowStep;
                float z = MeadowMinZ+row*MeadowStep;
                float y = coast.HeightAt(x,z);
                float rockRelief = coast.GrassHeightAt(x,z)-y;
                var root = new Vector3(x,y+.008f,z);
                roots[index] = root;
                normals[index] = Vector3.up;
                if (y < sea+.45f || y > sea+18.5f) continue;
                var biome = coast.CoastBiomeAt(x,z,y);
                if (biome.x < .07f) continue;
                if (wood[index])
                {
                    ProtectedWoodSamplesV44++;
                    continue;
                }
                if (exclusions.Exists(box => box.Contains(root))) continue;

                float runX = coast.HeightAt(x+.35f,z)-coast.HeightAt(x-.35f,z);
                float runZ = coast.HeightAt(x,z+.35f)-coast.HeightAt(x,z-.35f);
                var normal = new Vector3(-runX,.70f,-runZ).normalized;
                normals[index] = normal;
                float nearestTree = float.PositiveInfinity;
                foreach (var tree in trees)
                    nearestTree = Mathf.Min(nearestTree,HorizontalDistanceSquared(tree,root));

                float edge = Mathf.Min(Mathf.Min(column,MeadowColumns-1-column),
                    Mathf.Min(row,MeadowRows-1-row))*MeadowStep;
                float grass = biome.x * BiomeRamp(.50f,.78f,normal.y) * BiomeRamp(.10f,1.30f,nearestTree) *
                              (1-BiomeRamp(.12f,.60f,rockRelief)) * BiomeRamp(0,3.2f,edge);
                if (grass < .09f) continue;
                float forest = Mathf.Max(biome.y*.75f,(1-BiomeRamp(2,34,nearestTree))*.85f);
                float band = .5f+.32f*Mathf.Sin(x*.11f+z*.085f+Mathf.Sin(z*.06f)*1.4f) +
                             .18f*Mathf.Cos(z*.14f-x*.09f);
                float grove = 1-BiomeRamp(4,52,nearestTree);
                float crimson = BiomeRamp(.38f,.85f,grass) * BiomeRamp(sea+1.55f,sea+3.40f,y) *
                                BiomeRamp(.46f,.72f,band+grove*.24f);
                float height = .86f+.24f*Mathf.Sin(x*.23f-z*.19f)+crimson*.12f;
                grassSamples++;
                if (crimson > .60f) flowerSamples++;
                growth[index] = Mathf.Clamp(height,.65f,1.18f);
                biomes[index] = new Color(grass,crimson,forest,1);
            }

            // The two triangles of a cell share the same terrain samples as their neighbours.
            var samples = new List<int>(grassSamples);
            var baseTriangles = new List<int>(grassSamples*6);
            var remap = new int[roots.Length];
            for (int i = 0; i < remap.Length; i++) remap[i] = -1;

            int MapSample(int index)
            {
                if (remap[index] >= 0) return remap[index];
                remap[index] = samples.Count;
                samples.Add(index);
                return remap[index];
            }

            for (int row = 0; row < MeadowRows-1; row++)
            for (int column = 0; column < MeadowColumns-1; column++)
            {
                int sw = row*MeadowColumns+column;
                int ne = sw+MeadowColumns+1;
                if (biomes[sw].r == 0 && biomes[sw+1].r == 0 &&
                    biomes[ne-1].r == 0 && biomes[ne].r == 0) continue;

                int a = MapSample(sw), b = MapSample(sw+1), c = MapSample(ne-1), d = MapSample(ne);
                baseTriangles.Add(a); baseTriangles.Add(c); baseTriangles.Add(d);
                baseTriangles.Add(a); baseTriangles.Add(d); baseTriangles.Add(b);
            }
            if (samples.Count == 0) return;

            var positions = new Vector3[samples.Count*VelvetShellCountV44];
            var meshNormals = new Vector3[positions.Length];
            var layers = new Vector2[positions.Length];
            var colors = new Color32[positions.Length];
            var triangles = new int[baseTriangles.Count*VelvetShellCountV44];
            for (int layer = 0; layer < VelvetShellCountV44; layer++)
            {
                // Submit the highest shell first so surviving fragments can occlude the layers below.
                float height = 1f-layer/(VelvetShellCountV44-1f);
                int first = layer*samples.Count;
                for (int i = 0; i < samples.Count; i++)
                {
                    int src = samples[i], dst = first+i;
                    positions[dst] = inlandScenery.InverseTransformPoint(roots[src]);
                    meshNormals[dst] = inlandScenery.InverseTransformDirection(normals[src]);
                    layers[dst] = new Vector2(height,growth[src]);
                    colors[dst] = biomes[src];
                }
                for (int i = 0; i < baseTriangles.Count; i++)
                    triangles[layer*baseTriangles.Count+i] = first+baseTriangles[i];
            }

            var mesh = new Mesh { name = "V44 Velvet Grass Shells", indexFormat = IndexFormat.UInt32 };
            mesh.vertices = positions;
            mesh.normals = meshNormals;
            mesh.uv = layers;
            mesh.colors32 = colors;
            mesh.triangles = triangles;
            var bounds = mesh.bounds;
            bounds.Expand(2.5f);
            mesh.bounds = bounds;
            v40Assets.Add(mesh);

            var owner = game.Group("V40_InlandWindMeadow",inlandScenery,Vector3.zero);
            AddRenderer(owner.gameObject,mesh,material);
            Debug.Log($"V44_MEADOW_READY shells={VelvetShellCountV44} grass={grassSamples} crimson={flowerSamples} " +
                      $"stairSamples={ProtectedWoodSamplesV44} triangles={triangles.Length/3}");
        }

        static void MarkWoodClearanceV44(MeshFilter wood, bool[] protectedSamples)
        {
            if (wood == null || wood.sharedMesh == null) return;
            var vertices = wood.sharedMesh.vertices;
            var triangles = wood.sharedMesh.triangles;
            for (int i = 0; i < vertices.Length; i++) vertices[i] = wood.transform.TransformPoint(vertices[i]);

            // The stairs and pier share one mesh. Project the locally overlapping tread faces
            // instead of excluding grass across the bounding rectangle of the entire pier.
            const float margin = .90f;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]];
                var b = vertices[triangles[i+1]];
                var c = vertices[triangles[i+2]];
                if (Vector3.Cross(b-a,c-a).y <= .008f ||
                    Mathf.Max(a.y,b.y,c.y) < SeaLifeRuntime.Sea+.08f) continue;

                int left = Mathf.Max(0,Mathf.CeilToInt((Mathf.Min(a.x,b.x,c.x)-margin-MeadowMinX)/MeadowStep));
                int right = Mathf.Min(MeadowColumns-1,Mathf.FloorToInt((Mathf.Max(a.x,b.x,c.x)+margin-MeadowMinX)/MeadowStep));
                int bottom = Mathf.Max(0,Mathf.CeilToInt((Mathf.Min(a.z,b.z,c.z)-margin-MeadowMinZ)/MeadowStep));
                int top = Mathf.Min(MeadowRows-1,Mathf.FloorToInt((Mathf.Max(a.z,b.z,c.z)+margin-MeadowMinZ)/MeadowStep));
                for (int row = bottom; row <= top; row++)
                for (int column = left; column <= right; column++)
                    protectedSamples[row*MeadowColumns+column] = true;
            }
        }

        static Mesh BuildFullerMapleCanopyV44(Mesh source)
        {
            var mesh = Instantiate(source);
            var vertices = source.vertices;
            var indices = source.GetTriangles(0);
            var kept = new List<int>(indices.Length*2/3);
            var corners = new int[6];

            for (int first = 0; first < indices.Length; first += 6)
            {
                if ((first/6)%3 == 2) continue;
                int count = 0;
                var center = Vector3.zero;
                for (int i = first; i < Mathf.Min(first+6,indices.Length); i++)
                {
                    int index = indices[i];
                    kept.Add(index);
                    if (System.Array.IndexOf(corners,index,0,count) >= 0) continue;
                    corners[count++] = index;
                    center += vertices[index];
                }
                center /= count;
                for (int i = 0; i < count; i++)
                    vertices[corners[i]] = center+(vertices[corners[i]]-center)*1.18f;
            }

            mesh.vertices = vertices;
            mesh.SetTriangles(kept,0);
            return mesh;
        }
    }
}
