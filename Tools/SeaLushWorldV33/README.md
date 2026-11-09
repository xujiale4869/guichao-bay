# 归潮湾 v33 构建

基于 v32 的 Unity 场景。继续使用 v30 界面、v31 的正式存档路径以及 v32 建筑和木石材质。通过构建定义 `DEEPSEA_V33` 启用新生态。

```powershell
./Tools/SeaLushWorldV33/Rebuild-Lush.ps1
./Tools/SeaVillageV31/Run-Review.ps1 -Lush -Evidence village-v31-lush-review01
./Tools/SeaVillageV31/Run-Review.ps1 -Lush -Native -Evidence village-v31-lush-native01
```

生成器输出十个 FBX 和同源米制网格数据。Unity 核验 FBX 可导入，使用同源数据烘焙带有法线、材质槽、UV 和顶点色的原生网格。新版程序位于 `Builds/SeaLushV33/Guichao-SeaLushV33.exe`。
