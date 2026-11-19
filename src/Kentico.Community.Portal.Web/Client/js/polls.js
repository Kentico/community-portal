import {
  BarController,
  BarElement,
  CategoryScale,
  LinearScale,
  Chart,
  Tooltip,
} from "chart.js";

Chart.register(BarController, BarElement, CategoryScale, LinearScale, Tooltip);

/**
 * @typedef BarChartOptions
 * @property {string} chartElemID
 * @property {Object.<string, string>} options
 * @property {string[]} [optionOrder] option keys in the form's order
 * @property {Object.<string, int>} results
 */

/**
 * One row per option: most votes first, the form's option order for ties.
 * Answers that are no longer an option of the form are kept (after the options).
 *
 * @param {BarChartOptions} opts
 * @returns {{ label: string, votes: number }[]}
 */
export function getPollRows({ options, optionOrder, results }) {
  const keys = [...new Set([...(optionOrder ?? []), ...Object.keys(options)])];
  Object.keys(results).forEach((key) => {
    if (!keys.includes(key)) {
      keys.push(key);
    }
  });

  return keys
    .map((key, index) => ({
      label: String(options[key] ?? key).trim(),
      votes: Number(results[key]) || 0,
      index,
    }))
    .sort((a, b) => b.votes - a.votes || a.index - b.index)
    .map(({ label, votes }) => ({ label, votes }));
}

/**
 * Renders poll results as a horizontal bar chart.
 * Colors, fonts and sizes come from the `--poll-chart-*` custom properties
 * of the `.poll-widget` ancestor (see 01_component/_poll.scss).
 *
 * @param {BarChartOptions} opts
 */
export function createBarChart({ chartElemID, options, optionOrder, results }) {
  const canvas = document.getElementById(chartElemID);
  if (!canvas) {
    return;
  }

  const rows = getPollRows({ options, optionOrder, results });
  const labels = rows.map((row) => row.label);
  const series = rows.map((row) => row.votes);
  const total = series.reduce((sum, votes) => sum + votes, 0);

  const theme = readTheme(canvas);
  const reducedMotion = window.matchMedia(
    "(prefers-reduced-motion: reduce)",
  ).matches;

  const chart = new Chart(canvas, {
    type: "bar",
    data: {
      labels,
      datasets: [
        {
          data: series,
          backgroundColor: labels.map(
            (_, i) => theme.barColors[i % theme.barColors.length],
          ),
          hoverBackgroundColor: labels.map(
            (_, i) => theme.barHoverColors[i % theme.barHoverColors.length],
          ),
          borderWidth: 0,
          borderRadius: theme.barRadius,
          borderSkipped: false,
          categoryPercentage: 1,
          barPercentage: theme.barRatio,
        },
      ],
    },
    options: {
      indexAxis: "y",
      responsive: true,
      maintainAspectRatio: false,
      // Size dependent tokens (container queries in _poll.scss) are read again
      // when the chart is resized; chart.js updates the chart afterwards
      onResize(resized) {
        const { labelSize, labelMinSize, labelInset, tickLimit } =
          readTheme(canvas);
        const labelOptions = resized.options.plugins.pollBarLabels;

        labelOptions.size = labelSize;
        labelOptions.minSize = labelMinSize;
        labelOptions.inset = labelInset;
        resized.options.scales.x.ticks.maxTicksLimit = tickLimit;
      },
      // The chart area spans the full canvas width, so bars, title and footer
      // share the same left/right edges
      layout: { padding: 0 },
      animation: reducedMotion ? false : { duration: 600 },
      // Hovering anywhere on a row shows its tooltip (0 vote rows have no bar)
      interaction: { mode: "index", axis: "y", intersect: false },
      font: { family: theme.fontFamily },
      plugins: {
        legend: { display: false },
        title: { display: false },
        tooltip: {
          padding: 10,
          displayColors: false,
          titleFont: { family: theme.fontFamily, size: 14 },
          bodyFont: { family: theme.fontFamily, size: 13 },
          callbacks: {
            title: ([item]) => item?.label ?? "",
            label: (item) => formatVotes(item.raw, total),
          },
        },
        pollBarLabels: {
          color: theme.labelColor,
          fontFamily: theme.fontFamily,
          size: theme.labelSize,
          minSize: theme.labelMinSize,
          inset: theme.labelInset,
        },
      },
      scales: {
        x: {
          type: "linear",
          beginAtZero: true,
          suggestedMax: 4,
          border: { display: false },
          // chart.js insets the chart area by the tick padding on both sides;
          // tick labels are aligned "inner", so no inset is needed
          afterFit(scale) {
            scale.paddingLeft = 0;
            scale.paddingRight = 0;
          },
          grid: {
            color: theme.gridColor,
            lineWidth: 1,
            drawTicks: false,
          },
          ticks: {
            // Votes are whole numbers
            precision: 0,
            maxTicksLimit: theme.tickLimit,
            padding: theme.tickPadding,
            // First/last tick labels stay inside the chart area instead of
            // insetting it by half a label
            align: "inner",
            color: theme.tickColor,
            font: { family: theme.fontFamily, size: theme.tickSize },
          },
        },
        y: {
          // Option labels are drawn inside the chart area by barLabelPlugin
          border: { display: false },
          // drawTicks: the (hidden) tick marks would still inset the bars
          grid: { display: false, drawTicks: false },
          ticks: { display: false },
        },
      },
    },
    plugins: [barLabelPlugin],
  });

  // Labels are drawn on the canvas: redraw once the web font is available
  document.fonts?.ready.then(() => {
    if (chart.ctx) {
      chart.draw();
    }
  });

  return chart;
}

/**
 * @param {number} votes
 * @param {number} total
 */
function formatVotes(votes, total) {
  const percent = total > 0 ? Math.round((votes / total) * 100) : 0;

  return `${votes} ${votes === 1 ? "vote" : "votes"} (${percent}%)`;
}

/**
 * Reads the chart theme from CSS custom properties
 * @param {HTMLElement} el
 */
function readTheme(el) {
  const styles = getComputedStyle(el);
  const prop = (name) => styles.getPropertyValue(name).trim();
  const num = (name, fallback) => {
    const value = parseFloat(prop(name));
    return Number.isFinite(value) ? value : fallback;
  };

  const barOpacity = num("--poll-chart-bar-opacity", 0.5);
  const barHoverOpacity = num("--poll-chart-bar-hover-opacity", 0.7);
  const bars = [
    ["--poll-chart-bar-1", "#7f09b7"],
    ["--poll-chart-bar-2", "#00b3fc"],
    ["--poll-chart-bar-3", "#f05a22"],
    ["--poll-chart-bar-4", "#fdb600"],
  ];

  return {
    fontFamily: styles.fontFamily || "sans-serif",
    barColors: bars.map(([name, fallback]) =>
      toRgba(prop(name), fallback, barOpacity),
    ),
    barHoverColors: bars.map(([name, fallback]) =>
      toRgba(prop(name), fallback, barHoverOpacity),
    ),
    barRatio: num("--poll-chart-bar-ratio", 0.85),
    barRadius: num("--poll-chart-bar-radius", 0),
    gridColor: toRgba(
      prop("--poll-chart-grid-color"),
      "rgba(35, 31, 32, 0.12)",
    ),
    tickColor: toRgba(prop("--poll-chart-tick-color"), "#908e8f"),
    tickSize: num("--poll-chart-tick-size", 12),
    tickPadding: num("--poll-chart-tick-padding", 13),
    tickLimit: num("--poll-chart-tick-limit", 5),
    labelColor: toRgba(prop("--poll-chart-label-color"), "#231f20"),
    labelSize: num("--poll-chart-label-size", 18),
    labelMinSize: num("--poll-chart-label-min-size", 13),
    labelInset: num("--poll-chart-label-inset", 21),
  };
}

/** @type {CanvasRenderingContext2D | null | undefined} */
let colorProbe;

/**
 * Resolves any CSS color (hex, rgb(), color-mix() ...) to an rgba() string
 * chart.js can parse, optionally scaling its alpha.
 *
 * @param {string} color
 * @param {string} fallback used when the color is empty or unsupported
 * @param {number} [opacity]
 */
function toRgba(color, fallback, opacity = 1) {
  if (colorProbe === undefined) {
    const probe = document.createElement("canvas");
    probe.width = probe.height = 1;
    colorProbe = probe.getContext("2d", { willReadFrequently: true });
  }
  if (!colorProbe) {
    return fallback;
  }

  colorProbe.clearRect(0, 0, 1, 1);
  // An invalid value is ignored, leaving the fallback in place
  colorProbe.fillStyle = fallback;
  if (color) {
    colorProbe.fillStyle = color;
  }
  colorProbe.fillRect(0, 0, 1, 1);

  const [r, g, b, a] = colorProbe.getImageData(0, 0, 1, 1).data;
  const alpha = Math.round((a / 255) * opacity * 1000) / 1000;

  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}

/**
 * Greedy word wrap
 * @param {CanvasRenderingContext2D} ctx
 * @param {string} text
 * @param {number} maxWidth
 */
function wrapText(ctx, text, maxWidth) {
  const lines = [];
  let line = "";

  for (const word of String(text).split(/\s+/).filter(Boolean)) {
    const candidate = line ? `${line} ${word}` : word;

    if (line && ctx.measureText(candidate).width > maxWidth) {
      lines.push(line);
      line = word;
    } else {
      line = candidate;
    }
  }
  if (line) {
    lines.push(line);
  }

  return lines;
}

/**
 * Trims a line until it fits with an ellipsis
 * @param {CanvasRenderingContext2D} ctx
 * @param {string} line
 * @param {number} maxWidth
 */
function ellipsize(ctx, line, maxWidth) {
  let text = line;

  while (text.length > 1 && ctx.measureText(`${text}…`).width > maxWidth) {
    text = text.slice(0, -1).trimEnd();
  }

  return `${text}…`;
}

/**
 * Draws each option label inside the chart area, left-aligned and vertically
 * centered on its row. Labels may extend past the end of the bar; when the
 * chart is narrow they wrap, then shrink, then truncate.
 */
const barLabelPlugin = {
  id: "pollBarLabels",
  afterDatasetsDraw(chart, _args, opts) {
    const { ctx, chartArea, data } = chart;
    const meta = chart.getDatasetMeta(0);
    if (!chartArea || meta.hidden) {
      return;
    }

    const inset = opts.inset ?? 21;
    const maxSize = opts.size ?? 17;
    const minSize = Math.min(opts.minSize ?? 13, maxSize);
    const fontFamily = opts.fontFamily || "sans-serif";
    const x = chartArea.left + inset;
    const maxWidth = Math.max(chartArea.right - x - inset / 2, 40);

    ctx.save();
    ctx.textAlign = "left";
    ctx.textBaseline = "middle";
    ctx.fillStyle = opts.color || "#231f20";

    meta.data.forEach((bar, index) => {
      const label = data.labels?.[index];
      if (!bar || !label) {
        return;
      }

      const rowHeight = Math.abs(bar.height) || maxSize * 1.2;
      let size = maxSize;
      let lineHeight = size * 1.2;
      let lines = [];

      // Wrap first, then shrink the font until the lines fit the row
      for (; size >= minSize; size -= 1) {
        ctx.font = `${size}px ${fontFamily}`;
        lineHeight = size * 1.2;
        lines = wrapText(ctx, label, maxWidth);

        if (lines.length * lineHeight <= rowHeight) {
          break;
        }
      }

      const maxLines = Math.max(1, Math.floor(rowHeight / lineHeight));
      if (lines.length > maxLines) {
        lines = lines.slice(0, maxLines);
        lines[maxLines - 1] = ellipsize(ctx, lines[maxLines - 1], maxWidth);
      }

      const top = bar.y - ((lines.length - 1) * lineHeight) / 2;
      lines.forEach((line, i) => ctx.fillText(line, x, top + i * lineHeight));
    });

    ctx.restore();
  },
};
