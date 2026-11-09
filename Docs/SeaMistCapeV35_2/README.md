# 归潮湾 v35.2 · 雾岬岛短峰波

Windows 程序位于 `Builds/SeaMistCapeV35_2/Guichao-SeaMistCapeV35_2.exe`。桌面入口为「深海灯塔—最新版」。沿用 v30 航海界面与 v34 玩法、存档。

海面高光来自交叉短峰和三层旋转的梯度噪声法线。空间扭曲和横向包络打断长亮纹，慢速移动的阵风斑块调制粗糙度。岸沫受水深约束，沿世界空间的流动斑块聚散，岸段之间留有空隙。短峰与毛细纹理作用于反光和折射，船只与浮标继续使用原几何涌浪的高度。

全景和近景均与 v35.1 冻结在同一机位、波相和船况下对照。折射取决于屏幕内可采样的深度与颜色。

## 实机验证

- 可见窗口原生 1920×1080。274 项运行检查和 30 项原生键鼠检查通过。
- 普通启动读取 19 座设施和 1 个已建展缸。10 个既有存档与备份文件的 SHA-256 在启动前后一致。
- 全景 [v35.2](preview.jpg) · [同机位 v35.1](00-v351-same-view.jpg)
- 近景 [v35.2](02-emerald-water-closeup.jpg) · [同机位 v35.1](03-v351-same-closeup.jpg)
- [运行检查](result.json) · [键鼠检查](native-result.json) · [普通启动](normal-startup.json)

复建使用 `Tools/SeaMistCapeV35_2/Rebuild-OrganicWater.ps1`。隔离运行检查使用 `Tools/SeaVillageV31/Run-Review.ps1 -MistCapeOrganic -Evidence village-v31-mist-organic-reviewXX`。加 `-Native` 运行原生键鼠检查。
