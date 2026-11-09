# 归潮湾 v35 · 雾岬岛翡翠水体

Windows 程序位于 `Builds/SeaMistCapeV35/Guichao-SeaMistCapeV35.exe`。桌面入口为「深海灯塔—最新版」。使用 v34 的存档与玩法，沿用 v30 航海界面。

浅滩保留暖沙和水下轮廓，礁盘呈青绿，深沟逐渐过渡到群青。两层 Voronoi 光纹沿相反方向缓慢移动，焦散在浅水亮部更清楚，随阴影和深度减弱。栈桥涟漪取自原木桩与水面的交点。船速与朝向驱动后方的 V 形浪花。

《雾岬岛·东岸》原画用于核对水色、焦散分布和画面重心。45° 实机对照沿用现有岸线、礁体和灯塔资产，构图及模型细节与原画仍有差距。

## 实机验证

- 可见窗口原生 1920×1080。266 项运行检查与 30 项原生键鼠检查通过。
- 普通启动读取 19 座设施和 1 个已建展缸。10 个已有存档及备份文件的 SHA-256 在启动前后一致。
- [45° 海湾全景](preview.jpg) · [水面近景](02-emerald-water-closeup.jpg) · [同机位 v34 水面对照](00-v34-same-view.jpg)
- [运行检查](result.json) · [键鼠检查](native-result.json) · [普通启动](normal-startup.json)

复建使用 `Tools/SeaMistCapeV35/Rebuild-MistCape.ps1`。隔离运行检查使用 `Tools/SeaVillageV31/Run-Review.ps1 -MistCape -Evidence village-v31-mist-cape-reviewXX`。加 `-Native` 运行原生键鼠检查。
