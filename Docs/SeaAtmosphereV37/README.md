# 归潮湾 v40.1 · 船舱水面修复

Windows 程序位于 `Builds/SeaAtmosphereV37/Guichao-SeaAtmosphereV37.exe`。桌面入口为「深海灯塔—最新版」。

- 依船模实际截面调整船舱遮罩。水面位置和相机深度重建的不透明表面位置都参与判断，近船折射也会避开船模。水面泡沫和历史尾迹共用遮罩。
- 船头、中部和船尾采样同一波场。两端的抬升按船身当前俯仰计算，横摇继续采样左右舷。
- 深水横侧、船首斜侧和推进近景里没有发现海水或泡沫覆盖舱内木板。低角度下船底外侧仍可见一条浅色细边。

## 1080p 实机验证

Intel Arc 130T，航行和建造分别预热 60 帧，再采样 480 帧。v40 使用同一台机器在本轮复测。

| 模式 | v40 平均帧率 | v40.1 平均帧率 | v40.1 P95 帧时间 |
| --- | ---: | ---: | ---: |
| 航行 | 93.14 FPS | 92.68 FPS | 11.63 ms |
| 建造 | 93.48 FPS | 93.17 FPS | 11.48 ms |

`village-v31-review12` 通过 298 项检查，含两端浮力、推进尾迹和三张近景检查。[验收结果](result.json) · [v40 同机复测](v40-control-result.json) · [光照数据](atmosphere-result.json)

## 实机截图

- [深水横侧](05-hull-broadside.jpg) · [船首斜侧](06-hull-bow-quarter.jpg) · [推进与尾迹](07-hull-underway.jpg)
- [海湾全景](preview.jpg) · [岸边近景](02-wet-shore-and-godrays.jpg)
- [旧光照全景](00-v361-same-view.jpg) · [旧光照岸边](03-v361-same-shore.jpg) · [v40 渔村草坡](04-inland-village.jpg)
