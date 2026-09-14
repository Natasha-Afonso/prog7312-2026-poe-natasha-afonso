import React, { useEffect, useRef } from "react";
import { Chart, type ChartData } from "chart.js/auto";

interface TelemetryChartProps {
  labels: string[];
  tempData: number[];
  pressureData: number[];
}

//(Gosavi,2026)
export const TelemetryChart: React.FC<TelemetryChartProps> = ({
  labels,
  tempData,
  pressureData,
}) => {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const chartRef = useRef<Chart | null>(null);

  // chart is created once, then updated in place 
  //  calls chart.update('none') on an
  // interval 
  useEffect(() => {
    if (!canvasRef.current) {
      return;
    }

    const data: ChartData<"line"> = {
      labels: [],
      datasets: [
        {
          label: "Temperature (°C)",
          data: [],
          borderColor: "#f59e0b",
          borderWidth: 2,
          pointRadius: 0,
          tension: 0.3,
          fill: false,
        },
        {
          label: "Pressure (bar)",
          data: [],
          borderColor: "var(--accent)",
          borderWidth: 2,
          pointRadius: 0,
          tension: 0.2,
          fill: false,
        },
      ],
    };

    ////www.chartjs.org,2026
    chartRef.current = new Chart(canvasRef.current, {
      type: "line",
      data,
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false } },
        // Substitute the appropriate scale IDs
        scales: {
          x: {
            grid: { color: "rgba(255,255,255,0.05)" },
            ticks: { color: "rgba(255,255,255,0.4)", font: { family: "monospace", size: 10 } },
          },
          y: {
            grid: { color: "rgba(255,255,255,0.06)" },
            ticks: { color: "rgba(255,255,255,0.4)", font: { family: "monospace", size: 10 } },
          },
        },
      },
    });
//www.chartjs.org,2026
    return () => {
      chartRef.current?.destroy();
    };
  }, []);
//www.chartjs.org,2026
  useEffect(() => {
    const chart = chartRef.current;

    if (!chart) {
      return;
    }

    chart.data.labels = labels;
    chart.data.datasets[0].data = tempData;
    chart.data.datasets[1].data = pressureData;
    chart.update("none");
  }, [labels, tempData, pressureData]);

  return <canvas ref={canvasRef}></canvas>;
};

export default TelemetryChart;

{/*References
  Gosavi,H.io.2026. GUI PANEL DESIGN ON The Time-Series Sensor Monitor. (Version 10) [Source code] Available at: https://codepen.io/HARSH-GOSAVI/details/NPbwGpN > [Accessed 12 September 2026].
  www.chartjs.org. 2026. Integration | Chart.js. (Version 10) [Source code] Available at: < https://www.chartjs.org/docs/latest/getting-started/integration.html > [Accessed 12 September 2026].
  */}