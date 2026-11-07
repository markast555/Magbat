"""Строит графики для презентации по CSV из папки Results.

Запуск из корня решения: python Analysis/make_charts.py Results Analysis/charts
"""
import glob
import sys

import matplotlib.pyplot as plt
import pandas as pd

KEY = ["server", "games", "think_ms", "delay_ms"]
MODELS = ["event", "tick10_delta", "tick30_delta", "tick60_delta"]
LABELS = {
    "event": "Событийная",
    "tick10_delta": "Цикл 10 Гц",
    "tick30_delta": "Цикл 30 Гц",
    "tick60_delta": "Цикл 60 Гц",
}
COLORS = {
    "event": "#E4572E",
    "tick10_delta": "#9ECAE1",
    "tick30_delta": "#4292C6",
    "tick60_delta": "#08519C",
}
HALF_PERIOD_MS = {"event": 0, "tick10_delta": 50.0, "tick30_delta": 1000 / 30 / 2, "tick60_delta": 1000 / 60 / 2}

plt.rcParams.update({
    "font.family": "DejaVu Sans",
    "font.size": 12,
    "axes.spines.top": False,
    "axes.spines.right": False,
    "axes.grid": True,
    "grid.alpha": 0.25,
    "axes.axisbelow": True,
})


def load(folder):
    frames = [pd.read_csv(p, sep=";", decimal=",", encoding="utf-8-sig")
              for p in sorted(glob.glob(f"{folder}/results_*.csv"))]
    df = pd.concat(frames, ignore_index=True)
    df = df.sort_values("started_at").drop_duplicates(KEY + ["repeat"], keep="last")
    df["srv_cpu_per_cmd_ms"] = df["server_cpu_pct"] / 100 * 1000 / df["cmds_per_s"]
    df["proc_cpu_per_cmd_ms"] = df["process_cpu_pct"] / 100 * 1000 / df["cmds_per_s"]
    df["idle_share"] = df["idle_ticks"] / df["ticks"] * 100
    return df


def stats(df, games, metric):
    """Среднее и стандартное отклонение по повторам для каждой модели."""
    sub = df[(df.games == games) & (df.think_ms == 500) & (df.delay_ms == 0)]
    g = sub.groupby("server")[metric].agg(["mean", "std"])
    return g.reindex(MODELS)


def chart_latency(df, out):
    s = stats(df, 100, "lat_mean_ms")
    fig, ax = plt.subplots(figsize=(10, 5.6))
    x = range(len(MODELS))
    bars = ax.bar(x, s["mean"], yerr=s["std"], capsize=4,
                  color=[COLORS[m] for m in MODELS], width=0.62, zorder=3)
    ax.set_yscale("log")
    ax.set_ylim(0.03, 400)
    ax.set_yticks([0.1, 1, 10, 100])
    ax.set_yticklabels(["0,1", "1", "10", "100"])
    for i, m in enumerate(MODELS):
        value = s.loc[m, "mean"]
        ax.text(i, value * 1.35, f"{value:.2f} мс" if value < 1 else f"{value:.0f} мс",
                ha="center", va="bottom", fontweight="bold")
        if HALF_PERIOD_MS[m] > 0:
            ax.plot(i, HALF_PERIOD_MS[m], marker="D", color="black", markersize=7, zorder=4,
                    label="Половина периода тика" if i == 1 else None)
    ax.set_xticks(list(x))
    ax.set_xticklabels([LABELS[m] for m in MODELS])
    ax.set_ylabel("Средний отклик, мс (логарифмическая шкала)")
    ax.set_title("Отклик на команду: 100 партий, пауза бота 0,5 с", loc="left", fontweight="bold")
    ax.legend(loc="upper right", frameon=False)
    fig.text(0.01, 0.01, "Столбцы: среднее по 5 повторам, усы: стандартное отклонение", fontsize=9, color="#555")
    fig.tight_layout(rect=(0, 0.03, 1, 1))
    fig.savefig(f"{out}/1_latency.png", dpi=200)
    plt.close(fig)


def chart_cpu(df, out):
    games_list = [100, 300, 1000]
    fig, axes = plt.subplots(1, 2, figsize=(12, 5.6), sharey=False)
    panels = [("srv_cpu_per_cmd_ms", "Поток сервера (учёт Windows)"),
              ("proc_cpu_per_cmd_ms", "Весь процесс (сервер, каналы, клиенты)")]
    width = 0.2
    for ax, (metric, title) in zip(axes, panels):
        for j, m in enumerate(MODELS):
            means, errs = [], []
            for games in games_list:
                s = stats(df, games, metric).loc[m]
                means.append(s["mean"])
                errs.append(s["std"])
            positions = [i + (j - 1.5) * width for i in range(len(games_list))]
            lower = [min(e, v) for v, e in zip(means, errs)]  # усы не уходят ниже нуля
            ax.bar(positions, means, width=width, yerr=[lower, errs], capsize=3,
                   color=COLORS[m], label=LABELS[m], zorder=3)
        ax.set_xticks(range(len(games_list)))
        ax.set_xticklabels([f"{g} партий" for g in games_list])
        ax.set_title(title, loc="left", fontweight="bold", fontsize=12)
        ax.set_ylabel("Процессорное время на команду, мс")
        ax.set_ylim(bottom=0)
    handles, labels = axes[0].get_legend_handles_labels()
    fig.legend(handles, labels, loc="lower center", ncol=4, frameon=False, bbox_to_anchor=(0.5, 0.045))
    fig.suptitle("Нагрузка на процессор на одну команду (пауза бота 0,5 с)", x=0.01, ha="left",
                 fontweight="bold", fontsize=14)
    fig.text(0.01, 0.01, "Столбцы: среднее по 5 повторам, усы: стандартное отклонение. "
                         "Учёт процессорного времени потока у Windows грубый", fontsize=9, color="#555")
    fig.tight_layout(rect=(0, 0.1, 1, 0.94))
    fig.savefig(f"{out}/2_cpu_per_command.png", dpi=200)
    plt.close(fig)


def chart_idle(df, out):
    games_list = [1, 10, 100, 300, 1000]
    fig, ax = plt.subplots(figsize=(10, 5.6))
    for m in ["tick10_delta", "tick30_delta", "tick60_delta"]:
        means, xs = [], []
        for games in games_list:
            sub = df[(df.server == m) & (df.games == games) & (df.think_ms == 500) & (df.delay_ms == 0)]
            if len(sub):
                xs.append(games)
                means.append(sub["idle_share"].mean())
        ax.plot(xs, means, marker="o", linewidth=2.5, color=COLORS[m], label=LABELS[m])
    ax.set_xscale("log")
    ax.set_xticks(games_list)
    ax.set_xticklabels([str(g) for g in games_list])
    ax.set_ylim(0, 100)
    ax.set_xlabel("Число одновременных партий")
    ax.set_ylabel("Пустые тики, % от всех")
    ax.set_title("Холостые итерации цикла: пауза бота 0,5 с", loc="left", fontweight="bold")
    ax.legend(frameon=False)
    fig.text(0.01, 0.01, "Среднее по 5 повторам", fontsize=9, color="#555")
    fig.tight_layout(rect=(0, 0.03, 1, 1))
    fig.savefig(f"{out}/3_idle_ticks.png", dpi=200)
    plt.close(fig)


def write_summary(df, out):
    sub = df[df.server.isin(MODELS)]
    g = sub.groupby(KEY).agg(
        runs=("repeat", "count"),
        lat_mean_ms=("lat_mean_ms", "mean"), lat_sd=("lat_mean_ms", "std"),
        lat_p95_ms=("lat_p95_ms", "mean"),
        srv_cpu_per_cmd_ms=("srv_cpu_per_cmd_ms", "mean"),
        proc_cpu_per_cmd_ms=("proc_cpu_per_cmd_ms", "mean"),
        idle_share=("idle_share", "mean"),
    ).round(4)
    g.to_csv(f"{out}/summary.csv", sep=";", decimal=",", encoding="utf-8-sig")


def main(folder, out):
    import os
    os.makedirs(out, exist_ok=True)
    df = load(folder)
    chart_latency(df, out)
    chart_cpu(df, out)
    chart_idle(df, out)
    write_summary(df, out)
    print(f"Готово: {out}")


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "Results",
         sys.argv[2] if len(sys.argv) > 2 else "Analysis/charts")