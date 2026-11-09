using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Random = System.Random;

namespace Deepsea
{
    public sealed partial class SeaSeamlessWorldAndNauticalUIV29
    {
#if DEEPSEA_V33
        static bool LushV33 => true;
#else
        static bool LushV33 => false;
#endif

        const string LushResources = "SeaLushWorldV33/";
        static readonly string[] LushModelNames = {
            "Land_0_CoastalPineTrunk", "Land_0_CoastalPineCanopy", "Land_1_LushFernAndHeather",
            "Land_2_WildflowerGrassTussock", "Land_3_MossyBoulderCluster", "Prop_0_DryingNetAndRack",
            "Prop_1_WoodenHandcartAndBarrels", "Prop_2_CampfireAndLogSeats", "Prop_3_RusticFenceSegment",
            "Prop_4_BeachedRowboatAndOars"
        };
        const int Fern = 2, Grass = 3, Rock = 4, Rack = 5, Cart = 6, Fire = 7, Fence = 8, Rowboat = 9;

        [Serializable] sealed class LushRoles { public LushRole[] parts; }
        [Serializable] sealed class LushRole { public string role; }

        Transform lushRootV33, landRootV33, reefRootV33;
        MeshFilter shoreRenderV33;
        Mesh shoreCollisionV33;
        readonly Mesh[] lushModelsV33 = new Mesh[LushModelNames.Length];
        readonly Material[][] lushMaterialsV33 = new Material[LushModelNames.Length][];
        readonly Material[,] marineMaterialsV33 = new Material[6,8];
        readonly List<Transform> pinesV33 = new List<Transform>(), maplesV33 = new List<Transform>();
        readonly List<Transform> reefsV33 = new List<Transform>();
        readonly List<(int kind, Transform node)> propsV33 = new List<(int,Transform)>();
        readonly List<(Vector3 point, float radius)> occupiedV33 = new List<(Vector3,float)>();
        readonly List<Vector3> walkingV33 = new List<Vector3>();
        readonly int[] meadowV33 = new int[3], footingsV33 = new int[3];
        readonly Dictionary<Vector2Int,Transform> groundTilesV33 = new Dictionary<Vector2Int,Transform>();
        Bounds[] landObstaclesV33;
        Material ecologyV33, mapleSourceV33;
        Random landRandomV33;

        void ApplyLushWorldAndVillageLifeV33()
        {
            if (!LushV33 || lushRootV33 != null) return;
            lushRootV33 = GroupV33(_v31Root,"V33_LushWorldAndVillageLife");
            landRootV33 = GroupV33(lushRootV33,"V33_Land");
            reefRootV33 = GroupV33(lushRootV33,"V33_OrganicReefs");
            landRandomV33 = new Random(330903);
            LoadLushModelsV33();
            Physics.SyncTransforms();
            ReserveVillageGroundV33();
            SoftenLandNormalsV33();
            ColorMaplesV33();
            PlaceVillageLifeV33();
            PlantPinesV33();
            PlantMeadowsV33();
            foreach (var tile in groundTilesV33.Values) MergeStaticRenderersV31(tile);
            ReplaceReefLayoutV33();
        }

        static Transform GroupV33(Transform parent, string name)
        {
            var node = new GameObject(name).transform;
            node.SetParent(parent,false);
            return node;
        }

        void LoadLushModelsV33()
        {
            ecologyV33 = Resources.Load<Material>(LushResources+"EcologyKeep");
            var craft = Resources.Load<Material>("SeaCraftV32/CraftKeep");
            if (ecologyV33 == null || craft == null || !ecologyV33.shader.isSupported)
                throw new InvalidOperationException("Required v33 foliage or v32 craft shader is missing.");
            var leaf = OwnV33(ecologyV33,"V33_MeadowGrass",new Color(.69f,.76f,.59f));
            var fern = OwnV33(ecologyV33,"V33_WoodlandFern",new Color(.59f,.72f,.60f));
            var pine = OwnV33(ecologyV33,"V33_CoastalNeedles",new Color(.59f,.72f,.62f));
            pine.SetFloat("_Wind",.027f);
            var ember = OwnV33(ecologyV33,"V33_FireGlow",new Color(1f,.79f,.55f));
            ember.SetFloat("_Ember",1);
            ember.SetFloat("_Wind",.09f);
            var timber = OwnV33(craft,"V33_WornPropTimber",new Color(.76f,.69f,.61f));
            timber.SetFloat("_NormalStrength",.32f);
            timber.SetFloat("_MossStrength",.40f);
            var stone = OwnV33(craft,"V33_MossyShoreStone",new Color(.57f,.61f,.55f));
            stone.SetFloat("_CraftMode",2);
            stone.SetFloat("_MossStrength",.72f);
            var rope = OwnV33(craft,"V33_Hemp",new Color(.74f,.68f,.56f));
            rope.SetFloat("_NormalStrength",.10f);
            var iron = OwnV33(craft,"V33_DarkIron",new Color(.42f,.43f,.40f));
            iron.SetFloat("_Roughness",.61f);
            var bark = OwnV33(craft,"V33_SaltWeatheredPineBark",new Color(.70f,.62f,.54f));
            bark.SetFloat("_CraftMode",3);
            for (int i = 0; i < LushModelNames.Length; i++)
            {
                string name = LushModelNames[i];
                var mesh = Resources.Load<Mesh>(LushResources+"Baked/"+name);
                var info = Resources.Load<TextAsset>(LushResources+"Roles/"+name);
                var roles = info != null ? JsonUtility.FromJson<LushRoles>(info.text) : null;
                if (mesh == null || roles?.parts == null || roles.parts.Length != mesh.subMeshCount)
                    throw new InvalidOperationException("Missing authored v33 model or material roles "+name);
                lushModelsV33[i] = mesh;
                int kind = i;
                lushMaterialsV33[i] = roles.parts.Select(p =>
                {
                    switch(p.role)
                    {
                        case "Foliage": return kind == 1 ? pine : kind == Fern ? fern : leaf;
                        case "Timber": return kind == 0 ? bark : timber;
                        case "Stone": return stone;
                        case "Rope": return rope;
                        case "Iron": return iron;
                        case "Ember": return ember;
                        default: throw new InvalidOperationException("Unknown v33 role "+p.role);
                    }
                }).ToArray();
            }
        }

        Material OwnV33(Material source, string name, Color color)
        {
            var material = new Material(source) { name = name, enableInstancing = true };
            material.SetColor("_BaseColor",color);
            _owned.Add(material);
            return material;
        }

        void SoftenLandNormalsV33()
        {
            shoreRenderV33 = worldRoot.Find("V29_ContinuousContinentalShelf").GetComponent<MeshFilter>();
            shoreCollisionV33 = shoreRenderV33.GetComponent<MeshCollider>().sharedMesh;
            var mesh = Instantiate(shoreRenderV33.sharedMesh);
            mesh.name = "V33_ShoreRenderNormals";
            var n = mesh.normals;
            var v = mesh.vertices;
            var smoothed = (Vector3[])n.Clone();
            const int row = 261; // GridMeshWithColors uses nx=260.
            if (v.Length != row*281) throw new InvalidOperationException("Unexpected v33 shelf grid.");
            for (int i = row+1; i < v.Length-row-1; i++)
            {
                if (i%row == 0 || i%row == row-1 || v[i].y <= SeaLifeRuntime.Sea+.70f) continue;
                Vector3 average = n[i-1]+n[i+1]+n[i-row]+n[i+row]+2*n[i];
                float amount = Mathf.SmoothStep(0,1,(v[i].y-SeaLifeRuntime.Sea-.70f)*.7f)*.35f;
                smoothed[i] = Vector3.Lerp(n[i],average.normalized,amount).normalized;
            }
            mesh.normals = smoothed;
            _owned.Add(mesh);
            shoreRenderV33.sharedMesh = mesh;
        }

        void ColorMaplesV33()
        {
            maplesV33.AddRange(worldRoot.Cast<Transform>().Where(t => t.name.StartsWith("V29_CoastMaple_")));
            if (maplesV33.Count == 0) return;
            mapleSourceV33 = maplesV33[0].Find("Leaves").GetComponent<MeshRenderer>().sharedMaterial;
            var colors = new[] { new Color(.88f,.23f,.18f), new Color(.87f,.40f,.19f),
                                 new Color(.83f,.61f,.24f), new Color(.60f,.19f,.32f) };
            var palette = colors.Select((c,i) => OwnV33(ecologyV33,"V33_MapleAutumn_"+i,c)).ToArray();
            foreach (var material in palette)
            {
                material.SetTexture("_BaseMap",mapleSourceV33.GetTexture("_BaseMap"));
                material.SetFloat("_LeafAtlas",1);
                material.SetFloat("_Cutoff",mapleSourceV33.GetFloat("_Cutoff"));
                material.SetFloat("_Wind",.11f);
            }
            for (int i = 0; i < maplesV33.Count; i++)
            {
                var tree = maplesV33[i];
                tree.Find("Leaves").GetComponent<MeshRenderer>().sharedMaterial = palette[i%4];
                tree.localScale = Vector3.Scale(tree.localScale,new Vector3(RangeV33(.89f,1.11f),RangeV33(.88f,1.16f),RangeV33(.88f,1.10f)));
                tree.localRotation *= Quaternion.Euler(RangeV33(-2f,2f),0,RangeV33(-3f,3f));
            }
        }

        void ReserveVillageGroundV33()
        {
            var route = _v31Root.Find("V31_CoastalNpcVillage/V31_VillageStonePaths");
            walkingV33.AddRange(route.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.StartsWith("石板路踏步_")).Select(t => t.position));
            walkingV33.AddRange(slipwayRouteV31);
            walkingV33.Add(game.dockBerth);
            var entry = game.exterior.Find("室外活动/海洋小馆 · 码头入口");
            if (entry != null)
                walkingV33.Add(entry.position);
            landObstaclesV33 = game.world.GetComponentsInChildren<Collider>()
                .Where(c => c.enabled && !c.isTrigger && !c.transform.IsChildOf(game.boat)
                    && !c.transform.IsChildOf(game.walker))
                .Select(c => c.bounds)
                .Where(b => b.max.y > SeaLifeRuntime.Sea+.1f && b.size.x < 22f && b.size.z < 22f)
                .ToArray();
        }

        float RangeV33(float lo, float hi) => Mathf.Lerp(lo,hi,(float)landRandomV33.NextDouble());
        static float GapV33(Vector3 a, Vector3 b)
            => (new Vector2(a.x-b.x,a.z-b.z)).sqrMagnitude;
        static float GapV33(Vector3 p, Bounds b)
        {
            float dx = Mathf.Max(b.min.x-p.x,0,p.x-b.max.x);
            float dz = Mathf.Max(b.min.z-p.z,0,p.z-b.max.z);
            return dx*dx+dz*dz;
        }
        static float Squared(float x) => x*x;

        bool LandOpenV33(Vector3 p, float radius)
        {
            // Keep the old lighthouse-to-gallery approach open, including its visual-only sections.
            return !(Mathf.Abs(p.z-2.4f) < 6.1f+radius && p.x > ShoreX(p.z)-13.5f)
                && !walkingV33.Any(w => GapV33(w,p) < Squared(radius+.74f))
                && !landObstaclesV33.Any(b => GapV33(p,b) < Squared(radius+.10f))
                && !occupiedV33.Any(o => GapV33(p,o.point) < Squared(radius+o.radius));
        }

        bool FindLandV33(Vector3 center, float radius, float reach, float relief, float freeboard, out Vector3 p, int attempts=32, float upperFreeboard=float.PositiveInfinity)
        {
            for (int i = 0; i < attempts; i++)
            {
                float phase = i*2.39996f;
                float offset = i == 0 ? 0 : reach*Mathf.Sqrt(i/(attempts-1f));
                p = center+new Vector3(Mathf.Cos(phase),0,Mathf.Sin(phase))*offset;
                if (!LandOpenV33(p,radius)) continue;
                float y = V31Ground(p.x,p.z);
                if (y < SeaLifeRuntime.Sea+freeboard || y > SeaLifeRuntime.Sea+upperFreeboard) continue;
                float sample = radius*.74f;
                float a = V31Ground(p.x-sample,p.z), b = V31Ground(p.x+sample,p.z);
                float c = V31Ground(p.x,p.z-sample), d = V31Ground(p.x,p.z+sample);
                float low = Mathf.Min(y,a,b,c,d), high = Mathf.Max(y,a,b,c,d);
                if (low < SeaLifeRuntime.Sea+.06f || high-low > relief) continue;
                p.y = y;
                return true;
            }
            p = Vector3.zero;
            return false;
        }

        Transform PlaceLushV33(int kind, string label, Vector3 p, float scale, float yaw, Transform parent)
        {
            var model = AddMesh(label,lushModelsV33[kind],lushMaterialsV33[kind][0],p,Vector3.one*scale,false,parent);
            model.transform.localRotation = Quaternion.Euler(0,yaw,0);
            var renderer = model.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = lushMaterialsV33[kind];
            if (kind <= 1 || kind >= Rack)
                renderer.shadowCastingMode = ShadowCastingMode.On;
            return model.transform;
        }

        Transform GroundTileV33(Vector3 p)
        {
            var key = new Vector2Int(Mathf.FloorToInt(p.x/14),Mathf.FloorToInt(p.z/14));
            if (!groundTilesV33.TryGetValue(key,out var tile))
            {
                tile = GroupV33(landRootV33,"V33_MeadowCell_"+key.x+"_"+key.y);
                groundTilesV33.Add(key,tile);
            }
            return tile;
        }

        void PlaceVillageLifeV33()
        {
            var props = GroupV33(landRootV33,"V33_WorkingVillage");
            for (int i = 0; i < housesV31.Count; i++)
            {
                var h = housesV31[i];
                PutPropV33(Rack,h.TransformPoint(new Vector3(i == 1 ? -6.8f : 5.6f,0,4.2f)),h.eulerAngles.y+12,.88f,1.75f,props);
                PutPropV33(Cart,h.TransformPoint(new Vector3(i == 1 ? 6.4f : -5.2f,0,4.3f)),h.eulerAngles.y-28,.94f,1.5f,props);
                var footing = h.Find("GroundedSeaStoneSkirt");
                for (int side = 0; side < 3; side++)
                for (int j = 0; j < 5; j++)
                {
                    float along = (j-2)*.18f;
                    var at = side == 0 ? new Vector3(along,0,-.65f) : new Vector3(side == 1 ? -.66f : .66f,0,along);
                    var desired = footing.TransformPoint(at);
                    int kind = j%2 == 0 ? Rock : Fern;
                    float scale = kind == Rock ? RangeV33(.22f,.35f) : RangeV33(.36f,.60f);
                    if (!FindLandV33(desired,.45f,1.0f,1.05f,.35f,out var p,12)) continue;
                    p.y -= kind == Rock ? .08f : .025f;
                    PlaceLushV33(kind,"V33_Footing_"+i+"_"+side+"_"+j,p,scale,RangeV33(0,360),GroundTileV33(p));
                    occupiedV33.Add((p,.36f));
                    footingsV33[i]++;
                }
            }
            var tavern = housesV31[1];
            PutPropV33(Rack,housesV31[2].TransformPoint(new Vector3(-7.8f,0,-4.6f)),102,.87f,1.72f,props);
            PutPropV33(Cart,new Vector3(ShoreX(14)-12,0,14),52,.88f,1.47f,props);
            PutPropV33(Fire,tavern.TransformPoint(new Vector3(-6.8f,0,9.4f)),40,.93f,2f,props);
            PutPropV33(Fire,new Vector3(ShoreX(24)-6,0,24),95,.86f,1.8f,props);
            for (int i = 0; i < 14; i++)
            {
                var house = housesV31[i%3];
                float sign = i%2==0 ? 1 : -1;
                var at = house.TransformPoint(new Vector3(sign*(4.9f+.7f*(i%3)),0,-4.7f+1.25f*i));
                PutPropV33(Fence,at,house.eulerAngles.y+(i%2==0 ? 87 : 102),.74f,1.27f,props,5.3f);
            }
            var lookout = housesV31[2];
            for (int i = 0; i < 12 && propsV33.Count(p => p.kind == Fire) < 2; i++)
            {
                var site = lookout.position+new Vector3(-8-i*.9f,0,((i%3)-1)*5f);
                PutPropV33(Fire,site,62+i*31,.85f,1.67f,props,7f);
            }

            foreach (float z in new[] {-35f,-15f,18f})
                PutPropV33(Rowboat,new Vector3(ShoreX(z)-3.1f,0,z),58+z*2,.84f,1.85f,props,9.5f);
            for (int i = 0; i < 22 && propsV33.Count(p => p.kind == Rowboat) < 3; i++)
            {
                float z = -78+i*8.4f;
                PutPropV33(Rowboat,new Vector3(ShoreX(z)-4.2f,0,z),68+i*37,.80f,1.65f,props,8f);
            }
        }

        void PutPropV33(int kind, Vector3 desired, float yaw, float scale, float radius, Transform parent, float reach=9f)
        {
            float freeboard = kind == Rowboat ? .15f : .35f;
            float relief = kind == Fence ? .86f : .63f;
            float upper = kind == Rowboat ? 1.95f : float.PositiveInfinity;
            if (!FindLandV33(desired,radius,reach,relief,freeboard,out var p,64,upper) &&
                !FindLandV33(desired-Vector3.right*3.4f,radius,reach*1.7f,relief,freeboard,out p,112,upper)) return;
            float dx = V31Ground(p.x-.55f,p.z)-V31Ground(p.x+.55f,p.z);
            float dz = V31Ground(p.x,p.z-.55f)-V31Ground(p.x,p.z+.55f);
            var normal = new Vector3(dx,1.10f,dz).normalized;
            var tilt = Quaternion.RotateTowards(Quaternion.identity,Quaternion.FromToRotation(Vector3.up,normal),11);
            p.y -= kind == Fire ? .045f : .065f;
            var node = PlaceLushV33(kind,"V33_"+LushModelNames[kind]+"_"+propsV33.Count,p,scale,yaw,parent);
            node.rotation = tilt*node.rotation;
            propsV33.Add((kind,node));
            occupiedV33.Add((p,radius));
            if (kind != Fire) return;
            var light = GroupV33(node,"V33_CampfireLight").gameObject.AddComponent<Light>();
            light.transform.localPosition = new Vector3(0,.67f,0);
            light.type = LightType.Point;
            light.range = 4.8f;
            light.intensity = .95f;
            light.color = new Color(1,.60f,.25f);
            light.shadows = LightShadows.None;
            light.gameObject.AddComponent<LushCampfireV33>().Initialize(light);
        }

        void PlantPinesV33()
        {
            var forest = GroupV33(landRootV33,"V33_MixedForest");
            for (int i = 0; i < 200 && pinesV33.Count < 26; i++)
            {
                float z = pinesV33.Count < 13 ? RangeV33(-43,48) : RangeV33(-105,109);
                var center = new Vector3(ShoreX(z)-RangeV33(9,39),0,z);
                float scale = RangeV33(.86f,1.34f);
                if (!FindLandV33(center,2.35f*scale,5.5f,1.65f,.75f,out var p,18)
                    || (Mathf.Abs(p.z-2.4f) < 6f && p.x > ShoreX(p.z)-16)
                    || maplesV33.Any(m => GapV33(m.position,p) < 12f)) continue;
                var tree = GroupV33(forest,"V33_CoastalPine_"+pinesV33.Count);
                tree.localPosition = p-Vector3.up*.13f;
                tree.localScale = new Vector3(scale*RangeV33(.88f,1.10f),scale,scale*RangeV33(.91f,1.13f));
                tree.localRotation = Quaternion.Euler(RangeV33(-3,3),RangeV33(0,360),RangeV33(-5,6));
                PlaceLushV33(0,"Trunk",Vector3.zero,1,0,tree);
                PlaceLushV33(1,"Needles",Vector3.zero,1,0,tree);
                pinesV33.Add(tree);
                occupiedV33.Add((p,1.5f*scale));
            }
        }

        void PlantMeadowsV33()
        {
            var anchors = new List<Vector3>();
            anchors.AddRange(pinesV33.Select(p => p.position));
            anchors.AddRange(maplesV33.Select(p => p.position));
            foreach (var h in housesV31)
            {
                anchors.Add(h.TransformPoint(new Vector3(-7,0,-3)));
                anchors.Add(h.TransformPoint(new Vector3(6,0,1)));
                anchors.Add(h.TransformPoint(new Vector3(-5,0,9)));
            }
            for (int i = 0; i < 18; i++)
            {
                float z = RangeV33(-100,104);
                anchors.Add(new Vector3(ShoreX(z)-RangeV33(3.2f,24f),0,z));
            }
            int[] kinds = { Grass,Fern,Rock }, targets = { 160,85,42 };
            for (int group = 0; group < targets.Length; group++)
            for (int attempt = 0; attempt < targets[group]*14 && meadowV33[group] < targets[group]; attempt++)
            {
                int kind = kinds[group];
                var a = anchors[landRandomV33.Next(anchors.Count)];
                float angle = RangeV33(0,Mathf.PI*2), spread = RangeV33(1.4f,6f);
                var center = a+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*spread;
                float scale = kind == Rock ? RangeV33(.43f,.81f) : RangeV33(.64f,1.43f);
                float radius = (kind == Rock ? 1.1f : .66f)*scale;
                if (!FindLandV33(center,radius,2.1f,kind == Rock ? 1.05f : .76f,.30f,out var p,12)) continue;
                p.y -= kind == Rock ? .10f : .038f;
                PlaceLushV33(kind,"V33_Meadow_"+group+"_"+meadowV33[group],p,scale,RangeV33(0,360),GroundTileV33(p));
                occupiedV33.Add((p,radius*.75f));
                meadowV33[group]++;
            }
        }

        void ReplaceReefLayoutV33()
        {
            var rng = new Random(330901);
            float Pick(float lo,float hi) => Mathf.Lerp(lo,hi,(float)rng.NextDouble());
            var centers = new List<Vector3>();
            float sea = SeaLifeRuntime.Sea;
            for (int i = 0; i < 1400 && centers.Count < 38; i++)
            {
                float z = centers.Count < 13 ? Pick(-42,45) : Pick(-95,100);
                float x = ShoreX(z)+Pick(5.2f,centers.Count<13 ? 24 : 40);
                float patch = .67f*Mathf.PerlinNoise(x*.125f+11,z*.092f+31)
                            + .33f*Mathf.PerlinNoise(x*.27f+41,z*.19f+7);
                if (patch < .47f) continue;
                var center = new Vector3(x,V31Ground(x,z),z);
                float depth = sea-center.y;
                if (depth < 1.05f || depth > 6.6f || centers.Any(c => GapV33(c,center) < 24f)
                    || GapV33(center,game.dockBerth) < 17f || GapV33(center,boatyardAnnexV31.position) < 16f)
                    continue;

                int wanted = 3+centers.Count%3;
                var children = new List<Vector3>();
                float heading = Pick(0,Mathf.PI*2);
                for (int j = 0; j < 24 && children.Count < wanted; j++)
                {
                    float angle = heading+j*2.39996f+Pick(-.34f,.34f);
                    float distance = Pick(.85f,2.4f);
                    var child = center+new Vector3(Mathf.Cos(angle)*distance,0,Mathf.Sin(angle)*distance*.76f);
                    child.y = V31Ground(child.x,child.z);
                    if (sea-child.y < .72f || sea-child.y > 6.85f || Mathf.Abs(child.y-center.y) > 1.55f
                        || GapV33(child,game.dockBerth) < 13f || GapV33(child,boatyardAnnexV31.position) < 12f
                        || children.Any(c => GapV33(c,child) < .39f)) continue;
                    children.Add(child);
                }
                if (children.Count < wanted) continue;
                int id = centers.Count;
                var group = GroupV33(reefRootV33,"V33_ReefPatch_"+id);
                int dominant = new[] {4,1,2,3}[id%4];
                AddMarineV33(group,center,dominant,id,Pick(1.12f,1.48f),Pick(1.22f,1.77f),Pick(0,360));
                var companions = new[] {0,5,2,3,4,1}.Where(s => s != dominant).ToArray();
                for (int c = 0; c < children.Count; c++)
                    AddMarineV33(group,children[c],companions[(id+c)%companions.Length],id+c+1,
                        Pick(.44f,.82f),Pick(.42f,.82f),Pick(0,360));
                reefsV33.Add(group);
                centers.Add(center);
            }
            if (reefsV33.Count < 30) throw new InvalidOperationException("Insufficient rooted v33 reef patches.");
            foreach (Transform node in worldRoot)
                if (node.name.StartsWith("V29_CoralCluster_") || node.name.StartsWith("V29_GiantKelp_"))
                    node.gameObject.SetActive(false);
        }

        void AddMarineV33(Transform parent, Vector3 point, int species, int tone, float width, float height, float yaw)
        {
            int shade = tone%8;
            var material = marineMaterialsV33[species,shade];
            if (material == null)
            {
                var source = species == 0 ? _v31EelgrassMat : species == 1 ? _v31BullKelpMat : _v31CoralSpeciesMats[species-2];
                float q = shade/7f;
                var color = source.GetColor("_BaseColor");
                material = OwnV33(source,"V33_Reef_"+species+"_"+shade,
                    color * new Color(Mathf.Lerp(.84f,1.04f,q),Mathf.Lerp(.93f,1.05f,q),Mathf.Lerp(1.06f,.90f,q),1));
                marineMaterialsV33[species,shade] = material;
            }
            var plant = AddMesh("V33_Species_"+species,_v31FloraMeshes[species],material,point,Vector3.one,false,parent);
            FitFloraV31(plant.transform,species,width,height,yaw);
            v31DiverseFloraCount++;
        }
    }

    sealed class LushCampfireV33 : MonoBehaviour
    {
        Light warm;
        public void Initialize(Light light) => warm = light;
        void Update()
        {
            if (warm != null)
                warm.intensity = .94f+.13f*Mathf.Sin(Time.time*10.2f)+.08f*Mathf.Sin(Time.time*17.4f+.6f);
        }
    }
}
