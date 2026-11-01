"""Проверка выбросов: сравнивает каждый прогон с медианой его конфигурации."""
import glob
import sys

import pandas as pd

# метрика: (допустимое относительное отклонение, минимальное абсолютное отклонение)
RULES = {
    "lat_mean_ms": (0.30, 1.0),
    "busy_ms_per_cmd": (0.30, 0.001),
    "bytes_per_cmd": (0.10, 5.0),
}
KEY = ["server", "games", "think_ms", "delay_ms"]
MIN_LAT_COUNT = 20  # меньше измерений отклика: лишь шум, не проверяем


def load(folder):
    frames = [pd.read_csv(p, sep=";", decimal=",", encoding="utf-8-sig")
              for p in sorted(glob.glob(f"{folder}/results_*.csv"))]
    df = pd.concat(frames, ignore_index=True)
    # при перезапуске того же повтора берём последний прогон
    df = df.sort_values("started_at").drop_duplicates(KEY + ["repeat"], keep="last")
    return df


def main(folder):
    df = load(folder)
    print(f"Прогонов: {len(df)}, конфигураций: {df.groupby(KEY).ngroups}")
    sizes = df.groupby(KEY).size()
    print("Повторов в конфигурации (мин/макс):", sizes.min(), sizes.max())

    flagged = []
    for key, group in df.groupby(KEY):
        for metric, (rel, floor) in RULES.items():
            if metric == "lat_mean_ms" and group["lat_count"].min() < MIN_LAT_COUNT:
                continue
            # у снимков байты на команду зависят от числа тиков в окне, это не выброс
            if metric == "bytes_per_cmd" and "snapshot" in key[0]:
                continue
            median = group[metric].median()
            for _, row in group.iterrows():
                dev = abs(row[metric] - median)
                if dev > floor and dev > rel * abs(median):
                    flagged.append((*key, int(row["repeat"]), metric,
                                    round(row[metric], 4), round(median, 4)))

    if not flagged:
        print("Выбросов по заданным правилам нет")
        return
    out = pd.DataFrame(flagged, columns=KEY + ["repeat", "metric", "value", "group_median"])
    print(f"\nПодозрительных значений: {len(out)}")
    print(out.to_string(index=False))


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "Results")