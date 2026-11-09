// v31: Generated multi-material coastal village and six marine morphologies.
// Geometry from the supplied parametric brief; preserve canonical water, boat and character assets.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Deepsea
{
    public sealed partial class SeaSeamlessWorldAndNauticalUIV29
    {
        [Header("v31 海陆生态演替与渔村 NPC 建筑统计")]
        public int v31DiverseFloraCount;
        public int v31ShorelineEcotoneCount;
        public int v31VillageHouseCount;

        private Transform _v31Root;
        private Mesh[] _v31FloraMeshes = new Mesh[6];
        private Mesh _v31ReedMesh;
        private Mesh _v31DriftwoodMesh;
        private Mesh[] _v31HouseMeshes = new Mesh[3];

        private Material _v31EelgrassMat;
        private Material _v31BullKelpMat;
        private Material _v31CoastalReedMat;
        private Material _v31VillageWoodMat;
        private Material _v31VillageRoofMat;
        private Material _v31VillageStoneMat;
        private Material _v31WarmWindowMat;
        private Material[] _v31CoralSpeciesMats = new Material[4];

        /// <summary>
        /// 由 PrepareWorld 自动调用（或运行时自动挂载初始化），构建 v31 多样化生态与海岬渔村
        /// </summary>
        public void BuildVillageAndBiodiversityV31()
        {
            if (!IsV31 || worldRoot == null || _v31Root != null) return;
            var existing = worldRoot.Find("V31_VillageAndBiodiversity");
            if (existing != null) throw new InvalidOperationException("v31 world already initialized");

            _v31Root = new GameObject("V31_VillageAndBiodiversity").transform;
            _v31Root.SetParent(worldRoot, false);

            InitV31MaterialsAndMeshes();
            if (!LushV33)
            {
                ReplaceRepetitiveUnderwaterFloraWithBiodiversity();
                ApplyCoralContrastV311();
            }
            PopulateShorelineEcotoneTransition();
            BuildThreeCoastalNpcVillageHouses();
            MergePathPartsV31();
            BuildBoatyardAnnexV31();
            if (LushV33)
            {
                ApplyLushWorldAndVillageLifeV33();
                ApplyCoralContrastV311();
            }

            SelectMorphologyViewsV31();Physics.SyncTransforms();
        }

        private void InitV31MaterialsAndMeshes()
        {
            var reefShader = Resources.Load<Material>("SeaHarborV27/ReefShaderKeep")?.shader
                          ?? Shader.Find("Deepsea/UnderwaterReefVibrantV27")
                          ?? unifiedTerrainMat.shader;
            var litShader = Resources.Load<Material>("SeaExpansionV28/SurfaceKeep")?.shader
                         ?? Shader.Find("Deepsea/SeaLifeSurface")
                         ?? Shader.Find("Universal Render Pipeline/Lit");
            var glowShader = Resources.Load<Material>("SeaHarborV27/GlowShaderKeep")?.shader
                          ?? Shader.Find("Universal Render Pipeline/Unlit")
                          ?? litShader;

            var palette = Resources.Load<Texture2D>("SeaHarborV27/Textures/CoralPaletteLUT_V27");
            var woodTex = Resources.Load<Texture2D>("SeaHarborV27/Textures/WeatheredPierWood_V27");

            // 1. 水下多样化植物材质 (鳗草嫩翠绿 / 公牛藻深橄榄琥珀绿 / 海岸芦苇暖青金绿)
            _v31EelgrassMat = new Material(reefShader) { name = "V31_EelgrassMeadowMat", enableInstancing = true };
            if (_v31EelgrassMat.HasProperty("_BaseColor")) _v31EelgrassMat.SetColor("_BaseColor", new Color(0.55f, 0.96f, 0.62f, 1f));
            if (_v31EelgrassMat.HasProperty("_IsKelp")) _v31EelgrassMat.SetFloat("_IsKelp", 1f);
            if (_v31EelgrassMat.HasProperty("_SwayAmplitude")) _v31EelgrassMat.SetFloat("_SwayAmplitude", 0.20f);
            _v31EelgrassMat.SetFloat("_SubsurfaceGlow",.08f);
            _owned.Add(_v31EelgrassMat);

            _v31BullKelpMat = new Material(reefShader) { name = "V31_BullKelpMat", enableInstancing = true };
            if (_v31BullKelpMat.HasProperty("_BaseColor")) _v31BullKelpMat.SetColor("_BaseColor", new Color(0.86f, 0.98f, 0.72f, 1f));
            if (_v31BullKelpMat.HasProperty("_IsKelp")) _v31BullKelpMat.SetFloat("_IsKelp", 1f);
            if (_v31BullKelpMat.HasProperty("_SwayAmplitude")) _v31BullKelpMat.SetFloat("_SwayAmplitude", 0.24f);
            _v31BullKelpMat.SetFloat("_SubsurfaceGlow",.08f);
            _owned.Add(_v31BullKelpMat);

            _v31CoastalReedMat = new Material(reefShader) { name = "V31_CoastalReedMat", enableInstancing = true };
            if (_v31CoastalReedMat.HasProperty("_BaseColor")) _v31CoastalReedMat.SetColor("_BaseColor", new Color(0.35f, 0.48f, 0.25f, 1f));
            if (_v31CoastalReedMat.HasProperty("_IsKelp")) _v31CoastalReedMat.SetFloat("_IsKelp", 1f);
            if (_v31CoastalReedMat.HasProperty("_SwayAmplitude")) _v31CoastalReedMat.SetFloat("_SwayAmplitude", 0.14f);
            _v31CoastalReedMat.SetFloat("_SubsurfaceGlow",.04f);_v31CoastalReedMat.SetFloat("_CausticsStrength",0);
            _owned.Add(_v31CoastalReedMat);

            for (int i = 0; i < 4; i++)
            {
                var cm = new Material(reefShader) { name = "V31_CoralSpeciesMat_" + i, enableInstancing = true };
                if (palette != null && cm.HasProperty("_PaletteMap")) cm.SetTexture("_PaletteMap", palette);
                if (cm.HasProperty("_PaletteBand")) cm.SetFloat("_PaletteBand", i);
                if (cm.HasProperty("_CoralVibrancy")) cm.SetFloat("_CoralVibrancy", 1.08f);
                if (cm.HasProperty("_SwayAmplitude")) cm.SetFloat("_SwayAmplitude", i == 0 ? 0.08f : 0.03f);
                cm.SetFloat("_SubsurfaceGlow",.12f);
                _v31CoralSpeciesMats[i] = cm;
                _owned.Add(cm);
            }

            // 2. 渔村建筑材质 (沧桑海盐木墙 / 深色板岩木瓦屋顶 / 防潮海石底座 / 温暖灯火窗格)
            _v31VillageWoodMat = new Material(litShader) { name = "V31_VillageTimberMat", enableInstancing = true };
            if (woodTex != null && _v31VillageWoodMat.HasProperty("_BaseMap")) _v31VillageWoodMat.SetTexture("_BaseMap", woodTex);
            if (_v31VillageWoodMat.HasProperty("_BaseColor")) _v31VillageWoodMat.SetColor("_BaseColor", new Color(0.72f, 0.60f, 0.46f, 1f));
            if (_v31VillageWoodMat.HasProperty("_Smoothness")) _v31VillageWoodMat.SetFloat("_Smoothness", 0.22f);
            _owned.Add(_v31VillageWoodMat);

            _v31VillageRoofMat = new Material(litShader) { name = "V31_VillageSlateRoofMat", enableInstancing = true };
            if (woodTex != null && _v31VillageRoofMat.HasProperty("_BaseMap")) _v31VillageRoofMat.SetTexture("_BaseMap", woodTex);
            if (_v31VillageRoofMat.HasProperty("_BaseColor")) _v31VillageRoofMat.SetColor("_BaseColor", new Color(0.28f, 0.34f, 0.38f, 1f));
            if (_v31VillageRoofMat.HasProperty("_Smoothness")) _v31VillageRoofMat.SetFloat("_Smoothness", 0.30f);
            _owned.Add(_v31VillageRoofMat);

            _v31VillageStoneMat = new Material(nativeRock != null ? nativeRock : _v31VillageWoodMat) { name = "V31_VillageFoundationStone" };
            _owned.Add(_v31VillageStoneMat);

            _v31WarmWindowMat = new Material(glowShader) { name = "V31_WarmCabinWindowGlow" };
            Color warmLight = new Color(1.0f, 0.78f, 0.36f, 1f);
            if (_v31WarmWindowMat.HasProperty("_BaseColor")) _v31WarmWindowMat.SetColor("_BaseColor", warmLight);
            if (_v31WarmWindowMat.HasProperty("_Color")) _v31WarmWindowMat.SetColor("_Color", warmLight);
            _owned.Add(_v31WarmWindowMat);

            // 3. 加载 Blender 生成的 11 个高细节模型（若未找到则调用内置 C# 参数化高细节网格生成器兜底）
            string[] floraPaths =
            {
                "SeaVillageV31/Models/Flora_0_SpiralEelgrass",
                "SeaVillageV31/Models/Flora_1_BullKelpPneumatocyst",
                "SeaVillageV31/Models/Flora_2_GorgonianSeaFan",
                "SeaVillageV31/Models/Flora_3_OrganPipeSponge",
                "SeaVillageV31/Models/Flora_4_TableAcropora",
                "SeaVillageV31/Models/Flora_5_SeaAnemoneCluster"
            };
            for (int i = 0; i < 6; i++)
            {
                _v31FloraMeshes[i] = RequiredV31Mesh(floraPaths[i]);
            }

            _v31ReedMesh = RequiredV31Mesh("SeaVillageV31/Models/Shore_0_CoastalReedAndSedge");
            _v31DriftwoodMesh = RequiredV31Mesh("SeaVillageV31/Models/Shore_1_BeachedDriftwoodSet");

            string[] housePaths =
            {
                "SeaVillageV31/Models/Village_0_BoatwrightHouse",
                "SeaVillageV31/Models/Village_1_HarborTavernInn",
                "SeaVillageV31/Models/Village_2_ScholarObservatory"
            };
            for (int i = 0; i < 3; i++)
            {
                _v31HouseMeshes[i] = RequiredV31Mesh(housePaths[i]);
            }
        }

        private static Mesh RequiredV31Mesh(string path)
        {
            string name = path.Substring(path.LastIndexOf('/')+1);
            var mesh = Resources.Load<Mesh>("SeaVillageV31/Baked/"+name);
            if(mesh==null)throw new InvalidOperationException("Missing v31 baked mesh: "+name+". Run SeaVillageBuildV31.PrepareAndBuild.");
            return mesh;
        }

        // ====================================================================
        // 一、 彻底解决“水下植物重复度高”：将旧版单一克隆替换为 6 类形态各异的生态群落混生
        // ====================================================================
        private void ReplaceRepetitiveUnderwaterFloraWithBiodiversity()
        {
            v31DiverseFloraCount = 0;
            var rng = new System.Random(310801);
            Func<float, float, float> rand = (a, b) => Mathf.Lerp(a, b, (float)rng.NextDouble());

            // 1. 将 SeamlessTerrainV29 中已经生成的 52 组珊瑚与 42 组海带，直接按位置水深与哈希重分配为 6 种截然不同的网格与非均匀姿态！
            foreach (Transform child in worldRoot)
            {
                if (child == null) continue;
                string cn = child.name;
                if (cn.StartsWith("V29_CoralCluster_"))
                {
                    int idx = v31DiverseFloraCount++;
                    // 在 4 种珊瑚/海绵形态 (柳珊瑚海扇 / 风琴管状海绵 / 三层桌形轴孔珊瑚 / 触手海葵簇 + 原版鹿角珊瑚) 之间交错混生
                    int morphType = idx % 5;
                    var mf = child.GetComponent<MeshFilter>();
                    var mr = child.GetComponent<MeshRenderer>();
                    if (mf != null && morphType < 4)
                    {
                        mf.sharedMesh = _v31FloraMeshes[2 + morphType]; // 2:SeaFan, 3:PipeSponge, 4:TableCoral, 5:Anemone
                    }
                    if (mr != null)
                    {
                        mr.sharedMaterial = _v31CoralSpeciesMats[(idx * 3 + morphType) % 4];
                    }
                    // 赋予非均匀长宽比与贴合礁石斜坡的自然微倾角，彻底打破克隆感
                    FitFloraV31(child, morphType<4?2+morphType:-1, rand(.85f,1.35f),rand(.80f,1.35f),rand(0,360));

                }
                else if (cn.StartsWith("V29_GiantKelp_"))
                {
                    int idx = v31DiverseFloraCount++;
                    var mf = child.GetComponent<MeshFilter>();
                    var mr = child.GetComponent<MeshRenderer>();
                    float depth = SeaLifeRuntime.Sea - child.localPosition.y;

                    // 浅水区 (< 2.2m) 替换为柔美的螺旋鳗草甸；深水区 (>= 2.2m) 交替使用气囊公牛藻与宽叶巨藻
                    if (depth < 2.2f || idx % 3 == 0)
                    {
                        if (mf != null) mf.sharedMesh = _v31FloraMeshes[0]; // SpiralEelgrass
                        if (mr != null) mr.sharedMaterial = _v31EelgrassMat;
                    }
                    else if (idx % 3 == 1)
                    {
                        if (mf != null) mf.sharedMesh = _v31FloraMeshes[1]; // BullKelpPneumatocyst
                        if (mr != null) mr.sharedMaterial = _v31BullKelpMat;
                    }
                    FitFloraV31(child, depth<2.2f||idx%3==0?0:idx%3==1?1:-1,rand(.85f,1.25f),rand(.85f,1.3f),rand(0,360));
                }
            }

            // 2. 在内湾浅沙底与南/北浅湾额外铺设 48 簇高低错落的【浅海螺旋鳗草草甸 + 礁边海扇 + 管状海绵伴生群落】
            var meadowGroup = new GameObject("V31_DiverseSubmergedMeadows").transform;
            meadowGroup.SetParent(_v31Root, false);

            for (int i = 0; i < 48; i++)
            {
                float z = Mathf.Lerp(-88f, 92f, i / 47f) + rand(-3.5f, 3.5f);
                float shore = ShoreX(z);
                // 分布在离岸 3.5m ~ 22m 的清澈浅湾沙床与礁盘过渡带
                float x = shore + rand(3.5f, 22.0f);
                float bedY = VisibleBed(x, z);
                float depth = SeaLifeRuntime.Sea - bedY;
                if (depth < 0.45f || depth > 5.8f) continue;

                // One primary plant per accepted site; actual successful placements are counted.
                Vector3 center = new Vector3(x, bedY, z);
                int primaryType = (depth < 1.8f) ? 0 : ((i % 4 == 0) ? 1 : (2 + (i % 4)));
                Mesh mainMesh = _v31FloraMeshes[primaryType];
                Material mainMat = primaryType == 0 ? _v31EelgrassMat : (primaryType == 1 ? _v31BullKelpMat : _v31CoralSpeciesMats[i % 4]);

                float maxH = Mathf.Max(0.25f, depth - 0.28f);
                float sy = Mathf.Min(rand(0.75f, 1.45f), maxH / Mathf.Max(0.2f, mainMesh.bounds.max.y));
                float sxz = rand(0.85f, 1.45f);

                var go = AddMesh("V31_BioFlora_" + i, mainMesh, mainMat, center, new Vector3(sxz, sy, sxz), false, meadowGroup);
                FitFloraV31(go.transform,primaryType,sxz,sy,rand(0,360));
                v31DiverseFloraCount++;
            }
        }

        // ====================================================================
        // 二、 彻底解决“海陆生态衔接不够出色”：构建潮间带【防风芦苇 + 搁浅浮木 + 藤壶湿石 + 涉水草丛】过渡走廊
        // ====================================================================
        private void PopulateShorelineEcotoneTransition()
        {
            v31ShorelineEcotoneCount = 0;
            var ecotoneGroup = new GameObject("V31_ShorelineEcotoneTransition").transform;
            ecotoneGroup.SetParent(_v31Root, false);

            var rng = new System.Random(310802);
            Func<float, float, float> rand = (a, b) => Mathf.Lerp(a, b, (float)rng.NextDouble());
            float sea = SeaLifeRuntime.Sea;

            // 沿着南北长达 220m 的海岸水陆交界线 (z ∈ [-105, 108])，在海拔 [-0.25m, +1.65m] 布置自然演替带：
            // 水际线以下 (-0.25m ~ +0.15m): 涉水芦苇丛 + 潮池海葵与藤壶石
            // 湿沙潮间带 (+0.10m ~ +0.75m): 搁浅沧桑浮木 + 海岸防风芒草丛
            // 岸坡草地前沿 (+0.65m ~ +1.65m): 浓密海岸芦苇群落 + 海岸矮灌木簇
            for (int i = 0; i < 68; i++)
            {
                float z = Mathf.Lerp(-102f, 105f, i / 67f) + rand(-2.2f, 2.2f);
                // 避开主栈桥与海洋小馆入口通道 (z ∈ [-2, 11])
                if (z > -2.5f && z < 11.5f) continue;

                float shoreX = ShoreX(z);
                // 在岸线两侧 [-5.5m (岸坡), +2.2m (浅水滩)] 之间形成有机交错带
                float offsetFromShore = rand(-5.5f, 2.2f);
                float x = shoreX + offsetFromShore;
                if(ReservedV31(x,z))continue;
                float y = V31Ground(x, z);
                float relH = y - sea;

                if (relH < -0.45f || relH > 2.2f) continue;

                // 1. 布置成簇的海岸防风芦苇与涉水芒草 (连接陆地草坡与水中海草的桥梁！)
                var reedGo = AddMesh(
                    "V31_ShoreReed_" + i,
                    _v31ReedMesh,
                    _v31CoastalReedMat,
                    new Vector3(x, y - 0.05f, z),
                    new Vector3(rand(0.9f, 1.6f), rand(0.75f, 1.45f), rand(0.9f, 1.6f)),
                    false,
                    ecotoneGroup
                );
                reedGo.transform.localRotation = Quaternion.Euler(rand(-8f, 8f), rand(0f, 360f), rand(-8f, 8f));
                v31ShorelineEcotoneCount++;

                // 2. 每隔 3 处布置一段半埋在潮间带湿沙中的沧桑浮木与海蚀鹅卵石
                if (i % 3 == 0 && relH > -0.15f && relH < 1.1f)
                {
                    float dx=x+rand(-1.2f,1.2f), dz=z+rand(-1.2f,1.2f);
                    Vector3 dwPos = new Vector3(dx,V31Ground(dx,dz)-.04f,dz);
                    var dwGo = AddMesh(
                        "V31_ShoreDriftwood_" + i,
                        _v31DriftwoodMesh,
                        _v31VillageWoodMat,
                        dwPos,
                         new Vector3(rand(0.85f, 1.45f), rand(0.85f, 1.25f), rand(0.85f, 1.45f)),
                        false,
                        ecotoneGroup
                    );
                    dwGo.GetComponent<MeshRenderer>().sharedMaterials=MaterialsV31(_v31DriftwoodMesh.name);
                    dwGo.transform.localRotation = Quaternion.Euler(rand(-6f, 6f), rand(0f, 360f), rand(-10f, 10f));
                    v31ShorelineEcotoneCount++;
                }
            }
        }

        // ====================================================================
        // 三、 彻底解决“陆地平坦区域太空”：建造 3 座海岬渔村 NPC 主题建筑、石板小径与生活院落
        // ====================================================================
        private void BuildThreeCoastalNpcVillageHouses()
        {
            v31VillageHouseCount = 0;
            var villageGroup = new GameObject("V31_CoastalNpcVillage").transform;
            villageGroup.SetParent(_v31Root, false);

            // 规划三座错落有致、面朝大海的海岬渔村 NPC 建筑据点：
            // 1. 【南湾 · 老船匠的船坞工坊】(z = -28m，紧靠南湾浅滩，前廊修船木台直接伸向水面)
            float zSouth = -28.0f;
            float xSouth = ShoreX(zSouth) - 4.2f;
            BuildNpcHouseCompound(
                villageGroup,
                0,
                "V31_NPC_House_0_老船匠船坞工坊",
                new Vector3(xSouth, HeightAt(xSouth, zSouth), zSouth),
                Quaternion.Euler(0f, 78f, 0f)
            );

            // 2. 【中岸 · 海风酒馆与渔获商栈】(z = -12.5m，位于主栈桥西南侧开阔平坦草坡上，一眼可见)
            float zMid = -12.5f;
            float xMid = ShoreX(zMid) - 8.5f;
            BuildNpcHouseCompound(
                villageGroup,
                1,
                "V31_NPC_House_1_海风酒馆商栈",
                new Vector3(xMid, HeightAt(xMid, zMid), zMid),
                Quaternion.Euler(0f, 96f, 0f)
            );

            // 3. 【北岬 · 潮汐学者的望海观测屋】(z = +32.0m，位于北侧红枫半岛缓坡台地上，俯瞰北湾珊瑚礁)
            float zNorth = 32.0f;
            float xNorth = ShoreX(zNorth) - 7.8f;
            BuildNpcHouseCompound(
                villageGroup,
                2,
                "V31_NPC_House_2_潮汐学者观测屋",
                new Vector3(xNorth, HeightAt(xNorth, zNorth), zNorth),
                Quaternion.Euler(0f, 108f, 0f)
            );

            // 4. 铺设连接三座 NPC 房屋与码头入口的【海岸石板踏步小径 + 路旁木桩暖光挂灯】
            BuildVillageSteppingStonePaths(villageGroup,housesV31[0].localPosition,housesV31[1].localPosition,housesV31[2].localPosition);
        }

        readonly List<Transform> housesV31=new List<Transform>();
        private void BuildNpcHouseCompound(Transform parent,int houseType,string name,Vector3 groundPos,Quaternion rotation)
        {
            groundPos=ChooseVillagePlotV31(groundPos,rotation,houseType);
            var compound=new GameObject(name).transform;compound.SetParent(parent,false);
            float low=float.PositiveInfinity, high=float.NegativeInfinity;
            for(int ix=-1;ix<=1;ix++)for(int iz=-1;iz<=1;iz++){
                Vector3 p=groundPos+rotation*new Vector3(ix*3.0f,0,iz*3.0f);float y=V31Ground(p.x,p.z);low=Mathf.Min(low,y);high=Mathf.Max(high,y);
            }
            groundPos.y=high-.20f;compound.localPosition=groundPos;compound.localRotation=rotation;
            var mesh=_v31HouseMeshes[houseType];
            var model=AddMesh("Authored_"+mesh.name,mesh,_v31VillageWoodMat,Vector3.zero,Vector3.one,false,compound);
            var craft = CoastalCraftsmanshipUpgradeV32.For(this);
            if (craft == null)
                model.GetComponent<MeshRenderer>().sharedMaterials=MaterialsV31(mesh.name);
            else
                craft.AssignAuthored(model, new[] { "Boatwright", "Tavern", "Observatory" }[houseType],
                    _v31WarmWindowMat, BrassV31(), _v31VillageWoodMat);
            // Continuous ground skirt spans the sampled slope; facade and original routes stay unchanged.
            float height=Mathf.Max(.35f,high-low+.5f);
            AddBoxPart("GroundedSeaStoneSkirt",compound,new Vector3(houseType==1?1.0f:houseType==2?.6f:0,.35f-height*.5f,0),new Vector3(houseType==1?8.4f:houseType==2?6.5f:4.9f,height,houseType==1?6.8f:5.1f),Quaternion.identity,_v31VillageStoneMat,true);
            var collider=model.AddComponent<BoxCollider>();collider.center=new Vector3(0,1.8f,0);collider.size=new Vector3(houseType==1?5.6f:4.2f,3.2f,houseType==1?6.2f:4.6f);
            BuildCourtyardV31(compound,houseType);housesV31.Add(compound);v31VillageHouseCount++;
        }

        Vector3 ChooseVillagePlotV31(Vector3 desired,Quaternion rotation,int kind){
            // Occupy stable inland shoulder rather than placing a tall plinth across native cliffs.
            var canopies=game.world.GetComponentsInChildren<MeshRenderer>().Where(r=>{
                string n=(r.name+" "+(r.transform.parent!=null?r.transform.parent.name:"")+" "+(r.sharedMaterial!=null?r.sharedMaterial.name:"")).ToLowerInvariant();
                return !n.Contains("substrate")&&!n.Contains("groundleaves")&&!n.Contains("shadow")&&(n.Contains("leaves")||n.Contains("leaf")||n.Contains("mapletree")||n.Contains("redmaple"));
            }).ToArray();
            float best=float.PositiveInfinity;Vector3 chosen=desired;Bounds chosenBounds=new Bounds();
            foreach(float dz in new[]{0f,-5,5,-10,10,-15,15,-20,20})foreach(float inland in new[]{7f,10,13,16,19,22,25,28,31}){
                float z=desired.z+dz,x=ShoreX(z)-inland;float lo=float.PositiveInfinity,hi=float.NegativeInfinity;
                for(int a=-1;a<=1;a++)for(int b=-1;b<=1;b++){
                    var q=new Vector3(x,0,z)+rotation*new Vector3(a*(kind==1?4.0f:3.0f),0,b*3.0f);float y=V31Ground(q.x,q.z);lo=Mathf.Min(lo,y);hi=Mathf.Max(hi,y);
                }
                var front=new Vector3(x,0,z)+rotation*new Vector3(0,0,6.0f);if(kind==0&&hi-V31Ground(front.x,front.z)>1.3f)continue;
                if(lo<SeaLifeRuntime.Sea+.4f||hi-lo>1.05f||housesV31.Any(h=>new Vector2(h.position.x-x,h.position.z-z).sqrMagnitude<225))continue;
                var local=_v31HouseMeshes[kind].bounds;var wb=new Bounds(new Vector3(x,hi+2.4f,z),Vector3.zero);
                for(int c=0;c<8;c++)wb.Encapsulate(new Vector3(x,hi-.2f,z)+rotation*new Vector3((c&1)==0?local.min.x:local.max.x,(c&2)==0?local.min.y:local.max.y,(c&4)==0?local.min.z:local.max.z));
                wb.Expand(new Vector3(1.2f,0,1.2f));
                if(canopies.Any(b=>b.bounds.Intersects(wb)&&GeneratedMapleRootV31(b.transform)==null))continue;
                int moveCount=canopies.Where(b=>b.bounds.Intersects(wb)).Select(b=>GeneratedMapleRootV31(b.transform)).Where(t=>t!=null).Distinct().Count();
                float score=(hi-lo)*10+Mathf.Abs(dz)*.32f+inland*.8f+moveCount*2;
                if(score<best){best=score;chosen=new Vector3(x,hi-.20f,z);chosenBounds=wb;}
            }
            if(float.IsInfinity(best))throw new InvalidOperationException("No clear gentle plot for v31 house "+kind);
            foreach(var tree in canopies.Where(b=>b.bounds.Intersects(chosenBounds)).Select(b=>GeneratedMapleRootV31(b.transform)).Where(t=>t!=null).Distinct()){
                var pos=tree.localPosition;pos.x-=24;pos.z+=pos.z<chosen.z?-10:10;pos.y=HeightAt(pos.x,pos.z)-.15f;tree.localPosition=pos;relocatedMaplesV31.Add(tree);
            }
            return chosen;
        }

        readonly HashSet<Transform> relocatedMaplesV31=new HashSet<Transform>();
        Transform GeneratedMapleRootV31(Transform t){while(t!=null&&t!=worldRoot){if(t.name.StartsWith("V29_CoastMaple_"))return t;t=t.parent;}return null;}
        Transform boatyardAnnexV31;float annexDepthV31;
        void BuildBoatyardAnnexV31(){
            float z,x,dep;List<Vector3> route;bool found=FindSlipwaySiteV31(housesV31[0].position.z,out x,out z,out dep,out route);annexDepthV31=dep;
            if(!found)throw new InvalidOperationException("No rooted shallow site for boatwright slipway");
            var source=_v31HouseMeshes[0];var vertices=source.vertices;var all=source.GetTriangles(0);var indices=new List<int>();
            for(int i=0;i<all.Length;i+=3){var a=vertices[all[i]];var b=vertices[all[i+1]];var c=vertices[all[i+2]];if((a.z+b.z+c.z)/3>3&&Mathf.Max(a.y,Mathf.Max(b.y,c.y))<1.8f)indices.AddRange(new[]{all[i],all[i+1],all[i+2]});}
            var used=indices.Distinct().ToArray();var map=used.Select((id,i)=>new{id,i}).ToDictionary(q=>q.id,q=>q.i);var normals=source.normals;var uv=source.uv;var mesh=new Mesh{name="V31_AuthoredBoatwrightSlipway"};mesh.vertices=used.Select(id=>vertices[id]).ToArray();mesh.normals=used.Select(id=>normals[id]).ToArray();mesh.uv=used.Select(id=>uv[id]).ToArray();var deck=new List<int>();var hull=new List<int>();for(int j=0;j<indices.Count;j+=3){var target=(vertices[indices[j]].y+vertices[indices[j+1]].y+vertices[indices[j+2]].y)/3>.62f?hull:deck;target.AddRange(new[]{map[indices[j]],map[indices[j+1]],map[indices[j+2]]});}mesh.subMeshCount=2;mesh.SetTriangles(deck,0);mesh.SetTriangles(hull,1);mesh.RecalculateBounds();_owned.Add(mesh);
            var rotation=Quaternion.Euler(0,78,0);var center=new Vector3(x,SeaLifeRuntime.Sea+.08f,z);
            var go=AddMesh(mesh.name,mesh,_v31VillageWoodMat,center-rotation*new Vector3(0,0,4.4f),Vector3.one,true,_v31Root.Find("V31_CoastalNpcVillage"));go.transform.localRotation=rotation;go.GetComponent<MeshRenderer>().sharedMaterials=new[]{_v31VillageWoodMat,HullAccentV31()};
            CoastalCraftsmanshipUpgradeV32.For(this)?.AssignAuthored(go, "Slipway",
                _v31WarmWindowMat, BrassV31(), _v31VillageWoodMat);
            boatyardAnnexV31=go.transform;BuildSlipwayBridgeV31(route);
            // Reuse the authored deck, hull ribs and piles; add supports all the way to the actual bed.
            for(int i=0;i<4;i++){var top=go.transform.TransformPoint(new Vector3((i&1)==0?-1.7f:1.7f,.36f,(i&2)==0?3.2f:5.5f));float floor=V31Ground(top.x,top.z)-.10f;float h=Mathf.Max(.2f,top.y-floor);var support=AddBoxPart("SlipwayRootedPile_"+i,_v31Root,new Vector3(top.x,floor+h*.5f,top.z),new Vector3(.18f,h,.18f),Quaternion.identity,_v31VillageWoodMat,true);}
        }

        private void BuildVillageSteppingStonePaths(Transform parent, Vector3 southHouse, Vector3 midHouse, Vector3 northHouse)
        {
            var pathGroup = new GameObject("V31_VillageStonePaths").transform;
            pathGroup.SetParent(parent, false);

            // 从南湾船坞 -> 海风酒馆 -> 码头小馆 -> 北岬学者屋，沿海岸缓坡铺设 36 块自然弯曲的石板踏步与路边木围栏柱
            Vector3[] waypoints =
            {
                southHouse + new Vector3(2.5f, 0f, 2.0f),
                midHouse + new Vector3(2.8f, 0f, 1.5f),
                new Vector3(-11.5f, 0f, 1.5f),
                new Vector3(-12.0f, 0f, 16.0f),
                northHouse + new Vector3(2.5f, 0f, -1.5f)
            };

            for (int seg = 0; seg < waypoints.Length - 1; seg++)
            {
                Vector3 a = waypoints[seg];
                Vector3 b = waypoints[seg + 1];
                int steps = 9;
                for (int s = 0; s < steps; s++)
                {
                    float t = s / (float)steps;
                    Vector3 p = Vector3.Lerp(a, b, t);
                    p.x += Mathf.Sin(t * Mathf.PI * 2f + seg) * 0.85f;
                    float y = V31Ground(p.x, p.z);
                    if (y < SeaLifeRuntime.Sea + 0.25f) continue;

                    AddBoxPart(
                        $"石板路踏步_{seg}_{s}",
                        pathGroup,
                        new Vector3(p.x, y + 0.03f, p.z),
                        new Vector3(1.15f, 0.12f, 0.85f),
                        Quaternion.Euler(0f, (seg * 35f + s * 19f), 0f),
                        _v31VillageStoneMat,
                        false
                    );

                    // 每隔 4 块踏步竖立一根路边低矮木栓柱
                    if (s % 4 == 0)
                    {
                        AddBoxPart(
                            $"路旁木柱_{seg}_{s}",
                            pathGroup,
                            new Vector3(p.x + 0.85f, y + 0.38f, p.z),
                            new Vector3(0.16f, 0.85f, 0.16f),
                            Quaternion.identity,
                            _v31VillageWoodMat,
                            false
                        );
                    }
                }
            }
        }

        private GameObject AddBoxPart(string name, Transform parent, Vector3 localPos, Vector3 localScale, Quaternion localRot, Material mat, bool withCollider)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;
            Mesh cubeMesh = CubeV31();
            go.AddComponent<MeshFilter>().sharedMesh = cubeMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            if (withCollider)
            {
                go.AddComponent<BoxCollider>();
            }
            CoastalCraftsmanshipUpgradeV32.For(this)?.VillagePart(go,
                mat == _v31VillageStoneMat, mat == _v31VillageWoodMat);
            return go;
        }

        Mesh cubeV31;
        Mesh CubeV31(){if(cubeV31!=null)return cubeV31;var t=GameObject.CreatePrimitive(PrimitiveType.Cube);t.SetActive(false);cubeV31=t.GetComponent<MeshFilter>().sharedMesh;Destroy(t);return cubeV31;}
        [Serializable] public sealed class PartRolesV31 {public string name;public RoleV31[] parts;}
        [Serializable] public sealed class RoleV31 {public string role;}
        Material[] MaterialsV31(string name){
            var data=Resources.Load<TextAsset>("SeaVillageV31/SourceMeshes/"+name);
            var role=JsonUtility.FromJson<PartRolesV31>(data.text);
            return role.parts.Select(p=>p.role=="SlateRoof"?_v31VillageRoofMat:p.role=="SeaStone"?_v31VillageStoneMat:p.role=="WarmGlass"?_v31WarmWindowMat:p.role=="Brass"?BrassV31():_v31VillageWoodMat).ToArray();
        }
        Material brassV31;
        Material BrassV31(){if(brassV31==null){brassV31=new Material(_v31VillageWoodMat){name="V31_WeatheredBrass"};brassV31.SetColor("_BaseColor",new Color(.48f,.38f,.18f));brassV31.SetFloat("_Metallic",.65f);brassV31.SetFloat("_Smoothness",.55f);brassV31.SetTexture("_BaseMap",Texture2D.whiteTexture);brassV31.SetColor("_BaseColor",new Color(.72f,.52f,.20f));_owned.Add(brassV31);}return brassV31;}
        readonly int[] speciesV31=new int[6];
        readonly Transform[] samplesV31=new Transform[6];
        readonly List<Transform> floraV31=new List<Transform>();
        bool ReservedV31(float x,float z){foreach(float hz in new[]{-28f,-12.5f,32f}){float hx=ShoreX(hz)-(hz==-28f?4.2f:hz==32f?7.8f:8.5f);if((x-hx)*(x-hx)+(z-hz)*(z-hz)<64)return true;}return false;}
        float V31Ground(float x,float z){
            float y=-100;foreach(var h in Physics.RaycastAll(new Vector3(x,25,z),Vector3.down,60,~0,QueryTriggerInteraction.Ignore)){
                if((_v31Root!=null&&h.transform.IsChildOf(_v31Root))||(game.boat!=null&&h.transform.IsChildOf(game.boat))||(game.walker!=null&&h.transform.IsChildOf(game.walker)))continue;
                y=Mathf.Max(y,h.point.y);
            }return y>-90?y:HeightAt(x,z);
        }
        void FitFloraV31(Transform t,int species,float width,float height,float yaw){
            var mesh=t.GetComponent<MeshFilter>().sharedMesh;var p=t.position;float ground=V31Ground(p.x,p.z);p.y=ground-.025f;t.position=p;
            float dx=V31Ground(p.x-.4f,p.z)-V31Ground(p.x+.4f,p.z),dz=V31Ground(p.x,p.z-.4f)-V31Ground(p.x,p.z+.4f);
            var normal=new Vector3(dx,.8f,dz).normalized;var tilt=Quaternion.RotateTowards(Quaternion.identity,Quaternion.FromToRotation(Vector3.up,normal),8);
            t.rotation=tilt*Quaternion.Euler(0,yaw,0);t.localScale=new Vector3(width,height,width*(.85f+.25f*Mathf.Abs(Mathf.Sin(yaw))));
            var bounds=mesh.bounds;float top=0;for(int a=0;a<8;a++){var q=new Vector3((a&1)==0?bounds.min.x:bounds.max.x,(a&2)==0?bounds.min.y:bounds.max.y,(a&4)==0?bounds.min.z:bounds.max.z);top=Mathf.Max(top,(t.rotation*Vector3.Scale(q,t.localScale)).y);}
            float available=SeaLifeRuntime.Sea-.30f-p.y;if(top>available)t.localScale*=Mathf.Clamp01(available/Mathf.Max(.01f,top));
            floraV31.Add(t);morphologyV31[t]=species;if(species>=0){speciesV31[species]++;if(samplesV31[species]==null)samplesV31[species]=t;}
        }
        void MergePathPartsV31(){
            var root=_v31Root.Find("V31_CoastalNpcVillage/V31_VillageStonePaths");if(root==null)return;
            var renderers=root.GetComponentsInChildren<MeshRenderer>();foreach(var group in renderers.GroupBy(r=>r.sharedMaterial)){
                var instances=group.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=root.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray();
                var mesh=new Mesh{name="V31_MergedPath_"+group.Key.name};mesh.CombineMeshes(instances,true,true);_owned.Add(mesh);
                var go=AddMesh(mesh.name,mesh,group.Key,Vector3.zero,Vector3.one,false,root);
                foreach(var renderer in group){renderer.gameObject.SetActive(false);Destroy(renderer.gameObject);}
            }
        }
    }
}
