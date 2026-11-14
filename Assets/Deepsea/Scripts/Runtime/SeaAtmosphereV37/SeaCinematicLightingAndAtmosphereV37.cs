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
            new Color(.42f,.54f,.62f,.19f), new Color(.42f,.60f,.67f,.18f),
            new Color(.48f,.51f,.58f,.22f), new Color(.08f,.18f,.27f,.16f)
        };
        static readonly Vector3[] Origins = {
            new Vector3(-14.2f,7.8f,6.2f), new Vector3(-12.6f,6.9f,3.4f), new Vector3(-10.4f,6.2f,1.8f),
            new Vector3(-10.8f,7.6f,-3.6f), new Vector3(-8.7f,6.1f,-6.1f), new Vector3(-8.2f,6.7f,5.8f),
            new Vector3(-4.4f,6.8f,4.6f), new Vector3(-5.6f,7.3f,10.8f), new Vector3(-2.7f,6.4f,8.2f)
        };
        static readonly Vector3[] ShaftDimensions = {
            new Vector3(2.5f,17f,1f), new Vector3(1.8f,14f,.72f), new Vector3(2.9f,14f,.77f),
            new Vector3(2.0f,16f,.68f), new Vector3(3.2f,17f,.60f), new Vector3(2.2f,15f,.70f),
            new Vector3(3.1f,16f,.43f), new Vector3(2.4f,15f,.39f), new Vector3(3.8f,17f,.34f)
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
        float daylightBeamOpacity;
        bool presented = true, previousInRoom;
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
            daylightBeamOpacity = beamMaterial.GetColor(RayColor).a;
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
            ApplyWatch(game.GameplayV34.State.watch);
            Debug.Log("SEA_ATMOSPHERE_V37_READY beams=" + Shafts + " contacts=" + Contacts);
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
            sun.color = watch == 2 ? new Color(1,.64f,.39f) : new Color(1,.92f,.77f);
            sun.intensity = watch == 1 ? 2.15f : watch == 0 ? 1.65f : 1.22f;
            sun.shadowStrength = .92f;
            var sky = new Color(.17f,.28f,.39f);
            var bounce = new Color(.14f,.12f,.10f);
            RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientEquatorColor = new Color(.16f,.24f,.30f);
            RenderSettings.ambientGroundColor = bounce;
            var probe = new SphericalHarmonicsL2();
            probe.AddAmbientLight(new Color(.14f,.18f,.22f));
            probe.AddDirectionalLight(Vector3.up, sky, .24f);
            probe.AddDirectionalLight(Vector3.down, bounce, .14f);
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

            float opacity = watch == 3 ? 0 : watch == 2 ? daylightBeamOpacity * .80f : daylightBeamOpacity;
            beamMaterial.SetColor(RayColor, new Color(1f,.83f,.57f,opacity));
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
                // Distant workshops can leave the focus range as the player sails out of the harbour.
                if ((bounds.center - boatTarget).sqrMagnitude > 42f * 42f ||
                    !GeometryUtility.TestPlanesAABB(cameraFrustum, bounds)) continue;
                float depth = Vector3.Dot(bounds.center - camera.position, camera.forward);
                float extent = Vector3.Dot(bounds.extents, depthAxes);
                near = Mathf.Min(near, Mathf.Max(1, depth - extent));
                far = Mathf.Max(far, depth + extent);
                VisibleLandmarkCount++;
            }

            // URP blur is proportional to 1 - focus/depth. Equalize the near and far subject error.
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
            volume.priority = 205; // The existing coastal profile has priority 200.
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;

            var vignette = profile.Add<Vignette>();
            vignette.color.Override(new Color(.035f,.065f,.095f));
            vignette.intensity.Override(.18f);
            vignette.smoothness.Override(.49f);
            var bloom = profile.Add<Bloom>();
            bloom.threshold.Override(1.16f);
            bloom.intensity.Override(.22f);
            bloom.scatter.Override(.61f);
            bloom.tint.Override(new Color(1,.94f,.85f));
            var color = profile.Add<ColorAdjustments>();
            color.postExposure.Override(.04f);
            color.contrast.Override(6f);
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
                    if (y < SeaLifeRuntime.Sea - .04f) continue;
                    float radius = rock ? Mathf.Clamp(target.lossyScale.x * .65f,1.15f,2.7f) : house || tower ? 3.1f : 1.12f;
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
            AddRenderer(shadows,shadeMesh,Resources.Load<Material>("SeaAtmosphereV37/ContactKeep"));
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
                vertices.Add(new Vector3(px,y + .045f,pz));
                uv.Add(t);
                byte alpha = (byte)Mathf.RoundToInt(Mathf.InverseLerp(SeaLifeRuntime.Sea,SeaLifeRuntime.Sea + .24f,y) * 255);
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
                new Vector3(-.25f,.5f,0), new Vector3(.25f,.5f,0)
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
            Destroy(beamMesh);
            Destroy(shadeMesh);
        }
    }
}
