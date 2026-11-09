using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Deepsea {
 public sealed partial class SeaSeamlessWorldAndNauticalUIV29 {
  Canvas nauticalCanvas;RectTransform drawer;TextMeshProUGUI subtitle,context,hoverStatus,materialText;
  TextMeshProUGUI quickBuildLabel;TextMeshProUGUI[] toolWatermarks=new TextMeshProUGUI[4];
  TextMeshProUGUI[] pills=new TextMeshProUGUI[5],toolLabels=new TextMeshProUGUI[4];Button[] toolButtons=new Button[4],cards=new Button[4];
  Image[] toolImages=new Image[4],cardImages=new Image[4];RectTransform[] toolRects=new RectTransform[4],cardRects=new RectTransform[4];TextMeshProUGUI[] selectedMarkers=new TextMeshProUGUI[4];
  GameObject contextPanel,quickActions;string hoverTitle="",hoverCost="";
  readonly Color ivory=new Color(.98f,.97f,.93f),gold=new Color(1,.82f,.28f),cyanAccent=new Color(.24f,.94f,.86f),dim=new Color(.76f,.88f,.86f);
  TextMeshProUGUI Text(Transform parent,string name,string content,Rect rect,int size,Color color){var r=CreateUIRect(name,parent,new Vector2(0,1),new Vector2(0,1),new Vector2(rect.x,-rect.y),rect.size);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=_cnFont;t.text=content;t.fontSize=size;t.color=color;t.enableWordWrapping=false;t.enableAutoSizing=true;t.fontSizeMin=Mathf.Max(16,size-2);t.fontSizeMax=size;t.overflowMode=TextOverflowModes.Ellipsis;t.raycastTarget=false;t.outlineWidth=.10f;t.outlineColor=new Color32(4,12,20,230);return t;}
  RectTransform Surface(Transform parent,string name,Vector2 anchor,Vector2 pivot,Vector2 pos,Vector2 size,Sprite sprite,bool blocks=true){var r=CreateUIRect(name,parent,anchor,pivot,pos,size);var im=r.gameObject.AddComponent<Image>();im.sprite=sprite;im.type=Image.Type.Sliced;im.raycastTarget=blocks;return r;}
  void ToggleBlueprintV30(){if(game.InRoom)game.OpenPanel(1);else bay.ToggleGodBuildMode(!bay.godBuildMode);}
  Button IconButton(Transform parent,string name,Rect rect,Sprite icon,string label,string key,Action action,out Image image,out RectTransform rt){
   rt=CreateUIRect(name,parent,Vector2.up,Vector2.up,new Vector2(rect.x,-rect.y),rect.size);image=rt.gameObject.AddComponent<Image>();image.sprite=_iconSlotSprite;image.type=Image.Type.Sliced;var b=rt.gameObject.AddComponent<Button>();b.targetGraphic=image;var colors=b.colors;colors.highlightedColor=new Color(1,.97f,.82f);colors.pressedColor=new Color(.70f,.94f,.88f);colors.disabledColor=new Color(.5f,.58f,.57f,.8f);b.colors=colors;b.onClick.AddListener(()=>{game.Cue();action();});
   var wm=Text(rt,"Watermark",key,new Rect(8,2,rect.width-16,44),25,new Color(1,.88f,.45f,.20f));wm.fontStyle=FontStyles.Bold|FontStyles.Italic;
   var ir=CreateUIRect("Icon",rt,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-12),new Vector2(42,42));var ic=ir.gameObject.AddComponent<Image>();ic.sprite=icon;ic.raycastTarget=false;
   var lb=Text(rt,"KeyLabel",label,new Rect(4,55,rect.width-8,34),18,ivory);lb.fontStyle=FontStyles.Bold;lb.alignment=TextAlignmentOptions.Center;return b;
  }
  void BuildNauticalUI(){
   _cnFont=Resources.Load<TMP_FontAsset>("SeaLifeV21/SeaLifeChinese");
   _roundedCardSprite=BakeNauticalNineSliceSprite("V30_PersonaSkewCard",96,96,10,new Color(.03f,.08f,.11f,.97f),new Color(.88f,.96f,.95f,.94f),2);
   _pillBadgeSprite=BakeNauticalNineSliceSprite("V30_PersonaSkewPill",72,42,8,new Color(.04f,.10f,.14f,.97f),new Color(.32f,.86f,.80f,.92f),1.6f);
   _iconSlotSprite=BakeNauticalNineSliceSprite("V30_PersonaToolSlot",80,80,9,new Color(.04f,.10f,.14f,.96f),new Color(.52f,.78f,.76f,.9f),1.8f);
   _iconSlotActiveSprite=BakeNauticalNineSliceSprite("V30_PersonaActiveSlot",80,80,9,new Color(.10f,.28f,.26f,.98f),new Color(1,.84f,.32f,1),2.6f);
   icons=new Sprite[7];for(int i=0;i<7;i++)icons[i]=BakeVectorIconSprite(i);
   var node=new GameObject("V29_NauticalHUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));node.transform.SetParent(transform,false);nauticalCanvas=node.GetComponent<Canvas>();nauticalCanvas.renderMode=RenderMode.ScreenSpaceOverlay;nauticalCanvas.sortingOrder=48;var scaler=node.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;var root=node.GetComponent<RectTransform>();
   var header=CreateUIRect("PoeticHeader",root,Vector2.up,Vector2.up,new Vector2(28,-22),new Vector2(1080,115));
   var title=Surface(header,"TitleSkewBanner",Vector2.up,Vector2.up,Vector2.zero,new Vector2(310,60),_iconSlotActiveSprite,false);title.localRotation=Quaternion.Euler(0,0,1.4f);Text(title,"RegionTitle","雾岬岛 · 东岸",new Rect(18,2,274,56),30,ivory).fontStyle=FontStyles.Bold;
   var sub=Surface(header,"SubtitleBacking",Vector2.up,Vector2.up,new Vector2(12,-62),new Vector2(910,46),_pillBadgeSprite,false);subtitle=Text(sub,"RegionSubtitle","红枫半岛 · 潮间浅湾",new Rect(20,1,870,44),18,gold);
   float px=320;int[] widths={126,128,126,136,136};for(int i=0;i<5;i++){var p=Surface(header,"ResourcePill"+i,Vector2.up,Vector2.up,new Vector2(px,i%2==0?-4:-8),new Vector2(widths[i],44),_pillBadgeSprite,false);p.localRotation=Quaternion.Euler(0,0,-1.2f);pills[i]=Text(p,"ResourceValue","",new Rect(8,2,widths[i]-16,40),18,ivory);pills[i].alignment=TextAlignmentOptions.Center;px+=widths[i]+10;}
   var quick=CreateUIRect("QuickActions",root,Vector2.one,Vector2.one,new Vector2(-28,-24),new Vector2(390,48));quickActions=quick.gameObject;SmallButton(quick,"Tab 活鱼箱",new Rect(0,0,124,46),()=>game.OpenPanel(2));SmallButton(quick,"B 海湾营造",new Rect(130,0,124,46),ToggleBlueprintV30);SmallButton(quick,"Esc 暂停",new Rect(260,0,124,46),()=>{game.Paused=!game.Paused;Time.timeScale=game.Paused?0:1;game.ui.RebuildPanel();});
   quickBuildLabel=quick.Find("B 海湾营造/Label").GetComponent<TextMeshProUGUI>();
   var dock=CreateUIRect("Tools",root,Vector2.zero,Vector2.zero,new Vector2(32,26),new Vector2(380,100));
   string[] labels={"提灯","挂钩","钓竿","蓝图"},keys={"L","02","01","B"};Action[] actions={ToggleLantern,()=>{if(game.InRoom)game.ToggleCloseView();else game.SelectTool(1);},()=>{if(game.InRoom)game.OpenPanel(1);else game.SelectTool(0);},()=>{if(game.InRoom)game.ToggleBoard();else ToggleBlueprintV30();}};
   for(int i=0;i<4;i++){toolButtons[i]=IconButton(dock,"Tool"+i,new Rect(i*92,-(i%2)*4,84,92),icons[i],labels[i],keys[i],actions[i],out toolImages[i],out toolRects[i]);toolLabels[i]=toolButtons[i].transform.Find("KeyLabel").GetComponent<TextMeshProUGUI>();toolWatermarks[i]=toolButtons[i].transform.Find("Watermark").GetComponent<TextMeshProUGUI>();}
   var prompt=Surface(root,"Context",new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,26),new Vector2(700,68),_roundedCardSprite,false);contextPanel=prompt.gameObject;context=Text(prompt,"ContextText","",new Rect(22,7,656,54),19,ivory);context.fontStyle=FontStyles.Bold;context.alignment=TextAlignmentOptions.Center;
   drawer=Surface(root,"BlueprintDrawer",new Vector2(1,0),new Vector2(1,0),new Vector2(-28,24),new Vector2(680,270),_roundedCardSprite);
   Text(drawer,"DrawerTitle","海湾营造蓝图 // BAY",new Rect(22,8,340,42),23,gold).fontStyle=FontStyles.Bold;SmallButton(drawer,"R 旋转",new Rect(398,8,122,40),bay.RotateV29);SmallButton(drawer,"G 返回",new Rect(530,8,122,40),()=>bay.ToggleGodBuildMode(false));materialText=Text(drawer,"BuildMaterials","",new Rect(22,48,636,36),18,cyanAccent);
   string[] names={"木栈桥","暖光航标","珊瑚生态园","捕蟹笼"},costs={"浮木 2","木 1 · 铜 1","石 2 · 苗 1","木 1 · 石 1"};int[] ids={4,0,5,6};
   for(int i=0;i<4;i++){int n=i;cards[i]=IconButton(drawer,"BuildCard"+i,new Rect(22+i*160,90,150,126),icons[ids[i]],names[i],"0"+(i+1),()=>bay.SelectV29(n),out cardImages[i],out cardRects[i]);var cost=Text(cards[i].transform,"Cost",costs[i],new Rect(4,87,142,34),17,dim);cost.alignment=TextAlignmentOptions.Center;selectedMarkers[i]=Text(cards[i].transform,"Selected","◆",new Rect(10,7,28,36),20,gold);var e=cards[i].gameObject.AddComponent<EventTrigger>();var enter=new EventTrigger.Entry{eventID=EventTriggerType.PointerEnter};enter.callback.AddListener(_=>{hoverTitle=names[n];hoverCost=costs[n];});e.triggers.Add(enter);var exit=new EventTrigger.Entry{eventID=EventTriggerType.PointerExit};exit.callback.AddListener(_=>{hoverTitle="";hoverCost="";});e.triggers.Add(exit);}
   hoverStatus=Text(drawer,"HoverStatus","",new Rect(22,226,636,36),18,ivory);drawer.gameObject.SetActive(false);Canvas.ForceUpdateCanvases();
  }
  void SmallButton(Transform parent,string title,Rect r,Action action){var p=Surface(parent,title,Vector2.up,Vector2.up,new Vector2(r.x,-r.y),r.size,_pillBadgeSprite);var b=p.gameObject.AddComponent<Button>();b.targetGraphic=p.GetComponent<Image>();b.onClick.AddListener(()=>{if(game.Busy)return;game.Cue();action();});var t=Text(p,"Label",title,new Rect(8,1,r.width-16,r.height-2),18,ivory);t.alignment=TextAlignmentOptions.Center;}
  void RefreshNauticalUI(){
   game.ui.HidePersistentV29();nauticalCanvas.enabled=game.ui.HudV29;bool planning=bay.godBuildMode,modal=game.Panel!=0||game.Paused||game.WorkshopOpenV34;float dt=1-Mathf.Exp(-12*Time.unscaledDeltaTime);
   RefreshGameplayUIV34(modal,planning);
   quickActions.SetActive(!planning&&!modal);drawer.gameObject.SetActive(planning&&!modal);contextPanel.SetActive(!planning&&!modal);
   var d=bay.DataV29;pills[0].text="金币 "+game.save.coins;pills[1].text="活鱼 "+game.CarryCount+"/"+game.MaxCarryCount;pills[2].text="浮木 "+d.wood;pills[3].text="潮汐石 "+d.stone;pills[4].text="珊瑚苗 "+d.coralSeed;
   subtitle.text=planning?"海湾营造 · WASD 漫游 / 滚轮缩放 / 左键建造 / 右键拆除":game.InRoom?"潮汐小馆 · 布置展缸与收藏":game.OnFoot?"红枫码头 · 沿岸探索":game.ActivePosition.z>26?"北岬红枫林 · 开阔半岛与珊瑚浅湾":game.ActivePosition.z<-22?"南湾半月浅滩 · 沿岸寻潮":"红枫半岛 · 潮间浅湾 / 滚轮远眺";
   for(int i=0;i<4;i++){bool active=game.InRoom?(i==1?game.CloseView:i==2?game.Panel==1:false):i==0?LanternOn:i==1?!planning&&game.SelectedTool==1:i==2?!planning&&game.SelectedTool==0:planning;toolImages[i].sprite=active?_iconSlotActiveSprite:_iconSlotSprite;toolButtons[i].interactable=!modal&&!game.Busy&&(game.InRoom?(i!=0&&(i!=1||game.save.tanks[game.SelectedTank].built)):(i!=1||!game.OnFoot));toolRects[i].anchoredPosition=Vector2.Lerp(toolRects[i].anchoredPosition,new Vector2(i*92,(i%2)*4+(active?10:0)),dt);toolRects[i].localScale=Vector3.Lerp(toolRects[i].localScale,Vector3.one*(active?1.06f:1),dt);toolRects[i].localRotation=Quaternion.Slerp(toolRects[i].localRotation,Quaternion.Euler(0,0,active?-3.5f:0),dt);bool selected=bay.SelectedV29==i;cardImages[i].sprite=selected?_iconSlotActiveSprite:_iconSlotSprite;selectedMarkers[i].gameObject.SetActive(selected);cardRects[i].anchoredPosition=Vector2.Lerp(cardRects[i].anchoredPosition,new Vector2(22+i*160,-90+(selected?8:0)),dt);cardRects[i].localScale=Vector3.Lerp(cardRects[i].localScale,Vector3.one*(selected?1.035f:1),dt);cards[i].interactable=!modal&&!game.Busy;}
   toolLabels[1].text=game.InRoom?(game.save.tanks[game.SelectedTank].built?"观赏":"先建展缸"):"挂钩";toolLabels[2].text=game.InRoom?"布置展室":"钓竿";toolLabels[3].text=game.InRoom?"返回码头":"蓝图";toolWatermarks[1].text=game.InRoom?"V":"02";toolWatermarks[2].text=game.InRoom?"B":"01";toolWatermarks[3].text=game.InRoom?"F":"B";quickBuildLabel.text=game.InRoom?"B 布置展室":"B 海湾营造";
   context.text=game.Busy?game.Hint:Time.unscaledTime<game.NoticeTime?game.Notice:game.Hint+(!game.InRoom&&!game.OnFoot?" · 滚轮看远海":"");materialText.text="浮木 "+d.wood+" · 潮汐石 "+d.stone+" · 珊瑚苗 "+d.coralSeed+" · 黄铜 "+d.brass;hoverStatus.text=bay.FeedbackV29.Length>0?bay.FeedbackV29:hoverTitle.Length>0?hoverTitle+" · "+hoverCost:bay.HoverV29;RefreshGallerySelectionV30();
  }
 }
}
