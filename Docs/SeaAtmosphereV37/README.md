# 归潮湾 v42 · 丝绒草毯与粉白花坡

桌面入口为「深海灯塔—最新版」。Windows 构建输出位于 Builds/SeaAtmosphereV37/Guichao-SeaAtmosphereV37.exe。

- 村落所在海岬按 0.48 米网格铺设 3D 草毯，复用现有地形的草地权重，根部贴合可见地形。保留沙滩、陡坡、村屋和步道的留白。
- 草叶与粉白花瓣共享一份网格和免贴图、免阴影采样的着色路径。实机生成 8163 个铺设格、6027 朵花，共 146013 个三角面。海风从草根到草尖递增。
- 隐藏 180 个旧松树、地被合批和潮间带芦苇渲染器。红枫继续使用贴地接触阴影，镜头跟随船只时取消附加实时树影，并按位移和视角变化刷新对焦。
- v40.1 的船舱遮罩和船体三点浮力通过回归检查。原版光照对照和 v40.1 的交付记录可从仓库历史查看。

## 1080p Windows 验收

Intel Arc 130T，预热 60 帧，各采样 480 帧。动态航行段持续推进和转向，采样内行驶 19.9 米并改变航向 34.5°。

| 场景 | 平均帧率 | P95 帧时间 |
| --- | ---: | ---: |
| 动态航行 | 105.25 FPS | 10.45 ms |
| 航行机位短测 | 98.65 FPS | 11.15 ms |
| 建造机位短测 | 107.52 FPS | 10.37 ms |

上述结果来自指定路线和机位的短测。v40.1 既有记录采用航行机位采样。验收 310 项通过，运行错误及警告均为 0。构建保留 25 项警告。本机此次打包使用 Rebuild-Atmosphere.ps1 -DisableBurst，绕过 Burst 原生插件打包卡住的问题。

[验收报告](result.json) · [动态航行数据](sailing-v42.json) · [光照数据](atmosphere-result.json) · [v40.1 交付](https://github.com/xujiale4869/guichao-bay/tree/77eabb654b2474617e286e3bca78bc84919993f3/Docs/SeaAtmosphereV37)

## 实机截图

- [海岬草坡](08-v42-meadow.jpg) · [花毯近景](09-v42-meadow-detail.jpg) · [同机位旧景](11-v42-meadow-baseline.jpg)
- [动态航行](10-v42-underway.jpg) · [海湾全景](preview.jpg) · [码头和浅滩](02-wet-shore-and-godrays.jpg)
- [船舱横侧](05-hull-broadside.jpg) · [船首斜侧](06-hull-bow-quarter.jpg) · [推进尾迹](07-hull-underway.jpg)
