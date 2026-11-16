# 归潮湾 v40 · 船舱、岸礁与陆地造景

Windows 程序位于 `Builds/SeaAtmosphereV37/Guichao-SeaAtmosphereV37.exe`。桌面入口为「深海灯塔—最新版」。

- 船舱遮罩跟随船身的浮动与倾斜，水面在船底逐渐下压。尾流加入横向涡流和泡沫团簇，近岸仍使用独立的岸浪泡沫。中午斜阳碎光略微扩宽，船首原有卷缆绳旁新增黄铜系缆件。
- 调整 20 个岸岩渲染器，包括红枫下的精雕石。用多面砂岩替换潮池浅石和灯柱台基，潮池碰撞网格与采集贝壳跟随新表面。
- 沿地形铺设 520 簇草、140 簇野花及三段石砌矮墙。三组几何分别合并成一个网格。
- 栈桥下的灰色水下剪影仍可见。当前场景中没有找到旧版蝠鲼渲染器，剪影来源仍需确认。

## 1080p 实机验证

Intel Arc 130T，`village-v31-review11` 通过 288 项检查。航行和建造分别预热 60 帧，再采样 480 帧。

| 模式 | 平均帧率 | P95 帧时间 |
| --- | ---: | ---: |
| 航行 | 110.80 FPS | 10.51 ms |
| 建造 | 112.42 FPS | 10.45 ms |

[验收结果](result.json) · [光照数据](atmosphere-result.json)

## 实机截图

- [海湾全景](preview.jpg) · [v39 同机位](https://github.com/xujiale4869/guichao-bay/blob/79846e45909608cbd3f62c7e8cf26ac7e71a565f/Docs/SeaAtmosphereV37/preview.jpg)
- [岸边近景](02-wet-shore-and-godrays.jpg) · [v39 同机位](https://github.com/xujiale4869/guichao-bay/blob/79846e45909608cbd3f62c7e8cf26ac7e71a565f/Docs/SeaAtmosphereV37/02-wet-shore-and-godrays.jpg)
- [渔村与陆地草坡](04-inland-village.jpg)
- [同机位旧版光照全景](00-v361-same-view.jpg) · [同机位旧版光照岸边](03-v361-same-shore.jpg)
