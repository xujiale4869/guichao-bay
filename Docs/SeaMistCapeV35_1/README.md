# 归潮湾 v35.1 · 雾岬岛水体精修

Windows 程序位于 `Builds/SeaMistCapeV35_1/Guichao-SeaMistCapeV35_1.exe`。桌面入口为「深海灯塔—最新版」。沿用 v30 航海界面与 v34 玩法、存档。

焦散沿折射后的太阳光方向投到深度缓冲重构的水下表面，经吸光后随沙床和礁体显现。三组几何涌浪与原船只浮力同步，第四组短波通过法线参与折射和反光。逐通道指数衰减保留极浅水暖沙，水下颜色随距离过渡到翡翠和深蓝。木桩周围由实际水线位置驱动碎沫，短程船尾冲流和随航路留下的泡沫共同组成尾迹。

全景及近景分别与 v35 冻结在同一机位、波相和船况下对照。折射使用屏幕内的不透明颜色与深度，遮挡和越界位置回退到原采样点。现有岸线、模型和概念画仍有差距。

## 实机验证

- 可见窗口原生 1920×1080。272 项运行检查和 30 项原生键鼠检查通过。
- 普通启动读取 19 座设施和 1 个已建展缸。10 个既有存档与备份文件的 SHA-256 在启动前后一致。
- 全景 [v35.1](preview.jpg) · [同机位 v35](00-v35-same-view.jpg)
- 近景 [v35.1](02-emerald-water-closeup.jpg) · [同机位 v35](03-v35-same-closeup.jpg)
- [运行检查](result.json) · [键鼠检查](native-result.json) · [普通启动](normal-startup.json)

复建使用 `Tools/SeaMistCapeV35_1/Rebuild-PhysicalWater.ps1`。隔离运行检查使用 `Tools/SeaVillageV31/Run-Review.ps1 -MistCapePhysical -Evidence village-v31-mist-physical-reviewXX`。加 `-Native` 运行原生键鼠检查。
