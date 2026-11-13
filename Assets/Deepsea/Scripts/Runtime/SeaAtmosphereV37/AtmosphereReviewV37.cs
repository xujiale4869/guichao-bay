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
            public float noonPitch, noonYaw, focusDistance;
            public int beams, contacts;
            public bool sameViewFrozenTime, planningKeepsSharpView, nightBeamsOff;
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
                contacts = atmosphere.Contacts
            };
            Check(proof.waterShader == SeaSunnyHarborAudioV36.WaterShader &&
                  game.GetComponent<ApplyMistCapeEmeraldWaterV35>() == null,
                  "v37 preserves the approved v34 water shader");
            Check(proof.terrainShader == "Deepsea/V24CoastShowcaseSurface" && nativeSand.shader.isSupported,
                  "v37 wet shore shader is bound to the continuous land mesh");
            Check(Mathf.Abs(proof.noonPitch-31) < .05f && Mathf.Abs(proof.noonYaw+44) < .05f,
                  "v37 noon light uses the slanted sun");
            Check(proof.beams == 9 && proof.contacts > 12 && proof.contacts <= 64 && atmosphere.BeamOpacity > 0,
                  "v37 shaft and terrain contact effects are populated and bounded");
            Check(atmosphere.FocusActive && atmosphere.FocusDistance > 90 && atmosphere.FocusDistance < 115 &&
                  data.renderPostProcessing && data.requiresDepthTexture,
                  "v37 lens focuses the real perspective sailing camera");

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
            Time.timeScale = 0;
            atmosphere.PresentForReview(false);
            yield return Capture("v37-shore-baseline-v361");
            atmosphere.PresentForReview(true);
            yield return Capture("v37-wet-shore-and-godrays");
            Time.timeScale = 1;

            bay.ToggleGodBuildMode(true);
            yield return null;
            proof.planningKeepsSharpView = bay.godBuildMode && !atmosphere.ExteriorEffects && !atmosphere.FocusActive;
            Check(proof.planningKeepsSharpView,"v37 planning mode keeps the construction grid sharp");
            bay.ToggleGodBuildMode(false);
            game.GameplayV34.CycleWatch();
            game.GameplayV34.CycleWatch();
            yield return null;
            proof.nightBeamsOff = game.GameplayV34.State.watch == 3 && atmosphere.BeamOpacity < .001f &&
                                  Mathf.Abs(sun.intensity-.30f) < .001f;
            Check(proof.nightBeamsOff,"v37 sunlight shafts switch off and the night watch retains moonlight");
            game.GameplayV34.CycleWatch();
            game.GameplayV34.CycleWatch();
            game.SetWaterReviewAngleV35(oldAngle);
            File.WriteAllText(Path.Combine(evidence,"atmosphere-v37.json"),JsonUtility.ToJson(proof,true));
        }
    }
}
