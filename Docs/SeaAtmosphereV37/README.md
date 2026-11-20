# 归潮湾 v44 · 丝绒草浪与绯红花毯

V44 候选版的 Windows 可执行文件另存为 Builds/SeaAtmosphereV37-v44-candidate/Guichao-SeaAtmosphereV37.exe。桌面「深海灯塔—最新版」继续启动已发布的 v43。

- 草甸改用 14 层共享地形采样的 Shell Texturing。双波段风场推动草浪，程序化草丝随绒面弯曲。近景使用全部 14 层，随屏幕投影缩小逐级减少远景壳层。
- 陆地底色改为深翠森绿和深黛林地。绯红花带分布在中高坡，沙滩保留暖沙过渡。
- 高坡红枫恢复约三分之二的叶卡，保留叶卡绕自身中心放大 18%。草地按实际木阶面留出带缓冲的通道。

## 1080p Windows 验收

Intel(R) Arc(TM) 130T GPU (16GB)。动态航行预热 60 帧，采样 480 帧，行驶 27.3 米并转向 44.8°。帧率对应本次指定路线和机位。

| 场景 | 平均帧率 | P95 帧时间 |
| --- | ---: | ---: |
| 动态航行 | 82.35 FPS | 19.24 ms |
| 航行机位短测 | 76.59 FPS | 17.87 ms |
| 建造机位短测 | 77.49 FPS | 18.48 ms |

315 项原有检查通过，新增的 90 FPS 动态航行指标未通过。同次 Windows v43 对照动态航行为 82.41 FPS。本机使用 Rebuild-Atmosphere.ps1 -DisableBurst 构建，构建报告记录 26 项警告。

[验收报告](result.json) · [动态航行数据](sailing-v44.json) · [光照数据](atmosphere-result.json) · [v43 验收记录](https://github.com/xujiale4869/guichao-bay/tree/5a5d4d73ebcb5b42bfef04af771b3f4908b73e0b/Docs/SeaAtmosphereV37)

## 实机截图

- [丝绒草浪](17-v44-velvet-grass-wave.jpg) · [绯红花毯](18-v44-crimson-carpet.jpg) · [海岸全景](19-v44-coast-panorama.jpg)
