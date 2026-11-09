using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Deepsea {
 public sealed partial class SeaSeamlessWorldAndNauticalUIV29 {
  RectTransform workshopModalV34;
  TextMeshProUGUI watchLabelV34;
  void BuildGameplayUIV34(){
   var parent=nauticalCanvas.transform;
   var clock=Surface(parent,"V34 Time",new Vector2(1,1),new Vector2(1,1),new Vector2(-28,-82),
       new Vector2(222,38),_pillBadgeSprite,false);
   watchLabelV34=Text(clock,"Watch","",new Rect(12,0,198,38),20,ivory);
   watchLabelV34.alignment=TextAlignmentOptions.Center;
  }
  void RefreshGameplayUIV34(bool modal,bool planning){
   if(watchLabelV34==null)return;
   watchLabelV34.transform.parent.gameObject.SetActive(!modal&&!planning&&!game.InRoom);
   watchLabelV34.text="T  "+SeaBayGameplayLoopV34.Watches[game.save.gameplayV34.watch];
  }
  internal void ShowWorkshopV34(){
   if(workshopModalV34!=null){workshopModalV34.gameObject.SetActive(false);Destroy(workshopModalV34.gameObject);}
   var gameplay=game.GameplayV34;
   int n=gameplay.Dialog;
   if(n<0)return;
   var root=nauticalCanvas.transform;
   // A full-screen graphic catches clicks behind the card while the world stays visible.
   var shade=CreateUIRect("V34 Workshop",root,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
   shade.anchorMax=Vector2.one;
   shade.offsetMin=Vector2.zero;shade.offsetMax=Vector2.zero;
   shade.gameObject.AddComponent<Image>().color=new Color(.015f,.035f,.05f,.24f);
   workshopModalV34=shade;
   var card=Surface(shade,"WorkshopCard",new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,42),new Vector2(820,340),_roundedCardSprite);
   Text(card,"Place",SeaBayGameplayLoopV34.Houses[n]+"  //  渔村工坊",new Rect(36,17,660,35),22,cyanAccent).fontStyle=FontStyles.Bold;
   var host=Text(card,"Host",SeaBayGameplayLoopV34.Workshops[n],new Rect(36,51,690,59),32,gold);
   host.fontStyle=FontStyles.Bold|FontStyles.Italic;
   host.enableAutoSizing=false;host.overflowMode=TextOverflowModes.Overflow;
   string details=n==0?"改装小船 · 当前极速 +"+25*gameplay.State.motor+"% · 活鱼舱 "+game.MaxCarryCount+" 条":
       n==1?"鱼获与肥蟹收购 · 悬赏奖励 金币 120 / 黄铜 3":
       "生态繁荣 "+gameplay.Prosperity+" · 珊瑚园每座 +12 · 月光水母 / 黄金锦鲤 / 极光雀鲷";
   Text(card,"Details",details,new Rect(36,111,748,38),23,ivory);
   SmallButton(card,"Esc 关闭",new Rect(675,21,108,40),gameplay.CloseWorkshop);
   for(int i=0;i<2;i++){
    int action=i;
    var rect=Surface(card,"Action"+i,Vector2.up,Vector2.up,new Vector2(37+i*375,-177),
        new Vector2(361,68),_pillBadgeSprite);
    var button=rect.gameObject.AddComponent<Button>();
    button.targetGraphic=rect.GetComponent<Image>();button.interactable=gameplay.ActionEnabled(i);
    button.onClick.AddListener(()=>{game.Cue();gameplay.RunWorkshopAction(action);});
    var label=Text(rect,"ActionLabel",gameplay.ActionLabel(i),new Rect(11,0,339,68),20,ivory);
    label.alignment=TextAlignmentOptions.Center;
   }
   var data=bay.DataV29;
   string stock="金币 "+game.save.coins+" · 浮木 "+data.wood+" · 黄铜 "+data.brass+" · 潮汐石 "+data.stone+" · 肥蟹 "+gameplay.State.crabs+" · 鱼饵 "+gameplay.State.bait;
   Text(card,"WorkshopStock",stock,new Rect(36,254,748,33),20,dim);
   Text(card,"WorkshopFeedback",gameplay.Feedback,new Rect(36,291,748,34),21,gold);
  }
 }
}
