import React, { useEffect, useRef } from "react";
import { Chart } from "chart.js/auto";

interface TimelineChartProps {
  labels: string[];
  values: number[];
  seriesName: string;
}

// draws one device's timeline, the points arrive from the API already in time order
// same pattern as TelemetryChart: create the chart once, then update it in place (chartjs.org, 2026)
export const TimelineChart: React.FC<TimelineChartProps> = ({ labels, values, seriesName }) => {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const chartRef = useRef<Chart | null>(null);

  useEffect(() => {
    if (!canvasRef.current) {
      return;
    }

    chartRef.current = new Chart(canvasRef.current, {
      type: "line",
      data: {
        labels: [],
        datasets: [
          {
            label: seriesName,
            data: [],
            borderColor: "#1fd1b2",
            borderWidth: 2,
            pointRadius: 2,
            tension: 0.3,
            fill: false,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false } },
        scales: {
          x: {
            grid: { color: "rgba(255,255,255,0.05)" },
            ticks: { color: "rgba(255,255,255,0.4)", font: { family: "monospace", size: 10 }, maxTicksLimit: 8 },
          },
          y: {
            grid: { color: "rgba(255,255,255,0.06)" },
            ticks: { color: "rgba(255,255,255,0.4)", font: { family: "monospace", size: 10 } },
          },
        },
      },
    });

    return () => {
      chartRef.current?.destroy();
    };
  }, []);

  useEffect(() => {
    const chart = chartRef.current;

    if (!chart) {
      return;
    }

    chart.data.labels = labels;
    chart.data.datasets[0].data = values;
    chart.data.datasets[0].label = seriesName;
    chart.update("none");
  }, [labels, values, seriesName]);

  return <canvas ref={canvasRef}></canvas>;
};

export default TimelineChart;

/*References
  chartjs.org. 2026. Integration | Chart.js. (Version 10) [Source code] Available at: < https://www.chartjs.org/docs/latest/getting-started/integration.html > [Accessed 12 September 2026].
  */