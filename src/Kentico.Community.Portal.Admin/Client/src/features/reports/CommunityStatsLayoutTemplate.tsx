import { usePageCommand } from '@kentico/xperience-admin-base';
import {
  Button,
  DateTimeRangeInput,
} from '@kentico/xperience-admin-components';
import * as Am5 from '@amcharts/amcharts5';
import Am5themesAnimated from '@amcharts/amcharts5/themes/Animated';
import * as Am5XY from '@amcharts/amcharts5/xy';
import React, { useId, useLayoutEffect, useState } from 'react';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '../../components/ui/card';
import './community-stats.css';

type CommunityStatsQuery = {
  rangeStart: Date;
  rangeEnd: Date;
  focusMetricKey?: string;
};

type DateRangeValue = {
  from: Date;
  to: Date;
};

type CommunityMetricPoint = {
  periodStart: string;
  label: string;
  value: number;
};

type CommunityMetricSummary = {
  key: string;
  label: string;
  description: string;
  rangeTotal: number;
  allTimeTotal: number;
  previousRangeTotal: number;
  rangeChangePercent?: number | null;
  latestMonthValue: number;
  previousMonthValue: number;
  changeValue: number;
  changePercent?: number | null;
};

type CommunityMetricSeries = {
  key: string;
  label: string;
  points: CommunityMetricPoint[];
};

type CommunityCompositionSlice = {
  key: string;
  label: string;
  value: number;
};

type CommunitySupplementalSignal = {
  key: string;
  label: string;
  value: string;
  context: string;
};

type CommunityMetricDetail = {
  key: string;
  label: string;
  description: string;
  rangeTotal: number;
  previousRangeTotal: number;
  changePercent?: number | null;
  averageMonthlyValue: number;
  peakMonth: CommunityMetricPoint;
  points: CommunityMetricPoint[];
  movingAveragePoints: CommunityMetricPoint[];
};

type CommunityStatsOverview = {
  newItemsInRange: number;
  trackedRecords: number;
  leadingMetricLabel: string;
  leadingMetricValue: number;
  peakMonthLabel: string;
  peakMonthTotal: number;
};

type CommunityStatsDashboard = {
  rangeStartDate: string;
  rangeEndDate: string;
  focusMetricKey: string;
  overview: CommunityStatsOverview;
  highlights: CommunityMetricSummary[];
  series: CommunityMetricSeries[];
  composition: CommunityCompositionSlice[];
  supplementalSignals: CommunitySupplementalSignal[];
  focusDetail: CommunityMetricDetail;
};

interface CommunityStatsClientProperties {
  stats: CommunityStatsDashboard;
  defaultFocusedMetricKey: string;
  title: string;
  description: string;
}

export const CommunityStatsLayoutTemplate = (
  props: CommunityStatsClientProperties,
) => {
  const [dashboard, setDashboard] = useState<CommunityStatsDashboard>(
    props.stats,
  );
  const [selectedRange, setSelectedRange] = useState<DateRangeValue>({
    from: parseDateOnlyString(props.stats.rangeStartDate),
    to: parseDateOnlyString(props.stats.rangeEndDate),
  });
  const [focusedMetricKey, setFocusedMetricKey] = useState(
    props.stats.focusMetricKey ?? props.defaultFocusedMetricKey,
  );
  const [isLoading, setIsLoading] = useState(false);
  const appliedRange = {
    from: parseDateOnlyString(dashboard.rangeStartDate),
    to: parseDateOnlyString(dashboard.rangeEndDate),
  };

  const { execute: loadData } = usePageCommand<
    CommunityStatsDashboard,
    CommunityStatsQuery
  >('LOADDATA', {
    before: () => {
      setIsLoading(true);
    },
    after: (commandResult) => {
      if (commandResult) {
        setDashboard(commandResult);
        setSelectedRange({
          from: parseDateOnlyString(commandResult.rangeStartDate),
          to: parseDateOnlyString(commandResult.rangeEndDate),
        });
        setFocusedMetricKey(commandResult.focusMetricKey);
      }

      setIsLoading(false);
    },
  });

  async function refreshDashboard(
    nextRange: DateRangeValue,
    nextFocusMetricKey: string,
  ) {
    setFocusedMetricKey(nextFocusMetricKey);
    await loadData({
      rangeStart: nextRange.from,
      rangeEnd: nextRange.to,
      focusMetricKey: nextFocusMetricKey,
    });
  }

  function handleDateRangeChange(nextRange: DateRangeValue | null) {
    if (!nextRange) {
      return;
    }

    setSelectedRange(nextRange);
  }

  async function applySelectedRange() {
    await refreshDashboard(selectedRange, focusedMetricKey);
  }

  const hasPendingRangeChange = !areRangesEqual(selectedRange, appliedRange);

  const activeSummary =
    dashboard.highlights.find((item) => item.key === focusedMetricKey) ??
    dashboard.highlights[0];
  const activeSeries =
    dashboard.series.find((item) => item.key === focusedMetricKey) ??
    dashboard.series[0];
  const activeFocusDetail = buildClientFocusDetail(activeSummary, activeSeries);
  const focusDrilldown = buildFocusDrilldown(dashboard, activeFocusDetail);

  return (
    <div className="community-stats-dashboard space-y-6 pb-8">
      <section className="community-stats-hero grid gap-6 xl:grid-cols-[minmax(0,2fr)_minmax(320px,1fr)]">
        <Card className="community-stats-surface community-stats-hero-panel border-0 shadow-lg">
          <CardHeader className="space-y-4">
            <div className="space-y-2">
              <CardTitle className="text-4xl font-semibold tracking-tight">
                {props.title}
              </CardTitle>
              <CardDescription className="max-w-2xl text-base text-[var(--color-text-low-emphasis)]">
                {props.description}
              </CardDescription>
            </div>

            <div className="grid gap-4 xl:grid-cols-[minmax(250px,0.9fr)_minmax(0,1.6fr)] xl:items-start">
              <div className="grid gap-4">
                <OverviewStatCard
                  label="Tracked activity"
                  value={dashboard.overview.newItemsInRange}
                  caption={getRangeCaption(dashboard)}
                />
                <OverviewStatCard
                  label="Leading signal"
                  value={dashboard.overview.leadingMetricValue}
                  caption={`${dashboard.overview.leadingMetricLabel} lead this window`}
                />
                <OverviewStatCard
                  label="Peak month"
                  value={dashboard.overview.peakMonthTotal}
                  caption={dashboard.overview.peakMonthLabel}
                />
              </div>

              <div className="rounded-[var(--radius-l)] border border-dashed border-[var(--color-border-default)] bg-[var(--color-background-highlighted)] p-4">
                <p className="text-sm font-medium text-[var(--color-text-default-on-light)]">
                  Additional signals
                </p>
                <div className="mt-3 grid gap-2">
                  {dashboard.supplementalSignals.map((item) => (
                    <div
                      key={item.key}
                      className="rounded-[var(--radius-m)] border border-[var(--color-border-low-emphasis)] bg-[var(--color-paper-background)] px-3 py-3"
                    >
                      <div className="flex items-baseline justify-between gap-3">
                        <span className="text-sm text-[var(--color-text-low-emphasis)]">
                          {item.label}
                        </span>
                        <span className="text-base font-semibold text-[var(--color-text-default-on-light)]">
                          {item.value}
                        </span>
                      </div>
                      <p className="mt-2 text-xs leading-5 text-[var(--color-text-hint)]">
                        {item.context}
                      </p>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </CardHeader>
        </Card>

        <Card className="community-stats-surface border-0 shadow-lg">
          <CardContent className="space-y-4 pt-5 text-sm text-[var(--color-text-low-emphasis)]">
            <div className="rounded-[var(--radius-l)] border border-[var(--color-border-low-emphasis)] bg-[var(--color-paper-background)] p-4 shadow-[var(--shadow-xs)]">
              <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-text-hint)]">
                All-time tracked records
              </p>
              <p className="mt-3 text-3xl font-semibold text-[var(--color-text-default-on-light)]">
                {formatNumber(dashboard.overview.trackedRecords)}
              </p>
              <p className="mt-2 text-sm text-[var(--color-text-low-emphasis)]">
                Total records across members, subscribers, blog posts,
                questions, and answers.
              </p>
            </div>
            <div className="community-stats-range-picker rounded-[var(--radius-l)] border border-[var(--color-border-low-emphasis)] bg-[var(--tile-toolbar-background)] p-3 backdrop-blur">
              <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-text-hint)]">
                Visible window
              </p>
              <div className="mt-3 min-w-[280px] space-y-3">
                <DateTimeRangeInput
                  allowClear={false}
                  disabled={isLoading}
                  showTime={false}
                  value={selectedRange}
                  onChange={(value) => {
                    handleDateRangeChange(value);
                  }}
                />
                <Button
                  className="w-full"
                  disabled={!hasPendingRangeChange || isLoading}
                  inProgress={isLoading}
                  label="Update dashboard"
                  onClick={() => {
                    void applySelectedRange();
                  }}
                  type="button"
                />
              </div>
            </div>
          </CardContent>
        </Card>
      </section>

      <section className="grid gap-6 2xl:grid-cols-[minmax(0,1.9fr)_minmax(320px,1fr)]">
        <Card className="community-stats-surface border-0 shadow-lg">
          <CardHeader className="space-y-2">
            <CardDescription className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-text-hint)]">
              Multi-metric trend
            </CardDescription>
            <CardTitle className="text-2xl">
              How each signal moves month to month
            </CardTitle>
            <CardDescription className="text-sm text-[var(--color-text-low-emphasis)]">
              This compares all tracked metrics within the selected window so
              you can spot divergence instead of reading isolated totals.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <MultiMetricTrendChart series={dashboard.series} />
          </CardContent>
        </Card>

        <Card className="community-stats-surface border-0 shadow-lg">
          <CardHeader className="space-y-2">
            <CardDescription className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-text-hint)]">
              Activity mix
            </CardDescription>
            <CardTitle className="text-2xl">
              Tracked activity mix in this window
            </CardTitle>
            <CardDescription className="text-sm text-[var(--color-text-low-emphasis)]">
              Created records and Q&amp;A engagement are ranked together so the
              biggest activity signals are obvious without reading percentages
              off a donut.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-5">
            <ActivityMixChart data={dashboard.composition} />
            <div className="space-y-3">
              {dashboard.composition.map((item) => {
                const share = getCompositionShare(
                  item.value,
                  dashboard.composition,
                );

                return (
                  <div
                    key={item.key}
                    className="flex items-center justify-between rounded-[var(--radius-l)] border border-[var(--color-border-low-emphasis)] bg-[var(--color-paper-background)] px-4 py-3 shadow-[var(--shadow-xs)]"
                  >
                    <div>
                      <span className="font-medium text-[var(--color-text-default-on-light)]">
                        {item.label}
                      </span>
                      <p className="mt-1 text-xs text-[var(--color-text-hint)]">
                        {share.toFixed(1)}% of tracked activity
                      </p>
                    </div>
                    <span className="text-sm font-semibold text-[var(--color-text-hint)]">
                      {formatNumber(item.value)}
                    </span>
                  </div>
                );
              })}
            </div>
          </CardContent>
        </Card>
      </section>

      <section className="grid gap-6 xl:grid-cols-[minmax(0,1.7fr)_minmax(300px,1fr)]">
        <Card className="community-stats-surface border-0 shadow-lg">
          <CardHeader className="space-y-2">
            <CardDescription className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-text-hint)]">
              Focus view
            </CardDescription>
            <CardTitle className="text-2xl">
              {activeFocusDetail.label}
            </CardTitle>
            <CardDescription className="text-sm text-[var(--color-text-low-emphasis)]">
              {activeFocusDetail.description}
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="grid gap-4 md:grid-cols-2 2xl:grid-cols-3">
              {dashboard.highlights.map((item) => (
                <button
                  key={item.key}
                  type="button"
                  className={getMetricCardClass(item.key === focusedMetricKey)}
                  disabled={isLoading}
                  onClick={() => setFocusedMetricKey(item.key)}
                >
                  <p
                    className={getMutedLabelClass(
                      item.key === focusedMetricKey,
                    )}
                  >
                    {item.label}
                  </p>
                  <p
                    className={getMetricValueClass(
                      item.key === focusedMetricKey,
                    )}
                  >
                    {formatNumber(item.rangeTotal)}
                  </p>
                  <p
                    className={getMetricDescriptionClass(
                      item.key === focusedMetricKey,
                    )}
                  >
                    {item.description}
                  </p>
                  <div className="mt-5 flex items-center justify-between gap-3 text-sm">
                    <span
                      className={getChangeClass(
                        item.changeValue,
                        item.key === focusedMetricKey,
                      )}
                    >
                      {formatChange(item.changeValue, item.changePercent)}
                    </span>
                    <span
                      className={getAllTimeClass(item.key === focusedMetricKey)}
                    >
                      All-time {formatNumber(item.allTimeTotal)}
                    </span>
                  </div>
                </button>
              ))}
            </div>

            <FocusMetricChart detail={activeFocusDetail} />
          </CardContent>
        </Card>

        <Card className="community-stats-surface border-0 shadow-lg">
          <CardHeader className="space-y-2 pb-3">
            <CardDescription className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-text-hint)]">
              Insight notes
            </CardDescription>
            <CardTitle className="text-2xl">
              {activeSummary.label} snapshot
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <InsightRow
              label="Range total"
              value={formatNumber(activeFocusDetail.rangeTotal)}
              caption={getRangeCaption(dashboard)}
            />
            <InsightRow
              label="Previous window"
              value={formatNumber(activeFocusDetail.previousRangeTotal)}
              caption={formatNullablePercent(activeFocusDetail.changePercent)}
            />
            <InsightRow
              label="Average per month"
              value={formatNumber(activeFocusDetail.averageMonthlyValue)}
              caption="Calculated across the visible window"
            />
            <InsightRow
              label="Peak month"
              value={formatNumber(activeFocusDetail.peakMonth.value)}
              caption={activeFocusDetail.peakMonth.label}
            />
            <div className="rounded-[var(--radius-l)] border border-dashed border-[var(--color-border-default)] bg-[var(--color-background-highlighted)] p-4">
              <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-text-hint)]">
                Pattern readout
              </p>
              <p className="mt-2 text-sm leading-6 text-[var(--color-text-low-emphasis)]">
                This version reveals why the selected metric matters by showing
                its share of total tracked activity, whether the latest month is
                running above or below trend, how often it is active, and where
                the softest month landed.
              </p>
              <div className="mt-4 grid gap-3 sm:grid-cols-2">
                <CompactInsightRow
                  label="Share of activity"
                  value={`${focusDrilldown.activitySharePercent.toFixed(1)}%`}
                  caption={focusDrilldown.activityShareCaption}
                />
                <CompactInsightRow
                  label="Latest month vs trend"
                  value={formatSignedNumber(
                    focusDrilldown.latestMonthDeltaFromTrend,
                  )}
                  caption={focusDrilldown.latestMonthDeltaCaption}
                />
                <CompactInsightRow
                  label="Active months"
                  value={`${focusDrilldown.activeMonths}/${focusDrilldown.totalMonths}`}
                  caption={focusDrilldown.activeMonthsCaption}
                />
                <CompactInsightRow
                  label="Softest month"
                  value={formatNumber(focusDrilldown.troughMonth.value)}
                  caption={focusDrilldown.troughMonth.label}
                />
              </div>
            </div>
          </CardContent>
        </Card>
      </section>

      <div aria-hidden="true" className="h-6" />
    </div>
  );
};

const OverviewStatCard = (props: {
  label: string;
  value: number;
  caption: string;
}) => {
  return (
    <div className="rounded-[calc(var(--radius-l)+8px)] border border-[var(--color-border-low-emphasis)] bg-[var(--color-paper-background)] p-5 shadow-[var(--shadow-xs)] backdrop-blur">
      <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-text-hint)]">
        {props.label}
      </p>
      <p className="mt-4 text-4xl font-semibold text-[var(--color-text-default-on-light)]">
        {formatNumber(props.value)}
      </p>
      <p className="mt-2 text-sm text-[var(--color-text-low-emphasis)]">
        {props.caption}
      </p>
    </div>
  );
};

const InsightRow = (props: {
  label: string;
  value: string;
  caption: string;
}) => {
  return (
    <div className="rounded-[var(--radius-l)] border border-[var(--color-border-low-emphasis)] bg-[var(--color-paper-background)] p-4 shadow-[var(--shadow-xs)]">
      <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-text-hint)]">
        {props.label}
      </p>
      <p className="mt-2 text-3xl font-semibold text-[var(--color-text-default-on-light)]">
        {props.value}
      </p>
      <p className="mt-2 text-sm text-[var(--color-text-low-emphasis)]">
        {props.caption}
      </p>
    </div>
  );
};

const CompactInsightRow = (props: {
  label: string;
  value: string;
  caption: string;
}) => {
  return (
    <div className="rounded-[var(--radius-m)] border border-[var(--color-border-low-emphasis)] bg-[var(--color-paper-background)] px-3 py-3 shadow-[var(--shadow-xs)]">
      <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-[var(--color-text-hint)]">
        {props.label}
      </p>
      <p className="mt-1 text-xl font-semibold leading-tight text-[var(--color-text-default-on-light)]">
        {props.value}
      </p>
      <p className="mt-1 text-xs leading-5 text-[var(--color-text-low-emphasis)]">
        {props.caption}
      </p>
    </div>
  );
};

const MultiMetricTrendChart = (props: { series: CommunityMetricSeries[] }) => {
  const chartId = useId().replace(/:/g, '-');

  useLayoutEffect(() => {
    const root = Am5.Root.new(chartId);
    root.setThemes([Am5themesAnimated.new(root)]);
    root.numberFormatter.setAll({ numberFormat: '#,###' });
    const chartTokens = getChartTokens();
    const palette = getChartPalette();

    const chart = root.container.children.push(
      Am5XY.XYChart.new(root, {
        panX: false,
        panY: false,
        wheelX: 'none',
        wheelY: 'none',
        pinchZoomX: false,
        layout: root.verticalLayout,
      }),
    );
    chart.get('colors').set('colors', palette);

    const xRenderer = Am5XY.AxisRendererX.new(root, {
      minGridDistance: 40,
      cellStartLocation: 0.15,
      cellEndLocation: 0.85,
    });
    xRenderer.labels.template.setAll({
      fontSize: 11,
      fill: Am5.color(chartTokens.textLow),
      oversizedBehavior: 'truncate',
    });
    xRenderer.grid.template.setAll({
      stroke: Am5.color(chartTokens.borderLow),
      strokeOpacity: 0.2,
    });

    const xAxis = chart.xAxes.push(
      Am5XY.CategoryAxis.new(root, {
        categoryField: 'label',
        renderer: xRenderer,
        tooltip: Am5.Tooltip.new(root, {}),
      }),
    );

    const yRenderer = Am5XY.AxisRendererY.new(root, { minGridDistance: 30 });
    yRenderer.labels.template.setAll({
      fontSize: 11,
      fill: Am5.color(chartTokens.textLow),
    });
    yRenderer.grid.template.setAll({
      stroke: Am5.color(chartTokens.borderLow),
      strokeOpacity: 0.8,
      strokeDasharray: [4, 4],
    });

    const yAxis = chart.yAxes.push(
      Am5XY.ValueAxis.new(root, {
        min: 0,
        extraMax: 0.15,
        renderer: yRenderer,
      }),
    );

    xAxis.data.setAll(props.series[0]?.points ?? []);

    props.series.forEach((seriesItem, index) => {
      const series = chart.series.push(
        Am5XY.LineSeries.new(root, {
          name: seriesItem.label,
          xAxis,
          yAxis,
          valueYField: 'value',
          categoryXField: 'label',
          tooltip: Am5.Tooltip.new(root, {
            labelText: '{name}\n{categoryX}: [bold]{valueY}[/]',
          }),
        }),
      );

      const color = chart.get('colors').getIndex(index);
      series.strokes.template.setAll({ stroke: color, strokeWidth: 3 });
      series.fills.template.setAll({ fill: color, fillOpacity: 0.1 });
      series.bullets.push(() =>
        Am5.Bullet.new(root, {
          sprite: Am5.Circle.new(root, {
            radius: 4,
            fill: color,
            stroke: Am5.color(chartTokens.surface),
            strokeWidth: 2,
          }),
        }),
      );

      series.data.setAll(seriesItem.points);
      void series.appear(600);
    });

    const legend = chart.children.push(
      Am5.Legend.new(root, {
        centerX: Am5.percent(50),
        x: Am5.percent(50),
        layout: root.horizontalLayout,
      }),
    );
    legend.labels.template.setAll({ fill: Am5.color(chartTokens.textDefault) });
    legend.data.setAll(chart.series.values);

    chart.set(
      'cursor',
      Am5XY.XYCursor.new(root, {
        behavior: 'none',
        xAxis,
      }),
    );

    void chart.appear(700, 100);

    return () => {
      root.dispose();
    };
  }, [chartId, props.series]);

  return (
    <div
      id={chartId}
      className="community-stats-chart community-stats-chart-lg"
    />
  );
};

const FocusMetricChart = (props: { detail: CommunityMetricDetail }) => {
  const chartId = useId().replace(/:/g, '-');

  useLayoutEffect(() => {
    const root = Am5.Root.new(chartId);
    root.setThemes([Am5themesAnimated.new(root)]);
    root.numberFormatter.setAll({ numberFormat: '#,###' });
    const chartTokens = getChartTokens();
    const palette = getChartPalette();

    const chart = root.container.children.push(
      Am5XY.XYChart.new(root, {
        panX: false,
        panY: false,
        wheelX: 'none',
        wheelY: 'none',
        pinchZoomX: false,
        layout: root.verticalLayout,
      }),
    );
    chart.get('colors').set('colors', palette);

    const xAxis = chart.xAxes.push(
      Am5XY.CategoryAxis.new(root, {
        categoryField: 'label',
        renderer: Am5XY.AxisRendererX.new(root, {
          minGridDistance: 40,
          cellStartLocation: 0.18,
          cellEndLocation: 0.82,
        }),
      }),
    );
    xAxis.get('renderer').labels.template.setAll({
      fontSize: 11,
      fill: Am5.color(chartTokens.textLow),
    });
    xAxis.get('renderer').grid.template.setAll({
      stroke: Am5.color(chartTokens.borderLow),
      strokeOpacity: 0.2,
    });
    xAxis.data.setAll(props.detail.points);

    const yAxis = chart.yAxes.push(
      Am5XY.ValueAxis.new(root, {
        min: 0,
        extraMax: 0.18,
        renderer: Am5XY.AxisRendererY.new(root, { minGridDistance: 30 }),
      }),
    );
    yAxis.get('renderer').labels.template.setAll({
      fontSize: 11,
      fill: Am5.color(chartTokens.textLow),
    });
    yAxis.get('renderer').grid.template.setAll({
      stroke: Am5.color(chartTokens.borderLow),
      strokeOpacity: 0.8,
      strokeDasharray: [4, 4],
    });

    const columns = chart.series.push(
      Am5XY.ColumnSeries.new(root, {
        name: 'Monthly total',
        xAxis,
        yAxis,
        valueYField: 'value',
        categoryXField: 'label',
        tooltip: Am5.Tooltip.new(root, {
          labelText: '{categoryX}: [bold]{valueY}[/]',
        }),
      }),
    );
    columns.columns.template.setAll({
      cornerRadiusTL: 14,
      cornerRadiusTR: 14,
      fillOpacity: 0.9,
      stroke: Am5.color(chartTokens.surface),
      strokeOpacity: 0,
      width: Am5.percent(70),
    });
    columns.data.setAll(props.detail.points);

    const movingAverage = chart.series.push(
      Am5XY.LineSeries.new(root, {
        name: '3-month rolling average',
        xAxis,
        yAxis,
        valueYField: 'value',
        categoryXField: 'label',
        tooltip: Am5.Tooltip.new(root, {
          labelText: '{name}\n{categoryX}: [bold]{valueY}[/]',
        }),
      }),
    );
    movingAverage.strokes.template.setAll({
      strokeWidth: 3,
      stroke: chart.get('colors').getIndex(1),
    });
    movingAverage.bullets.push(() =>
      Am5.Bullet.new(root, {
        sprite: Am5.Circle.new(root, {
          radius: 4,
          fill: chart.get('colors').getIndex(1),
          stroke: Am5.color(chartTokens.surface),
          strokeWidth: 2,
        }),
      }),
    );
    movingAverage.data.setAll(props.detail.movingAveragePoints);

    const legend = chart.children.push(
      Am5.Legend.new(root, {
        centerX: Am5.percent(50),
        x: Am5.percent(50),
      }),
    );
    legend.labels.template.setAll({ fill: Am5.color(chartTokens.textDefault) });
    legend.data.setAll(chart.series.values);

    chart.set(
      'cursor',
      Am5XY.XYCursor.new(root, {
        behavior: 'none',
        xAxis,
      }),
    );

    void chart.appear(700, 100);

    return () => {
      root.dispose();
    };
  }, [chartId, props.detail]);

  return (
    <div
      id={chartId}
      className="community-stats-chart community-stats-chart-lg"
    />
  );
};

const ActivityMixChart = (props: { data: CommunityCompositionSlice[] }) => {
  const chartId = useId().replace(/:/g, '-');

  useLayoutEffect(() => {
    const root = Am5.Root.new(chartId);
    root.setThemes([Am5themesAnimated.new(root)]);
    root.numberFormatter.setAll({ numberFormat: '#,###' });
    const chartTokens = getChartTokens();

    const palette = getChartPalette();
    const sortedData = [...props.data].sort(
      (left, right) => right.value - left.value,
    );

    const chart = root.container.children.push(
      Am5XY.XYChart.new(root, {
        panX: false,
        panY: false,
        wheelX: 'none',
        wheelY: 'none',
        pinchZoomX: false,
        layout: root.verticalLayout,
      }),
    );

    const xAxis = chart.xAxes.push(
      Am5XY.ValueAxis.new(root, {
        min: 0,
        extraMax: 0.08,
        renderer: Am5XY.AxisRendererX.new(root, { minGridDistance: 30 }),
      }),
    );
    xAxis.get('renderer').labels.template.setAll({
      fontSize: 11,
      fill: Am5.color(chartTokens.textLow),
    });
    xAxis.get('renderer').grid.template.setAll({
      stroke: Am5.color(chartTokens.borderLow),
      strokeOpacity: 0.8,
      strokeDasharray: [4, 4],
    });

    const yAxis = chart.yAxes.push(
      Am5XY.CategoryAxis.new(root, {
        categoryField: 'label',
        renderer: Am5XY.AxisRendererY.new(root, { minGridDistance: 26 }),
      }),
    );
    yAxis.get('renderer').labels.template.setAll({
      fontSize: 11,
      fill: Am5.color(chartTokens.textLow),
      oversizedBehavior: 'truncate',
      maxWidth: 140,
    });
    yAxis.get('renderer').grid.template.setAll({
      forceHidden: true,
    });
    yAxis.data.setAll(sortedData);

    const series = chart.series.push(
      Am5XY.ColumnSeries.new(root, {
        name: 'Activity mix',
        xAxis,
        yAxis,
        valueXField: 'value',
        categoryYField: 'label',
        tooltip: Am5.Tooltip.new(root, {
          labelText: '{categoryY}: [bold]{valueX}[/]',
        }),
      }),
    );
    series.columns.template.setAll({
      cornerRadiusTR: 12,
      cornerRadiusBR: 12,
      strokeOpacity: 0,
      height: Am5.percent(74),
    });
    series.columns.template.adapters.add('fill', (_fill, target) => {
      const index = target.dataItem?.get('index') ?? 0;
      return palette[index % palette.length];
    });
    series.columns.template.adapters.add('stroke', (_stroke, target) => {
      const index = target.dataItem?.get('index') ?? 0;
      return palette[index % palette.length];
    });
    series.data.setAll(sortedData);

    chart.set(
      'cursor',
      Am5XY.XYCursor.new(root, {
        behavior: 'none',
        yAxis,
      }),
    );

    void series.appear(700);
    void chart.appear(700, 100);

    return () => {
      root.dispose();
    };
  }, [chartId, props.data]);

  return (
    <div
      id={chartId}
      className="community-stats-chart community-stats-chart-md"
    />
  );
};

function getCompositionShare(
  value: number,
  items: CommunityCompositionSlice[],
) {
  const total = items.reduce((sum, item) => sum + item.value, 0);

  if (total === 0) {
    return 0;
  }

  return (value / total) * 100;
}

function formatNumber(value: number) {
  return new Intl.NumberFormat().format(value);
}

function formatSignedNumber(value: number) {
  return `${value > 0 ? '+' : ''}${formatNumber(value)}`;
}

function formatDate(value: string) {
  const [year, month, day] = value.split('-').map((part) => parseInt(part, 10));
  const parsedDate = new Date(year, month - 1, day);

  return new Intl.DateTimeFormat(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }).format(parsedDate);
}

function buildClientFocusDetail(
  summary: CommunityMetricSummary,
  series: CommunityMetricSeries,
): CommunityMetricDetail {
  const points = series?.points ?? [];
  const peakMonth = points.reduce<CommunityMetricPoint | null>(
    (currentPeak, point) => {
      if (!currentPeak || point.value > currentPeak.value) {
        return point;
      }

      return currentPeak;
    },
    null,
  ) ?? {
    label: '',
    periodStart: '',
    value: 0,
  };

  const averageMonthlyValue =
    points.length > 0
      ? Math.round(
          points.reduce((total, point) => total + point.value, 0) /
            points.length,
        )
      : 0;

  return {
    key: summary.key,
    label: summary.label,
    description: summary.description,
    rangeTotal: summary.rangeTotal,
    previousRangeTotal: summary.previousRangeTotal,
    changePercent: summary.rangeChangePercent,
    averageMonthlyValue,
    peakMonth,
    points,
    movingAveragePoints: buildMovingAveragePoints(points, 3),
  };
}

function buildMovingAveragePoints(
  points: CommunityMetricPoint[],
  windowSize: number,
) {
  return points.map((point, index) => {
    const startIndex = Math.max(0, index - windowSize + 1);
    const window = points.slice(startIndex, index + 1);
    const averageValue = Math.round(
      window.reduce((total, item) => total + item.value, 0) / window.length,
    );

    return {
      ...point,
      value: averageValue,
    };
  });
}

function areRangesEqual(left: DateRangeValue, right: DateRangeValue) {
  return (
    left.from.getTime() === right.from.getTime() &&
    left.to.getTime() === right.to.getTime()
  );
}

function formatNullablePercent(value: number | null | undefined) {
  if (value === null || value === undefined) {
    return 'No previous baseline available';
  }

  return `${value > 0 ? '+' : ''}${value.toFixed(1)}% vs previous window`;
}

function formatChange(
  changeValue: number,
  changePercent: number | null | undefined,
) {
  const value = `${changeValue > 0 ? '+' : ''}${formatNumber(changeValue)}`;

  if (changePercent === null || changePercent === undefined) {
    return `${value} vs prior month`;
  }

  return `${value} (${changePercent > 0 ? '+' : ''}${changePercent.toFixed(1)}%)`;
}

function getRangeCaption(dashboard: CommunityStatsDashboard) {
  return `${formatDate(dashboard.rangeStartDate)} to ${formatDate(dashboard.rangeEndDate)}`;
}

function buildFocusDrilldown(
  dashboard: CommunityStatsDashboard,
  detail: CommunityMetricDetail,
) {
  const compositionSlice = dashboard.composition.find(
    (item) => item.key === detail.key,
  );
  const activitySharePercent = compositionSlice
    ? getCompositionShare(compositionSlice.value, dashboard.composition)
    : 0;
  const latestPoint = detail.points[detail.points.length - 1] ?? {
    label: '',
    periodStart: '',
    value: 0,
  };
  const latestTrendPoint =
    detail.movingAveragePoints[detail.movingAveragePoints.length - 1] ??
    latestPoint;
  const latestMonthDeltaFromTrend = latestPoint.value - latestTrendPoint.value;
  const activeMonths = detail.points.filter((point) => point.value > 0).length;
  const totalMonths = Math.max(detail.points.length, 1);
  const activeMonthRatePercent = (activeMonths / totalMonths) * 100;
  const troughMonth = detail.points.reduce<CommunityMetricPoint | null>(
    (currentTrough, point) => {
      if (!currentTrough || point.value < currentTrough.value) {
        return point;
      }

      return currentTrough;
    },
    null,
  ) ?? {
    label: '',
    periodStart: '',
    value: 0,
  };

  return {
    activitySharePercent,
    activityShareCaption: compositionSlice
      ? `${formatNumber(compositionSlice.value)} of ${formatNumber(dashboard.overview.newItemsInRange)} tracked records in this window`
      : 'This metric is currently outside the tracked-activity mix ranking',
    latestMonthDeltaFromTrend,
    latestMonthDeltaCaption: `${latestPoint.label}: ${formatNumber(latestPoint.value)} vs 3-month trend ${formatNumber(latestTrendPoint.value)}`,
    activeMonths,
    totalMonths,
    activeMonthsCaption: `${activeMonthRatePercent.toFixed(1)}% of visible months recorded activity`,
    troughMonth,
  };
}

function getMetricCardClass(isActive: boolean) {
  return [
    'rounded-[28px] border p-5 text-left transition-all duration-[var(--transition-duration-default)]',
    isActive
      ? 'border-[var(--color-product)] bg-[image:var(--gradient-product)] text-[var(--color-text-default-on-dark)] shadow-[var(--shadow-m)]'
      : 'border-[var(--color-border-low-emphasis)] bg-[var(--color-paper-background)] text-[var(--color-text-default-on-light)] shadow-[var(--shadow-xs)] hover:-translate-y-0.5 hover:shadow-[var(--shadow-s)]',
  ].join(' ');
}

function getChangeClass(changeValue: number, isActive: boolean) {
  if (isActive) {
    if (changeValue > 0) {
      return 'font-semibold text-[var(--palette-neon-green-20)]';
    }

    if (changeValue < 0) {
      return 'font-semibold text-[var(--palette-red-20)]';
    }

    return 'font-semibold text-[var(--warm-grey-20)]';
  }

  if (changeValue > 0) {
    return 'font-semibold text-[var(--color-success-text)]';
  }

  if (changeValue < 0) {
    return 'font-semibold text-[var(--color-alert-text)]';
  }

  return 'font-semibold text-[var(--color-text-hint)]';
}

function getMutedLabelClass(isActive: boolean) {
  return [
    'text-xs font-semibold uppercase tracking-[0.24em]',
    isActive ? 'text-[var(--warm-grey-20)]' : 'text-[var(--color-text-hint)]',
  ].join(' ');
}

function getMetricValueClass(isActive: boolean) {
  return [
    'mt-3 text-4xl font-semibold',
    isActive
      ? 'text-[var(--color-text-default-on-dark)]'
      : 'text-[var(--color-text-default-on-light)]',
  ].join(' ');
}

function getMetricDescriptionClass(isActive: boolean) {
  return [
    'mt-3 text-sm leading-6',
    isActive
      ? 'text-[var(--warm-grey-20)]'
      : 'text-[var(--color-text-low-emphasis)]',
  ].join(' ');
}

function getAllTimeClass(isActive: boolean) {
  return isActive
    ? 'text-[var(--warm-grey-20)]'
    : 'text-[var(--color-text-hint)]';
}

function getChartTokens() {
  return {
    textDefault: getCssToken('--color-text-default-on-light', '#151515'),
    textLow: getCssToken('--color-text-low-emphasis', '#525252'),
    borderLow: getCssToken('--color-border-low-emphasis', '#dfdfdf'),
    surface: getCssToken('--color-paper-background', '#ffffff'),
  };
}

function getChartPalette() {
  return [
    Am5.color(getCssToken('--color-product', '#7f09b7')),
    Am5.color(getCssToken('--color-info-background-high-emphasis', '#3d5dff')),
    Am5.color(
      getCssToken('--color-success-background-high-emphasis', '#007d72'),
    ),
    Am5.color(
      getCssToken('--color-warning-background-high-emphasis', '#db9d00'),
    ),
    Am5.color(getCssToken('--color-alert-background-high-emphasis', '#e10007')),
  ];
}

function getCssToken(tokenName: string, fallback: string) {
  if (typeof window === 'undefined') {
    return fallback;
  }

  const tokenValue = getComputedStyle(document.documentElement)
    .getPropertyValue(tokenName)
    .trim();

  return tokenValue || fallback;
}

function parseDateOnlyString(value: string) {
  const [year, month, day] = value.split('-').map((part) => parseInt(part, 10));
  return new Date(year, month - 1, day);
}
