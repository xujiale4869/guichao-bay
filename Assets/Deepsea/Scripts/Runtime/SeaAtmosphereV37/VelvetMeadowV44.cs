using System.Collections.Generic;
using UnityEngine;
using static Deepsea.SeaSeamlessWorldAndNauticalUIV29;

namespace Deepsea
{
    public sealed partial class SeaCinematicLightingAndAtmosphereV37
    {
        internal const int VelvetShellCountV44 = 12;
        // The outer mesh retains the same heights as the matching inner shells.
        internal static readonly int[] OuterVelvetLayersV442 = { 11, 9, 8, 6, 5, 3, 2, 0 };
        const float MeadowMinX = -155f, MeadowMinZ = -165f, MeadowStep = .72f;
        const int ChunkCells = 24;
        static readonly int[] InnerVelvetLayers = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };
        static readonly int[] MediumVelvetLayers = { 9, 6, 3 };
        static readonly int[] FarVelvetLayers = { 6 };
        float velvetThicknessV442;
        readonly Dictionary<MeshFilter,MeadowMeshes> meadowMeshes = new Dictionary<MeshFilter,MeadowMeshes>();

        struct MeadowMeshes
        {
            internal Mesh full, medium, far;
            internal Bounds bounds;
        }

        internal int ProtectedWoodSamplesV44 { get; private set; }
        internal Vector3 GoldenMeadowFocusV441 { get; private set; }
        internal float UplandLeafCardFractionV441 { get; private set; }
        internal int OuterMapleCountV442 { get; private set; }

        struct MeadowSample
        {
            internal bool ready;
            internal Vector3 root, normal;
            internal Color32 biome;
            internal float growth;
        }

        struct MeadowGrid
        {
            internal int columns, rows;
            internal RectInt core;
            internal MeadowSample[] samples;
            internal bool[] wood;
            internal int grass, gold;
            internal float bestFlowerWeight;

            internal int Index(int x, int z) => z*columns+x;
        }

        static int CoarseIndex(float world, float origin, bool upper)
        {
            float cells = (world-origin)/(MeadowStep*2);
            return 2*(upper ? Mathf.CeilToInt(cells) : Mathf.FloorToInt(cells));
        }

        void BuildVelvetMeadowV44(List<Bounds> exclusions, List<Vector3> trees, MeshFilter harborWood)
        {
            var material = Resources.Load<Material>("SeaAtmosphereV37/VelvetShellGrassV44");
            if (material == null || !material.shader.isSupported)
            {
                Debug.LogError("V44 velvet grass material is unavailable");
                return;
            }

            velvetThicknessV442 = material.GetFloat("_ShellThickness");

            // Extend to the terrain's submerged west, north and south margins. All grass
            // above the tidal band lies west of +18 m, including the authored shore.
            var whole = new RectInt(0,0,CoarseIndex(18,MeadowMinX,true),CoarseIndex(175,MeadowMinZ,true));
            int left = CoarseIndex(-64,MeadowMinX,false), bottom = CoarseIndex(-64,MeadowMinZ,false);
            var core = new RectInt(left,bottom,CoarseIndex(14,MeadowMinX,true)-left,
                                   CoarseIndex(68,MeadowMinZ,true)-bottom);
            var grid = new MeadowGrid { columns=whole.width+1, rows=whole.height+1, core=core };
            grid.samples = new MeadowSample[grid.columns*grid.rows];
            grid.wood = new bool[grid.samples.Length];
            MarkWoodClearanceV442(harborWood,grid);

            var owner = game.Group("V40_InlandWindMeadow",inlandScenery,Vector3.zero);
            var inner = game.Group("V44_InnerIslandWindMeadow",owner,Vector3.zero);
            var outer = game.Group("V44_OuterIslandWindMeadow",owner,Vector3.zero);
            int innerTiles = 0, outerTiles = 0, innerFaces = 0, outerFaces = 0;
            BuildMeadowRegion(core,1,inner,material,ref grid,exclusions,trees,ref innerTiles,ref innerFaces);
            BuildMeadowRegion(new RectInt(0,0,whole.xMax,core.yMin),2,outer,material,ref grid,exclusions,trees,ref outerTiles,ref outerFaces);
            BuildMeadowRegion(new RectInt(0,core.yMax,whole.xMax,whole.yMax-core.yMax),2,outer,material,ref grid,exclusions,trees,ref outerTiles,ref outerFaces);
            BuildMeadowRegion(new RectInt(0,core.yMin,core.xMin,core.height),2,outer,material,ref grid,exclusions,trees,ref outerTiles,ref outerFaces);
            BuildMeadowRegion(new RectInt(core.xMax,core.yMin,whole.xMax-core.xMax,core.height),2,outer,material,ref grid,exclusions,trees,ref outerTiles,ref outerFaces);

            Debug.Log($"V442_MEADOW_READY innerTiles={innerTiles} outerTiles={outerTiles} innerFaces={innerFaces} " +
                      $"outerFaces={outerFaces} grass={grid.grass} gold={grid.gold} stairSamples={ProtectedWoodSamplesV44} outerMaples={OuterMapleCountV442}");
        }


        void BuildMeadowRegion(RectInt region, int stride, Transform parent, Material material, ref MeadowGrid grid,
            List<Bounds> exclusions, List<Vector3> trees, ref int tiles, ref int faces)
        {
            int chunk = ChunkCells*stride;
            for (int z = region.yMin; z < region.yMax; z += chunk)
            for (int x = region.xMin; x < region.xMax; x += chunk)
            {
                var tile = new RectInt(x,z,Mathf.Min(chunk,region.xMax-x),Mathf.Min(chunk,region.yMax-z));
                var meshes = BuildVelvetShellPatchV44(tile,stride,ref grid,exclusions,trees,out int indexCount);
                if (meshes.full == null) continue;
                var part = game.Group("Grass Tile "+x+" "+z,parent,Vector3.zero);
                AddRenderer(part.gameObject,meshes.full,material);
                meshes.bounds = part.GetComponent<MeshRenderer>().bounds;
                meadowMeshes.Add(part.GetComponent<MeshFilter>(),meshes);
                v40Assets.Add(meshes.full);
                v40Assets.Add(meshes.medium);
                v40Assets.Add(meshes.far);
                tiles++;
                faces += indexCount/3;
            }
        }

        int SampleMeadow(int ix, int iz, ref MeadowGrid grid, List<Bounds> exclusions, List<Vector3> trees)
        {
            int index = grid.Index(ix,iz);
            ref var sample = ref grid.samples[index];
            if (sample.ready) return index;
            sample.ready = true;

            float x = MeadowMinX+ix*MeadowStep, z = MeadowMinZ+iz*MeadowStep;
            float y = coast.HeightAt(x,z);
            var root = new Vector3(x,y+.008f,z);
            sample.root = inlandScenery.InverseTransformPoint(root);
            sample.normal = Vector3.up;
            float sea = SeaLifeRuntime.Sea;
            if (y < sea+.45f) return index;
            var biome = coast.CoastBiomeAt(x,z,y);
            if (biome.x < .04f) return index;
            if (grid.wood[index])
            {
                ProtectedWoodSamplesV44++;
                return index;
            }
            foreach (var box in exclusions)
                if (box.Contains(root)) return index;

            float runX = coast.HeightAt(x+.35f,z)-coast.HeightAt(x-.35f,z);
            float runZ = coast.HeightAt(x,z+.35f)-coast.HeightAt(x,z-.35f);
            var normal = new Vector3(-runX,.70f,-runZ).normalized;
            sample.normal = inlandScenery.InverseTransformDirection(normal);
            float nearestTree = float.PositiveInfinity;
            foreach (var tree in trees)
                nearestTree = Mathf.Min(nearestTree,HorizontalDistanceSquared(tree,root));

            float rockRelief = coast.GrassHeightAt(x,z)-y;
            float grass = biome.x * BiomeRamp(.22f,.60f,normal.y) * BiomeRamp(.07f,1.10f,nearestTree) *
                          (1-BiomeRamp(.18f,.67f,rockRelief));
            if (grass < .04f) return index;

            float forest = Mathf.Max(biome.y*.75f,(1-BiomeRamp(2,34,nearestTree))*.85f);
            float band = .5f+.34f*Mathf.Sin(x*.12f-z*.095f+.8f)+.16f*Mathf.Cos(z*.15f+x*.08f);
            float clearing = BiomeRamp(28,50,nearestTree) * (1-.35f*BiomeRamp(.60f,.98f,biome.y));
            float villageDistance = HorizontalDistanceSquared(root,new Vector3(-31,0,1));
            float gold = clearing * BiomeRamp(.22f,.69f,grass) * BiomeRamp(.13f,.53f,band) *
                         BiomeRamp(sea+1.4f,sea+2.8f,y) * (1-BiomeRamp(2704,10700,villageDistance));
            sample.growth = Mathf.Clamp(.88f+.24f*Mathf.Sin(x*.21f-z*.18f)+gold*.08f,.68f,1.18f);
            int seamDistance = Mathf.Min(Mathf.Min(ix-grid.core.xMin,grid.core.xMax-ix),
                                         Mathf.Min(iz-grid.core.yMin,grid.core.yMax-iz));
            sample.biome = new Color(grass,gold,forest,BiomeRamp(0,6,seamDistance));
            grid.grass++;
            if (gold > .35f) grid.gold++;

            if (gold > grid.bestFlowerWeight)
            {
                float structureGap = float.PositiveInfinity;
                foreach (var box in exclusions)
                    if (box.size.x >= 5f) structureGap = Mathf.Min(structureGap,box.SqrDistance(root));
                float score = gold * BiomeRamp(4,36,structureGap);
                if (score > grid.bestFlowerWeight)
                {
                    grid.bestFlowerWeight = score;
                    GoldenMeadowFocusV441 = root;
                }
            }
            return index;
        }


        MeadowMeshes BuildVelvetShellPatchV44(RectInt tile, int stride, ref MeadowGrid grid,
            List<Bounds> exclusions, List<Vector3> trees, out int indexCount)
        {
            var samples = grid.samples;
            var layerSamples = new List<int>();
            var indices = new List<int>();
            var mapped = new Dictionary<int,int>();

            int Vertex(int source)
            {
                if (mapped.TryGetValue(source,out int vertex)) return vertex;
                vertex = layerSamples.Count;
                layerSamples.Add(source);
                mapped.Add(source,vertex);
                return vertex;
            }

            void Triangle(int a, int b, int c)
            {
                indices.Add(Vertex(a));
                indices.Add(Vertex(b));
                indices.Add(Vertex(c));
            }

            void StitchEdge(int center, int first, int midpoint, int last)
            {
                if (midpoint < 0) Triangle(center,first,last);
                else
                {
                    Triangle(center,first,midpoint);
                    Triangle(center,midpoint,last);
                }
            }

            RectInt core = grid.core;
            for (int z = tile.yMin; z < tile.yMax; z += stride)
            for (int x = tile.xMin; x < tile.xMax; x += stride)
            {
                int x1 = x+stride, z1 = z+stride;
                int sw = SampleMeadow(x,z,ref grid,exclusions,trees);
                int nw = SampleMeadow(x,z1,ref grid,exclusions,trees);
                int ne = SampleMeadow(x1,z1,ref grid,exclusions,trees);
                int se = SampleMeadow(x1,z,ref grid,exclusions,trees);

                bool joinsX = stride == 2 && z >= core.yMin && z1 <= core.yMax;
                bool joinsZ = stride == 2 && x >= core.xMin && x1 <= core.xMax;
                int west = joinsX && x == core.xMax ? SampleMeadow(x,z+1,ref grid,exclusions,trees) : -1;
                int north = joinsZ && z1 == core.yMin ? SampleMeadow(x+1,z1,ref grid,exclusions,trees) : -1;
                int east = joinsX && x1 == core.xMin ? SampleMeadow(x1,z+1,ref grid,exclusions,trees) : -1;
                int south = joinsZ && z == core.yMax ? SampleMeadow(x+1,z,ref grid,exclusions,trees) : -1;

                if (samples[sw].biome.r == 0 && samples[nw].biome.r == 0 &&
                    samples[ne].biome.r == 0 && samples[se].biome.r == 0 &&
                    (west < 0 || samples[west].biome.r == 0) &&
                    (north < 0 || samples[north].biome.r == 0) &&
                    (east < 0 || samples[east].biome.r == 0) &&
                    (south < 0 || samples[south].biome.r == 0)) continue;

                if (west < 0 && north < 0 && east < 0 && south < 0)
                {
                    Triangle(sw,nw,ne);
                    Triangle(sw,ne,se);
                    continue;
                }

                // The shared edge uses the inner grid's exact midpoint and deformation inputs.
                int center = SampleMeadow(x+1,z+1,ref grid,exclusions,trees);
                StitchEdge(center,sw,west,nw);
                StitchEdge(center,nw,north,ne);
                StitchEdge(center,ne,east,se);
                StitchEdge(center,se,south,sw);
            }

            indexCount = 0;
            if (indices.Count == 0) return default;

            var fullLayers = stride == 2 ? OuterVelvetLayersV442 : InnerVelvetLayers;
            var full = CreateShellMesh("Full Velvet Grass",fullLayers,samples,layerSamples,indices);
            var medium = CreateShellMesh("Medium Velvet Grass",MediumVelvetLayers,samples,layerSamples,indices);
            var far = CreateShellMesh("Distant Velvet Grass",FarVelvetLayers,samples,layerSamples,indices);
            indexCount = indices.Count*fullLayers.Length;
            return new MeadowMeshes { full=full, medium=medium, far=far };
        }

        static Mesh CreateShellMesh(string label, int[] shells, MeadowSample[] samples, List<int> layerSamples, List<int> indices)
        {
            var positions = new Vector3[layerSamples.Count*shells.Length];
            var normals = new Vector3[positions.Length];
            var uv = new Vector2[positions.Length];
            var colors = new Color32[positions.Length];
            var triangles = new int[indices.Count*shells.Length];
            for (int layer = 0; layer < shells.Length; layer++)
            {
                float height = shells[layer]/(VelvetShellCountV44-1f);
                int offset = layer*layerSamples.Count;
                for (int i = 0; i < layerSamples.Count; i++)
                {
                    var sample = samples[layerSamples[i]];
                    positions[offset+i] = sample.root;
                    normals[offset+i] = sample.normal;
                    uv[offset+i] = new Vector2(height,sample.growth);
                    colors[offset+i] = sample.biome;
                }
                for (int i = 0; i < indices.Count; i++)
                    triangles[layer*indices.Count+i] = offset+indices[i];
            }

            var mesh = new Mesh { name=label };
            mesh.vertices = positions;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.colors32 = colors;
            mesh.triangles = triangles;
            var bounds = mesh.bounds;
            bounds.Expand(2.5f);
            mesh.bounds = bounds;
            return mesh;
        }

        internal Mesh FullGrassMeshV442(MeshFilter filter) => meadowMeshes.TryGetValue(filter,out var meshes) ? meshes.full : null;

        void RefreshVelvetMeshesV442()
        {
            var camera = game.view;
            float projectedHeight = camera.pixelHeight*Mathf.Abs(camera.projectionMatrix.m11)*.5f*velvetThicknessV442;
            var forward = camera.transform.forward;
            var depthAxes = new Vector3(Mathf.Abs(forward.x),Mathf.Abs(forward.y),Mathf.Abs(forward.z));
            var position = camera.transform.position;
            bool orthographic = camera.orthographic;
            foreach (var entry in meadowMeshes)
            {
                var meshes = entry.Value;
                float nearestDepth = Vector3.Dot(meshes.bounds.center-position,forward)-Vector3.Dot(meshes.bounds.extents,depthAxes);
                float pixels = orthographic ? projectedHeight : projectedHeight/Mathf.Max(1,nearestDepth);
                // Conservative threshold against the shader's per-vertex 18/22 px clipping.
                var selected = pixels < 17.5f ? meshes.far : pixels < 21.5f ? meshes.medium : meshes.full;
                if (entry.Key.sharedMesh != selected) entry.Key.sharedMesh = selected;
            }
        }

        static void MarkWoodClearanceV442(MeshFilter wood, MeadowGrid grid)
        {
            if (wood == null || wood.sharedMesh == null) return;
            var vertices = wood.sharedMesh.vertices;
            var triangles = wood.sharedMesh.triangles;
            for (int i = 0; i < vertices.Length; i++) vertices[i] = wood.transform.TransformPoint(vertices[i]);

            const float margin = .90f;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]];
                var b = vertices[triangles[i+1]];
                var c = vertices[triangles[i+2]];
                if (Vector3.Cross(b-a,c-a).y <= .008f ||
                    Mathf.Max(a.y,b.y,c.y) < SeaLifeRuntime.Sea+.08f) continue;

                int left = Mathf.Max(0,Mathf.CeilToInt((Mathf.Min(a.x,b.x,c.x)-margin-MeadowMinX)/MeadowStep));
                int right = Mathf.Min(grid.columns-1,Mathf.FloorToInt((Mathf.Max(a.x,b.x,c.x)+margin-MeadowMinX)/MeadowStep));
                int bottom = Mathf.Max(0,Mathf.CeilToInt((Mathf.Min(a.z,b.z,c.z)-margin-MeadowMinZ)/MeadowStep));
                int top = Mathf.Min(grid.rows-1,Mathf.FloorToInt((Mathf.Max(a.z,b.z,c.z)+margin-MeadowMinZ)/MeadowStep));
                for (int row = bottom; row <= top; row++)
                for (int column = left; column <= right; column++)
                    grid.wood[grid.Index(column,row)] = true;
            }
        }


        Mesh BuildFullerMapleCanopyV44(Mesh source)
        {
            var mesh = Instantiate(source);
            var vertices = source.vertices;
            var indices = source.GetTriangles(0);
            var kept = new List<int>(Mathf.CeilToInt(indices.Length*.85f));
            var corners = new int[6];

            for (int first = 0; first < indices.Length; first += 6)
            {
                int card = first/6;
                if ((card*3)%20 < 3) continue;
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
                    vertices[corners[i]] = center+(vertices[corners[i]]-center)*1.22f;
            }

            mesh.vertices = vertices;
            mesh.SetTriangles(kept,0);
            UplandLeafCardFractionV441 = (float)kept.Count/indices.Length;
            return mesh;
        }
    }
}
