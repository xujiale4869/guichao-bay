using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deepsea
{
    [DefaultExecutionOrder(965)]
    public sealed class ApplyMistCapeEmeraldWaterV35 : MonoBehaviour
    {
        internal const string ShaderName = "Deepsea/MistCapeEmeraldWaterV35";
        const int MaxContacts = 16;
        static readonly int ContactsId = Shader.PropertyToID("_MistCapeContactsV35");
        static readonly int ContactCountId = Shader.PropertyToID("_MistCapeContactCountV35");
        static readonly int WatchId = Shader.PropertyToID("_MistCapeWatchV35");
        static readonly int BoatId = Shader.PropertyToID("_MistCapeBoatV35");
        static readonly int HeadingId = Shader.PropertyToID("_MistCapeHeadingV35");

        internal static bool Enabled
        {
            get
            {
#if DEEPSEA_V35
                return true;
#else
                return false;
#endif
            }
        }

        SeaLifeRuntime game;
        internal Shader HarborBaseline { get; private set; }
        internal Shader Emerald { get; private set; }
        internal int ContactCount { get; private set; }
        internal float WakeSpeed { get; private set; }

        // V26 owns the live material and V27 selects its shader. The new shader
        // shares those existing textures and controls, including the F6/F8 comparisons.
        internal static Shader Bind(SeaLifeRuntime game, Shader harborBaseline)
        {
            if (!Enabled) return harborBaseline;
            var kept = Resources.Load<Material>("SeaMistCapeV35/EmeraldShaderKeep");
            if (kept == null || kept.shader.name != ShaderName || !kept.shader.isSupported)
            {
                Debug.LogError("MIST_CAPE_V35_SHADER_UNAVAILABLE");
                return harborBaseline;
            }

            var water = game.gameObject.AddComponent<ApplyMistCapeEmeraldWaterV35>();
            water.game = game;
            water.HarborBaseline = harborBaseline;
            water.Emerald = kept.shader;
            water.PublishPierContacts();
            water.PublishFrame();
            Debug.Log("MIST_CAPE_V35_READY surface=ShowcaseWater contacts=" + water.ContactCount);
            return water.Emerald;
        }

        void LateUpdate()
        {
            if (game != null) PublishFrame();
        }

        void PublishFrame()
        {
            var pos = game.boat.position;
            var forward = game.boat.forward;
            float signedSpeed = Vector3.Dot(game.body.velocity, forward);
            WakeSpeed = !game.OnFoot && !game.Busy ? Mathf.Abs(signedSpeed) : 0;
            if (signedSpeed < 0) forward = -forward;
            Shader.SetGlobalVector(BoatId, new Vector4(pos.x, pos.z, WakeSpeed, 1));
            Shader.SetGlobalVector(HeadingId, new Vector4(forward.x, forward.z, 0, 0));
            Shader.SetGlobalFloat(WatchId, game.save.gameplayV34.watch);
        }

        void PublishPierContacts()
        {
            var crossings = new List<Vector2>();
            foreach (var filter in game.world.GetComponentsInChildren<MeshFilter>())
            {
                if (!filter.name.StartsWith("EN_Pier_Wood_", StringComparison.Ordinal)) continue;
                var mesh = filter.sharedMesh;
                if (mesh == null || !mesh.isReadable) continue;
                var vertices = mesh.vertices;
                for (int i = 0; i < vertices.Length; i++)
                    vertices[i] = filter.transform.TransformPoint(vertices[i]);

                var triangles = mesh.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    var a = vertices[triangles[i]];
                    var b = vertices[triangles[i + 1]];
                    var c = vertices[triangles[i + 2]];
                    AddCrossing(a, b, crossings);
                    AddCrossing(b, c, crossings);
                    AddCrossing(c, a, crossings);
                }
            }

            // Waterline intersections group by the footprint of a support, even
            // when UV splits duplicate its vertices in the combined pier mesh.
            var groups = new List<List<Vector2>>();
            foreach (var point in crossings)
            {
                List<Vector2> group = null;
                for (int i = groups.Count - 1; i >= 0; i--)
                {
                    if (!groups[i].Exists(p => (p - point).sqrMagnitude <= .33f * .33f)) continue;
                    if (group == null) group = groups[i];
                    else
                    {
                        group.AddRange(groups[i]);
                        groups.RemoveAt(i);
                    }
                }
                if (group == null)
                {
                    group = new List<Vector2>();
                    groups.Add(group);
                }
                group.Add(point);
            }

            var contacts = new List<Vector4>();
            foreach (var group in groups)
            {
                if (group.Count < 3) continue;
                Vector2 lo = group[0], hi = lo;
                foreach (var p in group)
                {
                    lo = Vector2.Min(lo, p);
                    hi = Vector2.Max(hi, p);
                }
                float width = Mathf.Max(hi.x - lo.x, hi.y - lo.y);
                if (width < .09f || width > .95f) continue;
                var center = (lo + hi) * .5f;
                contacts.Add(new Vector4(center.x, center.y, width * .52f, 0));
            }

            var dock = new Vector2(game.dockBerth.x, game.dockBerth.z);
            contacts.Sort((a,b) => (new Vector2(a.x,a.y)-dock).sqrMagnitude
                .CompareTo((new Vector2(b.x,b.y)-dock).sqrMagnitude));
            var slots = new Vector4[MaxContacts];
            ContactCount = Mathf.Min(contacts.Count, slots.Length);
            contacts.CopyTo(0, slots, 0, ContactCount);
            Shader.SetGlobalVectorArray(ContactsId, slots);
            Shader.SetGlobalInt(ContactCountId, ContactCount);
        }

        static void AddCrossing(Vector3 a, Vector3 b, List<Vector2> points)
        {
            float da = a.y - SeaLifeRuntime.Sea;
            float db = b.y - SeaLifeRuntime.Sea;
            if ((da < 0) == (db < 0)) return;
            var p = Vector3.LerpUnclamped(a, b, da / (da - db));
            points.Add(new Vector2(p.x, p.z));
        }

        void OnDestroy()
        {
            Shader.SetGlobalInt(ContactCountId, 0);
        }
    }
}
