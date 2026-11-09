using UnityEngine;
namespace Deepsea {
 public sealed partial class SeaLifeNavigationV23 {
  float throttle,rudder,yawRate;Mesh hullMesh;PhysicMaterial hullFriction;
  public bool NavigationReady {get{return ready;}}
  public float Throttle {get{return throttle;}}
  public float YawRate {get{return yawRate;}}
  void SetupNavigation(){
   game.body.drag=0;game.body.angularDrag=0;
   var oldHull=game.boat.GetComponent<BoxCollider>();if(oldHull!=null)oldHull.enabled=false;
   // A shallow, tapered collision hull prevents rectangular bow corners catching submerged ledges.
   var outline=new[]{new Vector2(0,1.96f),new Vector2(.59f,1.36f),new Vector2(.76f,0),new Vector2(.64f,-1.40f),new Vector2(.34f,-1.84f),new Vector2(-.34f,-1.84f),new Vector2(-.64f,-1.40f),new Vector2(-.76f,0),new Vector2(-.59f,1.36f)};
   int n=outline.Length;var v=new Vector3[n*2];var triangles=new System.Collections.Generic.List<int>();
   for(int i=0;i<n;i++){v[i]=new Vector3(outline[i].x,-.05f,outline[i].y);v[i+n]=new Vector3(outline[i].x,.70f,outline[i].y);int j=(i+1)%n;triangles.AddRange(new[]{i,j,j+n,i,j+n,i+n});}
   for(int i=1;i<n-1;i++)triangles.AddRange(new[]{0,i+1,i,n,n+i,n+i+1});
   hullMesh=new Mesh{name="v23浅吃水收尖船体碰撞"};hullMesh.vertices=v;hullMesh.SetTriangles(triangles,0);hullMesh.RecalculateNormals();hullMesh.RecalculateBounds();
   var collider=game.boat.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=hullMesh;collider.convex=true;
   hullFriction=new PhysicMaterial("v23船沿滑动"){staticFriction=0,dynamicFriction=0,bounciness=0,frictionCombine=PhysicMaterialCombine.Minimum,bounceCombine=PhysicMaterialCombine.Minimum};collider.sharedMaterial=hullFriction;
  }
  public void Drive(){
   if(!ready||game.Paused||game.OnFoot||game.body.isKinematic){throttle=0;rudder=0;yawRate=0;return;}
   bool control=!game.Busy&&!game.UiBlocking;
   var input=control?(game.AutoInput?game.DriveInput:new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"))):Vector2.zero;
   input.x=Mathf.Clamp(input.x,-1,1);input.y=Mathf.Clamp(input.y,-1,1);
   float dt=Time.fixedDeltaTime;throttle=Mathf.MoveTowards(throttle,input.y,dt*(Mathf.Abs(input.y)<Mathf.Abs(throttle)?3.8f:1.9f));rudder=Mathf.MoveTowards(rudder,input.x,dt*3.5f);
   var forward=game.boat.forward;var right=game.boat.right;float along=Vector3.Dot(game.body.velocity,forward),side=Vector3.Dot(game.body.velocity,right);
   float thrust=throttle>=0?3.2f*throttle:1.22f*throttle;
   // Opposite input brakes the hull promptly before reverse builds up.
   if(throttle*along<-.05f)thrust=throttle*3.4f;
   float resistanceSpeed=along>0&&throttle>0?along/game.ForwardSpeedV34:along;
   float drag=NavigationResistanceV23.ViscousAcceleration(resistanceSpeed,game.body.mass,.58f,4.6f)-resistanceSpeed*.40f;
   if(along<0)drag-=along*Mathf.Abs(along)*.24f;
   float lateral=NavigationResistanceV23.ViscousAcceleration(side,game.body.mass,2.2f,1.9f)-side*2.25f;
   game.body.AddForce(forward*(thrust+drag)+right*lateral,ForceMode.Acceleration);
   float authority=.13f+.87f*Mathf.Clamp01(Mathf.Abs(along)/2.6f),reverse=along<-.15f?-1:1;
   float wanted=rudder*56*authority*reverse;yawRate=Mathf.MoveTowards(yawRate,wanted,dt*(Mathf.Abs(wanted)<Mathf.Abs(yawRate)?120:90));
   if(!control&&Mathf.Abs(along)<.2f)yawRate=0;
   game.body.MoveRotation(game.body.rotation*Quaternion.Euler(0,yawRate*dt,0));
   if(input.sqrMagnitude<.001f&&game.body.velocity.sqrMagnitude<.001f)game.body.velocity=Vector3.zero;
   SeaBayExpansionAndBuildSpaceV28.ClampBoat(game.body);
  }
  Vector3 HullNormal(Vector3 p){
   var forward=game.boat.forward;var right=game.boat.right;
   float pitch=(Height(p+forward*1.75f)-Height(p-forward*1.75f))/3.5f;
   float roll=(Height(p+right*.75f)-Height(p-right*.75f))/1.5f;
   return (Vector3.up-forward*pitch-right*roll).normalized;
  }
 }
}
