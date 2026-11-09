using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Deepsea
{
    public sealed partial class SeaSeamlessWorldAndNauticalUIV29
    {
        [Serializable] sealed class LushEvidence
        {
            public int maples, mapleColors, pines, grass, ferns, boulders, groundTiles, reefPatches, marinePlants;
            public int[] houseFootings, propCounts, marineSpecies;
        }

        IEnumerator ReviewLushWorldV33()
        {
            int mapleColors = maplesV33.Select(t => t.Find("Leaves").GetComponent<MeshRenderer>().sharedMaterial).Distinct().Count();
            int[] propCounts = Enumerable.Range(Rack,5).Select(kind => propsV33.Count(p => p.kind == kind)).ToArray();
            Check(lushModelsV33.All(m => m != null && m.vertexCount == m.colors.Length), "v33 ten authored mesh assets preserve vertex colors");
            Check(maplesV33.Count == 18 && mapleColors == 4 && pinesV33.Count == 26,
                "v33 four autumn variants and twenty-six grounded coastal pines");
            Check(meadowV33.SequenceEqual(new[] {160,85,42}), "v33 160 flower/grass clumps, 85 ferns and 42 mossy stone clusters");
            Check(footingsV33.All(count => count >= 5), "v33 all three foundations have planted stone and fern transitions");
            Check(propCounts.SequenceEqual(new[] {4,4,2,14,3}) && propsV33.All(p => !p.node.GetComponent<Collider>()),
                "v33 4 racks, 4 carts, 2 fires, 14 fences and 3 boats are placed without new collision obstructions");
            Check(propsV33.Where(p => p.kind == Fire).All(p => p.node.GetComponentInChildren<Light>() != null),
                "v33 campfire has a localized warm light");
            Check(reefsV33.Count == 38 && reefsV33.All(p => p.childCount >= 4 && p.childCount <= 6),
                "v33 thirty-eight irregular reef patches have one parent and three to five companions");
            Check(reefsV33.All(p => p.Cast<Transform>().Select(t => morphologyV31[t]).Distinct().Count() == p.childCount),
                "v33 reef companions diversify species within each patch");
            Check(!worldRoot.Cast<Transform>().Any(t => t.gameObject.activeSelf &&
                (t.name.StartsWith("V29_CoralCluster_") || t.name.StartsWith("V29_GiantKelp_"))),
                "v33 planted patches replace the legacy repeating layer");
            Check(shoreRenderV33.sharedMesh != shoreCollisionV33
                && shoreRenderV33.GetComponent<MeshCollider>().sharedMesh == shoreCollisionV33
                && shoreRenderV33.sharedMesh.vertices.SequenceEqual(shoreCollisionV33.vertices),
                "v33 land normals are softened without moving terrain vertices or colliders");
            int children = lushRootV33.GetComponentsInChildren<Transform>().Length;
            int assets = _owned.Count;
            ApplyLushWorldAndVillageLifeV33();
            Check(lushRootV33.GetComponentsInChildren<Transform>().Length == children && _owned.Count == assets,
                "v33 repeated initialization keeps the same objects and owned resources");

            var stats = new LushEvidence
            {
                maples = maplesV33.Count, mapleColors = mapleColors, pines = pinesV33.Count,
                grass = meadowV33[0], ferns = meadowV33[1], boulders = meadowV33[2],
                groundTiles = groundTilesV33.Count, reefPatches = reefsV33.Count,
                marinePlants = floraV31.Count, houseFootings = footingsV33,
                propCounts = propCounts, marineSpecies = speciesV31
            };
            File.WriteAllText(Path.Combine(evidence,"lush-stats.json"),JsonUtility.ToJson(stats,true));

            var tavern = housesV31[1];
            var gallery = game.exterior.Find("室外活动/海洋小馆 · 码头入口");
            Vector3 harbor = Vector3.Lerp(tavern.position,gallery.position,.58f)+Vector3.right*5.5f;
            harbor.y = 1.4f;
            harbor += Quaternion.Euler(44,-66,0)*Vector3.up*4.1f;
            yield return VillageFrameV31("v33-preview",harbor,25.5f,44,-66);

            var detail = tavern.GetChild(0).GetComponent<Renderer>().bounds;
            foreach (int kind in new[] {Rack,Cart,Fire,Fence})
            {
                var closest = propsV33.Where(p => p.kind == kind)
                    .OrderBy(p => GapV33(p.node.position,tavern.position)).FirstOrDefault().node;
                if (closest != null && GapV33(closest.position,tavern.position) < 260f)
                    detail.Encapsulate(closest.GetComponent<Renderer>().bounds);
            }
            yield return LushBoundsFrameV33("v33-lush-ground-and-village-life",detail,45,-55,7f);

            var reef = reefsV33.OrderBy(p =>
                Mathf.Abs(p.GetChild(0).position.z+11)*.07f
                + Mathf.Abs(SeaLifeRuntime.Sea-p.GetChild(0).position.y-2.35f)
                + Mathf.Abs(p.GetChild(0).position.x-8)*.05f).First();
            var pool = reef.GetChild(0).GetComponent<Renderer>().bounds;
            foreach (Transform plant in reef) pool.Encapsulate(plant.GetComponent<Renderer>().bounds);
            yield return LushBoundsFrameV33("v33-organic-reef-patch",pool,67,-35,4.6f);
        }

        IEnumerator LushBoundsFrameV33(string name, Bounds box, float pitch, float yaw, float minimum)
        {
            var orientation = Quaternion.Euler(pitch,yaw,0);
            var inverse = Quaternion.Inverse(orientation);
            Vector3 focus = box.center + orientation*Vector3.up*.45f;
            float size = minimum;
            for (int i = 0; i < 8; i++)
            {
                var point = new Vector3((i&1)==0 ? box.min.x : box.max.x,
                                        (i&2)==0 ? box.min.y : box.max.y,
                                        (i&4)==0 ? box.min.z : box.max.z);
                var local = inverse*(point-focus);
                size = Mathf.Max(size,(Mathf.Abs(local.y)+1.1f)/.79f,(Mathf.Abs(local.x)+1.2f)/game.view.aspect);
            }
            yield return VillageFrameV31(name,focus,size,pitch,yaw);
        }
    }
}
