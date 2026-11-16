using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Deepsea
{
    public sealed partial class SeaLifeRuntime
    {
        internal Light AtmosphereSunV37 => sun;

        internal void SetTideShellHeightV381(int index, float height)
        {
            var p = shellPositions[index];
            p.y = height;
            shellPositions[index] = p;
            shellObjects[index].position = p;
        }

        internal Vector3 TideShellPositionV381(int index) => shellPositions[index];
        internal int TideShellCountV381 => shellPositions.Count;
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
        static readonly int V40Presentation = Shader.PropertyToID("_CoastV40Presentation");
        static readonly int V40HullWorldToLocal = Shader.PropertyToID("_CoastV40HullWorldToLocal");
        static readonly int V40PlantTint = Shader.PropertyToID("_CoastV40PlantTint");

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
        readonly List<MeshRenderer> hiddenClutter = new List<MeshRenderer>();
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
        Mesh tidePoolMesh, originalTidePool, originalTidePoolCollision;
        MeshFilter tidePool;
        MeshCollider tidePoolCollider;
        Renderer tidePoolRenderer, shallowManta;
        Material originalTidePoolMaterial, tidePoolSandstone;
        Transform boatVisual, bowCraft, inlandScenery;
        bool shallowMantaWasEnabled, v40Presented;
        readonly List<Object> v40Assets = new List<Object>();
        readonly List<(Renderer renderer, Material[] original, Material[] refined)> rockBindings =
            new List<(Renderer, Material[], Material[])>();
        Vector3 tidePoolOriginalPosition, tidePoolOriginalScale;
        float[] originalShellHeight, refinedShellHeight;
        bool presented = true, previousInRoom;
        Vector3 focusCameraOffset, lastBoatTarget, lastShaftCameraPosition, lastShaftDirection;
        Quaternion focusCameraRotation;
        Matrix4x4 focusProjection;
        bool focusCurrent, shaftsCurrent;
        int shaftWatch = -1;

        internal int Contacts { get; private set; }
        internal int UplandMapleCount { get; private set; }
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
            BuildDriftingLeavesV381();
            BuildBowCraftV40();
            CollectCoastalRockBindingsV40();
            CollectClutter();
            BuildInlandSceneryV40();

            ApplyWatch(game.GameplayV34.State.watch);
            Debug.Log("SEA_ATMOSPHERE_V37_READY beams=" + Shafts + " contacts=" + Contacts);
        }

        void BuildDriftingLeavesV381()
        {
            var leafGo = new GameObject("V38_DriftingMapleLeaves");
            leafGo.transform.SetParent(visual, false);
            leafGo.transform.position = new Vector3(-6.5f, 4.2f, 2.5f);
            var mapleLeafParticles = leafGo.AddComponent<ParticleSystem>();
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
            Debug.Log($"V381_LEAVES_READY maxParticles={main.maxParticles}");
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
                tidePoolCollider = pool.GetComponent<MeshCollider>();
                if (tidePoolCollider != null) originalTidePoolCollision = tidePoolCollider.sharedMesh;
                tidePoolOriginalPosition = pool.position;
                tidePoolOriginalScale = pool.localScale;
                tidePoolMesh = BuildFacetedStone("V40 Tide Pool Sandstone",true);
                tidePoolRenderer = pool.GetComponent<Renderer>();
                originalTidePoolMaterial = tidePoolRenderer != null ? tidePoolRenderer.sharedMaterial : null;
                var limestone = Resources.Load<Material>("CoastMaple/Limestone");
                if (limestone != null)
                {
                    tidePoolSandstone = new Material(limestone) { name = "V40 Warm Tidal Sandstone" };
                    tidePoolSandstone.SetColor("_BaseColor",new Color(.88f,.76f,.57f));
                    tidePoolSandstone.SetFloat("_NormalStrength",.16f);
                    v40Assets.Add(tidePoolSandstone);
                }
                SetTidePoolPresentation(true);
                Physics.SyncTransforms();
                originalShellHeight = new float[game.TideShellCountV381];
                refinedShellHeight = new float[originalShellHeight.Length];
                for (int i = 0; i < originalShellHeight.Length; i++)
                {
                    Vector3 shell = game.TideShellPositionV381(i);
                    originalShellHeight[i] = refinedShellHeight[i] = shell.y;
                    if (tidePoolCollider != null && tidePoolCollider.Raycast(new Ray(shell + Vector3.up * 2, Vector3.down), out var hit, 4))
                        refinedShellHeight[i] = hit.point.y + .04f;
                    game.SetTideShellHeightV381(i,refinedShellHeight[i]);
                }
            }
            Debug.Log($"V38_COAST_FIXTURES_READY pierFoot={fixedPosts} tidePool={(tidePoolMesh != null)}");
        }

        void SetTidePoolPresentation(bool refined)
        {
            if (tidePool == null) return;
            var pool = tidePool.transform;
            var position = tidePoolOriginalPosition;
            if (refined) position.y = SeaLifeRuntime.Sea - .16f;
            pool.position = position;
            pool.localScale = refined ? new Vector3(6.2f,.92f,4.2f) : tidePoolOriginalScale;
            tidePool.sharedMesh = refined ? tidePoolMesh : originalTidePool;
            if (tidePoolRenderer != null && tidePoolSandstone != null)
                tidePoolRenderer.sharedMaterial = refined ? tidePoolSandstone : originalTidePoolMaterial;
            if (tidePoolCollider != null)
                tidePoolCollider.sharedMesh = refined ? tidePoolMesh : originalTidePoolCollision;
            var heights = refined ? refinedShellHeight : originalShellHeight;
            if (heights != null)
                for (int i = 0; i < heights.Length; i++) game.SetTideShellHeightV381(i,heights[i]);
        }

        void FitPierLanternFoot(Vector3 anchor)
        {
            anchor.y = SeaLifeRuntime.Sea;
            var root = game.Group("V381_PierHeadLanternFoot",visual,Vector3.zero);
            root.position = anchor;
            BuildLanternPlinthV381(game,root,Resources.Load<Material>("CoastMaple/Limestone"),
                Resources.Load<Material>("CoastRefinement/Wood"));
            bay.CombineStaticParts(root.gameObject);
        }

        internal static void BuildLanternPlinthV381(SeaLifeRuntime runtime, Transform root, Material stone, Material timber)
        {
            var coast = runtime.GetComponent<SeaSeamlessWorldAndNauticalUIV29>();
            float bed = coast != null ? coast.HeightAt(root.position.x,root.position.z) : root.position.y - 1.3f;
            float bottom = Mathf.Min(-.3f,bed - root.position.y - .12f);
            var foundation = BuildFacetedStone("V40 Lantern Foundation",false);
            (root.GetComponent<BayOwnedGeometryV28>() ?? root.gameObject.AddComponent<BayOwnedGeometryV28>()).meshes.Add(foundation);
            float rockHeight = (.57f - bottom) / foundation.bounds.size.y;
            float center = bottom - foundation.bounds.min.y * rockHeight;
            AddFoundation(root,foundation,stone,new Vector3(0,center,0),new Vector3(2.6f,rockHeight,2.35f),24);
            AddFoundation(root,foundation,stone,new Vector3(.77f,-.31f,-.35f),new Vector3(1.22f,1.05f,1.08f),-35);
            AddFoundation(root,foundation,stone,new Vector3(-.72f,-.30f,.40f),new Vector3(1.16f,.98f,1.14f),51);
            var corners = new[] { new Vector3(-.72f,0,-.60f),new Vector3(.72f,0,-.60f),
                new Vector3(.72f,0,.60f),new Vector3(-.72f,0,.60f) };
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 corner = corners[i];
                AddPlinthPart(runtime,root,PrimitiveType.Cylinder,"Fender pile",corner + Vector3.up*.20f,
                    new Vector3(.24f,.65f,.24f),Quaternion.identity,timber);
                Vector3 next = corners[(i+1)%corners.Length];
                Vector3 span = next-corner;
                AddPlinthPart(runtime,root,PrimitiveType.Cube,"Mooring collar",(corner+next)*.5f+Vector3.up*.60f,
                    new Vector3(.14f,.15f,span.magnitude+.22f),Quaternion.LookRotation(span),timber);
            }
        }

        static void AddFoundation(Transform parent, Mesh mesh, Material material, Vector3 position, Vector3 scale, float yaw)
        {
            var part = new GameObject("Faceted foundation rock");
            part.transform.SetParent(parent,false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.transform.localRotation = Quaternion.Euler(0,yaw,0);
            AddRenderer(part,mesh,material);
        }

        static void AddPlinthPart(SeaLifeRuntime runtime, Transform root, PrimitiveType primitive, string partName,
            Vector3 position, Vector3 scale, Quaternion rotation, Material material)
        {
            var part = runtime.Primitive(partName,primitive,root,position,scale,material);
            part.localRotation = rotation;
            if (part.TryGetComponent<Collider>(out var collider)) collider.enabled = false;
        }

        void BuildBowCraftV40()
        {
            boatVisual = game.boat.Find("船身随波 · 视觉层") ?? game.boat;
            var shader = Shader.Find("Deepsea/CoastMapleSurface");
            if (shader == null) return;
            var brass = new Material(shader) { name = "V40 Weathered Brass" };
            brass.SetColor("_BaseColor",new Color(.66f,.46f,.23f));
            brass.SetFloat("_Mode",7);
            brass.SetFloat("_NormalStrength",0);
            brass.SetFloat("_Metallic",.74f);
            brass.SetFloat("_Roughness",.39f);
            v40Assets.Add(brass);
            bowCraft = game.Group("V40 Bow Cleat",boatVisual,Vector3.zero);
            AddPlinthPart(game,bowCraft,PrimitiveType.Cylinder,"Cleat mounting plate",
                new Vector3(0,.28f,1.47f),new Vector3(.20f,.025f,.16f),Quaternion.identity,brass);
            AddPlinthPart(game,bowCraft,PrimitiveType.Cylinder,"Cleat post",
                new Vector3(0,.35f,1.47f),new Vector3(.065f,.065f,.065f),Quaternion.identity,brass);
            AddPlinthPart(game,bowCraft,PrimitiveType.Capsule,"Cleat horns",
                new Vector3(0,.41f,1.47f),new Vector3(.043f,.105f,.043f),Quaternion.Euler(0,0,90),brass);
            bay.CombineStaticParts(bowCraft.gameObject);
            bowCraft.gameObject.SetActive(false);
        }

        void CollectCoastalRockBindingsV40()
        {
            var rockShader = Shader.Find("Deepsea/CoastMapleSurface");
            var copies = new Dictionary<Material,Material>();
            foreach (var renderer in game.world.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.name == "V26_ShallowMantaRay")
                {
                    shallowManta = renderer;
                    shallowMantaWasEnabled = renderer.enabled;
                }
                string label = renderer.name;
                bool coastalRock = label.Contains("Land_Rock") || label.StartsWith("EN_MapleFoot",System.StringComparison.Ordinal) ||
                    label.StartsWith("EN_MapleHeadland",System.StringComparison.Ordinal) ||
                    label.StartsWith("EN_Foreground_Rock_",System.StringComparison.Ordinal) ||
                    label.StartsWith("EN_FineShore_LOD",System.StringComparison.Ordinal);
                if (rockShader == null || !coastalRock) continue;
                var original = renderer.sharedMaterials;
                var refined = (Material[])original.Clone();
                for (int i = 0; i < original.Length; i++)
                {
                    var source = original[i];
                    if (source == null) continue;
                    if (!copies.TryGetValue(source,out var rock))
                    {
                        rock = new Material(source) { name = source.name + " V40 rock", shader = rockShader };
                        if (source.HasProperty("_MainTex") && !source.HasProperty("_BaseMap"))
                            rock.SetTexture("_BaseMap",source.GetTexture("_MainTex"));
                        bool authoredUv = source.shader.name == "Deepsea/CoastFineStone" ||
                            source.HasProperty("_Mode") && source.GetFloat("_Mode") > 1.5f;
                        rock.SetFloat("_Mode",authoredUv ? 8f : 0f);
                        rock.SetFloat("_NormalStrength",Mathf.Min(rock.GetFloat("_NormalStrength"),.35f));
                        copies.Add(source,rock);
                        v40Assets.Add(rock);
                    }
                    refined[i] = rock;
                }
                rockBindings.Add((renderer,original,refined));
            }
            Debug.Log($"V40_COAST_ART_READY rockRenderers={rockBindings.Count} shallowManta={(shallowManta != null)}");
        }

        void RefreshV40Presentation(bool value)
        {
            if (v40Presented != value)
            {
                v40Presented = value;
                Shader.SetGlobalFloat(V40Presentation,value ? 1 : 0);
                if (bowCraft != null) bowCraft.gameObject.SetActive(value);
                if (inlandScenery != null) inlandScenery.gameObject.SetActive(value);
                if (shallowManta != null) shallowManta.enabled = !value && shallowMantaWasEnabled;
                foreach (var renderer in hiddenClutter)
                    if (renderer != null) renderer.enabled = !value;
                foreach (var rock in rockBindings)
                    if (rock.renderer != null) rock.renderer.sharedMaterials = value ? rock.refined : rock.original;
            }
            if (value && boatVisual != null)
                Shader.SetGlobalMatrix(V40HullWorldToLocal,boatVisual.worldToLocalMatrix);
        }

        static bool IsLegacyPine(Transform node) =>
            node.name.StartsWith("V33_CoastalPine_",System.StringComparison.Ordinal);

        internal bool ClutterVisibilityMatches(bool visible)
        {
            if (hiddenClutter.Count == 0) return false;
            foreach (var renderer in hiddenClutter)
                if (renderer == null || renderer.enabled != visible) return false;
            return true;
        }

        void CollectClutter()
        {
            foreach (var renderer in game.exterior.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!renderer.enabled) continue;
                var material = renderer.sharedMaterial;
                var parent = renderer.transform.parent;
                if (material != null && parent != null &&
                    parent.name.StartsWith("V33_MeadowCell_",System.StringComparison.Ordinal) &&
                    (material.name == "V33_MeadowGrass" || material.name == "V33_WoodlandFern"))
                {
                    hiddenClutter.Add(renderer);
                    continue;
                }
                for (var node = renderer.transform; node != null && node != game.exterior; node = node.parent)
                {
                    Vector3 position = node.position;
                    bool beachReed = node.name.StartsWith("V31_ShoreReed_",System.StringComparison.Ordinal) &&
                        (position.y < SeaLifeRuntime.Sea+.75f ||
                         coast.CoastBiomeAt(position.x,position.z,position.y+.05f).x <= .42f);
                    if (!beachReed && !IsLegacyPine(node) &&
                        !node.name.StartsWith("V33_Meadow_0_",System.StringComparison.Ordinal) &&
                        !node.name.StartsWith("V33_Meadow_1_",System.StringComparison.Ordinal)) continue;
                    hiddenClutter.Add(renderer);
                    break;
                }
            }
        }

        void BuildInlandSceneryV40()
        {
            var shader = Shader.Find("Deepsea/V24CoastShowcaseSurface");
            if (shader == null) return;
            inlandScenery = game.Group("V40 Inland Scenery",game.exterior,Vector3.zero);
            var exclusions = new List<Bounds>();
            var trees = new List<Vector3>();
            Transform mapleSource = null;
            foreach (var node in game.exterior.GetComponentsInChildren<Transform>())
            {
                if (node.name.StartsWith("V31_NPC_House_",System.StringComparison.Ordinal))
                    exclusions.Add(new Bounds(node.position,new Vector3(8,200,8)));
                else if (node.parent != null && node.parent.name == "V31_VillageStonePaths")
                    exclusions.Add(new Bounds(node.position,new Vector3(2.4f,200,2.4f)));
                else if (node.name.StartsWith("V29_CoastMaple_",System.StringComparison.Ordinal))
                {
                    trees.Add(node.position);
                    if (mapleSource == null) mapleSource = node;
                }
                else if (node.name.StartsWith("EN_RedMaple",System.StringComparison.Ordinal) &&
                         node.name.EndsWith("_Bark_LOD0",System.StringComparison.Ordinal))
                    trees.Add(node.GetComponent<MeshRenderer>().bounds.center);
            }
            exclusions.Add(new Bounds(new Vector3(-9,0,2.8f),new Vector3(8.4f,200,6.4f)));
            BuildUplandGrove(mapleSource,exclusions:exclusions,trees:trees);
            BuildCoastalMeadow(shader,exclusions,trees);

            var stone = Resources.Load<Material>("CoastMaple/Limestone");
            var wallParts = new List<CombineInstance>();
            var blocks = BuildFacetedStone("Terrace wall block",false);
            var starts = new[] { new Vector2(-21,10),new Vector2(-21,-4),new Vector2(-25,-23) };
            var directions = new[] { new Vector2(.22f,.98f),new Vector2(-.24f,-.97f),new Vector2(.34f,.94f) };
            int wallSections = 0;
            for (int wall = 0; wall < starts.Length; wall++)
            {
                var run = FindTerraceRun(starts[wall],directions[wall],exclusions);
                if (run.Count == 0) continue;
                float yaw = Mathf.Atan2(directions[wall].x,directions[wall].y)*Mathf.Rad2Deg;
                for (int i = 0; i < run.Count; i++)
                {
                    var position = run[i];
                    var rotation = Quaternion.Euler(0,yaw+(i%2==0 ? 5 : -5),0);
                    AddInlandPart(wallParts,blocks,position+Vector3.up*.13f,rotation,new Vector3(.72f,.68f,.91f));
                    AddInlandPart(wallParts,blocks,position+Vector3.up*(i%3==0 ? .47f : .39f),
                        Quaternion.Euler(0,yaw+180,0),new Vector3(.63f,.48f,.83f));
                }
                wallSections++;
            }
            if (stone != null) AddInlandBatch("V40_InlandTerraceStoneWalls",wallParts,stone);
            Destroy(blocks);
            inlandScenery.gameObject.SetActive(false);
            Debug.Log($"V40_INLAND_READY wallSections={wallSections} batches={inlandScenery.childCount}");
        }

        List<Vector3> FindTerraceRun(Vector2 start, Vector2 direction, List<Bounds> exclusions)
        {
            const int blockCount = 9;
            var best = new List<Vector3>(blockCount);
            var run = new List<Vector3>(blockCount);
            for (int zShift = -3; zShift <= 3; zShift++)
            for (int xShift = 0; xShift <= 20; xShift++)
            {
                float shift = ((xShift+1)/2)*(xShift%2==0 ? -1.25f : 1.25f);
                var anchor = start + new Vector2(shift,zShift);
                run.Clear();
                for (int i = 0; i < blockCount; i++)
                {
                    var point = anchor+direction*(i*.76f);
                    if (!TryInlandPlacement(point.x,point.y,exclusions,out var position))
                    {
                        run.Clear();
                        continue;
                    }
                    if (run.Count>0 && Mathf.Abs(run[run.Count-1].y-position.y)>.33f)
                        run.Clear();
                    run.Add(position);
                    if (run.Count>best.Count)
                    {
                        best.Clear();
                        best.AddRange(run);
                        if (best.Count==blockCount) return best;
                    }
                }
            }
            return best;
        }

        bool TryInlandPlacement(float x, float z, List<Bounds> exclusions, out Vector3 position)
        {
            float y = coast.HeightAt(x,z);
            position = new Vector3(x,y-.035f,z);
            if (y < SeaLifeRuntime.Sea+.85f || y > SeaLifeRuntime.Sea+9.5f) return false;
            foreach (var bounds in exclusions)
                if (bounds.Contains(position)) return false;
            return Mathf.Abs(coast.HeightAt(x+.25f,z)-coast.HeightAt(x-.25f,z)) < .49f &&
                Mathf.Abs(coast.HeightAt(x,z+.25f)-coast.HeightAt(x,z-.25f)) < .49f;
        }

        void AddInlandPart(List<CombineInstance> batch, Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            batch.Add(new CombineInstance { mesh=mesh, transform=inlandScenery.worldToLocalMatrix*Matrix4x4.TRS(position,rotation,scale) });
        }

        void AddInlandBatch(string name, List<CombineInstance> parts, Material material)
        {
            if (parts.Count == 0) return;
            var combined = new Mesh { name=name, indexFormat=IndexFormat.UInt32 };
            combined.CombineMeshes(parts.ToArray(),true,true);
            var bounds = combined.bounds;
            bounds.Expand(.24f);
            combined.bounds = bounds;
            var owner = game.Group(name,inlandScenery,Vector3.zero);
            AddRenderer(owner.gameObject,combined,material);
            v40Assets.Add(combined);
        }

        void BuildUplandGrove(Transform source, List<Vector3> trees, List<Bounds> exclusions)
        {
            if (source == null) return;
            var bark = source.Find("Bark");
            var leaves = source.Find("Leaves");
            if (bark == null || leaves == null) return;
            var barkMesh = bark.GetComponent<MeshFilter>().sharedMesh;
            var leafSource = leaves.GetComponent<MeshFilter>().sharedMesh;
            var barkMaterial = bark.GetComponent<MeshRenderer>().sharedMaterial;
            var leafMaterial = leaves.GetComponent<MeshRenderer>().sharedMaterial;
            if (barkMesh == null || leafSource == null || barkMaterial == null || leafMaterial == null) return;

            // Preserve complete authored leaf cards and use one third of them in the upland grove.
            // The island's original feature trees retain their full canopies.
            var leafMesh = Instantiate(leafSource);
            var sourceIndices = leafSource.GetTriangles(0);
            var leafIndices = new List<int>(sourceIndices.Length/3);
            for (int first = 0; first+2 < sourceIndices.Length; first+=18)
                for (int index = first; index < Mathf.Min(first+6,sourceIndices.Length); index++)
                    leafIndices.Add(sourceIndices[index]);
            leafMesh.SetTriangles(leafIndices,0);
            v40Assets.Add(leafMesh);

            var random = new System.Random(43029);
            const int tileCount = 4;
            var barkParts = new List<CombineInstance>[tileCount];
            var leafParts = new List<CombineInstance>[tileCount];
            for (int tile = 0; tile < tileCount; tile++)
            {
                barkParts[tile] = new List<CombineInstance>();
                leafParts[tile] = new List<CombineInstance>();
            }
            var shadowVertices = new List<Vector3>();
            var shadowUv = new List<Vector2>();
            var shadowIndices = new List<int>();
            var shadowColors = new List<Color32>();
            const float minimumGap = 3.8f;
            const int target = 16;
            for (int attempt = 0; attempt < 300 && UplandMapleCount < target; attempt++)
            {
                float x = Mathf.Lerp(-44,-19,(float)random.NextDouble());
                float z = Mathf.Lerp(-32,35,(float)random.NextDouble());
                float y = coast.GrassHeightAt(x,z);
                var position = new Vector3(x,y-.12f,z);
                if (coast.CoastBiomeAt(x,z,y).y < .34f || GrassNormal(x,z).y < .78f ||
                    exclusions.Exists(bounds => bounds.Contains(position))) continue;
                if (trees.Exists(tree => HorizontalDistanceSquared(tree,position) < minimumGap*minimumGap)) continue;

                float yaw = UplandMapleCount*137.5f;
                float scale = Mathf.Lerp(.80f,1.09f,(float)random.NextDouble());
                int tile = Mathf.Clamp(Mathf.FloorToInt((z+32)/17),0,tileCount-1);
                var rotation = Quaternion.Euler(0,yaw,0);
                AddInlandPart(barkParts[tile],barkMesh,position,rotation,Vector3.one*scale);
                AddInlandPart(leafParts[tile],leafMesh,position,rotation,Vector3.one*scale);
                trees.Add(position);
                AddGroundPatch(position,2.5f*scale,shadowVertices,shadowUv,shadowIndices,shadowColors,true);
                UplandMapleCount++;
            }
            for (int tile = 0; tile < tileCount; tile++)
            {
                AddInlandBatch("V43 Upland Maple Bark "+tile,barkParts[tile],barkMaterial);
                AddInlandBatch("V43 Upland Maple Leaves "+tile,leafParts[tile],leafMaterial);
            }
            var contact = Resources.Load<Material>("SeaAtmosphereV37/ContactKeep");
            if (contact != null && shadowVertices.Count > 0)
            {
                var shadows = new Mesh { name = "V43 Grove Contacts" };
                shadows.SetVertices(shadowVertices);
                shadows.SetUVs(0,shadowUv);
                shadows.SetColors(shadowColors);
                shadows.SetTriangles(shadowIndices,0);
                var owner = game.Group("V43 Grove Contact Shadows",inlandScenery,Vector3.zero);
                AddRenderer(owner.gameObject,shadows,contact);
                v40Assets.Add(shadows);
            }
        }

        static float HorizontalDistanceSquared(Vector3 first,Vector3 second)
        {
            float x = first.x-second.x;
            float z = first.z-second.z;
            return x*x+z*z;
        }

        Vector3 GrassNormal(float x,float z)
        {
            float xRise = coast.GrassHeightAt(x+.35f,z)-coast.GrassHeightAt(x-.35f,z);
            float zRise = coast.GrassHeightAt(x,z+.35f)-coast.GrassHeightAt(x,z-.35f);
            return new Vector3(-xRise,.70f,-zRise).normalized;
        }

        void BuildCoastalMeadow(Shader shader, List<Bounds> exclusions, List<Vector3> trees)
        {
            const float spacing = .48f;
            const float minimumWeight = .13f;
            const float seaMargin = .45f;
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var colors = new List<Color32>();
            var triangles = new List<int>();
            var meadowRoot = new Color(.22f,.35f,.16f);
            var meadowTip = new Color(.37f,.49f,.23f);
            var duneRoot = new Color(.40f,.38f,.23f);
            var duneTip = new Color(.55f,.51f,.30f);
            var litter = new Color(.31f,.225f,.132f);
            var moss = new Color(.27f,.37f,.18f);
            int duneCells = 0, meadowCells = 0, groveCells = 0;
            uint hash = 43;
            float sea = SeaLifeRuntime.Sea;
            var local = inlandScenery.worldToLocalMatrix;

            void Vertex(Vector3 position, Vector3 normal, Color color)
            {
                vertices.Add(local.MultiplyPoint3x4(position));
                normals.Add(normal);
                colors.Add(color);
            }

            for (int row = 0; row <= 175; row++)
            for (int column = 0; column <= 112; column++)
            {
                hash = unchecked(hash*1664525u+1013904223u);
                float x = -48f+column*spacing+((hash & 255)/255f-.5f)*.24f;
                float z = -42f+row*spacing+(((hash>>8) & 255)/255f-.5f)*.24f;
                float y = coast.GrassHeightAt(x,z);
                if (y < sea+seaMargin) continue;
                var biome = coast.CoastBiomeAt(x,z,y);
                float coverage = SeaSeamlessWorldAndNauticalUIV29.BiomeRamp(minimumWeight,.48f,biome.x);
                var center = new Vector3(x,y,z);
                if (coverage <= 0 || exclusions.Exists(bounds => bounds.Contains(center))) continue;
                float gap = float.PositiveInfinity;
                foreach (var tree in trees) gap = Mathf.Min(gap,HorizontalDistanceSquared(tree,center));
                float forest = Mathf.Max(biome.y,(1-SeaSeamlessWorldAndNauticalUIV29.BiomeRamp(2,24,gap))*.85f);
                float chance = ((hash>>16)&255)/255f;
                if (gap < 1.35f || chance >= coverage*Mathf.Lerp(1,.56f,forest)) continue;
                var normal = GrassNormal(x,z);
                if (normal.y < .37f) continue;

                float growth = SeaSeamlessWorldAndNauticalUIV29.BiomeRamp(minimumWeight,.75f,biome.x);
                float dune = 1-SeaSeamlessWorldAndNauticalUIV29.BiomeRamp(minimumWeight,.65f,biome.x);
                float phase = ((hash>>20) & 255)*(Mathf.PI*2/255);
                Color rootColor = Color.Lerp(Color.Lerp(meadowRoot,duneRoot,dune),litter,forest);
                rootColor.a = 0;
                var forestTip = (hash & 7) == 0 ? new Color(.51f,.32f,.16f) : moss;
                Color tipColor = Color.Lerp(Color.Lerp(meadowTip,duneTip,dune),forestTip,forest);
                Color midColor = Color.Lerp(rootColor,tipColor,.46f);
                midColor.a = .46f;
                int blades = Mathf.RoundToInt(Mathf.Lerp(2,5,growth)*Mathf.Lerp(1,.66f,forest));
                int start = vertices.Count;
                for (int blade = 0; blade < blades; blade++)
                {
                    float angle = phase+blade*(Mathf.PI*2/blades);
                    var heading = new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                    var side = new Vector3(-heading.z,0,heading.x);
                    var foot = center+heading*Mathf.Lerp(.045f,.10f,growth);
                    float height = Mathf.Lerp(.14f,.47f,growth)*Mathf.Lerp(1,.70f,forest) *
                        (.87f+(blade%3)*.12f+((hash>>28)&7)*.014f);
                    float lean = height*(.50f+(blade%2)*.18f);
                    float halfWidth = Mathf.Lerp(.041f,.078f,growth);
                    var left = foot-side*halfWidth;
                    var right = foot+side*halfWidth;
                    float leftHeight = coast.GrassHeightAt(left.x,left.z);
                    float rightHeight = coast.GrassHeightAt(right.x,right.z);
                    if (leftHeight < sea+seaMargin || rightHeight < sea+seaMargin ||
                        coast.CoastBiomeAt(left.x,left.z,leftHeight).x <= minimumWeight ||
                        coast.CoastBiomeAt(right.x,right.z,rightHeight).x <= minimumWeight ||
                        exclusions.Exists(bounds => bounds.Contains(left) || bounds.Contains(right))) continue;

                    left.y = leftHeight-.030f;
                    right.y = rightHeight-.030f;
                    foot.y = (left.y+right.y)*.5f;
                    var middle = foot+heading*(lean*.43f)+Vector3.up*(height*.53f);
                    var tip = foot+heading*lean+Vector3.up*height;
                    int first = vertices.Count;
                    Vertex(left,normal,rootColor);
                    Vertex(right,normal,rootColor);
                    Vertex(middle-side*(halfWidth*.68f),normal,midColor);
                    Vertex(middle+side*(halfWidth*.68f),normal,midColor);
                    Vertex(tip,normal,tipColor);
                    triangles.Add(first); triangles.Add(first+2); triangles.Add(first+1);
                    triangles.Add(first+1); triangles.Add(first+2); triangles.Add(first+3);
                    triangles.Add(first+2); triangles.Add(first+4); triangles.Add(first+3);
                }
                if (vertices.Count == start) continue;
                if (forest > .55f) groveCells++;
                else if (dune > .50f) duneCells++;
                else meadowCells++;
            }

            if (vertices.Count == 0) return;
            var mesh = new Mesh { name = "V43 Natural Coastal Meadow", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles,0);
            var bounds = mesh.bounds;
            bounds.Expand(.50f);
            mesh.bounds = bounds;
            var material = new Material(shader) { name = "V43 Coastal Grass" };
            material.SetFloat("_Mode",9f);
            material.SetFloat("_Sway",.18f);
            var owner = game.Group("V40_InlandWindMeadow",inlandScenery,Vector3.zero);
            AddRenderer(owner.gameObject,mesh,material);
            v40Assets.Add(mesh);
            v40Assets.Add(material);
            Debug.Log($"V43_BIOMES_READY dune={duneCells} meadow={meadowCells} grove={groveCells} " +
                $"maples={UplandMapleCount} grassTriangles={triangles.Count/3} hiddenClutter={hiddenClutter.Count}");
        }

        static Mesh BuildFacetedStone(string name, bool shelf)
        {
            const int sides = 12;
            var heights = shelf ? new[]{-.44f,-.29f,.10f,.205f,.235f} : new[]{-.48f,-.31f,.18f,.37f,.45f};
            var radii = shelf ? new[]{.40f,.53f,.53f,.46f,.34f} : new[]{.34f,.51f,.50f,.38f,.23f};
            var rings = new Vector3[heights.Length,sides];
            for (int ring = 0; ring < heights.Length; ring++)
            for (int side = 0; side < sides; side++)
            {
                float angle = side*Mathf.PI*2/sides+(ring%2)*.12f;
                float radius = radii[ring]*(1+.10f*Mathf.Sin(side*2.03f+.63f));
                float y = heights[ring]+.021f*Mathf.Sin(side*1.74f+ring*.66f);
                rings[ring,side] = new Vector3(Mathf.Cos(angle)*radius,y,Mathf.Sin(angle)*radius);
            }
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            void Face(Vector3 a, Vector3 b, Vector3 c)
            {
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                uv.Add(new Vector2(a.x+.5f,a.z+.5f)); uv.Add(new Vector2(b.x+.5f,b.z+.5f)); uv.Add(new Vector2(c.x+.5f,c.z+.5f));
            }
            for (int side = 0; side < sides; side++)
            {
                int next = (side+1)%sides;
                for (int ring = 1; ring < heights.Length; ring++)
                {
                    Face(rings[ring-1,side],rings[ring,side],rings[ring,next]);
                    Face(rings[ring-1,side],rings[ring,next],rings[ring-1,next]);
                }
                int top = heights.Length-1;
                Face(new Vector3(0,heights[0],0),rings[0,side],rings[0,next]);
                Face(new Vector3(0,heights[top],0),rings[top,next],rings[top,side]);
            }
            var indices = new int[vertices.Count];
            for (int i = 0; i < indices.Length; i++) indices[i]=i;
            var mesh = new Mesh { name=name };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0,uv);
            mesh.SetTriangles(indices,0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            return mesh;
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
            SetTidePoolPresentation(value);
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
            float weight = sailingView ? 1 : 0;
            if (volume.weight != weight) volume.weight = weight;
            bool focusActive = sailingView && !game.OnFoot;
            if (dof.active != focusActive) dof.active = focusActive;
            var focus = game.ActivePosition + Vector3.up * .75f;
            if (focusActive) RefreshFocus(focus);
            else focusCurrent = false;
            RefreshV40Presentation(outdoors);

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
            if (visual.gameObject.activeSelf != sailingView)
                visual.gameObject.SetActive(sailingView);
            if (!sailingView)
            {
                shaftsCurrent = false;
                return;
            }

            if (shaftWatch != watch)
            {
                shaftWatch = watch;
                var rayColor = daylightBeamColor;
                rayColor.a *= watch == 3 ? 0 : watch == 2 ? .80f : 1f;
                beamMaterial.SetColor(RayColor, rayColor);
                foreach (var shaft in shafts) shaft.gameObject.SetActive(rayColor.a > 0);
                shaftsCurrent = false;
            }
            if (watch == 3 || daylightBeamColor.a <= 0) return;
            var camera = game.view.transform;
            Vector3 direction = sun.transform.forward;
            if (shaftsCurrent && (camera.position - lastShaftCameraPosition).sqrMagnitude < .0001f &&
                (direction - lastShaftDirection).sqrMagnitude < .000001f) return;
            shaftsCurrent = true;
            lastShaftCameraPosition = camera.position;
            lastShaftDirection = direction;
            for (int i = 0; i < shafts.Length; i++)
            {
                var center = Origins[i] + direction * (ShaftDimensions[i].y * .42f);
                var side = Vector3.Cross(direction, camera.position - center).normalized;
                if (side.sqrMagnitude < .01f) side = camera.right;
                shafts[i].SetPositionAndRotation(center, Quaternion.LookRotation(Vector3.Cross(side,-direction),-direction));
            }
        }

        void RefreshFocus(Vector3 boatTarget)
        {
            var view = game.view;
            var camera = view.transform;
            var projection = view.projectionMatrix;
            Vector3 offset = camera.position - boatTarget;
            if (focusCurrent && (boatTarget-lastBoatTarget).sqrMagnitude < 2.25f &&
                (offset-focusCameraOffset).sqrMagnitude < 2.25f &&
                Quaternion.Angle(camera.rotation,focusCameraRotation) < 1.5f && projection == focusProjection) return;

            GeometryUtility.CalculateFrustumPlanes(view,cameraFrustum);
            focusCurrent = true;
            lastBoatTarget = boatTarget;
            focusCameraOffset = offset;
            focusCameraRotation = camera.rotation;
            focusProjection = projection;
            FocusSailingView(boatTarget);
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
                    if (IsLegacyPine(target)) continue;
                    bool tree = label.StartsWith("V29_CoastMaple_") ||
                                label.StartsWith("Maple_") || label.StartsWith("BlackPine_");
                    bool rock = label.StartsWith("V29_CoastRock_");
                    bool tower = label == "HeroLighthouse" || label == "灯塔";
                    bool house = label.StartsWith("V31_NPC_House_");
                    if (!tree && !rock && !house && !tower) continue;
                    var position = target.position;
                    if (position.y < SeaLifeRuntime.Sea - 1.3f) continue;
                    if (house || tower)
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
            List<int> triangles, List<Color32> masks, bool grove = false)
        {
            int first = vertices.Count;
            for (int z = 0; z < Grid; z++)
            for (int x = 0; x < Grid; x++)
            {
                var t = new Vector2(x,z) / (Grid-1);
                float px = center.x + (t.x * 2 - 1) * radius;
                float pz = center.z + (t.y * 2 - 1) * radius;
                float y = grove ? coast.GrassHeightAt(px,pz) : coast.HeightAt(px,pz);
                vertices.Add(new Vector3(px,y + .035f,pz));
                uv.Add(t);
                byte alpha = (byte)Mathf.RoundToInt(Mathf.InverseLerp(SeaLifeRuntime.Sea + .35f,SeaLifeRuntime.Sea + .85f,y) * (grove ? 126 : 180));
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
            RefreshV40Presentation(false);
            foreach (var caster in newCasters)
                if (caster != null) caster.shadowCastingMode = ShadowCastingMode.Off;
            if (visual != null) Destroy(visual.gameObject);
            if (volume != null) Destroy(volume.gameObject);
            Destroy(profile);
            Destroy(beamMaterial);
            SetTidePoolPresentation(false);
            Destroy(tidePoolMesh);
            Destroy(beamMesh);
            Destroy(shadeMesh);
            if (bowCraft != null) Destroy(bowCraft.gameObject);
            if (inlandScenery != null) Destroy(inlandScenery.gameObject);
            foreach (var asset in v40Assets) Destroy(asset);
        }
    }
}
