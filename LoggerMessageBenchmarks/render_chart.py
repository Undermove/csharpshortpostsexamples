#!/usr/bin/env python3
"""Render the LoggerMessage benchmark visualization for the post."""
import matplotlib.pyplot as plt
import matplotlib.patheffects as pe

LABELS = ['$"..."', '"{X}", args', 'LoggerMessage.Define', '[LoggerMessage]\nsourcegen']
SHORT  = ['interpolated', 'templated', 'Define', 'sourcegen']
ALLOC_DISABLED = [128, 96, 0, 0]
TIME_DISABLED  = [29.3, 15.7, 0.49, 0.0]
ALLOC_ENABLED  = [128, 224, 128, 128]
TIME_ENABLED   = [29.0, 77.9, 39.6, 39.4]

BG     = '#0F1115'
FG     = '#E8ECF1'
MUTED  = '#6E7785'
ACCENT = '#FF6B5A'
COOL   = '#3FB6CF'
GOOD   = '#5BD0A0'

def style_axes(ax):
    ax.set_facecolor(BG)
    for s in ('top', 'right'):
        ax.spines[s].set_visible(False)
    for s in ('bottom', 'left'):
        ax.spines[s].set_color(MUTED)
    ax.tick_params(colors=FG, labelsize=11)
    ax.grid(axis='y', color='#22272E', linewidth=0.7, zorder=0)
    ax.set_axisbelow(True)

def bar_colors(values):
    return [ACCENT if v > 0 else GOOD for v in values]

def annotate_bars(ax, bars, values, suffix, color_zero=GOOD):
    for bar, v in zip(bars, values):
        h = bar.get_height()
        x = bar.get_x() + bar.get_width() / 2
        if v == 0:
            ax.text(x, max(ax.get_ylim()) * 0.04, '0', ha='center', va='bottom',
                    color=color_zero, fontsize=18, fontweight='bold',
                    path_effects=[pe.withStroke(linewidth=3, foreground=BG)])
        else:
            ax.text(x, h, f'{v}{suffix}', ha='center', va='bottom',
                    color=FG, fontsize=12, fontweight='bold')

fig, axes = plt.subplots(1, 2, figsize=(13, 7.2))
fig.patch.set_facecolor(BG)
fig.suptitle('LoggerMessage в .NET 10 — что происходит при ВЫКЛЮЧЕННОМ Information',
             color=FG, fontsize=18, fontweight='bold', y=0.97)
fig.text(0.5, 0.92,
         'один вызов лога, цифры на M4 / .NET 10 / BenchmarkDotNet',
         color=MUTED, fontsize=12, ha='center', style='italic')

# Allocations
ax1 = axes[0]
bars1 = ax1.bar(SHORT, ALLOC_DISABLED, color=bar_colors(ALLOC_DISABLED),
                edgecolor='none', zorder=2, width=0.62)
ax1.set_title('Аллокации, байт / вызов', color=FG, fontsize=14, pad=14, fontweight='bold')
ax1.set_ylabel('B / call', color=MUTED, fontsize=11)
ax1.set_ylim(0, max(ALLOC_DISABLED) * 1.25)
style_axes(ax1)
annotate_bars(ax1, bars1, ALLOC_DISABLED, ' B')

# Time
ax2 = axes[1]
bars2 = ax2.bar(SHORT, TIME_DISABLED, color=bar_colors(TIME_DISABLED),
                edgecolor='none', zorder=2, width=0.62)
ax2.set_title('Время, наносекунд / вызов', color=FG, fontsize=14, pad=14, fontweight='bold')
ax2.set_ylabel('ns / call', color=MUTED, fontsize=11)
ax2.set_ylim(0, max(TIME_DISABLED) * 1.25)
style_axes(ax2)
annotate_bars(ax2, bars2, TIME_DISABLED, ' ns')

# Highlight callout
fig.text(0.5, 0.04,
         '128 B  →  0 B    |    29 ns  →  ~0 ns       sourcegen полностью выкидывает работу при выключенном уровне',
         color=GOOD, fontsize=14, fontweight='bold', ha='center',
         path_effects=[pe.withStroke(linewidth=2, foreground=BG)])

plt.subplots_adjust(left=0.07, right=0.98, top=0.85, bottom=0.13, wspace=0.22)

out = '/Users/dmitryafonchenko/repos/csharpshortpostsexamples/LoggerMessageBenchmarks/post2_chart_disabled.png'
plt.savefig(out, dpi=160, facecolor=BG)
print(f'wrote {out}')
