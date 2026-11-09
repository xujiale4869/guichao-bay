# 归潮湾 v36.1 · 阳光海港配乐

Windows 程序位于 `Builds/SeaSunnyAudioV36_1/Guichao-SeaSunnyAudioV36_1.exe`，桌面入口为「深海灯塔—最新版」。

水面沿用 v34 原版港湾渲染，实际使用 `Deepsea/SeaSurfaceSparkleHarborV27`。玩法与存档基于 v34，航海界面采用 v30。

- 白昼航行采用尼龙吉他和马林巴，靠岸后淡入手风琴和尤克里里音色。
- 黄昏与静夜使用木吉他和拇指琴。四条分轨以同一段 19.2 秒 G 大调和声循环同步播放。
- 实际提竿后叠加马林巴并降低底乐音量。留鱼、打捞、收蟹和悬赏交付触发短木琴与水珠音。海浪环境声独立于配乐。
- 音源由合成生成，四条分轨与收获音效均为 44.1 kHz 双声道 PCM。资源缺失时启用内置合成。

## 实机验证

1920×1080 可见窗口通过 269 项运行检查、30 项原生键鼠检查和 18 项合成回退检查。

普通启动载入 19 座设施与 1 个展缸。启动前后 10 个存档及备份文件的校验和一致。

- [实机水面全景](preview.jpg)
- [声轨与收获检查](audio-result.json) · [运行检查](result.json) · [键鼠检查](native-result.json)
- [合成回退检查](fallback-result.json) · [普通启动](normal-startup.json)

使用 `python Tools/SeaAudioV36/generate_sunny_harbor_bgm_v36_1.py` 生成 WAV，使用 `Tools/SeaAudioV36/Rebuild-SunnyAudio.ps1` 重建。运行 `Tools/SeaVillageV31/Run-Review.ps1 -SunnyAudio -Evidence village-v31-sunny-audio-reviewXX` 复查。
