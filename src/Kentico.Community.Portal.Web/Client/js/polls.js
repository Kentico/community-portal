import {
  BarController,
  BarElement,
  CategoryScale,
  LinearScale,
  Chart,
  Tooltip,
} from "chart.js";

/**
 * @typedef BarChartOptions
 * @property {string} chartElemID
 * @property {Object.<string, string>} options
 * @property {Object.<string, int>} results
 */

/**
 *
 * @param {BarChartOptions} opts
 */
export function createBarChart({ chartElemID, options, results }) {
  Object.keys(options).forEach((key) => {
    if (!(key in results)) {
      results[key] = 0;
    }
  });
  const sortedKeys = Object.keys(results).sort(
    (a, b) => results[b] - results[a],
  );
  const labels = sortedKeys.map((k) => options[k]);
  const series = sortedKeys.map((k) => results[k] ?? 0);

  Chart.register(
    BarController,
    BarElement,
    CategoryScale,
    LinearScale,
    Tooltip,
    barLabelPlugin,
  );

  const bgColors = generatePastelColors(labels.length, 0.8);
  const borderColors = generatePastelColors(labels.length, 1.0);

  const datasets = labels.map((label, i) => ({
    label,
    barPercentage: 0.9,
    categoryPercentage: 1,
    data: [series[i]],
    backgroundColor: bgColors[i],
    borderColor: borderColors[i],
    borderWidth: 2,
  }));

  new Chart(chartElemID, {
    type: "bar",
    data: {
      labels: [""],
      datasets,
    },
    options: {
      maintainAspectRatio: false,
      responsive: true,
      plugins: {
        tooltip: {
          padding: 10,
          callbacks: {
            title: ([context]) => labels[context.datasetIndex],
            beforeLabel: () => "",
            label: (item) => ` ${item.raw}`,
          },
        },
      },
      indexAxis: "y",
      scales: {
        x: {
          grace: "1%",
          type: "linear",
          beginAtZero: true,
          ticks: {
            stepSize: 1,
            font: {
              size: 16,
            },
          },
        },
        y: {
          ticks: {
            font: {
              size: 14,
            },
          },
        },
      },
    },
  });

  function generatePastelColors(length, opacity) {
    const colors = [];

    for (let i = 0; i < length; i++) {
      // Cycle through primary colors: Red → Green → Blue
      const hue = (i * 30) % 360;

      // Convert HSL to RGB with pastel lightness
      const saturation = 70; // 70% saturation for vibrancy
      const lightness = 80; // 80% lightness for pastel effect

      // Convert HSL to RGB
      const rgb = hslToRgb(hue, saturation, lightness);
      colors.push(`rgba(${rgb.r}, ${rgb.g}, ${rgb.b}, ${opacity})`);
    }

    return colors;
  }

  // Helper function: Convert HSL to RGB
  function hslToRgb(h, s, l) {
    s /= 100;
    l /= 100;
    let c = (1 - Math.abs(2 * l - 1)) * s,
      x = c * (1 - Math.abs(((h / 60) % 2) - 1)),
      m = l - c / 2,
      r = 0,
      g = 0,
      b = 0;

    if (h < 60) {
      r = c;
      g = x;
      b = 0;
    } else if (h < 120) {
      r = x;
      g = c;
      b = 0;
    } else if (h < 180) {
      r = 0;
      g = c;
      b = x;
    } else if (h < 240) {
      r = 0;
      g = x;
      b = c;
    } else if (h < 300) {
      r = x;
      g = 0;
      b = c;
    } else {
      r = c;
      g = 0;
      b = x;
    }

    return {
      r: Math.round((r + m) * 255),
      g: Math.round((g + m) * 255),
      b: Math.round((b + m) * 255),
    };
  }
}

const barLabelPlugin = {
  id: "barLabelPlugin",
  afterDatasetsDraw(chart, args, options) {
    const { ctx, data } = chart;

    ctx.textAlign = "left";
    ctx.textBaseline = "bottom";
    ctx.font = options.font || "bold 14px Arial";
    ctx.fillStyle = options.color || "#333";

    data.datasets.forEach((dataset, datasetIndex) => {
      const meta = chart.getDatasetMeta(datasetIndex);

      meta.data.forEach((bar, index) => {
        if (!bar || meta.hidden) {
          return;
        }

        ctx.fillText(dataset.label, 20, bar.y + 5);
      });
    });
  },
};
