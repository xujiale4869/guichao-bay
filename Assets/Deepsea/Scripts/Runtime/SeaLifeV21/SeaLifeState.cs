using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Deepsea {
 [Serializable] public sealed class SeaLifeFish {public string id;public int species;public int tank=-1;public string area;public int variantV34;}
 [Serializable] public sealed class SeaLifeTank {public bool built,kelp;}
 [Serializable] public sealed class SeaLifeSave {
  public int schema=1,coins=30;public List<SeaLifeFish> fish=new List<SeaLifeFish>();
  public SeaLifeTank[] tanks={new SeaLifeTank(),new SeaLifeTank(),new SeaLifeTank()};
  public bool[] shells=new bool[4];public bool salvaged,shellDisplay,anchorDisplay;public float boatX=.8f,boatZ=5.2f,boatYaw=100;
  public SeaGameplayProgressV34 gameplayV34;
  public void Normalize(){if(fish==null)fish=new List<SeaLifeFish>();fish.RemoveAll(f=>f==null||string.IsNullOrEmpty(f.id)||f.species<0||f.species>2||f.tank < -1||f.tank>2);var ids=new HashSet<string>();fish.RemoveAll(f=>!ids.Add(f.id));if(tanks==null||tanks.Length!=3)tanks=new[]{new SeaLifeTank(),new SeaLifeTank(),new SeaLifeTank()};for(int i=0;i<3;i++)if(tanks[i]==null)tanks[i]=new SeaLifeTank();if(shells==null||shells.Length!=4)shells=new bool[4];coins=Math.Max(0,coins);
   if(SeaBayGameplayLoopV34.Enabled){
    if(gameplayV34==null)gameplayV34=new SeaGameplayProgressV34();
    gameplayV34.Normalize();foreach(var f in fish)f.variantV34=Mathf.Clamp(f.variantV34,0,3);
   }
  }
 }
 public sealed class SeaLifeStore {
  public readonly string PathName;public SeaLifeStore(string path){PathName=path;}
  public SeaLifeSave Load(){foreach(var p in new[]{PathName,PathName+".bak"}){if(!File.Exists(p))continue;try{var s=JsonUtility.FromJson<SeaLifeSave>(File.ReadAllText(p));if(s==null||s.schema!=1)continue;s.Normalize();return s;}catch(Exception e){Debug.LogWarning("Sea life save recovery: "+e.Message);}}return new SeaLifeSave();}
  public bool Save(SeaLifeSave data){try{Directory.CreateDirectory(Path.GetDirectoryName(PathName));var temp=PathName+".tmp";File.WriteAllText(temp,JsonUtility.ToJson(data,true));if(File.Exists(PathName)){File.Copy(PathName,PathName+".bak",true);File.Replace(temp,PathName,null);}else File.Move(temp,PathName);return true;}catch(Exception e){Debug.LogError("Sea life save failed: "+e.Message);return false;}}
 }
 public sealed class SeaLifeFishMotion:MonoBehaviour {
  public Vector3 center;public Vector3 extent=new Vector3(.8f,.3f,.4f);public float phase,speed=.4f;public bool outdoor;float clock;
  void Update(){clock+=Time.deltaTime;float a=clock*speed+phase;var p=center+new Vector3(Mathf.Sin(a)*extent.x,Mathf.Sin(a*1.7f+phase)*extent.y,Mathf.Cos(a)*extent.z);var q=center+new Vector3(Mathf.Sin(a+.04f)*extent.x,Mathf.Sin((a+.04f)*1.7f+phase)*extent.y,Mathf.Cos(a+.04f)*extent.z);transform.localPosition=p;if((q-p).sqrMagnitude>.00001f)transform.localRotation=Quaternion.LookRotation(q-p);}
 }
}
