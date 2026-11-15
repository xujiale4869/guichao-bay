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
        readonly List<Renderer> treeCasters = new List<Renderer>();
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
        Vector3 tidePoolOriginalPosition, tidePoolOriginalScale;
        float[] originalShellHeight, refinedShellHeight;
        bool presented = true, previousInRoom;
        Vector3 focusCameraPosition, lastBoatTarget, lastShaftCameraPosition, lastShaftDirection;
        Quaternion focusCameraRotation;
        Matrix4x4 focusProjection;
        float nextFocusRefresh, nextShadowRefresh;
        bool focusCurrent, shaftsCurrent;
        int shaftWatch = -1;

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
            BuildDriftingLeavesV381();

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
            AddPlinthPart(runtime,root,PrimitiveType.Sphere,"Foundation rock",
                new Vector3(0,.12f,0),new Vector3(2.2f,1.32f,1.9f),Quaternion.Euler(8,24,-7),stone);
            AddPlinthPart(runtime,root,PrimitiveType.Sphere,"Side rock",
                new Vector3(.72f,-.09f,-.38f),new Vector3(1.08f,.62f,.94f),Quaternion.Euler(9,-35,-12),stone);
            AddPlinthPart(runtime,root,PrimitiveType.Sphere,"Side rock",
                new Vector3(-.69f,-.13f,.39f),new Vector3(.96f,.62f,.98f),Quaternion.Euler(-13,51,6),stone);
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

        static void AddPlinthPart(SeaLifeRuntime runtime, Transform root, PrimitiveType primitive, string partName,
            Vector3 position, Vector3 scale, Quaternion rotation, Material material)
        {
            var part = runtime.Primitive(partName,primitive,root,position,scale,material);
            part.localRotation = rotation;
            if (part.TryGetComponent<Collider>(out var collider)) collider.enabled = false;
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
            nextShadowRefresh = 0;
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
            {
                visual.gameObject.SetActive(sailingView);
                nextShadowRefresh = 0;
            }
            if (Time.unscaledTime >= nextShadowRefresh) RefreshTreeShadows(sailingView,focus);
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
            bool viewChanged = !focusCurrent || (camera.position - focusCameraPosition).sqrMagnitude >= .0001f ||
                Quaternion.Angle(camera.rotation,focusCameraRotation) >= .025f || projection != focusProjection;
            if (!viewChanged && (boatTarget - lastBoatTarget).sqrMagnitude < .0001f &&
                Time.unscaledTime < nextFocusRefresh) return;
            if (viewChanged)
            {
                GeometryUtility.CalculateFrustumPlanes(view,cameraFrustum);
                focusCameraPosition = camera.position;
                focusCameraRotation = camera.rotation;
                focusProjection = projection;
            }
            focusCurrent = true;
            lastBoatTarget = boatTarget;
            nextFocusRefresh = Time.unscaledTime + .15f;
            FocusSailingView(boatTarget);
        }

        void RefreshTreeShadows(bool sailingView, Vector3 focus)
        {
            nextShadowRefresh = Time.unscaledTime + .25f;
            foreach (var caster in treeCasters)
            {
                if (caster == null) continue;
                var bounds = caster.bounds;
                bool casting = caster.shadowCastingMode != ShadowCastingMode.Off;
                float range = casting ? 46f : 42f;
                var size = bounds.size;
                bool needed = sailingView && Mathf.Max(size.x,Mathf.Max(size.y,size.z)) >= 1.2f &&
                    (bounds.center - focus).sqrMagnitude < range * range;
                if (casting != needed)
                    caster.shadowCastingMode = needed ? ShadowCastingMode.On : ShadowCastingMode.Off;
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
                            if (tree) treeCasters.Add(renderer);
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
            SetTidePoolPresentation(false);
            Destroy(tidePoolMesh);
            Destroy(beamMesh);
            Destroy(shadeMesh);
        }
    }
}
