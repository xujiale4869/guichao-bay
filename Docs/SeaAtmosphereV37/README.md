# 归潮湾 v43 · 海岬地形与红枫林

桌面入口为「深海灯塔—最新版」。Windows 构建输出位于 Builds/SeaAtmosphereV37/Guichao-SeaAtmosphereV37.exe。

- 抬高灯塔海岬背后的低槽，在坡面向内陆延伸时逐步过渡到山脊。中央海湾原生高度验收和海侧地形保留。
- 沙滩、低矮沙生草、草甸和林下腐殖土共用地形权重。撤下 v42 粉白花坡，改为暖杏沙、橄榄绿和落叶赭色，并恢复暖砂岩的纹理对比。
- 实机生成沙草 150 格、草甸 3470 格和林下草 2804 格。草地共用 79260 个三角面的一份网格。山坡使用原有树干和红枫叶网格新增 16 棵红枫，四片空间分区按材质合批，新增树冠使用约三分之一的原生叶卡，并烘成一份贴地接触阴影网格。

## 1080p Windows 验收

Intel Arc 130T，预热 60 帧，各采样 480 帧。动态航行时持续推进和转向，采样内行驶 21.0 米、转向 35.9°。

| 场景 | 平均帧率 | P95 帧时间 |
| --- | ---: | ---: |
| 动态航行 | 101.10 FPS | 10.82 ms |
| 航行机位短测 | 94.74 FPS | 11.59 ms |
| 建造机位短测 | 96.17 FPS | 11.41 ms |

315 项检查通过。构建有 25 项警告。本机使用 Rebuild-Atmosphere.ps1 -DisableBurst 构建。性能数字对应本次指定路线和机位。

[验收报告](result.json) · [动态航行数据](sailing-v43.json) · [光照数据](atmosphere-result.json) · [v42 记录](https://github.com/xujiale4869/guichao-bay/tree/5b2fcf0fc0cbf07d94855f55244401269c3a0566/Docs/SeaAtmosphereV37)

## 实机截图

- [沙滩与草甸](12-v43-coast-biomes.jpg) · [海岬坡面](14-v43-headland-profile.jpg) · [红枫林近景](13-v43-upland-grove.jpg)
- [动态航行](15-v43-sailing.jpg) · [v42 同机位](08-v42-meadow.jpg) · [v43 同机位旧光照](16-v43-coast-baseline.jpg)
- [船舱横侧](05-hull-broadside.jpg) · [船首斜侧](06-hull-bow-quarter.jpg) · [推进尾迹](07-hull-underway.jpg)
