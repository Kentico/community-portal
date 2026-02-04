import * as Am5 from '@amcharts/amcharts5';
import Am5themesAnimated from '@amcharts/amcharts5/themes/Animated';
import * as Am5XY from '@amcharts/amcharts5/xy';
import {
  Colors,
  Headline,
  HeadlineSize,
} from '@kentico/xperience-admin-components';
import React, { useLayoutEffect } from 'react';

export interface TimeSeriesEntry {
  label: string;
  value: number;
}

export const BarChart = (props: {
  data: TimeSeriesEntry[];
  chartName: string;
  id: string;
}) => {
  useLayoutEffect(() => {
    const root = Am5.Root.new(props.id);
    root.setThemes([Am5themesAnimated.new(root)]);

    const chart = root.container.children.push(
      Am5XY.XYChart.new(root, {
        panY: false,
        layout: root.verticalLayout,
        maxTooltipDistance: 0,
      }),
    );

    const yAxis = chart.yAxes.push(
      Am5XY.ValueAxis.new(root, {
        renderer: Am5XY.AxisRendererY.new(root, {}),
      }),
    );

    const xAxis = chart.xAxes.push(
      Am5XY.CategoryAxis.new(root, {
        renderer: Am5XY.AxisRendererX.new(root, {}),
        categoryField: 'label',
      }),
    );
    xAxis.data.setAll(props.data);

    const series = chart.series.push(
      Am5XY.ColumnSeries.new(root, {
        name: props.chartName,
        xAxis: xAxis,
        yAxis: yAxis,
        valueYField: 'value',
        categoryXField: 'label',
        legendLabelText: '{name}: {categoryX}',
        legendRangeLabelText: '{name}',
        legendValueText: '- {valueY}',
      }),
    );
    series.data.setAll(props.data);

    const legend = chart.children.push(Am5.Legend.new(root, {}));
    legend.data.setAll(chart.series.values);

    chart.set(
      'cursor',
      Am5XY.XYCursor.new(root, {
        behavior: 'zoomXY',
        xAxis: xAxis,
      }),
    );

    yAxis.set(
      'tooltip',
      Am5.Tooltip.new(root, {
        themeTags: ['axis'],
      }),
    );

    return () => {
      root.dispose();
    };
  }, [props.data, props.chartName]);

  return (
    <>
      <h1>
        <Headline size={HeadlineSize.L} labelColor={Colors.TextDefaultOnLight}>
          {props.chartName}
        </Headline>
      </h1>
      <div id={props.id} style={{ width: '100%', height: '500px' }}></div>
    </>
  );
};
