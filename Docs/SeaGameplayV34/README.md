# 归潮湾 v34 · 海湾玩法

Windows 程序位于 `Builds/SeaGameplayV34/Guichao-SeaGameplayV34.exe`。桌面使用「深海灯塔—最新版」。工坊沿用 v30 航海界面的字体、卡片和配色。

| 操作 | 玩法 |
| --- | --- |
| `2`，停船按 `E` | 甩钩打捞浮木、黄铜箱、海晶瓶、潮汐矿石，获得建材、金币与鱼饵 |
| `T` 与 `L` | 流转四个时段。黄昏和夜间的船灯或航标灯 8 米内会聚集稀有鱼，浮漂等待由 2 秒降到 0.8 秒 |
| 接近金色浮标按 `E` | 起笼。每次需等 45 秒，可收肥蟹、鱼饵、18 金币和 1 浮木 |
| 靠码头按 `F`，走到工坊按 `E` | 索恩升级马达和活鱼舱，玛琳以 +35% 收购鱼蟹，艾瑟尔培育珊瑚苗和发放保护基金 |

玛琳的每日悬赏需要 3 条随身鱼，其中至少 1 条稀有鱼，再加 1 只肥蟹。奖励 120 金币和 3 黄铜。每座珊瑚园提供 12 繁荣度，每日可在艾瑟尔处按繁荣度领取金币。日常领取以本机日期为准。培育消耗 4 潮汐石和 30 金币，得到 3 株极光珊瑚苗，可直接用于建造珊瑚园。

v34 首次启动导入 v33 的玩家与海湾布局，后续分别写入 `sea-life-v34.json` 和 `sea-bay-expansion-v34.json`。起笼冷却和漂流物补充时间跨重启计时。

## 实机验证

- 可见窗口原生 1920×1080。247 项运行检查、30 项键鼠检查通过。
- 普通启动读入 19 座设施、1 个已建展缸。验证前后 5 份已有普通存档的 SHA-256 一致。
- [夜景与挂钩打捞](preview.jpg) · [工坊升级](02-npc-workshops-and-salvage.jpg) · [待收蟹笼](03-pot-harvest.jpg) · [灯下鱼获](04-night-catch.jpg) · [酒馆收购](05-tavern.jpg) · [学者工坊](06-scholar.jpg)
- [运行检查](result.json) · [键鼠检查](native-result.json) · [存档导入](normal-startup.json)

复建使用 `Tools/SeaGameplayV34/Rebuild-Gameplay.ps1`。隔离检查使用 `Tools/SeaVillageV31/Run-Review.ps1 -Gameplay -Evidence village-v31-gameplay-reviewXX`。加 `-Native` 运行原生键鼠检查。
