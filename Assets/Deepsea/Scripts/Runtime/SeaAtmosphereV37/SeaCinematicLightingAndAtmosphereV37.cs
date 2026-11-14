using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Deepsea
{
    public sealed partial class SeaLifeRuntime
    {
        internal Light AtmosphereSunV37 => sun;
    }

    [DefaultExecutionOrder(1200)]
    public sealed class SeaCinematicLightingAndAtmosphereV37 : MonoBehaviour
    {
        internal static bool Enabled => true;

        const int MaximumContacts = 64;
        const int Grid = 5;
        const float DefaultFocusDistance = 21.5f;
        internal const float FocalLengthMm = 155f;
        const float MinimumAperture = 3.2f;
        internal const float MaximumSubjectCoC = .042f;
        static readonly int Coast = Shader.PropertyToID("_CoastAtmosphereV37");
        static readonly int Mist = Shader.PropertyToID("_CoastMistV37");
        static readonly int RayColor = Shader.PropertyToID("_RayColor");
        static readonly int ShaftWeight = Shader.PropertyToID("_ShaftWeight");

        static readonly Color[] MistColors = {
            new Color(.46f,.58f,.68f,.18f), new Color(.48f,.66f,.76f,.17f),
            new Color(.52f,.55f,.64f,.20f), new Color(.10f,.20f,.30f,.15f)
        };

        static readonly Vector3[] Origins = {
            new Vector3(-16.2f, 9.4f,  8.2f),
            new Vector3(-14.4f, 8.8f,  5.4f),
            new Vector3(-12.8f, 8.2f,  2.6f),
            new Vector3(-11.8f, 8.4f, -2.2f),
            new Vector3(-10.5f, 7.6f, -5.8f),
            new Vector3( -9.8f, 8.0f,  6.8f),
            new Vector3( -7.6f, 7.6f,  9.4f),
            new Vector3( -6.8f, 7.4f,  4.2f),
            new Vector3( -5.2f, 7.2f, 10.2f)
        };

        static readonly Vector3[] ShaftDimensions = {
            new Vector3(6.4f, 17f, .46f), new Vector3(5.6f, 15f, .38f), new Vector3(6.8f, 15f, .36f),
            new Vector3(5.8f, 16f, .38f), new Vector3(7.2f, 17f, .34f), new Vector3(5.6f, 15f, .36f),
            new Vector3(6.8f, 16f, .30f), new Vector3(6.0f, 14f, .28f), new Vector3(7.6f, 17f, .26f)
        };

        readonly List<Renderer> newCasters = new List<Renderer>();
        readonly List<MeshRenderer> focusLandmarks = new List<MeshRenderer>();
        readonly Plane[] cameraFrustum = new Plane[6];
        SeaLifeRuntime game;
        SeaBayExpansionAndBuildSpaceV28 bay;
        SeaSeamlessWorldAndNauticalUIV29 coast;
        Light sun;
        Transform visual;
        Material beamMaterial;
        Mesh beamMesh, shadeMesh;
        Volume volume;
        VolumeProfile profile;
        DepthOfField dof;
        readonly Transform[] shafts = new Transform[Origins.Length];
        float priorShadowStrength;
        Color daylightBeamColor;
        readonly List<Material> ecologyMaterials = new List<Material>();
        Mesh mantaMesh, gullWingMesh, tidePoolMesh;
        MeshFilter tidePool;
        Mesh originalTidePool;
        bool presented = true, previousInRoom;

        // v38 新增：全局生态生命力动画列表（盘旋海鸥 + 水下银色沙丁鱼群 + 滑翔蝠鲼）
        readonly List<Transform> soaringGulls = new List<Transform>();
        readonly List<Transform> gullLeftWings = new List<Transform>();
        readonly List<Transform> gullRightWings = new List<Transform>();
        readonly List<Transform> schoolingFish = new List<Transform>();
        readonly List<Transform> glidingMantas = new List<Transform>();
        ParticleSystem mapleLeafParticles;

        internal int Contacts { get; private set; }
        internal int Shafts => shafts.Length;
        internal bool ExteriorEffects { get; private set; }
        internal bool FocusActive => dof.active;
        internal float FocusDistance => dof.focusDistance.value;
        internal float FocusAperture => dof.aperture.value;
        internal float SubjectNearDepth { get; private set; }
        internal float SubjectFarDepth { get; private set; }
        internal int VisibleLandmarkCount { get; private set; }
        internal float BeamOpacity => beamMaterial.GetColor(RayColor).a;

        internal static void Attach(SeaLifeRuntime runtime, SeaBayExpansionAndBuildSpaceV28 layout,
            SeaSeamlessWorldAndNauticalUIV29 scenery, Transform roots)
        {
            var effect = runtime.gameObject.AddComponent<SeaCinematicLightingAndAtmosphereV37>();
            effect.Initialize(runtime, layout, scenery, roots);
        }

        void Initialize(SeaLifeRuntime runtime, SeaBayExpansionAndBuildSpaceV28 layout,
            SeaSeamlessWorldAndNauticalUIV29 scenery, Transform roots)
        {
            game = runtime;
            bay = layout;
            coast = scenery;
            sun = game.AtmosphereSunV37;
            priorShadowStrength = sun.shadowStrength;
            visual = new GameObject("V37 Coast Atmosphere").transform;
            visual.SetParent(game.exterior, false);

            beamMaterial = new Material(Resources.Load<Material>("SeaAtmosphereV37/BeamKeep"));
            daylightBeamColor = beamMaterial.GetColor(RayColor);
            beamMesh = MakeBeam();
            for (int i = 0; i < shafts.Length; i++)
            {
                var card = new GameObject("Sun shaft " + (i + 1));
                card.transform.SetParent(visual, false);
                card.transform.localScale = new Vector3(ShaftDimensions[i].x, ShaftDimensions[i].y, 1);
                AddRenderer(card, beamMesh, beamMaterial, ShaftDimensions[i].z);
                shafts[i] = card.transform;
            }
            CollectFocusLandmarks();
            BuildContactShade(roots);
            ConfigureVolume();

            FitCoastalFixturesV38();
            BuildLivingCoastalEcosystemV38();

            ApplyWatch(game.GameplayV34.State.watch);
            Debug.Log("SEA_ATMOSPHERE_V37_READY beams=" + Shafts + " contacts=" + Contacts);
        }

        /// <summary>
        /// v38 全局动态生命力系统：
        /// 1. 5 只在灯塔与海湾上空盘旋滑翔并投下移动地面斜影的海岸白鸥；
        /// 2. 24 条在浅海珊瑚礁边缘结群游动、闪烁银光的沙丁鱼群；
        /// 3. 2 只在深水区优雅滑翔的蝠鲼剪影；
        /// 4. 随海风从红枫林飘向栈桥的红枫落叶粒子。
        /// </summary>
        void BuildLivingCoastalEcosystemV38()
        {
            var ecoRoot = new GameObject("V38_LivingCoastalEcosystem").transform;
            ecoRoot.SetParent(visual, false);

            Shader shader = Shader.Find("Deepsea/V24CoastShowcaseSurface");
            Material gullWhiteMat = EcologyMaterial(shader, "V38_SeagullWhite", new Color(.98f,.97f,.94f));
            Material silverFishMat = EcologyMaterial(shader, "V38_SilverSchoolFish", new Color(.78f,.92f,.98f));
            Material mantaMat = EcologyMaterial(shader, "V38_MantaSilhouette", new Color(.10f,.24f,.34f));
            mantaMesh = BuildMantaMesh();
            gullWingMesh = BuildGullWingMesh();

            // 1. 生成 5 只展翅盘旋的白色海鸥 (带真实地面/水面移动阴影)
            soaringGulls.Clear();
            gullLeftWings.Clear();
            gullRightWings.Clear();
            for (int i = 0; i < 5; i++)
            {
                var gull = new GameObject("V38_Seagull_" + i).transform;
                gull.SetParent(ecoRoot, false);

                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                RemoveDecorationCollider(body.GetComponent<Collider>());
                body.transform.SetParent(gull, false);
                body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                body.transform.localScale = new Vector3(0.14f, 0.28f, 0.12f);
                body.GetComponent<MeshRenderer>().sharedMaterial = gullWhiteMat;

                var lWingPivot = new GameObject("LeftWing").transform;
                lWingPivot.SetParent(gull, false);
                var lWing = new GameObject("Wing");
                lWing.transform.SetParent(lWingPivot, false);
                lWing.transform.localScale = new Vector3(-1,1,1);
                AddRenderer(lWing,gullWingMesh,gullWhiteMat);
                lWing.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;

                var rWingPivot = new GameObject("RightWing").transform;
                rWingPivot.SetParent(gull, false);
                var rWing = new GameObject("Wing");
                rWing.transform.SetParent(rWingPivot, false);
                AddRenderer(rWing,gullWingMesh,gullWhiteMat);
                rWing.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;

                soaringGulls.Add(gull);
                gullLeftWings.Add(lWingPivot);
                gullRightWings.Add(rWingPivot);
            }

            // 2. 生成 24 条水下结群游动的银光沙丁鱼 (Schooling Silver Sardines)
            schoolingFish.Clear();
            for (int i = 0; i < 24; i++)
            {
                var fish = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                RemoveDecorationCollider(fish.GetComponent<Collider>());
                fish.name = "V38_SilverFish_" + i;
                fish.transform.SetParent(ecoRoot, false);
                fish.transform.localScale = new Vector3(0.08f, 0.05f, 0.32f);
                var mr = fish.GetComponent<MeshRenderer>();
                mr.sharedMaterial = silverFishMat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                schoolingFish.Add(fish.transform);
            }

            // 3. 生成 2 只深水区优雅滑翔的蝠鲼剪影 (Gliding Manta Rays)
            glidingMantas.Clear();
            for (int i = 0; i < 2; i++)
            {
                var manta = new GameObject("V38_GlidingManta_" + i).transform;
                manta.SetParent(ecoRoot, false);
                manta.gameObject.AddComponent<MeshFilter>().sharedMesh = mantaMesh;
                var mr = manta.gameObject.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mantaMat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                glidingMantas.Add(manta);
            }

            // 4. 生成从西岸红枫林随风飘向栈桥的红枫落叶粒子 (Drifting Red Maple Leaves)
            var leafGo = new GameObject("V38_DriftingMapleLeaves");
            leafGo.transform.SetParent(ecoRoot, false);
            leafGo.transform.position = new Vector3(-6.5f, 4.2f, 2.5f);
            mapleLeafParticles = leafGo.AddComponent<ParticleSystem>();
            var main = mapleLeafParticles.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 12f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.85f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.09f, 0.16f);
            main.startRotation3D = true;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.92f, 0.32f, 0.18f, 0.88f),
                new Color(0.98f, 0.58f, 0.22f, 0.92f)
            );
            main.maxParticles = 45;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = mapleLeafParticles.emission;
            emission.rateOverTime = 4.2f;

            var shape = mapleLeafParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(14f, 4.5f, 16f);

            var vel = mapleLeafParticles.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(0.45f, 0.95f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.38f, -0.15f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.35f, 0.25f);

            var psRenderer = leafGo.GetComponent<ParticleSystemRenderer>();
            psRenderer.sharedMaterial = Resources.Load<Material>("CoastMaple/MapleLeaf");
            psRenderer.shadowCastingMode = ShadowCastingMode.Off;

            mapleLeafParticles.useAutoRandomSeed = false;
            mapleLeafParticles.randomSeed = 38;
            AnimateEcosystemV38(0f);
            Debug.Log($"V38_ECOLOGY_READY gulls={soaringGulls.Count} fish={schoolingFish.Count} mantas={glidingMantas.Count} leaves={main.maxParticles}");
        }

        void FitCoastalFixturesV38()
        {
            var harborHead = game.world.Find("V27_HarborAndReefArt/V27_PierHeadLanternAndCargo");
            int fixedPosts = 0;
            if (harborHead != null)
            {
                FitPierLanternFoot(harborHead.position);
                fixedPosts++;
            }

            var pool = game.exterior.Find("室外活动/栈桥旁潮池/潮池浅石");
            if (pool != null && pool.TryGetComponent(out tidePool))
            {
                originalTidePool = tidePool.sharedMesh;
                tidePoolMesh = Instantiate(originalTidePool);
                tidePoolMesh.name = "V38 Irregular Tide Pool Rim";
                var vertices = tidePoolMesh.vertices;
                for (int i = 0; i < vertices.Length; i++)
                {
                    var point = vertices[i];
                    float angle = Mathf.Atan2(point.z,point.x);
                    float radius = new Vector2(point.x,point.z).magnitude;
                    float relief = .09f*Mathf.Sin(5*angle+.65f) + .045f*Mathf.Sin(9*angle);
                    point.x *= 1 + relief;
                    point.z *= 1 + relief;
                    point.y += .09f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.29f,.50f,radius))*Mathf.Sin(6*angle);
                    vertices[i] = point;
                }
                tidePoolMesh.vertices = vertices;
                tidePoolMesh.RecalculateNormals();
                tidePoolMesh.RecalculateBounds();
                tidePool.sharedMesh = tidePoolMesh;
            }
            Debug.Log($"V38_COAST_FIXTURES_READY pierFoot={fixedPosts} tidePool={(tidePoolMesh != null)}");
        }

        void FitPierLanternFoot(Vector3 anchor)
        {
            const float sea = SeaLifeRuntime.Sea;
            anchor.y = sea;
            var root = game.Group("V38_PierHeadLanternFoot",visual,Vector3.zero);
            root.position = anchor;
            var stone = Resources.Load<Material>("CoastMaple/Limestone");
            var timber = Resources.Load<Material>("CoastRefinement/Wood");
            game.Primitive("Shore rock",PrimitiveType.Sphere,root,new Vector3(0,-.10f,0),new Vector3(1.38f,.68f,1.2f),stone)
                .localRotation = Quaternion.Euler(12,24,-8);
            game.Primitive("Shore rock",PrimitiveType.Sphere,root,new Vector3(.44f,-.18f,-.30f),new Vector3(.82f,.46f,.66f),stone)
                .localRotation = Quaternion.Euler(8,-35,-11);
            game.Primitive("Shore rock",PrimitiveType.Sphere,root,new Vector3(-.40f,-.19f,.31f),new Vector3(.78f,.48f,.77f),stone)
                .localRotation = Quaternion.Euler(-13,51,6);
            for(int i=0;i<3;i++)
            {
                float angle=i*120*Mathf.Deg2Rad+.4f;
                var pile=game.Primitive("Fender pile",PrimitiveType.Cylinder,root,
                    new Vector3(Mathf.Cos(angle)*.22f,.32f,Mathf.Sin(angle)*.22f),new Vector3(.11f,.42f,.11f),timber);
                pile.localRotation=Quaternion.Euler(7,0,i*5-5);
            }
        }

        Material EcologyMaterial(Shader shader, string materialName, Color tint)
        {
            var material = new Material(shader) { name = materialName };
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Mode", 7);
            ecologyMaterials.Add(material);
            return material;
        }

        static void RemoveDecorationCollider(Collider collider)
        {
            collider.enabled = false;
            Destroy(collider);
        }

        static Mesh BuildGullWingMesh()
        {
            var mesh = new Mesh { name = "V38 Swept Gull Wing" };
            mesh.vertices = new[] {
                new Vector3(0,0,.12f),new Vector3(.25f,.04f,.14f),new Vector3(.48f,.06f,.03f),
                new Vector3(.70f,.04f,-.18f),new Vector3(.53f,.03f,-.145f),
                new Vector3(.31f,.015f,-.065f),new Vector3(0,0,-.09f)
            };
            mesh.triangles = new[] { 0,1,6, 1,5,6, 1,2,5, 2,4,5, 2,3,4 };
            mesh.RecalculateNormals();
            return mesh;
        }

        static Mesh BuildMantaMesh()
        {
            var mesh = new Mesh { name = "V38 Manta Silhouette" };
            mesh.vertices = new[] {
                new Vector3(0,.08f,0), new Vector3(0,0,.56f), new Vector3(-.35f,0,.30f),
                new Vector3(-1.04f,.10f,-.22f), new Vector3(-.30f,0,-.45f), new Vector3(0,0,-.60f),
                new Vector3(.30f,0,-.45f), new Vector3(1.04f,.10f,-.22f), new Vector3(.35f,0,.30f),
                new Vector3(-.035f,0,-1.06f), new Vector3(.018f,0,-1.06f)
            };
            mesh.triangles = new[] { 0,2,1, 0,3,2, 0,4,3, 0,5,4, 0,6,5, 0,7,6, 0,8,7, 0,1,8, 4,9,5, 5,9,10, 5,10,6 };
            mesh.RecalculateNormals();
            return mesh;
        }

        static readonly Vector3[] GullCenters = {
                new Vector3(-2.5f, 7.2f,  2.8f),
                new Vector3( 4.2f, 8.0f,  5.5f),
                new Vector3(-6.8f, 8.6f, -1.5f),
                new Vector3( 7.5f, 7.5f, -3.2f),
                new Vector3( 1.2f, 9.1f,  8.4f)
        };

        void AnimateEcosystemV38(float t)
        {
            float sea = SeaLifeRuntime.Sea;
            for (int i = 0; i < soaringGulls.Count; i++)
            {
                var gull = soaringGulls[i];
                if (gull == null) continue;
                float speed = 0.38f + i * 0.05f;
                float phase = t * speed + i * 1.45f;
                float rx = 6.5f + (i % 3) * 2.2f;
                float rz = 5.2f + (i % 2) * 2.4f;
                Vector3 c = GullCenters[i];
                Vector3 pos = new Vector3(
                    c.x + Mathf.Cos(phase) * rx,
                    c.y + Mathf.Sin(phase * 1.7f) * 0.45f,
                    c.z + Mathf.Sin(phase) * rz
                );
                Vector3 vel = new Vector3(-Mathf.Sin(phase) * rx, Mathf.Cos(phase * 1.7f) * 0.45f, Mathf.Cos(phase) * rz).normalized;
                float bankAngle = Mathf.Sin(phase) * 22f;
                gull.position = pos;
                if (vel.sqrMagnitude > 0.001f)
                    gull.rotation = Quaternion.LookRotation(vel, Vector3.up) * Quaternion.Euler(0f, 0f, bankAngle);

                // 间歇振翅与乘风滑翔交替
                float flapEnvelope = Mathf.SmoothStep(0.15f, 0.85f, Mathf.Sin(t * 0.9f + i * 2.1f) * 0.5f + 0.5f);
                float wingAngle = Mathf.Sin(t * 7.5f + i * 1.3f) * 24f * flapEnvelope + 8f * (1f - flapEnvelope);
                gullLeftWings[i].localRotation = Quaternion.Euler(0f, 0f, wingAngle);
                gullRightWings[i].localRotation = Quaternion.Euler(0f, 0f, -wingAngle);
            }

            // 2. 水下银色沙丁鱼群流沿着浅湾环游
            Vector3 schoolCenter = new Vector3(
                4.8f + Mathf.Cos(t * 0.28f) * 4.2f,
                sea - 0.85f,
                1.5f + Mathf.Sin(t * 0.36f) * 3.6f
            );
            Vector3 schoolDir = new Vector3(-Mathf.Sin(t * 0.28f) * 4.2f, 0f, Mathf.Cos(t * 0.36f) * 3.6f).normalized;
            if (schoolDir.sqrMagnitude < 0.001f) schoolDir = Vector3.forward;
            Vector3 schoolRight = Vector3.Cross(Vector3.up, schoolDir).normalized;

            for (int i = 0; i < schoolingFish.Count; i++)
            {
                var fish = schoolingFish[i];
                if (fish == null) continue;
                float row = (i / 4) - 2.5f;
                float col = (i % 4) - 1.5f;
                float wiggle = Mathf.Sin(t * 5.2f + i * 0.8f) * 0.14f;
                Vector3 offset = schoolDir * (row * 0.52f) + schoolRight * (col * 0.36f + wiggle) + Vector3.up * (Mathf.Sin(i * 1.7f + t * 1.5f) * 0.18f);
                Vector3 position = schoolCenter + offset;
                float bed = coast.HeightAt(position.x,position.z);
                fish.gameObject.SetActive(bed < sea - .45f);
                position.y = Mathf.Min(sea - .28f,Mathf.Max(position.y,bed + .20f));
                fish.position = position;
                fish.rotation = Quaternion.LookRotation(schoolDir + schoolRight * (wiggle * 1.2f), Vector3.up);
            }

            // 3. 深海沟蝠鲼优雅滑翔
            for (int i = 0; i < glidingMantas.Count; i++)
            {
                var manta = glidingMantas[i];
                if (manta == null) continue;
                float mp = t * 0.16f + i * 3.14f;
                Vector3 mPos = new Vector3(
                    13.5f + Mathf.Cos(mp) * 5.5f,
                    sea - 2.6f - i * 0.6f,
                    0.5f + Mathf.Sin(mp) * 6.8f
                );
                Vector3 mDir = new Vector3(-Mathf.Sin(mp) * 5.5f, 0f, Mathf.Cos(mp) * 6.8f).normalized;
                float bed = coast.HeightAt(mPos.x,mPos.z);
                manta.gameObject.SetActive(bed < sea - 1.15f);
                mPos.y = Mathf.Min(sea - .65f,Mathf.Max(mPos.y,bed + .50f));
                manta.position = mPos;
                if (mDir.sqrMagnitude > 0.001f)
                    manta.rotation = Quaternion.LookRotation(mDir, Vector3.up) * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.8f + i) * 8f);
            }
        }

        internal void ApplyWatch(int watch)
        {
            if (!presented || game.InRoom) return;
            if (watch == 3)
            {
                sun.shadowStrength = priorShadowStrength;
                return;
            }
            float pitch = watch == 1 ? 31f : watch == 0 ? 24f : 16f;
            float yaw = watch == 2 ? 34f : -44f;
            sun.transform.rotation = Quaternion.Euler(pitch, yaw, 0);
            sun.color = watch == 2 ? new Color(1, .68f, .42f) : new Color(1, .93f, .79f);
            sun.intensity = watch == 1 ? 2.05f : watch == 0 ? 1.62f : 1.22f;
            sun.shadowStrength = .75f;
            var sky = new Color(.28f, .42f, .56f);
            var bounce = new Color(.25f, .22f, .18f);
            RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientEquatorColor = new Color(.26f, .38f, .48f);
            RenderSettings.ambientGroundColor = bounce;
            var probe = new SphericalHarmonicsL2();
            probe.AddAmbientLight(new Color(.22f, .28f, .34f));
            probe.AddDirectionalLight(Vector3.up, sky, .32f);
            probe.AddDirectionalLight(Vector3.down, bounce, .22f);
            RenderSettings.ambientProbe = probe;
        }

        internal void PresentForReview(bool value)
        {
            if (presented == value) return;
            presented = value;
            foreach (var caster in newCasters)
                if (caster != null) caster.shadowCastingMode = value ? ShadowCastingMode.On : ShadowCastingMode.Off;
            sun.shadowStrength = priorShadowStrength;
            game.SetWatchLightingV34(game.GameplayV34.State.watch);
            if (tidePool != null) tidePool.sharedMesh = value ? tidePoolMesh : originalTidePool;
            RefreshView();
        }

        void LateUpdate()
        {
            RefreshView();
        }

        void RefreshView()
        {
            bool outdoors = presented && !game.InRoom;
            bool sailingView = outdoors && !bay.godBuildMode && !game.view.orthographic;
            ExteriorEffects = sailingView;
            volume.weight = sailingView ? 1 : 0;
            dof.active = sailingView && !game.OnFoot;
            var camera = game.view.transform;
            var focus = game.ActivePosition + Vector3.up * .75f;
            if (dof.active) FocusSailingView(focus);

            int watch = game.GameplayV34.State.watch;
            bool lightChanged = presented && game.InRoom != previousInRoom;
            previousInRoom = game.InRoom;
            if (lightChanged)
            {
                sun.shadowStrength = priorShadowStrength;
                game.SetWatchLightingV34(watch);
            }
            var cool = MistColors[watch];
            Shader.SetGlobalVector(Coast, sailingView ? new Vector4(focus.x,focus.y,focus.z,watch == 3 ? .44f : 1) : Vector4.zero);
            Shader.SetGlobalColor(Mist, cool);
            visual.gameObject.SetActive(sailingView);
            if (!sailingView) return;

            AnimateEcosystemV38(Time.time);

            var rayColor = daylightBeamColor;
            rayColor.a *= watch == 3 ? 0 : watch == 2 ? .80f : 1f;
            beamMaterial.SetColor(RayColor, rayColor);
            float opacity = rayColor.a;
            Vector3 direction = sun.transform.forward;
            for (int i = 0; i < shafts.Length; i++)
            {
                shafts[i].gameObject.SetActive(opacity > 0);
                if (opacity <= 0) continue;
                var center = Origins[i] + direction * (ShaftDimensions[i].y * .42f);
                var side = Vector3.Cross(direction, camera.position - center).normalized;
                if (side.sqrMagnitude < .01f) side = camera.right;
                shafts[i].SetPositionAndRotation(center, Quaternion.LookRotation(Vector3.Cross(side,-direction),-direction));
            }
        }

        void CollectFocusLandmarks()
        {
            Transform exterior = game.exterior;
            foreach (var renderer in exterior.GetComponentsInChildren<MeshRenderer>(true))
            {
                for (var node = renderer.transform; node != null && node != exterior; node = node.parent)
                {
                    string name = node.name;
                    if (!name.StartsWith("EN_Pier_Wood_", System.StringComparison.Ordinal) &&
                        !name.StartsWith("V31_NPC_House_", System.StringComparison.Ordinal) &&
                        name != "HeroLighthouse" && !name.Contains("海洋小馆 · 码头入口")) continue;
                    focusLandmarks.Add(renderer);
                    break;
                }
            }
        }

        void FocusSailingView(Vector3 boatTarget)
        {
            var view = game.view;
            var camera = view.transform;
            float boatDepth = Vector3.Dot(boatTarget - camera.position, camera.forward);
            VisibleLandmarkCount = 0;
            if (!(boatDepth > view.nearClipPlane && boatDepth <= view.farClipPlane))
            {
                SubjectNearDepth = SubjectFarDepth = DefaultFocusDistance;
                dof.focusDistance.value = DefaultFocusDistance;
                dof.aperture.value = MinimumAperture;
                return;
            }

            float near = Mathf.Max(1, boatDepth - 2.4f);
            float far = boatDepth + 2.4f;
            GeometryUtility.CalculateFrustumPlanes(view, cameraFrustum);
            var depthAxes = new Vector3(Mathf.Abs(camera.forward.x), Mathf.Abs(camera.forward.y),
                Mathf.Abs(camera.forward.z));
            foreach (var landmark in focusLandmarks)
            {
                if (landmark == null || !landmark.enabled || !landmark.gameObject.activeInHierarchy) continue;
                Bounds bounds = landmark.bounds;
                if ((bounds.center - boatTarget).sqrMagnitude > 42f * 42f ||
                    !GeometryUtility.TestPlanesAABB(cameraFrustum, bounds)) continue;
                float depth = Vector3.Dot(bounds.center - camera.position, camera.forward);
                float extent = Vector3.Dot(bounds.extents, depthAxes);
                near = Mathf.Min(near, Mathf.Max(1, depth - extent));
                far = Mathf.Max(far, depth + extent);
                VisibleLandmarkCount++;
            }

            float focus = 2f * near * far / (near + far);
            float error = (far - near) / (near + far);
            float focalMetres = FocalLengthMm * .001f;
            float safeAperture = FocalLengthMm * focalMetres * error /
                (Mathf.Max(.01f,focus - focalMetres) * MaximumSubjectCoC);
            SubjectNearDepth = near;
            SubjectFarDepth = far;
            dof.focusDistance.value = focus;
            dof.aperture.value = Mathf.Clamp(safeAperture,MinimumAperture,32f);
        }

        void ConfigureVolume()
        {
            var camera = game.view.GetUniversalAdditionalCameraData();
            camera.renderPostProcessing = true;
            camera.requiresDepthTexture = true;
            var rig = new GameObject("V37 Coast Lens");
            rig.transform.SetParent(transform, false);
            volume = rig.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.weight = 0;
            volume.priority = 205;
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;

            var vignette = profile.Add<Vignette>();
            vignette.color.Override(new Color(.04f,.09f,.14f));
            vignette.intensity.Override(.16f);
            vignette.smoothness.Override(.52f);
            var bloom = profile.Add<Bloom>();
            bloom.threshold.Override(1.08f);
            bloom.intensity.Override(.32f);
            bloom.scatter.Override(.68f);
            bloom.tint.Override(new Color(1,.94f,.84f));
            var color = profile.Add<ColorAdjustments>();
            color.postExposure.Override(.07f);
            color.contrast.Override(6f);
            color.saturation.Override(6f);
            dof = profile.Add<DepthOfField>();
            dof.mode.Override(DepthOfFieldMode.Bokeh);
            dof.focusDistance.Override(DefaultFocusDistance);
            dof.focalLength.Override(FocalLengthMm);
            dof.aperture.Override(MinimumAperture);
        }

        void BuildContactShade(Transform roots)
        {
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            var masks = new List<Color32>();
            foreach (var root in new[] { game.world, roots })
            {
                foreach (var target in root.GetComponentsInChildren<Transform>())
                {
                    string label = target.name;
                    bool tree = label.StartsWith("V29_CoastMaple_") || label.StartsWith("V33_CoastalPine_") ||
                                label.StartsWith("Maple_") || label.StartsWith("BlackPine_");
                    bool rock = label.StartsWith("V29_CoastRock_");
                    bool tower = label == "HeroLighthouse" || label == "灯塔";
                    bool house = label.StartsWith("V31_NPC_House_");
                    if (!tree && !rock && !house && !tower) continue;
                    var position = target.position;
                    if (position.y < SeaLifeRuntime.Sea - 1.3f) continue;
                    if (tree || house || tower)
                    {
                        foreach (var renderer in target.GetComponentsInChildren<MeshRenderer>())
                        {
                            if (renderer.shadowCastingMode != ShadowCastingMode.Off) continue;
                            renderer.shadowCastingMode = ShadowCastingMode.On;
                            newCasters.Add(renderer);
                        }
                    }
                    if (Contacts >= MaximumContacts) continue;
                    float y = coast.HeightAt(position.x,position.z);
                    if (y < SeaLifeRuntime.Sea + .35f) continue;
                    float radius = rock ? Mathf.Clamp(target.lossyScale.x * .52f,.85f,1.8f) : house || tower ? 2.4f : .95f;
                    AddGroundPatch(position,radius,vertices,uv,triangles,masks);
                    Contacts++;
                }
            }
            if (Contacts == 0) return;
            shadeMesh = new Mesh { name = "V37 Ground Contacts" };
            shadeMesh.SetVertices(vertices);
            shadeMesh.SetUVs(0,uv);
            shadeMesh.SetColors(masks);
            shadeMesh.SetTriangles(triangles,0);
            var shadows = new GameObject("Terrain fitted contact shadows");
            shadows.transform.SetParent(visual,false);
            var contactMat = Resources.Load<Material>("SeaAtmosphereV37/ContactKeep");
            AddRenderer(shadows,shadeMesh,contactMat);
        }

        void AddGroundPatch(Vector3 center, float radius, List<Vector3> vertices, List<Vector2> uv,
            List<int> triangles, List<Color32> masks)
        {
            int first = vertices.Count;
            for (int z = 0; z < Grid; z++)
            for (int x = 0; x < Grid; x++)
            {
                var t = new Vector2(x,z) / (Grid-1);
                float px = center.x + (t.x * 2 - 1) * radius;
                float pz = center.z + (t.y * 2 - 1) * radius;
                float y = coast.HeightAt(px,pz);
                vertices.Add(new Vector3(px,y + .035f,pz));
                uv.Add(t);
                byte alpha = (byte)Mathf.RoundToInt(Mathf.InverseLerp(SeaLifeRuntime.Sea + .35f,SeaLifeRuntime.Sea + .85f,y) * 180);
                masks.Add(new Color32(255,255,255,alpha));
                if (x == 0 || z == 0) continue;
                int c = first + z * Grid + x;
                triangles.Add(c-Grid-1);
                triangles.Add(c-1);
                triangles.Add(c-Grid);
                triangles.Add(c-Grid);
                triangles.Add(c-1);
                triangles.Add(c);
            }
        }

        static Mesh MakeBeam()
        {
            var mesh = new Mesh { name = "V37 Sun Shaft" };
            mesh.vertices = new[] {
                new Vector3(-.50f,-.5f,0), new Vector3(.50f,-.5f,0),
                new Vector3(-.38f,.5f,0), new Vector3(.38f,.5f,0)
            };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            mesh.triangles = new[] { 0,2,1, 2,3,1 };
            return mesh;
        }

        static void AddRenderer(GameObject owner, Mesh mesh, Material material, float weight = 1)
        {
            owner.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = owner.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (weight < 1)
            {
                var props = new MaterialPropertyBlock();
                props.SetFloat(ShaftWeight,weight);
                renderer.SetPropertyBlock(props);
            }
        }

        void OnDestroy()
        {
            Shader.SetGlobalVector(Coast,Vector4.zero);
            foreach (var caster in newCasters)
                if (caster != null) caster.shadowCastingMode = ShadowCastingMode.Off;
            if (visual != null) Destroy(visual.gameObject);
            if (volume != null) Destroy(volume.gameObject);
            Destroy(profile);
            Destroy(beamMaterial);
            foreach (var material in ecologyMaterials) Destroy(material);
            Destroy(mantaMesh);
            Destroy(gullWingMesh);
            if (tidePool != null) tidePool.sharedMesh = originalTidePool;
            Destroy(tidePoolMesh);
            Destroy(beamMesh);
            Destroy(shadeMesh);
        }
    }
}
