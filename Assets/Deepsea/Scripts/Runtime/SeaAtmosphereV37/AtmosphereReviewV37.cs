using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Deepsea
{
    public sealed partial class SeaSeamlessWorldAndNauticalUIV29
    {
        [Serializable]
        sealed class AtmosphereEvidenceV37
        {
            public string waterShader, terrainShader;
            public float noonPitch, noonYaw, focusDistance, shoreFocusDistance, focusAperture,
                subjectNearDepth, subjectFarDepth, subjectCoC, noonBeamOpacity;
            public int beams, contacts, visibleLandmarks;
            public bool sameViewFrozenTime, planningKeepsSharpView, nightBeamsOff;
        }

        [Serializable]
        sealed class SailingEvidenceV44
        {
            public int frames;
            public float averageFps, p95FrameMs, boatTravelMeters, headingChangeDegrees;
        }

        IEnumerator ReviewAtmosphereV37()
        {
            var atmosphere = game.GetComponent<SeaCinematicLightingAndAtmosphereV37>();
            Check(atmosphere != null, "v37 component attached to the live sailing scene");
            if (atmosphere == null) yield break;

            for (int i=0; i<4 && game.GameplayV34.State.watch!=1; i++) game.GameplayV34.CycleWatch();
            Check(game.GameplayV34.State.watch==1,"v37 lighting review restores the noon watch");
            game.DriveInput = Vector2.zero;
            game.NoticeTime = 0;
            customSailZoom = 21.5f;
            game.PlaceBoat(new Vector3(9.2f,SeaLifeRuntime.Sea,5.65f),255);
            var angle = Quaternion.Euler(45,-18,0);
            var oldAngle = game.SetWaterReviewAngleV35(angle,customSailZoom);
            yield return new WaitForSecondsRealtime(.8f);

            var sun = game.AtmosphereSunV37;
            var water = waterFilter.GetComponent<Renderer>().sharedMaterial;
            var data = game.view.GetUniversalAdditionalCameraData();
            var proof = new AtmosphereEvidenceV37
            {
                waterShader = water.shader.name,
                terrainShader = nativeSand.shader.name,
                noonPitch = Mathf.DeltaAngle(0,sun.transform.eulerAngles.x),
                noonYaw = Mathf.DeltaAngle(0,sun.transform.eulerAngles.y),
                focusDistance = atmosphere.FocusDistance,
                beams = atmosphere.Shafts,
                contacts = atmosphere.Contacts,
                noonBeamOpacity = atmosphere.BeamOpacity,
                focusAperture = atmosphere.FocusAperture,
                subjectNearDepth = atmosphere.SubjectNearDepth,
                subjectFarDepth = atmosphere.SubjectFarDepth,
                visibleLandmarks = atmosphere.VisibleLandmarkCount
            };
            Check(proof.waterShader == SeaSunnyHarborAudioV36.WaterShader &&
                  game.GetComponent<ApplyMistCapeEmeraldWaterV35>() == null,
                  "v37 preserves the approved v34 water shader");
            Check(proof.terrainShader == "Deepsea/V24CoastShowcaseSurface" && nativeSand.shader.isSupported,
                  "v37 wet shore shader is bound to the continuous land mesh");
            Check(Mathf.Abs(proof.noonPitch-31) < .05f && Mathf.Abs(proof.noonYaw+44) < .05f,
                  "v37 noon light uses the slanted sun");
            Check(proof.beams == 9 && proof.contacts > 12 && proof.contacts <= 64 &&
                  Mathf.Abs(proof.noonBeamOpacity-.24f)<.001f,
                  "v37 nine shafts use .24 noon opacity and bounded terrain contacts");
            float focal = SeaCinematicLightingAndAtmosphereV37.FocalLengthMm;
            float cocScale = focal * focal * .001f / (proof.focusAperture * (proof.focusDistance-focal*.001f));
            proof.subjectCoC = cocScale * Mathf.Max(Mathf.Abs(1-proof.focusDistance / proof.subjectNearDepth),
                Mathf.Abs(1-proof.focusDistance / proof.subjectFarDepth));
            Check(atmosphere.FocusActive && proof.visibleLandmarks > 0 &&
                  proof.focusDistance >= proof.subjectNearDepth && proof.focusDistance <= proof.subjectFarDepth &&
                  proof.subjectCoC <= SeaCinematicLightingAndAtmosphereV37.MaximumSubjectCoC + .0005f &&
                  data.renderPostProcessing && data.requiresDepthTexture,
                  "v37 perspective focus covers the boat and visible nearby landmarks within the blur budget");

            Time.timeScale = 0;
            Vector3 viewPosition = game.view.transform.position;
            Quaternion viewRotation = game.view.transform.rotation;
            atmosphere.PresentForReview(false);
            yield return Capture("v37-baseline-v361");
            atmosphere.PresentForReview(true);
            yield return Capture("v37-preview");
            proof.sameViewFrozenTime = Vector3.Distance(viewPosition,game.view.transform.position) < .05f &&
                Quaternion.Angle(viewRotation,game.view.transform.rotation) < .05f;
            Check(proof.sameViewFrozenTime,"v37 baseline and preview use the same camera and frozen water phase");
            Time.timeScale = 1;

            customSailZoom = 13.8f;
            game.PlaceBoat(new Vector3(-3.8f,SeaLifeRuntime.Sea,.9f),196);
            game.SetWaterReviewAngleV35(angle,customSailZoom);
            yield return new WaitForSecondsRealtime(.85f);
            proof.shoreFocusDistance = atmosphere.FocusDistance;
            Check(Mathf.Abs(proof.shoreFocusDistance-proof.focusDistance)>.02f,
                  "v37 lens recalculates focus after sailing to the wharf");
            Time.timeScale = 0;
            atmosphere.PresentForReview(false);
            yield return Capture("v37-shore-baseline-v361");
            atmosphere.PresentForReview(true);
            yield return Capture("v37-wet-shore-and-godrays");
            Time.timeScale = 1;

            bay.ToggleGodBuildMode(true);
            // Sample the planning camera and lighting after LateUpdate.
            yield return new WaitForEndOfFrame();
            proof.planningKeepsSharpView = bay.godBuildMode && game.view.orthographic &&
                !atmosphere.ExteriorEffects && !atmosphere.FocusActive;
            Check(proof.planningKeepsSharpView,"v37 planning mode keeps the construction grid sharp");
            bay.ToggleGodBuildMode(false);
            game.GameplayV34.CycleWatch();
            game.GameplayV34.CycleWatch();
            yield return new WaitForEndOfFrame();
            proof.nightBeamsOff = game.GameplayV34.State.watch == 3 && atmosphere.BeamOpacity < .001f &&
                                  Mathf.Abs(sun.intensity-.30f) < .001f;
            Check(proof.nightBeamsOff,"v37 sunlight shafts switch off and the night watch retains moonlight");
            game.GameplayV34.CycleWatch();
            game.GameplayV34.CycleWatch();
            yield return ReviewHullClearanceV401();
            yield return ReviewVelvetMeadowV44(atmosphere);
            game.SetWaterReviewAngleV35(oldAngle);
            File.WriteAllText(Path.Combine(evidence,"atmosphere-v37.json"),JsonUtility.ToJson(proof,true));
        }

        IEnumerator ReviewHullClearanceV401()
        {
            var navigation = game.GetComponent<SeaLifeNavigationV23>();
            var visual = game.boat.Find("船身随波 · 视觉层");
            Check(navigation != null && visual != null,"v40.1 waterline follows the animated hull");
            if (navigation == null || visual == null) yield break;
            Vector3 previousPosition = game.boat.position;
            float previousHeading = game.boat.eulerAngles.y;
            float previousZoom = customSailZoom;
            customSailZoom = 8.5f;
            var position = new Vector3(14.2f,SeaLifeRuntime.Sea,5.65f);
            var sideView = Quaternion.Euler(26,-18,0);
            game.PlaceBoat(position,72);
            game.SetWaterReviewAngleV35(sideView,customSailZoom);
            yield return new WaitForSecondsRealtime(.8f);
            Check(FloatingSupportsClearV401(navigation,visual),"v40.1 deep-water broadside clears the bow and stern support points");
            yield return Capture("v401-hull-broadside");
            game.PlaceBoat(position,33);
            game.SetWaterReviewAngleV35(sideView,customSailZoom);
            yield return new WaitForSecondsRealtime(.65f);
            Check(FloatingSupportsClearV401(navigation,visual),"v40.1 bow-quarter view clears the bow and stern support points");
            yield return Capture("v401-hull-bow-quarter");

            game.PlaceBoat(position,72);
            game.SetWaterReviewAngleV35(sideView,customSailZoom);
            game.DriveInput = Vector2.up;
            yield return new WaitForSecondsRealtime(1.25f);
            Check(navigation.WakeCount > 1 && FloatingSupportsClearV401(navigation,visual),
                "v40.1 deep-water sailing emits a wake while keeping both end supports clear");
            yield return Capture("v401-hull-underway");
            game.DriveInput = Vector2.zero;
            game.PlaceBoat(previousPosition,previousHeading);
            customSailZoom = previousZoom;
        }

        IEnumerator ReviewVelvetMeadowV44(SeaCinematicLightingAndAtmosphereV37 atmosphere)
        {
            Transform carpet = game.exterior.Find("V40 Inland Scenery/V40_InlandWindMeadow");
            var renderer = carpet != null ? carpet.GetComponent<MeshRenderer>() : null;
            var grass = carpet != null ? carpet.GetComponent<MeshFilter>().sharedMesh : null;
            var grassLayers = grass != null ? grass.uv : Array.Empty<Vector2>();
            int samplesPerShell = grassLayers.Length / SeaCinematicLightingAndAtmosphereV37.VelvetShellCountV44;
            int crimsonSamples = 0;
            var grassColors = grass != null ? grass.colors32 : Array.Empty<Color32>();
            for (int i = 0; i < samplesPerShell; i++)
                if (grassColors[i].g >= 153) crimsonSamples++;

            bool layered = samplesPerShell > 0 && grassLayers.Length % SeaCinematicLightingAndAtmosphereV37.VelvetShellCountV44 == 0;
            for (int layer = 0; layered && layer < SeaCinematicLightingAndAtmosphereV37.VelvetShellCountV44; layer++)
            {
                float expected = 1f - layer / (SeaCinematicLightingAndAtmosphereV37.VelvetShellCountV44 - 1f);
                layered &= Mathf.Abs(grassLayers[layer*samplesPerShell].x - expected) < .0001f;
            }

            Check(renderer != null && renderer.sharedMaterial.shader.name == "Deepsea/VelvetShellGrassV44" &&
                  renderer.sharedMaterial.shader.isSupported &&
                  renderer.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off &&
                  grass.subMeshCount == 1 && layered && crimsonSamples > 100 && atmosphere.ProtectedWoodSamplesV44 > 0,
                "v44 fourteen unshadowed grass shells include coherent crimson coverage and leave the wooden treads clear");

            var shelf = worldRoot.Find("V29_ContinuousContinentalShelf").GetComponent<MeshFilter>().sharedMesh;
            var colors = shelf.colors;
            var vertices = shelf.vertices;
            int shorePlants = 0, meadow = 0, undergrowth = 0;
            bool dryBiomes = true;
            for (int i = 0; i < colors.Length; i++)
            {
                if (vertices[i].y < SeaLifeRuntime.Sea+.45f)
                    dryBiomes &= colors[i].r == 0 && colors[i].g == 0;
                if (colors[i].r > .12f && colors[i].r < .55f) shorePlants++;
                if (colors[i].r > .68f && colors[i].g < .28f) meadow++;
                if (colors[i].g > .68f) undergrowth++;
            }
            Check(shorePlants > 100 && meadow > 100 && undergrowth > 100 && dryBiomes,
                "v43 terrain blends dune grass, meadow and upland leaf litter above the tidal zone");
            Check(atmosphere.UplandMapleCount == 16,"v43 sixteen upland maples are planted using the existing maple meshes");

            int climbingProfiles = 0;
            for (int z = -16; z <= 24; z+=4)
                if (HeightAt(-46,z) > HeightAt(-24,z)+1.25f) climbingProfiles++;
            Check(climbingProfiles >= 9,"v43 the upland ridge rises above the headland across the central coast");

            Vector3 previous = game.boat.position;
            float previousHeading = game.boat.eulerAngles.y;
            float previousZoom = customSailZoom;
            customSailZoom = 27f;
            game.PlaceBoat(new Vector3(8.2f,SeaLifeRuntime.Sea,6.5f),0);
            game.SetWaterReviewAngleV35(Quaternion.Euler(39,-48,0),customSailZoom);
            yield return new WaitForSecondsRealtime(.75f);
            if (carpet != null)
            {
                Time.timeScale = 0;
                atmosphere.PresentForReview(false);
                Check(!carpet.gameObject.activeInHierarchy && atmosphere.ClutterVisibilityMatches(true),
                    "v43 baseline restores legacy shore plants and hides coastal grass and upland groves");
                yield return Capture("v44-coast-baseline");
                atmosphere.PresentForReview(true);
                Check(carpet.gameObject.activeInHierarchy && atmosphere.ClutterVisibilityMatches(false),
                    "v43 presentation restores the new grass and upland groves and hides legacy shore clutter");
                yield return Capture("v44-coast-panorama");
                Time.timeScale = 1;
            }

            var meadowView = new Vector3(-23f,GrassHeightAt(-23f,12f),12f);
            yield return VillageFrameV31("v44-velvet-grass-wave",meadowView,9.5f,33,-65);
            var grove = new Vector3(-31f,GrassHeightAt(-31f,21f),21f);
            yield return VillageFrameV31("v44-crimson-carpet",grove,12.5f,37,-54);

            customSailZoom = 21.5f;
            game.PlaceBoat(new Vector3(11f,SeaLifeRuntime.Sea,-8.5f),350);
            game.SetWaterReviewAngleV35(Quaternion.Euler(42,-34,0),customSailZoom);
            game.DriveInput = new Vector2(.14f,1);
            for (int i = 0; i < 60; i++) yield return new WaitForEndOfFrame();

            var frameMs = new float[480];
            Vector3 start = game.boat.position;
            float startHeading = game.boat.eulerAngles.y;
            float totalMs = 0;
            for (int i = 0; i < frameMs.Length; i++)
            {
                yield return new WaitForEndOfFrame();
                frameMs[i] = Time.unscaledDeltaTime * 1000;
                totalMs += frameMs[i];
            }

            Array.Sort(frameMs);
            var proof = new SailingEvidenceV44
            {
                frames = frameMs.Length,
                averageFps = 1000 * frameMs.Length / totalMs,
                p95FrameMs = frameMs[(int)(frameMs.Length*.95f)],
                boatTravelMeters = Vector3.Distance(start,game.boat.position),
                headingChangeDegrees = Mathf.Abs(Mathf.DeltaAngle(startHeading,game.boat.eulerAngles.y))
            };
            Check(proof.boatTravelMeters > 7 && proof.headingChangeDegrees > 3 && game.Speed > 1.1f,
                "v44 performance sample includes actual propulsion, steering and a moving camera");
            Check(Screen.width == 1920 && Screen.height == 1080 && proof.averageFps >= 90,
                "v44 moving 1080p camera averages at least 90 FPS");
            File.WriteAllText(Path.Combine(evidence,"sailing-v44.json"),JsonUtility.ToJson(proof,true));
            yield return Capture("v44-sailing");
            game.DriveInput = Vector2.zero;
            game.PlaceBoat(previous,previousHeading);
            customSailZoom = previousZoom;
        }

        bool FloatingSupportsClearV401(SeaLifeNavigationV23 navigation, Transform visual)
        {
            const float distance = SeaLifeNavigationV23.HarborSupportDistance;
            var forward = game.boat.forward * distance;
            var pitch = visual.forward.y * distance;
            float bow = visual.position.y + pitch - navigation.Height(game.boat.position + forward);
            float stern = visual.position.y - pitch - navigation.Height(game.boat.position - forward);
            return Mathf.Min(bow,stern) >= .015f;
        }
    }
}
