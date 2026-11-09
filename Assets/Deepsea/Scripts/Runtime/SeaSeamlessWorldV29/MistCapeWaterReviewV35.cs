using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Deepsea
{
    public sealed partial class SeaLifeRuntime
    {
        internal Quaternion SetWaterReviewAngleV35(Quaternion angle, float size = 0)
        {
            var previous = cameraRotation;
            cameraRotation = angle;
            if (size > 0) cameraSize = size;
            ApplyCamera(true);
            return previous;
        }
    }

    public sealed partial class SeaSeamlessWorldAndNauticalUIV29
    {
        IEnumerator ReviewMistCapeWaterV35()
        {
            var water = game.GetComponent<ApplyMistCapeEmeraldWaterV35>();
            var renderer = waterFilter.GetComponent<Renderer>();
            var material = renderer.sharedMaterial;
            bool physical = ApplyMistCapeEmeraldWaterV35.PhysicalEnabled;
            bool organic = ApplyMistCapeEmeraldWaterV35.OrganicEnabled;
            string prefix = organic ? "v352-" : physical ? "v351-" : "v35-";
            string expected = organic ? ApplyMistCapeEmeraldWaterV35.OrganicShaderName
                : physical ? ApplyMistCapeEmeraldWaterV35.PhysicalShaderName : ApplyMistCapeEmeraldWaterV35.ShaderName;
            Check(water != null && material.shader.name == expected,
                "v35 shader active on the actual ShowcaseWater surface");
            if (water == null) yield break;
            if (physical)
            {
                var cameraData = game.view.GetUniversalAdditionalCameraData();
                Check(cameraData.requiresDepthTexture && cameraData.requiresColorTexture,
                    "v35.1 real sailing camera supplies depth and opaque colour");
                Check(water.PreviousEmerald.name == ApplyMistCapeEmeraldWaterV35.ShaderName && water.PhysicalWake,
                    "v35.1 historical wake uses physical foam while v35 comparison remains available");
            }

            if (organic)
            {
                Check(water.PreviousPhysical != null && water.PreviousPhysical.name == ApplyMistCapeEmeraldWaterV35.PhysicalShaderName,
                    "v35.2 compares with the original v35.1 shader");
                Check(material.HasProperty("_ChopIrregularity") && Mathf.Abs(material.GetFloat("_ChopIrregularity")-1.25f)<.001f,
                    "v35.2 short crest and gust breakup is active on the live material");
            }
            Check(game.world.GetComponentsInChildren<Renderer>(true).Count(r => r.sharedMaterials.Any(m => m != null && m.shader == water.Emerald)) == 1,
                "v35 shader binds to one surface and leaves other renderers alone");
            Check(water.ContactCount >= 4 && water.ContactCount <= 16,
                "v35 actual pier mesh yields bounded waterline contacts (" + water.ContactCount + ")");
            Check(material.GetTexture("_CausticsMap") != null && material.GetTexture("_FoamMap") != null,
                "v35 retained the mipmapped Voronoi and foam texture inputs");
            Check(game.save.gameplayV34.watch == 1 && game.OnFoot == false, "v35 daytime camera starts on the original boat");
            Check(water.Emerald.isSupported && water.HarborBaseline != null, "v35 shader and v34 comparison shader are supported");

            game.NoticeTime = 0;
            customSailZoom = 21.5f;
            game.PlaceBoat(new Vector3(9.2f, SeaLifeRuntime.Sea, 5.65f), 255);
            var oldAngle = game.SetWaterReviewAngleV35(Quaternion.Euler(45, -18, 0), customSailZoom);
            yield return new WaitForSeconds(.85f);

            // Both images use the same boat, geometry, camera, lighting and water phase.
            Time.timeScale = 0;
            var comparison = organic ? water.PreviousPhysical : physical ? water.PreviousEmerald : water.HarborBaseline;
            material.shader = comparison;
            yield return Capture(prefix + (organic ? "same-view-v351" : physical ? "same-view-v35" : "same-view-v34"));
            material.shader = water.Emerald;
            yield return Capture(prefix + "preview");
            Check(Quaternion.Angle(game.view.transform.rotation, Quaternion.Euler(45, -18, 0)) < .05f,
                "v35 comparison uses the 45 degree perspective sailing view");
            Time.timeScale = 1;

            customSailZoom = 12.7f;
            game.PlaceBoat(new Vector3(12.0f, SeaLifeRuntime.Sea, 7.7f), 263);
            game.DriveInput = Vector2.up;
            yield return new WaitForSeconds(1.38f);
            Check(water.WakeSpeed > 1.1f && game.Speed > 1.1f, "v35 directional wake uses real propulsion speed");
            if (physical)
            {
                if (organic)
                {
                    game.DriveInput = Vector2.zero;
                    customSailZoom = 13.8f;
                    game.PlaceBoat(new Vector3(-3.8f, SeaLifeRuntime.Sea, 0.9f), 196);
                    game.SetWaterReviewAngleV35(Quaternion.Euler(45, -18, 0), customSailZoom);
                    yield return new WaitForSeconds(.85f);
                }
                game.SetWaterReviewAngleV35(Quaternion.Euler(45,-18,0),customSailZoom);
                Time.timeScale = 0;
                material.shader = comparison;
                yield return Capture(prefix + (organic ? "same-closeup-v351" : "same-closeup-v35"));
                Check(water.PhysicalWake == organic, organic ? "v35.2 comparison retains the same v35.1 wake"
                    : "v35.1 comparison restores v35 historical wake rendering");
                material.shader = water.Emerald;
                yield return Capture(prefix + "emerald-water-closeup");
                Check(water.PhysicalWake, "v35.1 local wash and historical foam resume together");
                Time.timeScale = 1;
            }
            else yield return Capture(prefix + "emerald-water-closeup");
            game.DriveInput = Vector2.zero;
            yield return new WaitForSeconds(.40f);
            game.PlaceBoat(new Vector3(9.2f, SeaLifeRuntime.Sea, 5.65f), 255);
            yield return new WaitForEndOfFrame();
            Check(game.Speed < .03f && water.WakeSpeed < .03f, "v35 moving wake decays at zero speed");
            customSailZoom = 16;

            game.GameplayV34.CycleWatch();
            game.GameplayV34.CycleWatch();
            yield return new WaitForSeconds(.5f);
            Check(game.save.gameplayV34.watch == 3 && Shader.GetGlobalFloat("_MistCapeWatchV35") == 3 &&
                renderer.sharedMaterial.shader == water.Emerald, "v35 water follows the real v34 night watch");
            yield return Capture(prefix + "night");
            game.GameplayV34.CycleWatch();
            game.GameplayV34.CycleWatch();
            Check(game.save.gameplayV34.watch == 1, "v35 review restores noon");

            File.WriteAllText(Path.Combine(evidence, organic ? "water-v352.json" : physical ? "water-v351.json" : "water-v35.json"),
                "{\"activeShader\":\""+material.shader.name+"\",\"pierWaterlineContacts\":"+water.ContactCount+
                ",\"pitch\":45,\"yaw\":-18,\"sameViewFrozenPhase\":true}");
            game.SetWaterReviewAngleV35(oldAngle);
        }
    }
}
