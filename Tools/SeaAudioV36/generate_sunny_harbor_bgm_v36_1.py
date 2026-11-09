#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Render Guichao Bay's warm G-major harbor score as synchronized PCM WAV."""


import argparse
import os
import math
import wave
import struct

SAMPLE_RATE = 44100


def midi_to_freq(midi_note):
    return 440.0 * (2.0 ** ((midi_note - 69.0) / 12.0))


def apply_stereo_reverb(left, right, wet, loop_samples=0):
    """Fold instrumental tails, then add stereo reflections at the seam."""
    if loop_samples:
        for i in range(loop_samples, len(left)):
            left[i - loop_samples] += left[i]
            right[i - loop_samples] += right[i]
        del left[loop_samples:]
        del right[loop_samples:]

    out_l, out_r = left.copy(), right.copy()
    for seconds, gain in ((0.019, 0.28), (0.043, 0.20), (0.079, 0.12)):
        delay = round(seconds * SAMPLE_RATE)
        gain *= wet
        for i in range(0 if loop_samples else delay, len(left)):
            out_l[i] += right[i - delay] * gain
            out_r[i] += left[i - delay] * gain * 0.96
    return out_l, out_r


def write_stereo_wav(filepath, left, right):
    os.makedirs(os.path.dirname(filepath), exist_ok=True)
    peak = max(
        max((abs(x) for x in left), default=1e-4),
        max((abs(x) for x in right), default=1e-4),
        1e-4,
    )
    norm = 0.86 / peak
    with wave.open(filepath, "wb") as wf:
        wf.setnchannels(2)
        wf.setsampwidth(2)
        wf.setframerate(SAMPLE_RATE)
        frames = bytearray()
        for l_val, r_val in zip(left, right):
            li = max(-32767, min(32767, int(l_val * norm * 32767.0)))
            ri = max(-32767, min(32767, int(r_val * norm * 32767.0)))
            frames += struct.pack("<hh", li, ri)
        wf.writeframes(frames)
    print(f"[SeaAudioV36.1] Generated: {filepath} ({len(left)/SAMPLE_RATE:.1f}s)")


def add_nylon_guitar_note(buf_l, buf_r, start_sec, dur_sec, midi_note, amp=0.24, pan=-0.15):
    """温暖尼龙弦古典木吉他指弹合成（带指腹拨弦瞬态与木质共鸣箱温暖衰减，无金属冰冷感）"""
    start_idx = round(start_sec * SAMPLE_RATE)
    num_samples = int(dur_sec * SAMPLE_RATE)
    end_idx = min(len(buf_l), start_idx + num_samples)
    freq = midi_to_freq(midi_note)
    w0 = 2.0 * math.pi * freq / SAMPLE_RATE

    pan_l = math.cos((pan + 1.0) * math.pi * 0.25) * amp
    pan_r = math.sin((pan + 1.0) * math.pi * 0.25) * amp

    for i in range(start_idx, end_idx):
        k = i - start_idx
        t = k / float(SAMPLE_RATE)
        attack = min(1.0, t / 0.006)
        # 尼龙弦特点：高次谐波迅速衰减，基频与二次谐波温暖圆润
        d1 = math.exp(-t * 2.1)
        d2 = math.exp(-t * 4.8)
        d3 = math.exp(-t * 9.5)
        pluck_thump = math.sin(w0 * 0.5 * k) * math.exp(-t * 38.0) * 0.18
        phase = w0 * k
        sig = (
            math.sin(phase) * d1
            + 0.38 * math.sin(phase * 2.0 + 0.4) * d2
            + 0.14 * math.sin(phase * 3.0 + 1.1) * d3
            + pluck_thump
        ) * attack * min(1.0, (end_idx - i - 1) / (0.040 * SAMPLE_RATE))
        buf_l[i] += sig * pan_l
        buf_r[i] += sig * pan_r


def add_rosewood_marimba_note(buf_l, buf_r, start_sec, dur_sec, midi_note, amp=0.20, pan=0.2):
    """圆润玫瑰木马林巴木琴（基频 + 4倍频共鸣管泛音，像阳光下的水滴一样Q弹温暖）"""
    start_idx = round(start_sec * SAMPLE_RATE)
    num_samples = int(dur_sec * SAMPLE_RATE)
    end_idx = min(len(buf_l), start_idx + num_samples)
    freq = midi_to_freq(midi_note)
    w0 = 2.0 * math.pi * freq / SAMPLE_RATE

    pan_l = math.cos((pan + 1.0) * math.pi * 0.25) * amp
    pan_r = math.sin((pan + 1.0) * math.pi * 0.25) * amp

    for i in range(start_idx, end_idx):
        k = i - start_idx
        t = k / float(SAMPLE_RATE)
        attack = min(1.0, t / 0.0035)
        phase = w0 * k
        # 马林巴木琴标志性声学特征：第 1 泛音在 4.0 倍频处，迅速衰减为圆润純音
        sig = (
            math.sin(phase) * math.exp(-t * 4.2)
            + 0.28 * math.sin(phase * 4.0 + 0.2) * math.exp(-t * 18.0)
            + 0.06 * math.sin(phase * 9.2) * math.exp(-t * 45.0)
        ) * attack * min(1.0, (end_idx - i - 1) / (0.040 * SAMPLE_RATE))
        buf_l[i] += sig * pan_l
        buf_r[i] += sig * pan_r


def add_kalimba_tine_note(buf_l, buf_r, start_sec, dur_sec, midi_note, amp=0.18, pan=0.15):
    """海滩篝火拇指琴 Kalimba（温暖木盒共鸣 + 柔和圆润余音）"""
    start_idx = round(start_sec * SAMPLE_RATE)
    num_samples = int(dur_sec * SAMPLE_RATE)
    end_idx = min(len(buf_l), start_idx + num_samples)
    freq = midi_to_freq(midi_note)
    w0 = 2.0 * math.pi * freq / SAMPLE_RATE

    pan_l = math.cos((pan + 1.0) * math.pi * 0.25) * amp
    pan_r = math.sin((pan + 1.0) * math.pi * 0.25) * amp

    for i in range(start_idx, end_idx):
        k = i - start_idx
        t = k / float(SAMPLE_RATE)
        attack = min(1.0, t / 0.005)
        phase = w0 * k
        sig = (
            math.sin(phase) * math.exp(-t * 2.5)
            + 0.16 * math.sin(phase * 2.0) * math.exp(-t * 7.0)
            + 0.12 * math.sin(phase * 5.85) * math.exp(-t * 24.0)
        ) * attack * min(1.0, (end_idx - i - 1) / (0.040 * SAMPLE_RATE))
        buf_l[i] += sig * pan_l
        buf_r[i] += sig * pan_r


def add_harbor_accordion_or_whistle(buf_l, buf_r, start_sec, dur_sec, midi_note, amp=0.17, pan=0.1, is_whistle=False):
    """海港温暖手风琴/口风琴 (Warm Musette Concertina) 或阳光陶笛/口哨 (Sunny Ocarina Whistle)"""
    start_idx = round(start_sec * SAMPLE_RATE)
    num_samples = int(dur_sec * SAMPLE_RATE)
    end_idx = min(len(buf_l), start_idx + num_samples)
    freq = midi_to_freq(midi_note)

    pan_l = math.cos((pan + 1.0) * math.pi * 0.25) * amp
    pan_r = math.sin((pan + 1.0) * math.pi * 0.25) * amp

    for i in range(start_idx, end_idx):
        k = i - start_idx
        t = k / float(SAMPLE_RATE)
        env_in = 0.5 - 0.5 * math.cos(min(1.0, t / min(0.12, dur_sec * 0.3)) * math.pi)
        env_out = 0.5 - 0.5 * math.cos(min(1.0, max(0.0, (dur_sec - t) / min(0.18, dur_sec * 0.35))) * math.pi)
        env = env_in * env_out

        if is_whistle:
            # 轻快阳光的陶笛/海风口哨（纯净正弦 + 微弱二次泛音与轻柔颤音）
            vib = 1.0 + 0.0035 * math.sin(2.0 * math.pi * 5.0 * t)
            ph = 2.0 * math.pi * freq * vib * t
            sig = (math.sin(ph) + 0.15 * math.sin(ph * 2.0)) * env
        else:
            # 温暖手风琴双簧片微差拍共鸣 (Warm Reed Chorus)
            ph1 = 2.0 * math.pi * (freq - 0.65) * t
            ph2 = 2.0 * math.pi * (freq + 0.65) * t
            sig = (
                0.5 * (math.sin(ph1) + math.sin(ph2))
                + 0.28 * math.sin(ph1 * 2.0 + 0.3)
                + 0.14 * math.sin(ph1 * 3.0 + 0.7)
            ) * 0.72 * env

        buf_l[i] += sig * pan_l
        buf_r[i] += sig * pan_r


def add_acoustic_upright_bass(buf_l, buf_r, start_sec, dur_sec, midi_note, amp=0.22):
    """温暖原木拨弦低音贝斯 (Warm Fingered Acoustic Bass)"""
    start_idx = round(start_sec * SAMPLE_RATE)
    end_idx = min(len(buf_l), start_idx + int(dur_sec * SAMPLE_RATE))
    w0 = 2.0 * math.pi * midi_to_freq(midi_note) / SAMPLE_RATE
    for i in range(start_idx, end_idx):
        k = i - start_idx
        t = k / float(SAMPLE_RATE)
        env = min(1.0, t / 0.012) * math.exp(-t * 2.8)
        ph = w0 * k
        sig = (math.sin(ph) + 0.32 * math.sin(ph * 2.0) * math.exp(-t * 5.5)) * env * amp * min(1.0, (end_idx - i - 1) / (0.045 * SAMPLE_RATE))
        buf_l[i] += sig * 0.72
        buf_r[i] += sig * 0.72


def generate_sunny_harbor_stems(out_dir):
    # 4/4 阳光海岸轻快 Bossa / 原木指弹律动 (100 BPM -> 每拍 0.60s，每小节 2.40s，8小节 19.2s 无缝循环)
    beat = 0.60
    eighth = beat * 0.5  # 0.30s
    bar_sec = beat * 4.0  # 2.40s
    total_bars = 8
    total_sec = bar_sec * total_bars  # 19.2s
    total_samples = round(total_sec * SAMPLE_RATE)
    render_samples = total_samples + 2 * SAMPLE_RATE

    # 阳光明媚的 G 大调爵士民谣大七/六九和弦进行 (温暖、松弛、治愈，绝无悲伤压抑感):
    # Bar 0: Gmaj7  (G2 / D4, F#4, G4, B4)
    # Bar 1: Cmaj9  (C3 / E4, G4, B4, D5)
    # Bar 2: Am9    (A2 / E4, G4, B4, C5)
    # Bar 3: D13sus (D3 / C4, E4, G4, A4)
    # Bar 4: Bm7    (B2 / D4, F#4, A4, D5)
    # Bar 5: Em9    (E2 / D4, F#4, G4, B4)
    # Bar 6: Am7    (A2 / C4, E4, G4, C5)
    # Bar 7: D9     (D3 / C4, E4, F#4, A4)
    chords = [
        (43, [62, 66, 67, 71, 67, 66, 62, 67]),  # Bar 0: Gmaj7
        (48, [64, 67, 71, 74, 71, 67, 64, 71]),  # Bar 1: Cmaj9
        (45, [64, 67, 71, 72, 71, 67, 64, 69]),  # Bar 2: Am9
        (50, [60, 64, 67, 69, 67, 64, 62, 66]),  # Bar 3: D13sus -> D7
        (47, [62, 66, 69, 74, 69, 66, 62, 69]),  # Bar 4: Bm7
        (40, [62, 66, 67, 71, 67, 66, 64, 67]),  # Bar 5: Em9
        (45, [60, 64, 67, 72, 67, 64, 60, 67]),  # Bar 6: Am7
        (50, [60, 64, 66, 69, 66, 64, 62, 66]),  # Bar 7: D9
    ]

    # 轻快温暖的阳光海岸主旋律 (barIdx, beatOffset, durBeats, midiNote)
    sunny_melody = [
        (0, 0.5, 1.5, 74), (0, 2.0, 1.0, 71), (0, 3.0, 1.0, 67),
        (1, 0.0, 2.0, 76), (1, 2.0, 2.0, 74),
        (2, 0.5, 1.5, 72), (2, 2.0, 1.0, 71), (2, 3.0, 1.0, 69),
        (3, 0.0, 3.5, 67),
        (4, 0.5, 1.5, 74), (4, 2.0, 1.0, 76), (4, 3.0, 1.0, 79),
        (5, 0.0, 2.0, 74), (5, 2.0, 2.0, 71),
        (6, 0.5, 1.5, 72), (6, 2.0, 1.0, 69), (6, 3.0, 1.0, 67),
        (7, 0.0, 3.5, 69),
    ]

    # =====================================================================
    # Stem 0: 白昼海湾《晴空木吉他的海风》(Sunny Bay - Nylon Guitar & Marimba)
    # =====================================================================
    s0_l, s0_r = [0.0] * render_samples, [0.0] * render_samples
    for b_idx, (bass_note, arp) in enumerate(chords):
        b_start = b_idx * bar_sec
        # Bossa Nova 轻柔原木贝斯 (第 1 拍与第 3 拍附点)
        add_acoustic_upright_bass(s0_l, s0_r, b_start, beat * 2.2, bass_note, amp=0.21)
        add_acoustic_upright_bass(s0_l, s0_r, b_start + beat * 2.5, beat * 1.4, bass_note + 7, amp=0.17)
        # 尼龙弦木吉他指弹分解和弦
        for step, n_midi in enumerate(arp):
            t_note = b_start + step * eighth
            add_nylon_guitar_note(s0_l, s0_r, t_note, eighth * 2.6, n_midi, amp=0.20, pan=-0.25 + (step % 3) * 0.22)
        # 玫瑰木马林巴木琴在弱拍轻巧点缀
        add_rosewood_marimba_note(s0_l, s0_r, b_start + eighth * 2, 0.9, arp[2] + 12, amp=0.14, pan=0.3)
        add_rosewood_marimba_note(s0_l, s0_r, b_start + eighth * 5, 0.9, arp[3] + 12, amp=0.13, pan=0.25)

    for b_idx, b_off, dur_b, m_note in sunny_melody:
        t0 = b_idx * bar_sec + b_off * beat
        # 主旋律由清脆的马林巴木琴与阳光陶笛轻柔呼应
        add_rosewood_marimba_note(s0_l, s0_r, t0, dur_b * beat, m_note, amp=0.19, pan=0.15)
        if b_idx >= 4:
            add_harbor_accordion_or_whistle(s0_l, s0_r, t0, dur_b * beat * 0.95, m_note, amp=0.12, pan=-0.15, is_whistle=True)

    s0_l, s0_r = apply_stereo_reverb(s0_l, s0_r, wet=0.22, loop_samples=total_samples)
    write_stereo_wav(os.path.join(out_dir, "BGM_0_SunnyBay_GuitarMarimba.wav"), s0_l, s0_r)
    del s0_l, s0_r

    # =====================================================================
    # Stem 1: 渔村港湾与酒馆《海风手风琴与尤克里里》(Harbor Accordion & Ukulele)
    # =====================================================================
    s1_l, s1_r = [0.0] * render_samples, [0.0] * render_samples
    for b_idx, (bass_note, arp) in enumerate(chords):
        b_start = b_idx * bar_sec
        add_acoustic_upright_bass(s1_l, s1_r, b_start, beat * 1.9, bass_note, amp=0.22)
        add_acoustic_upright_bass(s1_l, s1_r, b_start + beat * 2.0, beat * 1.9, bass_note + 7, amp=0.19)
        for step, n_midi in enumerate(arp):
            t_note = b_start + step * eighth
            # 欢快高八度尤克里里指弹
            add_nylon_guitar_note(s1_l, s1_r, t_note, eighth * 1.8, n_midi + 12, amp=0.15, pan=0.25 if step % 2 == 0 else -0.25)

    for b_idx, b_off, dur_b, m_note in sunny_melody:
        t0 = b_idx * bar_sec + b_off * beat
        # 温暖惬意的渔港手风琴独奏主旋律
        add_harbor_accordion_or_whistle(s1_l, s1_r, t0, dur_b * beat, m_note, amp=0.20, pan=0.05, is_whistle=False)

    s1_l, s1_r = apply_stereo_reverb(s1_l, s1_r, wet=0.2, loop_samples=total_samples)
    write_stereo_wav(os.path.join(out_dir, "BGM_1_CozyTavern_HarborAccordion.wav"), s1_l, s1_r)
    del s1_l, s1_r

    # =====================================================================
    # Stem 2: 黄昏与篝火静夜《篝火木吉他与拇指琴夜曲》(Campfire Guitar & Kalimba)
    # =====================================================================
    s2_l, s2_r = [0.0] * render_samples, [0.0] * render_samples
    for b_idx, (bass_note, arp) in enumerate(chords):
        b_start = b_idx * bar_sec
        add_nylon_guitar_note(s2_l, s2_r, b_start, bar_sec * 0.95, bass_note, amp=0.20, pan=-0.1)
        for step in (0, 2, 4, 6):
            t_note = b_start + step * eighth
            add_nylon_guitar_note(s2_l, s2_r, t_note, eighth * 3.5, arp[step], amp=0.17, pan=-0.2)
            add_kalimba_tine_note(s2_l, s2_r, t_note + eighth, eighth * 3.2, arp[step + 1] + 12, amp=0.15, pan=0.25)

    for b_idx, b_off, dur_b, m_note in sunny_melody:
        t0 = b_idx * bar_sec + b_off * beat
        add_kalimba_tine_note(s2_l, s2_r, t0, dur_b * beat * 1.2, m_note + 12, amp=0.18, pan=0.1)

    s2_l, s2_r = apply_stereo_reverb(s2_l, s2_r, wet=0.28, loop_samples=total_samples)
    write_stereo_wav(os.path.join(out_dir, "BGM_2_StarlitCampfire_Kalimba.wav"), s2_l, s2_r)
    del s2_l, s2_r

    # =====================================================================
    # Stem 3: 钓鱼收杆轻快互动曲《欢快马林巴水花律动》(Splashy Marimba Groove)
    # =====================================================================
    s3_l, s3_r = [0.0] * render_samples, [0.0] * render_samples
    for b_idx, (bass_note, arp) in enumerate(chords):
        b_start = b_idx * bar_sec
        # 轻快跳跃的马林巴木琴与木吉他切音伴奏 (像《潜水员戴夫》钓鱼时的欢快节奏)
        for step in range(8):
            t_sub = b_start + step * eighth
            n_pick = arp[step] + (12 if step % 2 == 1 else 0)
            add_rosewood_marimba_note(s3_l, s3_r, t_sub, eighth * 1.2, n_pick, amp=0.21, pan=-0.3 if step % 2 == 0 else 0.3)

    s3_l, s3_r = apply_stereo_reverb(s3_l, s3_r, wet=0.18, loop_samples=total_samples)
    write_stereo_wav(os.path.join(out_dir, "BGM_3_LivelyCatch_MarimbaGroove.wav"), s3_l, s3_r)
    del s3_l, s3_r

    # =====================================================================
    # SFX 0: 收获/打捞/收蟹时的【温暖马林巴木琴琶音 + 清脆水花入篓声】
    # =====================================================================
    sfx_samples = int(2.2 * SAMPLE_RATE)
    fx_l, fx_r = [0.0] * sfx_samples, [0.0] * sfx_samples
    # Gmaj9 阳光上行木琴琶音 (G4 - B4 - D5 - F#5 - A5 - B5 - D6)
    warm_notes = [67, 71, 74]
    for idx, m_n in enumerate(warm_notes):
        add_rosewood_marimba_note(fx_l, fx_r, idx * 0.055, 1.2, m_n, amp=0.19, pan=-0.25 + idx * 0.18)
        add_nylon_guitar_note(fx_l, fx_r, idx * 0.055, 1.4, m_n - 12, amp=0.14, pan=0.15)
    # 叠加清脆自然的水珠泡泡音 (Water Bubble Bloop)
    for b_idx, f_base in enumerate([580.0, 860.0, 1180.0]):
        st = int((0.02 + b_idx * 0.07) * SAMPLE_RATE)
        dur = int(0.08 * SAMPLE_RATE)
        for k in range(dur):
            if st + k < sfx_samples:
                t = k / float(SAMPLE_RATE)
                # 水泡音频率快速上滑
                phase = 2.0 * math.pi * f_base * (t + 4.25 * t * t)
                env = math.sin(min(1.0, t / 0.08) * math.pi)
                val = math.sin(phase) * env * 0.16
                fx_l[st + k] += val
                fx_r[st + k] += val

    fx_l, fx_r = apply_stereo_reverb(fx_l, fx_r, wet=0.20)
    write_stereo_wav(os.path.join(out_dir, "SFX_WarmMarimbaSplash.wav"), fx_l, fx_r)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out-dir", default="Assets/Deepsea/Resources/SeaAudioV36")
    args = parser.parse_args()
    generate_sunny_harbor_stems(os.path.abspath(args.out_dir))


if __name__ == "__main__":
    main()
