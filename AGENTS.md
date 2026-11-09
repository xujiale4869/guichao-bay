# 深海灯塔当前工程

当前最新版为 v36.1。四态阳光海港配乐使用尼龙吉他、马林巴、手风琴与拇指琴，收获使用短木琴水珠音。水面沿用用户认可的 v34 原版港湾渲染，玩法和存档沿用 v34，航海界面沿用 v30。见 Docs/SeaAudioV36_1/README.md，程序 Builds/SeaSunnyAudioV36_1/Guichao-SeaSunnyAudioV36_1.exe。

历史海岸美术版本：v11，2026-10-23 完成独立程序验收。主要分析与制作记录在 `C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔/art-v11/reference-analysis.md`，项目内副本见 `Docs/原图对照与美术推进-v11.md`。

- 保留原船只控制、上下船、步行、物理地形和存档；新岩层、珊瑚和岸边收口为外观层，没有新增碰撞体。
- 海带保持用户认可的干净绿色；已改为按实际显示的岩面采样根部，避免外观层与物理岩体不同造成悬浮。
- 三维参考制作使用 `C:/Users/许嘉乐/.agents/skills/blender-image-to-3d/SKILL.md`；素材交付使用 `C:/Users/许嘉乐/.agents/skills/ai-game-art-pipeline/SKILL.md`；二维图标与界面风格可用 `C:/Users/许嘉乐/.agents/skills/style-loop/SKILL.md`。先读相应技能。
- Blender 为 `C:/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe`；图像脚本优先使用 Codex workspace dependencies 中带 NumPy/Pillow 的 Python。
- v11 最终检查：`Verification/coast-art-v11/result.json`；参考实机图：该目录下 `reference.png`。原生1080p固定镜头1200帧平均92.02 FPS，不能表述为全地图长期稳定表现。
- 中间 `coast-art-v11-failed-hidden-window` 是黑帧失败记录；`coast-art-v11-visual` 的较早偏黄画面不是最终版本。以后以最终验收目录为基线。
- 继续改进应优先处理：主礁台大形与重复轮廓、浅水的青蓝层次、灯塔和码头资产、前后景组织。尚未达到用户原图的美术质量。
- 编译入口：`Deepsea.Editor.CoastMaterialsBuild.Build`，输出 `Builds/CoastArtV11/GuichaoBay-ArtV11.exe`。独立验收参数：`-coast-materials -coast-ecology -whitebox-check -screen-fullscreen 0 -screen-width 1920 -screen-height 1080`，运行目录必须为工程根目录。

## 2026-10-23 当前进度：v12

已使用固定版本 Blender Rock Generator，接入本地入口 `C:/Users/许嘉乐/Desktop/深海灯塔-Unity海陆-v0.3/Tools/CoastArtV12/Rebuild-ReefArt.ps1`。先读 `C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔/art-v12/reference-analysis.md` 与 Tools/CoastArtV12/README-tools.md。39 处高礁采用生成主体加单层扫描岩冠，近岸仍用扫描石；统一折射焦散，浅水改为青蓝。六种模型各 1600 面，0 警告、0 错误。最终实机原生1080p固定镜头1200帧平均 86.22 FPS，航行、步行、上下船、穿洞通过。证据为 Verification/coast-art-v12/result.json，桌面入口“深海灯塔-海岸美术-v12”。

继续改进需优先解决连续礁台/海床过渡、珊瑚群落、灯塔码头与前景；尚未达到原图质量。绿色海带、原玩法与物理地形继续保留。不要使用初稿或将 v12 当成已匹配原图。构建入口仍为 Deepsea.Editor.CoastMaterialsBuild.Build，输出 Builds/CoastArtV12/GuichaoBay-ArtV12.exe；独立程序要用可见窗口截图，Hidden 会黑帧。历史 v11 程序与备份已保留。

Rock Generator 默认同步 Tools/CoastArtV12/accepted 冻结资产，-Regenerate 才生成新稿。上游生成及几何处理未通过逐字节复现，生成新稿后要重验，不能当成相同已验收网格。

## 2026-10-23 当前版本 v13

最新报告：C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔/art-v13/reference-analysis.md。连续礁台8块共35044面，5种珊瑚模型5020面，最终512丛；98丛绿色海带；远沟108条小银鱼，总鱼数196。木板外观与灯具已补充，塔身增加风化。新旧海床使用覆盖遮罩在颜色/深度/阴影通道一致裁切；植物查可见网格。原物理地形、船控、步行、存档未改。

原生1080p新展示镜头1200帧平均 70.97 FPS，P95 16.09ms；航行、上下船、12点步行、穿洞和31360次可见海床鱼群路线检查通过。展示镜头相对v12偏移(1.8,-.5,-2.8)，严格前后对照用 reference-baseline.png。最新入口为桌面“深海灯塔-海岸美术-v13”。构建仍用 Deepsea.Editor.CoastMaterialsBuild.Build，输出 Builds/CoastArtV13/GuichaoBay-ArtV13.exe；独立验收要使用可见窗口，参数 -coast-materials -coast-ecology -whitebox-check。

GitHub：复用已存在 FastNoiseLite 1.1.1（同提交785f37a），珊瑚生长参考 NejcHirci/procedural-corals（25e89f9，MIT），保留源码/许可。工具入口 Tools/CoastArtV13/Rebuild-CoastArt.ps1：默认同步已验收珊瑚，-Build 构建，-GenerateCorals 产生需重验的新稿。海床C#配方在 CoastShelfShape/CoastShelfBuild，GLB海床是几何查看档，精确引擎数据在 ShelfMeshes.json。

仍须改善：岸脚硬边、陡壁破碎、浅水反射/浪沫、塔身和码头细节、暖冷光焦点。不要声称已达到原图。保留旧版程序和备份，中间失败稿不作为结果。

## 2026-10-23 用户对 v13 的反馈

用户指出与原图仍差距很大，小改动缺少整体提升；上一版本水面可以，v13 水面变差。v12/v13 同镜头截图与源码已核对：v13 浅水偏灰发白，水红色吸收 .95→.28、折射 .0022→.008、波纹斜率增强，同时替换可见海床材质和几何。将 v12 水面作为用户认可的保留基线，后续需按同镜头实机对照恢复，并单独适配新海床。此记录时尚未执行回退。

v13 工程检查通过不等于美术获认可。后续优先构图、地形大形、整体光照与材质统一，不能继续用装饰数量和工具接入代替可见的画质进步。


## 2026-10-24 当前 v14：基础模型与材质重做

用户要求停止堆装饰，先修基础。已恢复 v12 水面核心着色器与 .0022 折射；收回浅滩覆盖并统一新旧海床材质。新灯塔 19320 面、8 材质组、真实拱窗门洞；分层裂隙礁面 36292 面；原地形只调整渲染法线。珊瑚实际 511、上限512，干净绿色海带重采样后108，没有新增种类或投放尝试次数。

最新报告 C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔/art-v14/reference-analysis.md。独立程序验收通过，原生1080p固定镜头1200帧平均 76.28 FPS，P95 14.66ms；31360次可见礁面路线检查无阻挡，航行、上下船、12点步行和穿洞通过。灯塔网格0警告0错误，灯塔/礁面GLB回读通过。原物理地形与核心玩法源码不变。

最新入口桌面“深海灯塔-基础美术-v14”；构建仍用 Deepsea.Editor.CoastMaterialsBuild.Build，输出 Builds/CoastArtV14/GuichaoBay-ArtV14.exe。工具 Tools/CoastArtV14/Rebuild-CoastFoundation.ps1 默认同步冻结模型，-GenerateLighthouse 仅生成需重验的新稿（UV存在约2.39e-7的浮点尾差，不承诺逐字节复现）。水面不得再次随意改动。

整景仍低于原图，用户尚未认可 v14 美术。下一步基础问题为岸线粗硬边、扫描岩体重复、步道与岸脚接合、礁壁过于陡直和整体主光层次；不要又转去增加装饰数量。失败验收和错误玻璃截图不是交付结果。


## 2026-10-24 当前 v15：岸线、石阶与礁面

最新报告 C:\Users\许嘉乐\Documents\ChatGPT\深海灯塔\art-v15\reference-analysis.md。可见岸脚细分至0.325m，299968面；原物理地形仍74992面，陆上步行带采样高度改动0。礁面8块36292面，重新设计宽肩、分段缓降、鞍部和岩层，陡面面积占比44.5%→31.1%（几何统计，不是审美评分）。石阶替换外观、地被色调和主光与环境光调整。坡壁材质试验因浅色平面问题已撤回，最终保留原材质结构。保留v12水核心哈希、0.0022折射与干净绿色海带，没有新增装饰种类或投放尝试。

最终原生1080p固定镜头1200帧平均72.66 FPS，P95 16.59ms；航行、上下船、12点往返步行、穿洞、31360次可见礁面鱼群检查通过。新地形/礁面GLB回读336260面、0错误；8文件源码配方在临时目录重建SHA256一致。冻结入口 Tools/CoastArtV15/Rebuild-CoastLandform.ps1（默认同步，-Build构建），程序 Builds/CoastArtV15/GuichaoBay-ArtV15.exe，桌面“深海灯塔-岸线基础-v15”。v14与修改前备份保留。

仍明显低于原图，用户尚未认可。下一步应逐段塑造礁坡大岩块/凹口，改善扫描岸石重复、洞口大平面、码头结构和前景光影。不得把陡面比例、帧率或数量当作画面质量认可。中间草稿、浅色三角条纹稿和浅色坡面稿不作交付。

## 2026-10-24 v16 独立三维美术样板（原可玩版仍为v15）

用户要求整体提升并授权“请制作”，已完成独立新场景/程序，不等于原游戏已升级到v16。报告 C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔/art-v16/reference-analysis.md；桌面入口“深海灯塔-独立美术样板-v16”。场景 Assets/Deepsea/Scenes/CoastArtShowcase.unity；构建入口 Deepsea.Editor.CoastShowcaseBuild.Build；程序 Builds/CoastArtV16/MistCape-ArtSample.exe。默认观察场景，不加载原航行、步行、穿洞和存档玩法。原v15程序保留。

新地形、扫描岩群、加宽转折礁台、码头/步道、松树；生成松针、木纹、珊瑚组织3张1254平方贴图。实际738144面、63网格组、608755索引顶点，311珊瑚、63干净绿色长带海带、178鱼。海带复用原网格/材质，v12水面核心与.0022折射未改；新海床和光照改变水面观感，不能称为逐像素相同。

最终可见窗口原生1080p，1200帧固定全景平均64.41FPS，P95 16.28ms；证据 Verification/coast-art-v16-final/result.json。6视角无黑帧/粉色/运行错误，水下远景边界已用一致的深水雾处理。28480次离线实际求值网格鱼群检查无阻挡。Blender0失败393警告（共点/分缝、封口多边形），GLB回读738144面0错误5张嵌入图。不要写成0警告、原玩法测试通过或用户认可美术。

329个原脚本/着色器与制作前备份逐字节一致。冻结9输入入口 Tools/CoastArtV16/Rebuild-ArtSample.ps1，默认恢复SHA256已检查输入，-Build构建，-GenerateGeometry才生成需重验新稿，不承诺生成器逐字节复现。Blender源文件只含新环境；灯塔、船、人、水与精确光照在Unity可编辑场景。GLB仅几何/部分材质查看档，整体抬高17.871675m，坐标见清单；Unity JSON无偏移。

样板仍明显低于原图，用户尚未认可；扫描岩石重复、岸脚接合、松树卡片感、地面空平、浅水焦散均匀、灯塔码头完成度和盘状珊瑚偏暗仍需改善。后续优先处理这些大形、基础资产和明暗组织；不要把新增数量、工具接入或技术检查当作质变证明。

## 2026-10-24 用户认可v16制作方向

用户实看后反馈“确实改善了很多，这个方向是对的”，并询问灯塔和岸石贴图。认可的是改善与方向，未表示已匹配原图。以后以v16独立样板构图继续；保留其水面核心与绿色海带。灯塔已有2K灰泥颜色/法线/ARM贴图，岸石已有扫描颜色/法线贴图、粗糙度目前统一.84；下一步应做有针对性的分层材质和几何接合。详细记录 C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔/art-v16/user-feedback.md。本次问答未修改运行材质或构建。

## 2026-10-24 v17：认可方向上的灯塔和局部岸石精修

用户认可v16“改善很多、方向正确”，授权先做灯塔与旁侧一组岸石。v17仍是独立美术样板，原可玩版v15与美术样板v16均保留。报告 C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔/art-v17/reference-analysis.md。桌面“深海灯塔-灯塔岸石精修-v17”；程序 Builds/CoastArtV17/MistCape-Materials.exe，场景 CoastArtRefined.unity，构建 Deepsea.Editor.CoastRefinementBuild.Build。

新灰白灰泥贴图1254平方，局部基座露石/窗下雨痕/轻微金属风化。43块真实基座石，4块岸石干湿/粗糙度/少量苔藓，359个底部顶点最大调整.08558m，3处干地接合补片；水下环状失败补片已去除。构图、太阳、后期、水核心、礁体和绿色海带不变。F7在同镜头切换真实v16/v17环境与塔材质，5/6为灯塔/岸石近景。

最终可见窗口原生1080p固定镜头1200帧平均62.21FPS，P95 18.84ms。环境743425面68组618510索引顶点；较v16增加5281面/5组。岸石/基座源档149281面，模型0警告0失败，GLB回读面数一致0错误；不把整个旧场景历史警告报成0。339旧脚本/着色器、v16冻结9输入和58项其他环境网格逐字节一致。新冻结入口Tools/CoastArtV17/Rebuild-MaterialStudy.ps1默认恢复6输入且已测试，-Build构建，-GenerateDraft为需重验新稿。完整源档含岸石/基座与灯塔两份Blender文件，精确风化在Unity。

未集成原航行/步行/存档，没有新玩法通过声明；本轮未重测沿用的鱼群路线。v17尚未由用户评价，仍有扫描岩形重复、岩脚硬边、门窗简化与植被卡片感，不能称为已匹配原图。后续在此方向继续，不退回旧构图和水面。

## 当前 v20：局部岸岩与独立水面碎光对照

用户授权“直接精做一段岸岩并导入实机对照”。报告 `art-v20/reference-analysis.md`；桌面“深海灯塔-岸岩精修-v20”，程序 `Builds/CoastArtV20/MistCape-FineShore.exe`，场景 `CoastArtFineShore.unity`，构建 `Deepsea.Editor.CoastFineShoreBuild.Build`。仍是独立样板，原可玩v15、v19样板均保留，未整合原玩法。

只替换木阶右侧Headland03/Foot03，使用397472面高模、6656面LOD0、3992面LOD1及四张2048烘焙图。法线是真实高低模投射，AO为隔离低模几何烘焙；底色复用v19图投射，粗糙度分层。Tree00抬高1.6267566m并重建7根，其他树不变。6看局部，F7真v19/v20对照，F9独立切碎光，F8暂停落叶。

原水核心SHA256仍4a7fdfa9164a208a8136c192cb3284ea335d2b60e5ec03f7fe7a652ac02c015c、.0022折射与水材质原文件未改；新增克隆水材质与独立shader，用真实太阳方向的统计微面元补充动态高光。实际参数2.4/8/.52/.9/.25/.6（强度/尺度/分布/密度/覆盖/速度），无全局太阳或曝光改动。首稿过稀的碎光和偏规整白亮的岩稿不是最终结果。

最终证据 `Verification/coast-art-v20-delivery/`，由已复核review2原始采集复制，未重复测试。原生1080p岸边1200帧平均65.4819FPS、P95 16.6558ms；关闭碎光另1200帧66.0107FPS、P95 16.6347ms，两时窗差值不等于精确shader耗时。12600落叶状态、7230新岩体空气路径、4200水面漂流、28480鱼群路线检查通过。局部模型0失败、1项3个0.1mm内近点提示；Tree00两网格0失败0提示；GLB回读6656面0错误、3嵌入图（含合并AO/粗糙度）。GLB抬高1.1088836m，blend/JSON无偏移。

184项旧脚本/着色器/水材质/v19场景与v19冻结14输入哈希一致；场景检查121对象、107网格、113材质槽及其余锚点/目标通过。QualitySettings/ProjectSettings不变，GraphicsSettings仅保留2个新增shader引用。工具 `Tools/CoastArtV20/Rebuild-FineShore.ps1` 默认校验恢复11冻结输入，-Build构建；已实测默认恢复。新生成需重验，不承诺跨环境逐字节生成。可编辑源、同镜头对照、12秒实际144帧/12fps视频均在art-v20。

仍有部分直线断缝、大平面和拼装感；其余海岸未精修，整体仍明显低于概念图。碎光方向与强度尚待用户实看反馈，不能把检查通过或帧率当作美术认可。SSAO逐帧噪点改变，前后截图仅同镜头/同水波时间，不声称逐像素相同。

## 2026-10-28 v21：海湾生活首轮可玩接入

用户认可分镜效果并授权尝试接入现有美术。现已在v20红枫海岬基础上新增独立入口，报告 `art-v21/reference-analysis.md`。桌面“深海灯塔-海湾生活-v21”，程序 `Builds/SeaLifeV21/Guichao-SeaLife.exe`，场景 `CoastSeaLife.unity`，构建 `Deepsea.Editor.SeaLifeBuild.Build`。旧v15程序与v20美术样板均保留；不加载旧GameDirector，不恢复旧救援/建设链。

本轮可玩：独立船控与上下船/步行、三处鱼区投钩/浮漂/提竿/收线张力、保留或放流、潮池四个拾贝点、固定吊箱船锚、小馆步行房间和三个固定展缸位、放鱼/取回/近景观赏、出售与独立存档。新鱼/缸体/房间是可编辑C#配方原型资产，未做城镇经营、自由缸体编辑、多岛地图或完整生态。继续保留红枫、水面核心/水材质和干净绿色海带；新跟随镜头与景观鱼表现不等于v20逐像素相同。

最终仅用 `Verification/sea-life-v21-delivery/`（副本 `art-v21/runtime/delivery/`）与 `art-v21/runtime/review-06.log`，73项检查通过；真实控制器步行路线、潮池接地、钓鱼/打捞/建缸/收藏唯一性/容量/金币不足/存档回读/720p按钮边界通过，运行错误0警告0，不声称全工程0警告。原生1080p固定镜头600帧、锁60FPS平均59.9867FPS/P95 16.6759ms，不能直接比较旧版不限帧采样。部分起点由自动验收设置；额外展鱼和容量鱼是明确临时测试样本，不进正常游玩存档。实际窗口另抽查E取竿、鼠标投钩、咬钩和E提竿；检测到用户输入后停止追加自动输入，没有人工完整收线验收声明。

170项旧文件哈希一致；冻结33项本轮输入在 `Tools/SeaLifeV21/accepted`，`Rebuild-SeaLife.ps1` 默认恢复/校验已实测，-VerifyOnly只检查，-Build构建；依赖原v20资产，不是全工程备份或逐字节构建复现承诺。Noto Sans SC 2.002字体元数据、SHA与OFL许可随包保存。独立视觉judge因账户额度失败，没有独立结论或评分；限定UI自查为provisional，不代替三维美术验收。用户尚未评价v21实机；新房间、缸内水光和鱼类细节仍需精修。



## 2026-10-28 v22：水面首轮修订
用户选择先处理水面后已完成独立版。报告 C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔/art-v22/reference-analysis.md，桌面“深海灯塔-水面修订-v22”，程序 Builds/SeaLifeWaterV22/Guichao-SeaWater.exe，场景 CoastSeaLifeWaterV22.unity。F7原/新水面对照；窗口仍沿用v21产品名和正常存档目录。移除新shader的随机碎光层，新增34869顶点/68992面缓浪网格、船身视觉响应、历史尾迹与浮漂/落叶高度接入，原控制器和碰撞不变。
最终 Verification/sea-water-v22-review03/ 与 delivery 副本，38项检查通过；原生1080p锁60固定镜头600帧59.98834FPS/P95 16.67730ms，运行0错误0警告，不代表全工程或全地图。旧源/水材质/旧场景/设置清单199项哈希一致。Tools/SeaLifeWaterV22/Rebuild-Water.ps1默认只校验23项冻结输入，不自动恢复覆盖；-Build使用保存好的v22场景，显式Prepare才重新生成。前两轮尾迹草稿不作交付。浅水搁浅、物理水阻/划桨、UI、生态与钓鱼动作未重做，用户尚未评价新水面观感。具体边界以报告和工具说明为准。

## 2026-10-28 Git仓库与第三方内容实际保存

桌面实际Unity工程已初始化Git主仓库（main）。五个既有上游工具采用固定提交子模块保存，来源在.gitmodules；三项UPM本地引用保持原路径。BoatAttack、Tiled Directional Flow、FastNoiseLite、NVJOB、Lucide的实际文件和许可已纳入主仓库，不把保留参考等同于当前场景启用。

Tools/AgentSkillSources保存五个已有美术/UI工具的185个文件，来源/版本/文件SHA256在sources.lock.json；原用户技能安装未改。上游.gitattributes以.upstream归档，映射可还原。Git属性保留原文件字节，大资产使用LFS，ProjectSettings保持普通可读文本。

本轮仓库检查57项通过，Unity依赖检查21项通过、当前v22场景221项引用可解析。本机从提交435e3be克隆主仓库、恢复五个子仓库与LFS、核对源码和工具快照通过；证据Verification/repository-integration/。这不代表新增船控/水面功能或v23实机验收。v23同时在制作，整理期间出现的后续改动须按git status另行核对，不能当成已验收稳定版。

远端尚未配置，GitHub推送未完成；需要明确目标仓库及可写连接后再推送，不能把本地提交称为GitHub已保存。详情Docs/GitHub内容接入-20261028.md。新增整理脚本是本轮维护入口，日常只读检查用Tools/RepositoryIntegration/verify_repository.py；不要反复执行一次性导入脚本覆盖档案。

## 2026-10-28 v23：GitHub波浪与惯性船控
用户授权查找GitHub水面流动和船控实现并直接应用，不兼容部分自行实现。已在v22基线接入Boat Attack Gerstner公式与Habrador水阻公式，固定提交、原始源码及许可见Tools/SeaLifeNavigationV23/ThirdParty。UPM/URP版本未改；推进、制动、舵向平滑、转向惯性、随波船身与历史尾迹为适配实现。
最新报告C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔/art-v23/reference-analysis.md。桌面“深海灯塔-流动水面与船控-v23”和“深海灯塔-v23航行演示”。程序Builds/SeaLifeNavigationV23/Guichao-SeaNavigation.exe；场景CoastSeaLifeNavigationV23.unity；构建Deepsea.Editor.SeaLifeNavigationBuildV23.Build。原v22程序保留，产品名/存档沿用v21。W推进、松键滑行、S制动倒车、A/D平滑转向；F7为v22水材质/原推进算法对照，共享新碰撞/尾迹/视觉层，不是完整旧版二进制还原。
最终Verification/sea-navigation-v23-review03，46项自动检查通过，运行0错误0警告；1080p锁60固定600帧帧率和P95见最终报告。实际引擎144帧/12fps演示，不是实时性能证据。57项旧文件清单中，仅SeaLifeRuntime.cs增加一处FixedUpdate路由，其余56哈希一致；不是全工程所有资产哈希声明。build-04成功，有20项构建警告，不报全工程无警告。工具Rebuild-Navigation.ps1默认只核对冻结输入，不自动覆盖；accepted依赖旧环境，非全工程备份。显式Prepare会从v22重新生成v23场景。
已测人工测试障碍阻挡与倒退脱离，没有全海湾浅水逐段验收；UI、生态群落与钓鱼人物动作未重做，没有完整人工钓鱼/水族馆回归或用户已认可美术的声明。

## 2026-10-28 v24：海面层次与性能样板
用户明确否定v23的波浪滚动感、海面/水下层次与性能。本轮独立v24样板以海面为主、浅水隐现为目标，增加方向性滚动波、深度/朝向/浪峰控制的可见度、青蓝反光。旧美术/干净绿色海带保留，用户尚未评价本轮效果；不将检查通过当作美术认可。
桌面“深海灯塔-海面层次与性能-v24”和“深海灯塔-v24海浪演示”。报告C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔/art-v24/reference-analysis.md；程序Builds/SeaSurfaceV24/Guichao-SeaSurface.exe；场景CoastSeaSurfaceV24.unity；默认构建Deepsea.Editor.SeaSurfaceBuildV24.Build。F7对照水面，F9帧率。正常产品名/存档沿用v21。
实际取消限帧，原v23状态同镜头1080p58.53–65.03FPS；v24最终147.55–148.92FPS，主画面renderScale=1。FXAA替4倍MSAA、半分辨率折射颜色副本、水下细节分级和单次接触阴影滤波。保留阴影与原AO采样精度/强度；画面存在抗锯齿和水下细节取舍。61项原输入清单只允许v23主类两个可选钩子变化，其余60项哈希一致；不是全工程哈希声明。
4K窗口请求被限制为2880x1800，采用3840x2160离屏场景加本机窗口呈现，B10G11R11_UFloatPack32、renderScale=1；最终约56.42FPS/P95 19.35ms，未达到稳定60或4K高刷，不称为4K显示器原生测试。最终证据Verification/sea-surface-v24-review05和sea-surface-v24-4k-review05；32项1080p检查通过，两次运行0错误0警告，build-05成功但仍有历史构建警告。
视频8秒240张1080p引擎帧按30fps固定步长录制，交付缩为720p；不是实时录屏或性能证据。测试使用唯一临时存档。未做全海湾浅水/全地图长期性能、完整人工钓鱼/水族馆回归，UI/生态/钓鱼人物动作未重做。
新增四份水下材质shader与运行时克隆，原材质/shader不改；0.25–0.85m水深渐变降低法线/遮罩/BRDF细节，岸上及水下相机保留完整路径。新增源码/Shader/资源在SeaSurfaceV24目录；v23主类只加波形采样和F7两个可选钩子，备份Tools/SeaSurfaceV24/Before/SeaLifeNavigationV23.cs。构建使用已验证Unity方法，冻结清单用于核对，不自动恢复覆盖；显式Prepare/TuneAndBuild/PrepareBudgetAndBuild属于生成步骤，会覆盖对应v24资源。accepted/源码包依赖既有v23环境，并非完整工程。


## 2026-10-28 v25：用户提供的鱼类与展缸包实际接入
用户要求执行所附六文件交接。新场景 CoastSeaLifeV25.unity 继承 v24 海面/船控，程序 Builds/SeaLifeV25/Guichao-SeaLifeV25.exe；正常存档路径/产品名保持 v21。桌面“深海灯塔-鱼类与展缸-v25”和“深海灯塔-v25展缸演示”；后者只有临时样本，不写正常收藏。报告 Docs/SeaLifeV25/README.md（另存 art-v25）。
Blender 实际导出六FBX、三张512贴图与六个可编辑源文件。修复原稿重复反向面异常、PNG上下翻转、眼睛UV、FBX朝向/单位、柜体沙床尺寸与材质槽；数据主类拆到匹配文件名。运行时等待Ready，鱼/缸创建时更新，无每帧全场景反射扫描；路径拥有朝向，摆尾不累积偏航，暂停立即停止。旧源码只在SeaLifeActivities/SeaLifeAquarium各加一个可选钩子，旧场景未挂组件时为空操作。
最终 build-02 成功，仍有历史 OceanSurface.cs 过时API警告；Verification/sea-life-v25-review02 60项检查通过、运行0错误0警告。正常存档SHA前后相同，256旧输入仅上述两文件变化（254项哈希相同）。review01暂停失败已修复，不作为最终证据。1080p固定镜头600帧，旧鱼137.88FPS/P95 8.23ms、新鱼136.99/P95 9.12ms；单缸近景369.23/P95 3.19ms。非全地图、长期或4K测试。
六秒视频为144帧/24fps固定步长引擎录制，分享720p，非实时性能证据。未将SO经济字段接管旧鱼价/容量，未做完整PBR贴图、真实体积散射、微气泡、UI/人物钓鱼动作/海湾生态重做；不称最终美术认可。普通再构建 Deepsea.Editor.SeaLifeV25Build.Build；显式 ImportAndBuild 才重新生成v25资产/场景。旧版本构建保留。


## 2026-10-29 v26：用户水面光学包接入
用户提供三份核心代码与交接说明，两份C#附件相同。独立场景CoastSeaSurfaceV26.unity继承v25，程序Builds/SeaSurfaceV26/Guichao-SeaSurfaceV26.exe；桌面深海灯塔－最新版，F6只对照水面材质，F9帧率。原v25及更早程序保留。
原始包在Tools/SeaSurfaceV26/OriginalInputs，详细适配与范围见Docs/SeaSurfaceV26/README.md。控制器等待Ready并只在v26场景/显式参数启动，继承v24波形与暂停时钟，接入焦散/泡沫/渐变贴图并保留Shader资源引用。修复384贴图被Unity缩放、原稿规则亮点、过强焦散、静止尾流与生态路径跳变；新增蝠鲼/72银鱼仍为程序化原型，不作为高精生态或全海湾路径验收。
263项旧输入中262项哈希一致；唯一变化为SeaSurfaceV24.cs的F7条件加v26 Active判空，避免新旧模式冲突。正常存档SHA不变。build-02成功、新Shader编译错误0，但全包仍24项历史警告；review-02的21项检查通过，运行0错误0警告。1080p可见固定镜头480帧：对照138.90FPS/P95 8.31ms，v26 129.49FPS/P95 8.70ms；非长期/全地图/4K性能声明。
最终截图与result.json在Docs/SeaSurfaceV26；draft01导入缩放失败且有规则亮点，不作交付依据。本轮未收到目标原图本体，不能称已匹配原图或用户已认可；未做完整人工钓鱼/水族馆回归。新生态两侧保留，F6不是全旧版二进制还原。重建使用Deepsea.Editor.SeaSurfaceBuildV26.Build；ImportAndBuild会显式重建场景。


## 2026-10-29 v26.1：完整一体包接入与最终复核

使用用户提供 gpt_v26_1_all_in_one 完整包；主控制器与 Shader 按原文提取（终止换行），SHA 见 Docs/SeaSurfaceV261/README.md。不得自行降低其焦散1.68、碎光3.45、泡沫1.28等原始参数；Resources水材质与运行时值已同步。72银鱼走连续纵向S路径，另28金色礁鱼；本轮未另行重绘珊瑚资产。

独立 partial SeaLegacySchoolIsolationV261 仅在新版控制器启用时停用精确名为“沿用海岬鱼群表现”的34条旧环境鱼，避免旧环形鱼群叠加；原包两文件保持原样。v26.0完整程序在Builds/SeaSurfaceV26-v26.0-backup，旧v26快捷入口改指备份，桌面最新版和v26.1入口指当前SeaSurfaceV26程序，启动参数-forceV26Water。

最终build-02成功、Shader编译错误0；增量构建报告1项历史过时API警告，首轮完整构建24项历史警告，不能称全工程0警告。最终review-02证据Docs/SeaSurfaceV26/result.json及三图：34项通过、运行0错误0警告；原生1080p固定镜头每状态480帧，原水155.72FPS/P95 8.21ms，新水143.55/P95 7.93ms，不外推全地图/长期/4K。F6共用材质切换方法经过检查，非OS键盘事件注入。正常存档SHA不变，临时存档隔离。268项旧输入265不变、3项预期源码变化；Water.mat另有参数序列化更新。完整原始包、基线与交付哈希在Tools/SeaSurfaceV261。

重建Deepsea.Editor.SeaSurfacePolishV261.SyncAndBuild从核心C#同步原始数值并构建现有场景；普通Build保留材质/场景，ImportAndBuild会重生成。review-01旧圈鱼叠加图不作为最终画面，旧v26证据在Docs/SeaSurfaceV261/PreviousV26。本轮仍未收到目标原图，未完成原图匹配/用户美术认可，也未作完整人工钓鱼展缸回归。



## 2026-11-05 v26.2：焦散/波光收敛与F7调参实机交付
用户新包明确将焦散/碎光/聚焦/泡沫默认值改为.92/1.85/72/.95，取代v26.1仅针对旧包的禁止削弱要求。Shader按新附件原文；C#光学/波形/生态按附件，GUI因实机重叠改为下移68px、实色背景和颜色状态恢复，并加v26.2提示。鼠标滑块输入通过可选指针判断拦截，原F7保护/旧鱼群隔离保留。
最终build-02成功、Shader错误0，仍有1项历史API编译警告。Verification/water-v262-review02的44项检查通过、运行0错误0警告；另water-v262-input01的11项通过，包括Windows实际F7、焦散滑块、黄金预设和关闭，操作时真实取竿仍保持未投钩。其余四滑块未逐一OS拖动；三预设5参数程序检查通过。正常存档SHA前后相同。270旧输入266哈希不变、4项预期源码变动，Water.mat另同步新参数。
原生1080p每状态480帧，最终原水128.64FPS/P95 8.58ms，v26.2为120.65/P95 8.87ms；短窗有波动，不外推长期/全图/4K。报告Docs/SeaSurfaceV262/README.md；Docs/SeaSurfaceV26正式预览与结果已更新。review01重叠截图不是最终版。旧v26.1在Builds/SeaSurfaceV26-v26.1-backup，旧版本入口指备份；最新版与v26.2入口指当前SeaSurfaceV26程序，-forceV26Water，F7调参/F6对照/F9帧率。普通重建SeaSurfaceBuildV26.Build；同步后重建SeaSurfacePolishV262.SyncAndBuild，勿用v26.1的旧参数断言入口。GUI滑块仅本次运行，不存玩家档。未做完整人工钓鱼/展缸/全海湾回归、目标图匹配或用户美术认可。


## 2026-11-05 v27：港湾提灯与两片浅礁实机交付
用户提供 gpt_v27_harbor_reef_all_in_one 包，原文保存在 Tools/SeaHarborV27/OriginalInputs。独立场景 CoastSeaHarborV27.unity、程序 Builds/SeaHarborV27/Guichao-SeaHarborV27.exe；桌面最新版/v27入口指新程序，旧 v26.2 完整保留，正常产品名/存档沿用 v21。
四个 Blender FBX、两张颜色贴图、四个 .blend 实际生成，基准空节点校准导入朝向/米制单位；不是原注释里的六个整件 FBX，也不是完整 PBR 贴图组。灯笼开放框架避免遮灯芯，船灯/货物挂随波视觉层；桥头暖灯、合并网格绳栏留靠船开口、小馆木檐接入。原包粗枝珊瑚增分叉与盘层，新群落6珊瑚/5海带收成两片，按可见海床落根/预算限制，继续保留干净绿色海带。
F8恢复所有材质槽、覆盖的旧灯网格和原水面Shader；F7/F6仍为v26.2调参/水面材质对照，.92/1.85/72/.95默认参数保留。新材质使用两盏无阴影灯的位置/颜色做局部暖光，无新增反射相机/碰撞体/阴影贴图；初始化后没有全场景反射扫描。暂停冻结新增动画。
最终build-03成功，v27三Shader错误0，全包仍24项既有警告。Runtime 33项 + Windows原生F8 9项检查通过，两次运行均0错误0警告；实际推进/栈桥步行/上下船通过，测试唯一临时存档。2772项旧源码/资源/场景/设置/包文件全部哈希不变，正常存档SHA不变。1920x1080固定镜头每状态480帧，对照143.06FPS/P95 7.91ms，v27 143.35/P95 8.03ms；仅短窗，先前120–134FPS有波动，非全地图/长期/4K结论。
正式 Docs/SeaHarborV27/README.md、Runtime和NativeInput；首轮零新增群落、原生输入前台保护中止，以及第二轮草稿均不作最终美术证据。尚未拿到目标原图或用户美术认可，未做完整人工钓鱼/展缸回归。普通重建 SeaHarborBuildV27.Build；ImportAndBuild显式重生成。Tools/SeaHarborV27/Rebuild-Harbor.ps1默认只核对冻结输入，-Build重建，-Regenerate -Build生成新稿需重验。


## 2026-11-05 v28：海域扩建与G键建造空间交付
用户提供 gpt_v28_bay_expansion_all_in_one 并要求接入、构建、截图及推送。独立场景 CoastSeaExpansionV28.unity，程序 Builds/SeaExpansionV28/Guichao-SeaExpansionV28.exe；桌面最新版/v28入口指新程序，旧 v27 保留，正常产品名/存档沿用 v21，建造另存 sea-bay-expansion-v28.json。
航行边界仅 v28 扩至 X[-10,86] Z[-50,66]（矩形面积约5.06倍，不是可建面积声明），惯性/后备推进、恢复/保存坐标同步；水面用300m网格保留世界坐标波浪而非拉伸Transform。连续A暖沙、B南北礁坪、C向东开口新月环礁；2m网格原点(-1,2.4)。四设施为桥/灯/珊瑚花园/蟹笼；按深度落根并验证四角、碰撞、船/上下船区域和物资。固定接口(0,0)允许旧栈桥重叠并实测连续步行，其他格保留限制。复用v27模型/木纹/绿色海带；缓存预览、合并静态网格、释放拆除几何、最多四盏新增无阴影灯。建造时停船、隐藏旧HUD、拦截旧操作与F7，退出恢复。
最终 build-06 增量构建有1项历史API警告，本轮完整构建曾24项；Shader错误0。Verification/bay-v28-review06的48项与bay-v28-native05的12项通过，两次运行均0错误0警告。原生G/W/3/R/鼠标左建右拆/G退出已测。总旧输入2937项，2934哈希不变，唯一三预期源码为SeaLifeRuntime、SeaLifeNavigationMotorV23、SeaWaterArtUpgradeV26；正常存档未改，旧v27 exe哈希未改。详见Docs/SeaExpansionV28/result.json。
1920x1080宽视野每镜头480帧：扩建全景105.53FPS/P95 10.74ms，规划网格108.58FPS/P95 10.23ms；镜头比v27测量宽，不作直接新旧开销对比、长期/全地图/4K结论。正式图在Docs/SeaExpansionV28。原生报告的性能计数为未采样默认0，勿引用。
原包在Tools/SeaExpansionV28/OriginalInputs。旧版全局自动加载、SmoothStep边缘、空布局重生、悬空固定深度、输入/镜头争用、材质粉色、边界保存和桥接缝已适配；失败/停止记录在Drafts，不作正式依据。native03/04的鼠标验收曾错误追踪变化中的hovered格，最终固定局部目标；驱动显式模块、前台PID保护、客户端坐标/DPI映射。普通重建Deepsea.Editor.SeaBayExpansionBuildV28.Build；Rebuild-Bay.ps1默认仅校验36输入，-Build构建，-Prepare才从v27重生成。
本轮不是完整经营系统：蟹笼无自动产蟹/收益，建材无采集补给链，未新增外海鱼点或新登船位置；普通游戏无测试补给按钮。新区域未完成整体环境精修，未收到用户美术认可。自由布局不保证所有步行路径连通；源包/冻结清单依赖原工程，非完整独立项目备份。

## 2026-11-05 v29：连续海域与航海界面交付
用户提供 gpt_v29_seamless_world_and_ui_all_in_one；原文保存在 Tools/SeaSeamlessV29/OriginalInputs。独立场景 CoastSeaSeamlessV29.unity、程序 Builds/SeaSeamlessV29/Guichao-SeaSeamlessV29.exe，桌面最新版/v29入口指新版；旧v28 exe与Assembly-CSharp哈希保持不变。正常产品名/收藏档沿用v21，建造独立sea-bay-expansion-v29.json，首次从v28复制布局而不改旧文件。
航行边界X[-22,128] Z[-88,108]，29,400m²，约v21矩形面积13.36倍，不声称15倍可建面积。420m海面、连续大陆架、西岸延伸、南北海岸、通海航道和外海礁脉；35个原网格真实三角栅格采样得到28,561覆盖节点，保守邻域下包络再压低.09m并14m混合，原资产不改。新增46岸岩、61礁、36珊瑚组、18绿色海草组、12红枫。六岩变体来自现有完整702三角扫描岩；未把大块地形表面当独立模型，未把无效简化称作优化。
B/G营造、WASD漫游、滚轮规划缩放、1–4设施、R旋转、左建右拆；航行滚轮8.5–34实际改变透视视场。L仅切换船灯芯/灯光/局部暖光，保留世界珊瑚。7个SDF轮廓图标、暗金圆角双边框、资源胶囊与紧凑建造抽屉；隐藏旧常驻HUD但保留实际钓鱼/鱼箱/暂停/展缸交互。修复全场景自动引导、G双切换、错字段反射、正交参数改透视镜头、海床遮原岩、文字无字形、礁顶随机种植失败。新代码拆分partial，新地形在建造网格/布局加载前生成，旧六处接口限定v29。
最终build-07增量构建1项历史API警告，完整构建24项；Verification/seamless-v29-review07的58项与seamless-v29-native02的12项通过，两次运行0错误0警告。实际Windows G/W/3/R/左右键/G/滚轮均测，测试存档在加载前隔离。2967旧工程文件仅六预期源码变化，2961哈希相同；正常收藏/旧布局测试前后相同。review06修地形块外缘权重和陆地肩坡，review07让Busy当前动作优先于旧Notice并加一致性检查。正式Docs/SeaSeamlessV29/result.json、Runtime、NativeInput；review01功能通过仍有可见缺陷、review04植被检查失败、review05旧地形直边与钓鱼阶段提示冲突，均保留Drafts。
原生1920x1080宽视野每状态480帧，航行71.84FPS/P95 15.70ms，规划67.00FPS/P95 17.27ms；非全图/长期/4K结论，不与不同镜头旧版本直接比较。航行/规划另有原生720p截图。普通重建Deepsea.Editor.SeaSeamlessBuildV29.Build；Rebuild-Seamless.ps1默认校验冻结清单，-Build构建保存场景，-Prepare显式重新采样/烘焙/生成，需重验。
美术状态为可运行初稿，独立视觉评审原文和缺失覆盖见Docs/SeaSeamlessV29/VisualReview；未完成全状态图标对照、720p全部弹窗/钓鱼、独立新内容迁移，不声称全游戏风格验证或用户美术认可。仍无新外海鱼点/登船点、蟹笼自动收益与材料补给链，自由建造不保证任意步行连通；源包/冻结清单依赖原工程，非全工程独立备份。

## 2026-11-06 v30：一体半岛、生态渐变与斜切海洋界面
用户上传 gpt_v30_unified_island_and_persona_ui_all_in_one，要求覆盖3核心源码、浅水平坡网格、实机截图、桌面最新版、GitHub提交推送。已在原SeaSeamlessWorldV29目录升级并补齐真实接口；原包留Tools/SeaUnifiedV30/OriginalInputs。独立场景CoastSeaUnifiedV30.unity，程序Builds/SeaUnifiedV30/Guichao-SeaUnifiedV30.exe，正常产品名/收藏仍v21；布局新sea-bay-expansion-v30.json首次复制v29而不改旧档。v29 exe/Assembly完整保留，精确旧源码检出58525e1。
350x340m单地形73341顶点，520m水面，58m余弦大陆坡，四边与背景同为sea-14m。修正原稿资源路径、未定义变量/方法、SmoothStep参数、背景高度不一致、旧HUD/弹窗争层、材质通道和植物高度预算。北南内陆完整抬升，46岸岩56礁52珊瑚42绿海草18红枫；原模型/材质只运行时克隆。旧大沙块实际来自六张MapleSubstrate条带的旧Ground渲染，已与FineShore/Land_Rock/内陆/岬角/根部统一生态过渡。南湾仍一段直线形薄岸坎，属已知局部minor，不称全图100%无接缝。普通放置与网格统一0.45–3.2m及四邻2m高差≤0.65；四块继承的起始码头接头保留以免断步道，不能用作任意深水放置。
七个SDF图标、青色错位斜切面、金色选中/菱形/抬升缩放倾斜、水印键、独立石/苗资源栏；黄铜库存保留。室内V观赏/B布置/F返回，空位禁用观赏并给前置条件提示；A/B/C按钮与对应命名地面框同步金青状态。图标一次烘焙、无逐帧创建纹理；原人物/船/绿色海带/光学参数继续保留。
最终build06增量1项既有API警告，首构建24项；review06为78项运行、native03为20项实际Windows键鼠检查，均0错误0警告。native02的V失败是空缸不满足前置条件；native03在唯一隔离档实际建首缸再验V。首轮旧接头未生成、错误叶片路径、沙块和快捷键混淆均保留草稿。3008旧输入仅七处预期源码变化，正常存档测试时不改，旧v29二进制不改。最终数据Docs/SeaUnifiedV30/result.json。
原生1080p每视角480帧最终航行68.09FPS/P95 17.09ms、规划64.96/P95 18.11ms；前一同类窗口航行56.93/P95 25.00ms，不挑选快值作稳定60/全图长期/4K结论。1080p航行/规划/北/南/钓鱼/鱼箱/暂停/展室，720p航行/规划，另A/B/C三态实机图。独立p030/p031原文及后续自验分开；无完整状态/满鱼箱/全部720p弹窗/人物表情/独立新内容迁移，全游戏风格仍provisional，未获用户美术定稿认可。
按交接更新Docs/SeaSeamlessV29四图，原v29图/README归PreviousV29；v30独立正式证据和可复用UI说明在Docs/SeaUnifiedV30。Rebuild-Unified.ps1默认哈希校验，-Build构建保存场景，-Prepare从v29另存生成新场景需重验。Finalize-Unified为首次隔离验收后交付流程，正常启动创建v30档后勿重跑其“新档应不存在”的基线断言。旧v29冻结清单会正确拒绝本次共享源码升级，别自动恢复旧包覆盖v30。仍无新增外海鱼点/登船点/材料采集/蟹笼自动收益，任意自由建造路径不保证连通。


## 2026-11-06 v31：渔村与生态多样性
用户提供v31一键包。原文Tools/SeaVillageV31/OriginalInputs，实际适配资源烘焙、材质槽、导入坐标、姿态限高、可见地面、温和空地和重入保护。11个Blender FBX/源文件与8890三角，运行时使用同源JSON烘焙的Unity Mesh，三座生成建筑实际渲染；非原稿方块回退。界面按本轮用户要求沿用已交付v30，四UI源码和原字体保持不变；保留绿色海带、水光学、人物/船与旧玩法。三建筑是后续NPC据点，没有NPC行为/室内/新收益系统。布局sea-bay-expansion-v31.json首次从v30复制，正常产品名仍v21。
最终运行126项/原生键鼠20项通过；旧3017输入仅四旧源码变化，正常档测试时不改，v30exe/dll保留。精确数据与视觉限制见Docs/SeaVillageV31，首稿高石基/遮挡失败证据保留Drafts，功能通过不等于美术认可。短窗性能不得称全图长期稳定或4K表现。默认Rebuild-Village校验冻结输入，-Build重建，-Prepare显式重生成需重新验收。不要重跑已普通启动后的首轮Finalize基线断言；旧v30冻结清单会拒绝共享源码升级，精确v30源码检出9069585。
