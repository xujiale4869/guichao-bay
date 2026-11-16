// ============================================================================
// 深海灯塔·归潮湾 (v30) — 一体化无缝半岛地形（消除沙草硬切/北岸细线/外海水面断层）
// File: Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29/SeamlessTerrainV29.cs
// ============================================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Deepsea
{
    public sealed partial class SeaSeamlessWorldAndNauticalUIV29
    {
        int coastCount, reefCount, coralCount, kelpCount, mapleCount;
        Material unifiedTerrainMat;

        public void PrepareWorld(SeaLifeRuntime g, Transform parent)
        {
            game = g;
            bay = g.GetComponent<SeaBayExpansionAndBuildSpaceV28>();
            native = Resources.Load<NativeBayHeightfieldV29>("SeaSeamlessV29/NativeHeightfield");

            worldRoot = new GameObject("V29_ContinuousCoastAndArchipelago").transform;
            worldRoot.SetParent(parent, false);

            // 1. 获取原版 V24CoastShowcaseSurface (_Mode = 1: Ground 模式，原生支持基于顶点色 c.r 的沙地->岩石->草地平滑过渡！)
            var baseSand = Resources.Load<Material>("CoastRefinement/Sand");
            var baseRock = Resources.Load<Material>("CoastRefinement/Rock");
            unifiedTerrainMat = new Material(baseSand != null ? baseSand : baseRock)
            {
                name = "V30_UnifiedIslandAndSeabedMat"
            };
            _owned.Add(unifiedTerrainMat);

            var budget = Resources.Load<Material>("SeaSurfaceV24/BudgetCoastShowcaseSurface");
            if (budget != null)
            {
                unifiedTerrainMat.shader = budget.shader;
                // _Mode = 1 启用 V24CoastShowcaseSurface 内置的 Sand + Rock + GroundCover(草地) 三合一顶点色混合管线！
                if (unifiedTerrainMat.HasProperty("_Mode")) unifiedTerrainMat.SetFloat("_Mode", 1.0f);
            }

            unifiedTerrainMat.enableInstancing=true;
            nativeSand = unifiedTerrainMat;
            nativeRock = new Material(baseRock != null ? baseRock : unifiedTerrainMat) { name = "V30_NativeReefRock" };
            if (budget != null) nativeRock.shader = budget.shader;
            nativeRock.enableInstancing=true;
            _owned.Add(nativeRock);

            rocks = new Mesh[6];
            for (int i = 0; i < rocks.Length; i++)
            {
                rocks[i] = Resources.Load<Mesh>("SeaSeamlessV29/Rock" + i);
                if (rocks[i] == null) throw new InvalidOperationException("Missing accepted native rock " + i);
            }

            // 2. 构建唯一一张覆盖全视野 (520m × 520m) 的无边界连续波浪海面
            waterFilter = game.world.Find("ShowcaseWater").GetComponent<MeshFilter>();
            priorWaterMesh = waterFilter.sharedMesh;
            priorWaterPosition = waterFilter.transform.position; priorWaterScale = waterFilter.transform.localScale;
            waterFilter.transform.localScale = Vector3.one;
            waterFilter.transform.position = new Vector3(20f, SeaLifeRuntime.Sea, 10f);
            Mesh waterMesh = GridMeshWithColors("V30_WaterSurface", -260f, 260f, -260f, 260f, 180, 180, (x, z) => 0f, false);
            var wb = waterMesh.bounds;
            wb.Expand(Vector3.up * 4f);
            waterMesh.bounds = wb;
            waterFilter.sharedMesh = waterMesh;

            // 3. 彻底废除 v29 中导致“一边沙地直接变草地”的两块硬拼接网格 (V29_WesternLandContinuation + V29_ContinuousContinentalShelf)！
            //    改为生成唯一一张横跨 [-165, 225] × [-190, 195] 的【一体化半岛陆地 + 缓坡浅湾 + 连续大陆坡海床网格】，
            //    并逐顶点写入平滑过渡的生态顶点色 (c.r 控制草地/沙地平滑渐变)，从根源上消灭沙草硬切线、北侧细线与外海水深断层！
            AddMesh(
                "V29_DeepFloor",
                GridMeshWithColors("V29_DeepFloor", -240f, 280f, -250f, 270f, 2, 2, (x, z) => SeaLifeRuntime.Sea - 14.0f, false),
                unifiedTerrainMat,
                Vector3.zero,
                Vector3.one,
                false,
                worldRoot
            );

            Mesh unifiedIslandMesh = GridMeshWithColors(
                "V29_ContinuousContinentalShelf",
                -155f, 195f,
                -165f, 175f,
                260, 280,
                HeightAt,
                true
            );
            AddMesh("V29_ContinuousContinentalShelf", unifiedIslandMesh, unifiedTerrainMat, Vector3.zero, Vector3.one, true, worldRoot);

            ApplyNativeEcologyV30();
            PopulateCoastAndBarrier();
            if(IsV31)BuildVillageAndBiodiversityV31();
        }

        /// <summary>
        /// v30 一体化海岬半岛与大陆坡连续高程方程：
        /// 1. 彻底解决“岛屿北侧陆地只有一条线”：凡在海岸线 ShoreX(z) 以西 (distFromShore < 0)，全部隆起为宽广厚实的内陆山体与台地！
        /// 2. 彻底解决“水面与外界相接不对劲”：浅湾向深海采用 60m 宽的余弦大陆坡平滑沉降，绝无矩形边缘断崖！
        /// </summary>
        private float BaseArchipelagoHeight(float x, float z)
        {
            float sea = SeaLifeRuntime.Sea; // -0.22f
            float deepAbyss = sea - 14.0f;

            // 1. 连贯自然的西岸主岛海岸线方程
            float mainShoreX = ShoreX(z);
            float distFromShore = x - mainShoreX;
            float regrade = HeadlandRegrade(x,z);

            // 原版核心微调保护区 (仅在原版精修岸岩正下方 x ∈ [-18, 8], z ∈ [-16, 20] 轻柔避让，绝不切断南北陆地！)
            float corePreserveMask = Mathf.Exp(-Mathf.Pow((x + 5f) / 11f, 4f) - Mathf.Pow((z - 2f) / 15f, 4f));

            // 2. 西侧与北侧厚实饱满的半岛内陆山体 + 东向缓坡翡翠浅滩
            float shelfWidth = 26.0f
                + 10.0f * Mathf.Exp(-Mathf.Pow((z - 4.0f) / 20f, 2f))   // 栈桥前方宽阔扇形浅坪
                + 12.0f * Mathf.Exp(-Mathf.Pow((z + 36f) / 24f, 2f))   // 南湾半月浅水湾
                + 9.5f * Mathf.Exp(-Mathf.Pow((z - 40f) / 22f, 2f));   // 北岬珊瑚浅湾

            float coastalY;
            if (distFromShore < 0f)
            {
                float inland = -distFromShore;
                float meadowRise = BiomeRamp(0,15,inland);
                float hillRise = BiomeRamp(6,52,inland);
                float headland = Mathf.Exp(-Mathf.Pow((z-2)/22,2));
                float backSlope = headland * BiomeRamp(1,11,inland) *
                    (2.25f + 1.95f * BiomeRamp(7,31,inland));
                float undulation = .85f * Mathf.Sin(x*.045f+z*.038f) +
                                    .55f * Mathf.Cos(z*.075f-x*.052f);
                float newSlope = sea + .12f + meadowRise*(3.25f+undulation) + hillRise*8.1f + backSlope;
                float originalRelief = 1.35f * Mathf.Sin(x*.045f+z*.038f) +
                                       .85f * Mathf.Cos(z*.075f-x*.052f);
                float existingSlope = sea + .12f + BiomeRamp(0,16,inland)*(3.4f+originalRelief) +
                                      BiomeRamp(10,75,inland)*7.8f;
                coastalY = Mathf.Lerp(existingSlope,newSlope,regrade);
            }
            else
            {
                // 浅海大陆架 + 60m 超宽余弦缓坡沉降（消除浅海水面与外海深蓝水面之间的硬断层！）
                float shallowT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(distFromShore / shelfWidth));
                float slopeT = .5f-.5f*Mathf.Cos(Mathf.PI*Mathf.Clamp01((distFromShore-shelfWidth*.65f)/58f));
                float terraceY = Mathf.Lerp(sea + 0.12f, sea - 1.85f, shallowT);
                coastalY = Mathf.Lerp(terraceY, deepAbyss, slopeT);
            }

            // 3. 外海蜿蜒珊瑚礁脉与环礁浅滩（采用宽缓高斯包络，去掉生硬刀切沟槽，让水色自然渐变）
            float trenchCenterX = 11.5f + 3.4f * Mathf.Sin(z * 0.065f - 0.3f) + 0.08f * Mathf.Abs(z);
            float outerRidgeX = trenchCenterX + 16.5f + 4.0f * Mathf.Cos(z * 0.055f);

            float distToRidge = Mathf.Abs(x - outerRidgeX);
            float ridgeHalfSpan = 18.0f + 5.0f * Mathf.Sin(z * 0.07f + 1.1f);
            float ridgeBell = Mathf.Exp(-Mathf.Pow(distToRidge / ridgeHalfSpan, 2.0f));
            // 栈桥正东方通海航道柔和降低
            float channelDip = 1.0f - 0.65f * Mathf.Exp(-Mathf.Pow((z - 4.5f) / 7.5f, 2f));
            float outerRidgeY = Mathf.Lerp(deepAbyss, sea - 1.25f, ridgeBell * channelDip);

            // 4. 远东连绵浅海沙坪 (与外海礁脊连成一片，不再是孤立豹纹斑块)
            float eastAtollMask = Mathf.Exp(-Mathf.Pow((x - (outerRidgeX + 22f)) / 28f, 2f) - Mathf.Pow(z / 75f, 2f));
            float eastAtollY = Mathf.Lerp(deepAbyss, sea - 1.55f, eastAtollMask * 0.82f);

            // 5. 柔和自然海床沙波纹
            float ripple = 0.07f * Mathf.Sin(x * 0.32f + Mathf.Sin(z * 0.24f))
                         + 0.05f * Mathf.Cos(z * 0.38f - x * 0.21f);

            float combinedY = Mathf.Max(coastalY, Mathf.Max(outerRidgeY, eastAtollY)) + ripple;

            // 温和避让原版内湾核心海床，杜绝陡峭切沟
            combinedY -= corePreserveMask * Mathf.Lerp(1,BiomeRamp(-2,0,distFromShore),regrade) * .55f;

            // 外围远海边界 (距离中心 > 130m) 统一平滑归零至 deepAbyss，确保网格边缘与 V29_DeepFloor 100% 齐平无接缝！
            float edgeFadeX = Mathf.SmoothStep(0,1,Mathf.InverseLerp(145,195,x));
            float edgeWest = Mathf.SmoothStep(0,1,Mathf.InverseLerp(-120,-155,x));
            float edgeSouth = Mathf.SmoothStep(0,1,Mathf.InverseLerp(-115,-165,z));
            float edgeNorth = Mathf.SmoothStep(0,1,Mathf.InverseLerp(125,175,z));
            float edge = 1-(1-edgeFadeX)*(1-edgeWest)*(1-edgeSouth)*(1-edgeNorth);
            combinedY = Mathf.Lerp(combinedY,deepAbyss,edge);

            return Mathf.Clamp(combinedY, deepAbyss, sea + 14.0f);
        }

        public float HeightAt(float x, float z)
        {
            float height = BaseArchipelagoHeight(x,z);
            if (native == null || !native.Probe(x,z,out float sampled,out float distance,true))
                return height;

            float shore = ShoreX(z);
            float inland = shore-x;
            float inlandClimb = BiomeRamp(0,7,inland) * HeadlandRegrade(x,z);
            float core = (1-BiomeRamp(16,36,Mathf.Abs(z-2))) *
                         (1-BiomeRamp(22,38,Mathf.Abs(x+2)));
            float legacy = Mathf.Lerp(height,sampled-.09f,core*(1-BiomeRamp(0,12,distance)));

            // Keep the authored harbor and its walkable shore. Further inland the new slope
            // can carry ground across low samples from the old rock-and-track capture.
            float backSlope = inlandClimb * BiomeRamp(18.1f,32f,-x);
            float supported = Mathf.Lerp(legacy,Mathf.Max(legacy,height),backSlope);
            if (distance > 0 && sampled > SeaLifeRuntime.Sea+.45f)
            {
                float falloff = 1-BiomeRamp(0,26,distance);
                float shoulder = sampled-.09f+distance*.12f;
                supported = Mathf.Lerp(supported,Mathf.Max(supported,shoulder),
                    falloff * BiomeRamp(0,3,distance) * inlandClimb * BiomeRamp(18.1f,28f,-x));
            }
            return supported;
        }

        static float HeadlandRegrade(float x,float z)
        {
            // Feather the new hill into the boatyard and observatory's established approaches.
            float headland = BiomeRamp(-18,-9,z) * (1-BiomeRamp(27,37,z));
            // The central tavern sits on the existing inland bench. Preserve its full footing and
            // blend the grade back into the new slope on all sides.
            float tavernBench = BiomeRamp(-59,-50,x) * (1-BiomeRamp(-37,-29,x)) *
                                BiomeRamp(-22,-15,z) * (1-BiomeRamp(-2,7,z));
            return headland*(1-tavernBench);
        }

        float ShoreX(float z)
        {
            return -11.5f
                + 4.2f * Mathf.Sin(z * 0.055f + 0.4f)
                + 2.4f * Mathf.Cos(z * 0.13f - 0.8f)
                - 6.5f * Mathf.Exp(-Mathf.Pow((z + 32f) / 18f, 2f))
                + 5.2f * Mathf.Exp(-Mathf.Pow((z - 42f) / 16f, 2f));
        }

        internal float GrassHeightAt(float x, float z)
        {
            float height = HeightAt(x,z);
            if (native != null && native.Probe(x,z,out float visible,out float distance) && distance == 0)
                height = Mathf.Max(height,visible);
            return height;
        }

        internal Vector2 CoastBiomeAt(float x,float z,float height) =>
            CoastBiome(x,z,height,ShoreX(z));

        internal static float BiomeRamp(float start,float end,float value) =>
            Mathf.SmoothStep(0,1,Mathf.InverseLerp(start,end,value));

        static Vector2 CoastBiome(float x,float z,float height,float shore)
        {
            float aboveSea = height-SeaLifeRuntime.Sea;
            float inland = shore-x;
            if (aboveSea < .45f || inland <= 0) return Vector2.zero;
            float grassNoise = .32f * Mathf.Sin(x*.17f+Mathf.Cos(z*.15f)*2f) +
                               .22f * Mathf.Cos(z*.27f-x*.21f);
            float grass = BiomeRamp(.45f,.72f,aboveSea) *
                          BiomeRamp(.45f,2.65f,aboveSea+grassNoise*.75f) *
                          BiomeRamp(0,15,inland+grassNoise*3.8f) * BiomeRamp(0,1,inland);
            float forestNoise = .28f * Mathf.Sin(x*.14f-z*.16f) + .20f * Mathf.Cos(z*.22f+x*.18f);
            float forest = BiomeRamp(2.1f,6.4f,aboveSea+forestNoise*.9f) *
                           BiomeRamp(11,30,inland+forestNoise*4.5f);
            return new Vector2(grass,forest);
        }

        // The visible terrain and the inland grass use the same red and green biome channels.
        Mesh GridMeshWithColors(string name, float minX, float maxX, float minZ, float maxZ, int nx, int nz, Func<float, float, float> heightFn, bool computeEcoColors)
        {
            int vCount = (nx + 1) * (nz + 1);
            var v = new Vector3[vCount];
            var uv = new Vector2[vCount];
            var cols = new Color[vCount];
            var tr = new int[nx * nz * 6];

            int k = 0;
            for (int z = 0; z <= nz; z++)
            {
                float zz = Mathf.Lerp(minZ, maxZ, z / (float)nz);
                float shoreX = ShoreX(zz);
                for (int x = 0; x <= nx; x++)
                {
                    float xx = Mathf.Lerp(minX, maxX, x / (float)nx);
                    float yy = heightFn(xx, zz);
                    v[k] = new Vector3(xx, yy, zz);
                    uv[k] = new Vector2(xx * 0.28f, zz * 0.28f);

                    if (computeEcoColors)
                    {
                        Vector2 biome = CoastBiome(xx,zz,yy,shoreX);
                        cols[k] = new Color(biome.x,biome.y,0,1);
                    }
                    else
                    {
                        cols[k] = new Color(0f, 0f, 0f, 1f);
                    }
                    k++;
                }
            }

            int ti = 0;
            for (int z = 0; z < nz; z++)
            {
                for (int x = 0; x < nx; x++)
                {
                    int a = z * (nx + 1) + x;
                    int b = a + 1;
                    int c = a + nx + 1;
                    int d = c + 1;
                    tr[ti++] = a; tr[ti++] = c; tr[ti++] = b;
                    tr[ti++] = b; tr[ti++] = c; tr[ti++] = d;
                }
            }

            var m = new Mesh
            {
                name = name,
                indexFormat = vCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
            };
            m.vertices = v;
            m.uv = uv;
            m.colors = cols;
            m.triangles = tr;
            m.RecalculateNormals();
            m.RecalculateBounds();
            _owned.Add(m);
            return m;
        }

        GameObject AddMesh(string name, Mesh mesh, Material mat, Vector3 position, Vector3 scale, bool collision, Transform parent = null)
        {
            var o = new GameObject(name);
            o.transform.SetParent(parent != null ? parent : worldRoot, false);
            o.transform.localPosition = position;
            o.transform.localScale = scale;
            o.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer=o.AddComponent<MeshRenderer>();renderer.sharedMaterial = mat;renderer.shadowCastingMode=ShadowCastingMode.Off;
            if (collision) o.AddComponent<MeshCollider>().sharedMesh = mesh;
            return o;
        }

        float VisibleBed(float x, float z)
        {
            var p = new Vector3(x, 25f, z);
            var hits = Physics.RaycastAll(p, Vector3.down, 60f, ~0, QueryTriggerInteraction.Ignore);
            float y = -100f;
            foreach (var h in hits)
            {
                if (game != null && ((game.boat != null && h.transform.IsChildOf(game.boat))||(game.walker!=null&&h.transform.IsChildOf(game.walker)))) continue;
                y = Mathf.Max(y, h.point.y);
            }
            return y > -90f ? y : HeightAt(x, z);
        }

        bool TryFloraBed(Vector3 center, int seed, float minDepth, out Vector3 point)
        {
            for (int j = 0; j < 13; j++)
            {
                float radius = j == 0 ? 0f : 1.4f + (j - 1) * 0.45f;
                float angle = (seed * 137.5f + j * 111.25f) * Mathf.Deg2Rad;
                float x = center.x + Mathf.Cos(angle) * radius;
                float z = center.z + Mathf.Sin(angle) * radius;
                float bedY = VisibleBed(x, z);
                float depth = SeaLifeRuntime.Sea - bedY;
                if (depth >= minDepth && depth <= 9.5f && x > -145f && x < 185f && z > -155f && z < 165f)
                {
                    point = new Vector3(x, bedY, z);
                    return true;
                }
            }
            point = center;
            return false;
        }

        /// <summary>
        /// 自然群落式海岸礁岩、红枫林与水下珊瑚生态分布（修复 (i/2)/22f 整数除法排排坐 Bug，采用母岩+子石自然聚落簇！）
        /// </summary>
        void PopulateCoastAndBarrier()
        {
            coastCount = 0; reefCount = 0; coralCount = 0; kelpCount = 0; mapleCount = 0;
            var rng = new System.Random(300511);
            Func<float, float, float> rand = (a, b) => Mathf.Lerp(a, b, (float)rng.NextDouble());
            var flora = new List<Vector3>();

            // 1. 南北延展海岸天然礁石聚落簇（共 46 块，分为天然错落的岬角礁群，根部深埋入地面杜绝悬空！）
            for (int i = 0; i < 46; i++)
            {
                bool south = (i % 2 == 0);
                float idx = i * 0.5f;
                // 聚落化分布：围绕几个自然海岬焦点错落散开，彻底告别排成一条直线！
                float baseT = idx / 22.5f;
                float clusterJitter = Mathf.Sin(i * 2.1f) * 0.04f;
                float t = Mathf.Clamp01(baseT + clusterJitter);
                float z = Mathf.Lerp(south ? -24f : 26f, south ? -115f : 118f, t);
                float lateralSpread = rand(-8.5f, 3.2f);
                float x = ShoreX(z) + lateralSpread;
                float groundY = HeightAt(x, z);

                var m = rocks[i % rocks.Length];
                float sx = rand(2.2f, 5.2f);
                float sy = rand(1.1f, 2.4f);
                float sz = rand(2.2f, 5.2f);
                // 将岩石中心下沉 35%，使其像天然基岩一样从沙坡/崖壁中长出来，绝不悬空！
                Vector3 rockPos = new Vector3(x, groundY - sy * 0.32f, z);
                var rockGo = AddMesh("V29_CoastRock_" + i, m, nativeRock, rockPos, new Vector3(sx, sy, sz), true);
                rockGo.transform.localRotation = Quaternion.Euler(rand(-12f, 12f), rand(0f, 360f), rand(-12f, 12f));
                coastCount++;

                if (groundY < SeaLifeRuntime.Sea - 0.3f)
                {
                    flora.Add(new Vector3(x + rand(-2f, 2f), groundY, z + rand(-2f, 2f)));
                }
            }

            // 2. 外海珊瑚礁盘与群岛暗礁群（60 块扁平宽阔的海底礁盘基石，散布在宽达 25m 的礁带内）
            for (int i = 0; i < 60; i++)
            {
                float t = i / 59f;
                float z = Mathf.Lerp(-110f, 115f, t) + rand(-3.5f, 3.5f);
                if (z > -1.0f && z < 10.0f) continue; // 留出主栈桥东向通海航道

                float trench = 11.5f + 3.4f * Mathf.Sin(z * 0.065f - 0.3f) + 0.08f * Mathf.Abs(z);
                float ridgeCenter = trench + 16.5f + 4.0f * Mathf.Cos(z * 0.055f);
                float x = ridgeCenter + rand(-11.0f, 14.0f);
                float bedY = HeightAt(x, z);

                var m = rocks[(i * 3) % rocks.Length];
                bool emergent = (i % 11 == 0) && Mathf.Abs(z) > 18f;
                Vector3 scale = emergent
                    ? new Vector3(rand(4.2f, 7.5f), rand(1.8f, 3.2f), rand(4.2f, 7.5f))
                    : new Vector3(rand(2.8f, 5.5f), rand(0.85f, 1.65f), rand(2.8f, 5.5f));

                Vector3 pos = new Vector3(x, bedY - scale.y * 0.28f, z);
                var rGo = AddMesh("V29_OuterBarrier_" + i, m, nativeRock, pos, scale, true);
                rGo.transform.localRotation = Quaternion.Euler(rand(-8f, 8f), rand(0f, 360f), rand(-8f, 8f));
                reefCount++;

                flora.Add(new Vector3(x, bedY, z));
            }

            Physics.SyncTransforms();

            // 3. 延续原版水下生态：在浅湾与外海礁脉上丰茂生长四色珊瑚簇与摇曳海带林
            var reefShader = Resources.Load<Material>("SeaHarborV27/ReefShaderKeep")?.shader
                          ?? Shader.Find("Deepsea/UnderwaterReefVibrantV27")
                          ?? unifiedTerrainMat.shader;
            var palette = Resources.Load<Texture2D>("SeaHarborV27/Textures/CoralPaletteLUT_V27");
            var coralMats = new Material[4];
            for (int i = 0; i < 4; i++)
            {
                coralMats[i] = new Material(reefShader) { name = "V30_CoralMat_" + i, enableInstancing=true };
                if (palette != null && coralMats[i].HasProperty("_PaletteMap")) coralMats[i].SetTexture("_PaletteMap", palette);
                if (coralMats[i].HasProperty("_PaletteBand")) coralMats[i].SetFloat("_PaletteBand", i);
                if (coralMats[i].HasProperty("_CoralVibrancy")) coralMats[i].SetFloat("_CoralVibrancy", 1.05f);
                _owned.Add(coralMats[i]);
            }

            var kelpMat = new Material(reefShader) { name = "V30_GreenKelp", enableInstancing = true };
            if (kelpMat.HasProperty("_BaseColor")) kelpMat.SetColor("_BaseColor", Color.white);
            if (kelpMat.HasProperty("_IsKelp")) kelpMat.SetFloat("_IsKelp", 1f);
            if (kelpMat.HasProperty("_SwayAmplitude")) kelpMat.SetFloat("_SwayAmplitude", 0.16f);
            _owned.Add(kelpMat);

            var cm = Resources.Load<Mesh>("SeaHarborV27/Baked/Coral_V27_Cluster") ?? Resources.Load<Mesh>("SeaHarborV27/Models/Coral_V27_Cluster");
            var km = Resources.Load<Mesh>("SeaHarborV27/Baked/Kelp_V27_GiantFrond") ?? Resources.Load<Mesh>("SeaHarborV27/Models/Kelp_V27_GiantFrond");

            int n = 0;
            foreach (var p in flora)
            {
                Vector3 at;
                if (cm != null && coralCount < 52 && TryFloraBed(p, n, 0.70f, out at))
                {
                    float budget=SeaLifeRuntime.Sea-.30f-at.y, scale=Mathf.Min(rand(.95f,1.55f),budget/Mathf.Max(.1f,cm.bounds.max.y));
                    var cGo = AddMesh("V29_CoralCluster_" + n, cm, coralMats[n % 4], at, Vector3.one * scale, false);
                    cGo.transform.localRotation = Quaternion.Euler(0f, rand(0f, 360f), 0f);
                    coralCount++;
                }
                n++;
                if (km != null && kelpCount < 42 && TryFloraBed(p, n + 17, 1.20f, out at))
                {
                    var kGo = AddMesh("V29_GiantKelp_" + n, km, kelpMat, at, new Vector3(rand(1.0f,1.35f),Mathf.Min(rand(1.2f,2.2f),(SeaLifeRuntime.Sea-.35f-at.y)/Mathf.Max(.1f,km.bounds.max.y)),rand(1.0f,1.35f)), false);
                    kGo.transform.localRotation = Quaternion.Euler(0f, rand(0f, 360f), 0f);
                    kelpCount++;
                }
            }

            // 4. 在南北延展的饱满半岛山坡上自然布置 18 棵高低错落的原版红枫树林，让陆地生态与原版红枫海岬完全连成一体！
            Mesh trunk = Resources.Load<Mesh>("SeaSeamlessV29/MapleBark");
            Mesh leaves = Resources.Load<Mesh>("SeaSeamlessV29/MapleLeaves");
            if (trunk != null && leaves != null)
            {
                Material barkMat = Resources.Load<Material>("CoastMaple/MapleBark") ?? unifiedTerrainMat;
                Material leafMat = Resources.Load<Material>("CoastMaple/MapleLeaf") ?? unifiedTerrainMat;

                for (int i = 0; i < 18; i++)
                {
                    bool south = (i < 9);
                    float idx = south ? i / 8f : (i - 9) / 8f;
                    float z = Mathf.Lerp(south ? -24f : 26f, south ? -96f : 98f, idx) + rand(-4f, 4f);
                    // 深入内陆草坡 6m ~ 28m 处成林分布，彻底告别单排树！
                    float x = ShoreX(z) - rand(6.0f, 28.0f);
                    float y = HeightAt(x, z);
                    if(y<=SeaLifeRuntime.Sea+.6f){x=ShoreX(z)-Mathf.Max(14,ShoreX(z)-x);y=HeightAt(x,z);}
                    if (y > SeaLifeRuntime.Sea + 0.6f)
                    {
                        var treeRoot = new GameObject("V29_CoastMaple_" + i).transform;
                        treeRoot.SetParent(worldRoot, false);
                        treeRoot.localPosition = new Vector3(x, y - 0.15f, z);
                        treeRoot.localRotation = Quaternion.Euler(0f, rand(0f, 360f), 0f);
                        treeRoot.localScale = Vector3.one * rand(0.82f, 1.32f);
                        AddMesh("Bark", trunk, barkMat, Vector3.zero, Vector3.one, false, treeRoot);
                        AddMesh("Leaves", leaves, leafMat, Vector3.zero, Vector3.one, false, treeRoot);
                        mapleCount++;
                    }
                }
            }

            Physics.SyncTransforms();
        }
    }
}
