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
import { cn } from '../../lib/utils';
import './contact-activity-stats.css';

type DateRangeValue = {
  from: Date;
  to: Date;
};

type ContactActivityStatsQuery = {
  rangeStart: Date;
  rangeEnd: Date;
  focusActivityTypeKey?: string;
};

type ContactActivityPoint = {
  periodStart: string;
  label: string;
  value: number;
};

type ContactActivityTypeSummary = {
  key: string;
  label: string;
  description: string;
  rangeTotal: number;
  activeContacts: number;
  activeMonths: number;
  peakMonthLabel: string;
  peakMonthValue: number;
  latestMonthValue: number;
  previousMonthValue: number;
  sharePercent: number;
  monthOverMonthChangePercent?: number | null;
};

type ContactActivitySeries = {
  key: string;
  label: string;
  points: ContactActivityPoint[];
};

type ContactActivityOverview = {
  totalActivities: number;
  activeContacts: number;
  repeatContacts: number;
  repeatContactRate: number;
  averageActivitiesPerContact: number;
  activityTypesTracked: number;
  activeDays: number;
  leadingActivityLabel: string;
  leadingActivityValue: number;
  peakMonthLabel: string;
  peakMonthValue: number;
};

type ContactActivitySignal = {
  key: string;
  label: string;
  value: string;
  context: string;
};

type ContactActivityStatsDashboard = {
  rangeStartDate: string;
  rangeEndDate: string;
  focusActivityTypeKey: string;
  overview: ContactActivityOverview;
  activityTypes: ContactActivityTypeSummary[];
  series: ContactActivitySeries[];
  signals: ContactActivitySignal[];
};

type ContactActivityDetail = {
  key: string;
  label: string;
  description: string;
  rangeTotal: number;
  activeContacts: number;
  averageMonthlyActivities: number;
  activeMonths: number;
  latestMonthValue: number;
  previousMonthValue: number;
  monthOverMonthChangePercent?: number | null;
  peakMonth: ContactActivityPoint;
  quietMonth: ContactActivityPoint;
  points: ContactActivityPoint[];
  movingAveragePoints: ContactActivityPoint[];
};

type ContactActivityHeatmapCell = {
  activityTypeKey: string;
  activityTypeLabel: string;
  periodLabel: string;
  value: number;
};

interface ContactActivityStatsClientProperties {
  stats: ContactActivityStatsDashboard;
  title: string;
  description: string;
}

export const ContactActivityStatsLayoutTemplate = (
  props: ContactActivityStatsClientProperties,
) => {
  const [dashboard, setDashboard] = useState<ContactActivityStatsDashboard>(
    props.stats,
  );
  const [selectedRange, setSelectedRange] = useState<DateRangeValue>({
    from: parseDateOnlyString(props.stats.rangeStartDate),
    to: parseDateOnlyString(props.stats.rangeEndDate),
  });
  const [focusedActivityTypeKey, setFocusedActivityTypeKey] = useState(
    props.stats.focusActivityTypeKey,
  );
  const [isLoading, setIsLoading] = useState(false);

  const appliedRange = {
    from: parseDateOnlyString(dashboard.rangeStartDate),
    to: parseDateOnlyString(dashboard.rangeEndDate),
  };

  const { execute: loadData } = usePageCommand<
    ContactActivityStatsDashboard,
    ContactActivityStatsQuery
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
        setFocusedActivityTypeKey(commandResult.focusActivityTypeKey);
      }

      setIsLoading(false);
    },
  });

  const summaries = dashboard.activityTypes ?? [];
  const series = dashboard.series ?? [];
  const heatmapData = buildHeatmapData(series);
  const activeSummary =
    summaries.find((item) => item.key === focusedActivityTypeKey) ??
    summaries[0];
  const activeSeries =
    series.find((item) => item.key === focusedActivityTypeKey) ?? series[0];
  const activeDetail = buildActivityDetail(activeSummary, activeSeries);
  const hasPendingRangeChange = !areRangesEqual(selectedRange, appliedRange);

  async function refreshDashboard(nextRange: DateRangeValue) {
    await loadData({
      rangeStart: nextRange.from,
      rangeEnd: nextRange.to,
      focusActivityTypeKey: focusedActivityTypeKey,
    });
  }

  function handleDateRangeChange(nextRange: DateRangeValue | null) {
    if (!nextRange) {
      return;
    }

    setSelectedRange(nextRange);
  }

  async function applySelectedRange() {
    await refreshDashboard(selectedRange);
  }

  return (
    <div className="contact-activity-dashboard space-y-6 pb-8">
      <section className="grid gap-6 xl:grid-cols-[minmax(0,2fr)_minmax(340px,1fr)]">
        <Card className="contact-activity-surface border-0 shadow-lg">
          <CardHeader className="space-y-4">
            <div className="space-y-2">
              <CardTitle className="text-4xl font-semibold tracking-tight">
                {props.title}
              </CardTitle>
              <CardDescription className="max-w-3xl text-base text-[var(--color-text-low-emphasis)]">
                {props.description}
              </CardDescription>
            </div>

            <div className="grid gap-4 md:grid-cols-3">
              <OverviewStatCard
                label="Tracked activities"
                value={dashboard.overview.totalActivities}
                caption={getRangeCaption(dashboard)}
              />
              <OverviewStatCard
                label="Active contacts"
                value={dashboard.overview.activeContacts}
                caption={`${formatNumber(dashboard.overview.repeatContacts)} repeated in this window`}
              />
              <OverviewStatCard
                label="Peak month"
                value={dashboard.overview.peakMonthValue}
                caption={dashboard.overview.peakMonthLabel}
              />
            </div>

            <div className="rounded-[var(--radius-l)] border border-dashed border-[var(--color-border-default)] bg-[var(--color-background-highlighted)] p-4">
              <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-text-hint)]">
                What this view is for
              </p>
              <p className="mt-2 text-sm leading-6 text-[var(--color-text-low-emphasis)]">
                This page emphasizes recent contact behavior instead of
                long-tail member engagement. It is tuned for the period where
                anonymous traffic is still actionable and before inactivity
                cleanup starts trimming older records.
              </p>
            </div>
          </CardHeader>
        </Card>

        <Card className="contact-activity-surface border-0 shadow-lg">
          <CardContent className="space-y-4 pt-5 text-sm text-[var(--color-text-low-emphasis)]">
            <div className="rounded-[var(--radius-l)] border border-[var(--color-border-low-emphasis)] bg-[var(--color-paper-background)] p-4 shadow-[var(--shadow-xs)]">
              <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-text-hint)]">
                Operating window
              </p>
              <p className="mt-3 text-3xl font-semibold text-[var(--color-text-default-on-light)]">
                Last 6 months by default
              </p>
              <p className="mt-2 text-sm text-[var(--color-text-low-emphasis)]">
                Anonymous contact histories can age out on a rolling 12-month
                basis, so older ranges are useful mainly for context rather than
                decision-making.
              </p>
            </div>

            <div className="contact-activity-range-picker rounded-[var(--radius-l)] border border-[var(--color-border-low-emphasis)] bg-[var(--tile-toolbar-background)] p-3 backdrop-blur">
              <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-text-hint)]">
                Visible range
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
                  label="Update activity view"
                  onClick={() => {
                    void applySelectedRange();
                  }}
                  type="button"
                />
              </div>
            </div>

            <div className="grid gap-3">
              {dashboard.signals.map((item) => (
                <SignalCard key={item.key} signal={item} />
              ))}
            </div>
          </CardContent>
        </Card>
      </section>

      <section className="grid gap-6 2xl:grid-cols-[minmax(0,1.9fr)_minmax(320px,1fr)]">
        <Card className="contact-activity-surface border-0 shadow-lg">
          <CardHeader className="space-y-2">
            <CardDescription className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-text-hint)]">
              Trend
            </CardDescription>
            <CardTitle className="text-2xl">
              Which activity types are driving the recent window
            </CardTitle>
            <CardDescription className="text-sm text-[var(--color-text-low-emphasis)]">
              Stacked monthly activity shows whether contact engagement is broad
              across behaviors or concentrated in a narrow set of tracked
              events.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <ContactActivityTrendChart maxSeriesCount={6} series={series} />
          </CardContent>
        </Card>

        <Card className="contact-activity-surface border-0 shadow-lg">
          <CardHeader className="space-y-2">
            <CardDescription className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-text-hint)]">
              Mix
            </CardDescription>
            <CardTitle className="text-2xl">
              Activity mix in the selected range
            </CardTitle>
            <CardDescription className="text-sm text-[var(--color-text-low-emphasis)]">
              Ranking totals beside the trend chart makes it easier to separate
              dominant behaviors from small but persistent signals.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <ActivityMixChart summaries={summaries} />
          </CardContent>
        </Card>
      </section>

      <section className="grid gap-6 xl:grid-cols-[minmax(0,1.7fr)_minmax(320px,1fr)]">
        <Card className="contact-activity-surface border-0 shadow-lg">
          <CardHeader className="space-y-2">
            <CardDescription className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-text-hint)]">
              Focus detail
            </CardDescription>
            <CardTitle className="text-2xl">
              {activeDetail?.label ?? 'No activity selected'}
            </CardTitle>
            <CardDescription className="text-sm text-[var(--color-text-low-emphasis)]">
              {activeDetail?.description ??
                'The focused chart isolates one signal so it is easier to see pace, consistency, and short-term acceleration without the noise of the full activity stack.'}
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-5">
            <div className="grid gap-3 md:grid-cols-2 2xl:grid-cols-3">
              {summaries.length === 0 ? (
                <EmptyState message="No contact activity was tracked in the selected window." />
              ) : (
                summaries.map((item) => (
                  <button
                    key={item.key}
                    type="button"
                    className={getSummaryCardClass(
                      item.key === focusedActivityTypeKey,
                    )}
                    disabled={isLoading}
                    onClick={() => setFocusedActivityTypeKey(item.key)}
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p
                          className={getSummaryLabelClass(
                            item.key === focusedActivityTypeKey,
                          )}
                        >
                          {item.label}
                        </p>
                        <p className="mt-1 text-xs text-[var(--color-text-hint)]">
                          {item.activeContacts.toLocaleString()} contacts,{' '}
                          {item.activeMonths} active months
                        </p>
                      </div>
                      <span
                        className={getSummaryValueClass(
                          item.key === focusedActivityTypeKey,
                        )}
                      >
                        {formatNumber(item.rangeTotal)}
                      </span>
                    </div>
                    <div className="mt-4 flex items-center justify-between gap-3 text-xs">
                      <span
                        className={getChangeClass(
                          item.monthOverMonthChangePercent,
                        )}
                      >
                        {formatPercentDelta(item.monthOverMonthChangePercent)}
                      </span>
                      <span className="text-[var(--color-text-hint)]">
                        {item.sharePercent.toFixed(1)}% of range activity
                      </span>
                    </div>
                  </button>
                ))
              )}
            </div>
            <FocusActivityChart detail={activeDetail} />
          </CardContent>
        </Card>

        <Card className="contact-activity-surface border-0 shadow-lg">
          <CardHeader className="space-y-2 pb-3">
            <CardDescription className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-text-hint)]">
              Insight notes
            </CardDescription>
            <CardTitle className="text-2xl">
              {activeDetail?.label ?? 'Recent activity snapshot'}
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {activeDetail ? (
              <>
                <InsightRow
                  label="Range total"
                  value={formatNumber(activeDetail.rangeTotal)}
                  caption={`${activeDetail.activeContacts.toLocaleString()} contacts contributed to this signal`}
                />
                <InsightRow
                  label="Average per month"
                  value={formatNumber(activeDetail.averageMonthlyActivities)}
                  caption={`${activeDetail.activeMonths}/${activeDetail.points.length} visible months were active`}
                />
                <InsightRow
                  label="Peak month"
                  value={formatNumber(activeDetail.peakMonth.value)}
                  caption={activeDetail.peakMonth.label}
                />
                <div className="rounded-[var(--radius-l)] border border-dashed border-[var(--color-border-default)] bg-[var(--color-background-highlighted)] p-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-text-hint)]">
                    Readout
                  </p>
                  <div className="mt-3 grid gap-3 sm:grid-cols-2">
                    <CompactInsightRow
                      label="Latest month"
                      value={formatNumber(activeDetail.latestMonthValue)}
                      caption={
                        activeDetail.points[activeDetail.points.length - 1]
                          ?.label ?? ''
                      }
                    />
                    <CompactInsightRow
                      label="Prior month"
                      value={formatNumber(activeDetail.previousMonthValue)}
                      caption={formatPercentDelta(
                        activeDetail.monthOverMonthChangePercent,
                      )}
                    />
                    <CompactInsightRow
                      label="Quietest month"
                      value={formatNumber(activeDetail.quietMonth.value)}
                      caption={activeDetail.quietMonth.label}
                    />
                    <CompactInsightRow
                      label="Share of all activity"
                      value={`${activeSummary?.sharePercent.toFixed(1) ?? '0.0'}%`}
                      caption="Portion of tracked contact activity in this range"
                    />
                  </div>
                </div>
              </>
            ) : (
              <EmptyState message="No focused activity is available for the selected range." />
            )}
          </CardContent>
        </Card>
      </section>

      <section>
        <Card className="contact-activity-surface border-0 shadow-lg">
          <CardHeader className="space-y-2">
            <CardDescription className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-text-hint)]">
              Density map
            </CardDescription>
            <CardTitle className="text-2xl">
              Where activity stays warm versus where it fades
            </CardTitle>
            <CardDescription className="text-sm text-[var(--color-text-low-emphasis)]">
              The heatmap reuses the same monthly activity data to show
              persistence across activity types, making drop-offs and sustained
              signals much more obvious than a table.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <ActivityHeatmapChart data={heatmapData} />
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

const SignalCard = (props: { signal: ContactActivitySignal }) => {
  return (
    <div className="rounded-[var(--radius-l)] border border-[var(--color-border-low-emphasis)] bg-[var(--color-paper-background)] px-4 py-4 shadow-[var(--shadow-xs)]">
      <div className="flex items-baseline justify-between gap-3">
        <span className="text-sm text-[var(--color-text-low-emphasis)]">
          {props.signal.label}
        </span>
        <span className="text-base font-semibold text-[var(--color-text-default-on-light)]">
          {props.signal.value}
        </span>
      </div>
      <p className="mt-2 text-xs leading-5 text-[var(--color-text-hint)]">
        {props.signal.context}
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

const EmptyState = (props: { message: string }) => {
  return (
    <div className="rounded-[var(--radius-l)] border border-dashed border-[var(--color-border-default)] bg-[var(--color-background-highlighted)] px-4 py-6 text-sm text-[var(--color-text-low-emphasis)]">
      {props.message}
    </div>
  );
};

const ContactActivityTrendChart = (props: {
  series: ContactActivitySeries[];
  maxSeriesCount: number;
}) => {
  const chartId = useId().replace(/:/g, '-');

  useLayoutEffect(() => {
    if (props.series.length === 0) {
      return undefined;
    }

    const visibleSeries = props.series.slice(0, props.maxSeriesCount);

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
      cellStartLocation: 0.14,
      cellEndLocation: 0.86,
    });
    xRenderer.labels.template.setAll({
      fontSize: 11,
      fill: Am5.color(chartTokens.textLow),
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

    const categories = visibleSeries[0]?.points ?? [];
    xAxis.data.setAll(categories);

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
        extraMax: 0.12,
        renderer: yRenderer,
      }),
    );

    visibleSeries.forEach((seriesItem, index) => {
      const series = chart.series.push(
        Am5XY.ColumnSeries.new(root, {
          name: seriesItem.label,
          xAxis,
          yAxis,
          stacked: true,
          categoryXField: 'label',
          valueYField: 'value',
          tooltip: Am5.Tooltip.new(root, {
            labelText: '{name}\n{categoryX}: [bold]{valueY}[/]',
          }),
        }),
      );

      const color = chart.get('colors').getIndex(index);
      series.columns.template.setAll({
        fill: color,
        stroke: Am5.color(chartTokens.surface),
        strokeOpacity: 0,
        width: Am5.percent(82),
        cornerRadiusTL: 10,
        cornerRadiusTR: 10,
      });

      series.data.setAll(seriesItem.points);
      void series.appear(500);
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
  }, [chartId, props.maxSeriesCount, props.series]);

  if (props.series.length === 0) {
    return (
      <EmptyState message="No monthly contact activity is available in this range." />
    );
  }

  return (
    <div
      id={chartId}
      className="contact-activity-chart contact-activity-chart-lg"
    />
  );
};

const ActivityMixChart = (props: {
  summaries: ContactActivityTypeSummary[];
}) => {
  const chartId = useId().replace(/:/g, '-');

  useLayoutEffect(() => {
    if (props.summaries.length === 0) {
      return undefined;
    }

    const root = Am5.Root.new(chartId);
    root.setThemes([Am5themesAnimated.new(root)]);
    root.numberFormatter.setAll({ numberFormat: '#,###' });

    const chartTokens = getChartTokens();
    const palette = getChartPalette();
    const data = props.summaries.slice(0, 8).map((item) => ({
      label: item.label,
      value: item.rangeTotal,
    }));

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
      maxWidth: 170,
    });
    yAxis.get('renderer').grid.template.setAll({ forceHidden: true });
    yAxis.data.setAll(data);

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
      height: Am5.percent(72),
    });
    series.columns.template.adapters.add('fill', (_fill, target) => {
      const index = target.dataItem?.get('index') ?? 0;
      return palette[index % palette.length];
    });
    series.columns.template.adapters.add('stroke', (_stroke, target) => {
      const index = target.dataItem?.get('index') ?? 0;
      return palette[index % palette.length];
    });
    series.data.setAll(data);

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
  }, [chartId, props.summaries]);

  if (props.summaries.length === 0) {
    return <EmptyState message="No activity mix is available in this range." />;
  }

  return (
    <div
      id={chartId}
      className="contact-activity-chart contact-activity-chart-md"
    />
  );
};

const FocusActivityChart = (props: {
  detail: ContactActivityDetail | undefined;
}) => {
  const chartId = useId().replace(/:/g, '-');

  useLayoutEffect(() => {
    if (!props.detail) {
      return undefined;
    }

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
        name: 'Monthly activity',
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
      fillOpacity: 0.92,
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

  if (!props.detail) {
    return (
      <EmptyState message="No focused activity trend is available in this range." />
    );
  }

  return (
    <div
      id={chartId}
      className="contact-activity-chart contact-activity-chart-lg"
    />
  );
};

const ActivityHeatmapChart = (props: {
  data: ContactActivityHeatmapCell[];
}) => {
  const chartId = useId().replace(/:/g, '-');

  useLayoutEffect(() => {
    if (props.data.length === 0) {
      return undefined;
    }

    const root = Am5.Root.new(chartId);
    root.setThemes([Am5themesAnimated.new(root)]);
    root.numberFormatter.setAll({ numberFormat: '#,###' });

    const chartTokens = getChartTokens();
    const startColor = Am5.color(
      getCssToken('--color-background-highlighted', '#f3efe8'),
    );
    const endColor = Am5.color(getCssToken('--color-product', '#7f09b7'));
    const xCategories = getDistinctValues(
      props.data.map((item) => item.periodLabel),
    ).map((label) => ({ label }));
    const yCategories = getDistinctValues(
      props.data.map((item) => item.activityTypeLabel),
    ).map((label) => ({ label }));

    const chart = root.container.children.push(
      Am5XY.XYChart.new(root, {
        panX: false,
        panY: false,
        wheelX: 'none',
        wheelY: 'none',
        paddingLeft: 0,
        layout: root.verticalLayout,
      }),
    );

    const yRenderer = Am5XY.AxisRendererY.new(root, {
      minGridDistance: 26,
      inversed: true,
      minorGridEnabled: true,
    });
    yRenderer.grid.template.set('visible', false);

    const yAxis = chart.yAxes.push(
      Am5XY.CategoryAxis.new(root, {
        maxDeviation: 0,
        renderer: yRenderer,
        categoryField: 'label',
      }),
    );
    yAxis.data.setAll(yCategories);
    yAxis.get('renderer').labels.template.setAll({
      fontSize: 11,
      fill: Am5.color(chartTokens.textLow),
      oversizedBehavior: 'truncate',
      maxWidth: 170,
    });

    const xRenderer = Am5XY.AxisRendererX.new(root, {
      minGridDistance: 30,
      opposite: true,
      minorGridEnabled: true,
    });
    xRenderer.grid.template.set('visible', false);

    const xAxis = chart.xAxes.push(
      Am5XY.CategoryAxis.new(root, {
        renderer: xRenderer,
        categoryField: 'label',
      }),
    );
    xAxis.data.setAll(xCategories);
    xAxis.get('renderer').labels.template.setAll({
      fontSize: 11,
      fill: Am5.color(chartTokens.textLow),
    });

    const series = chart.series.push(
      Am5XY.ColumnSeries.new(root, {
        calculateAggregates: true,
        stroke: Am5.color(chartTokens.surface),
        clustered: false,
        xAxis,
        yAxis,
        categoryXField: 'periodLabel',
        categoryYField: 'activityTypeLabel',
        valueField: 'value',
      }),
    );

    series.columns.template.setAll({
      tooltipText: '{activityTypeLabel} in {periodLabel}: [bold]{value}[/]',
      strokeOpacity: 1,
      strokeWidth: 2,
      width: Am5.percent(100),
      height: Am5.percent(100),
    });

    const heatLegend = chart.bottomAxesContainer.children.push(
      Am5.HeatLegend.new(root, {
        orientation: 'horizontal',
        startColor,
        endColor,
      }),
    );

    series.columns.template.events.on('pointerover', (event) => {
      const dataItem = event.target.dataItem;
      if (dataItem) {
        heatLegend.showValue(dataItem.get('value', 0));
      }
    });

    series.events.on('datavalidated', () => {
      heatLegend.set('startValue', series.getPrivate('valueHigh'));
      heatLegend.set('endValue', series.getPrivate('valueLow'));
    });

    series.set('heatRules', [
      {
        target: series.columns.template,
        min: startColor,
        max: endColor,
        dataField: 'value',
        key: 'fill',
      },
    ]);

    series.data.setAll(props.data);
    xAxis.data.setAll(xCategories);
    yAxis.data.setAll(yCategories);

    void chart.appear(700, 100);

    return () => {
      root.dispose();
    };
  }, [chartId, props.data]);

  if (props.data.length === 0) {
    return (
      <EmptyState message="No activity density map is available in this range." />
    );
  }

  return (
    <div
      id={chartId}
      className="contact-activity-chart contact-activity-chart-xl"
    />
  );
};

function buildHeatmapData(
  series: ContactActivitySeries[],
): ContactActivityHeatmapCell[] {
  const cells: ContactActivityHeatmapCell[] = [];

  for (const item of series) {
    for (const point of item.points) {
      cells.push({
        activityTypeKey: item.key,
        activityTypeLabel: item.label,
        periodLabel: point.label,
        value: point.value,
      });
    }
  }

  return cells;
}

function buildActivityDetail(
  summary: ContactActivityTypeSummary | undefined,
  series: ContactActivitySeries | undefined,
): ContactActivityDetail | undefined {
  if (!summary || !series) {
    return undefined;
  }

  const points = series.points ?? [];
  const peakMonth = points.reduce<ContactActivityPoint | null>(
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

  const quietMonth = points.reduce<ContactActivityPoint | null>(
    (currentQuiet, point) => {
      if (!currentQuiet || point.value < currentQuiet.value) {
        return point;
      }

      return currentQuiet;
    },
    null,
  ) ?? {
    label: '',
    periodStart: '',
    value: 0,
  };

  const averageMonthlyActivities =
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
    activeContacts: summary.activeContacts,
    averageMonthlyActivities,
    activeMonths: summary.activeMonths,
    latestMonthValue: summary.latestMonthValue,
    previousMonthValue: summary.previousMonthValue,
    monthOverMonthChangePercent: summary.monthOverMonthChangePercent,
    peakMonth,
    quietMonth,
    points,
    movingAveragePoints: buildMovingAveragePoints(points, 3),
  };
}

function buildMovingAveragePoints(
  points: ContactActivityPoint[],
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

function getDistinctValues(values: string[]) {
  return Array.from(new Set(values));
}

function formatNumber(value: number) {
  return new Intl.NumberFormat().format(value);
}

function formatPercentDelta(value: number | null | undefined) {
  if (value === null || value === undefined) {
    return 'No prior-month baseline';
  }

  return `${value > 0 ? '+' : ''}${value.toFixed(1)}% vs prior month`;
}

function getRangeCaption(dashboard: ContactActivityStatsDashboard) {
  return `${formatDate(dashboard.rangeStartDate)} to ${formatDate(dashboard.rangeEndDate)}`;
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

function parseDateOnlyString(value: string) {
  const [year, month, day] = value.split('-').map((part) => parseInt(part, 10));
  return new Date(year, month - 1, day);
}

function getSummaryCardClass(isActive: boolean) {
  return cn(
    'w-full rounded-[var(--radius-l)] border px-4 py-4 text-left transition-all',
    isActive
      ? 'border-[var(--color-product)] bg-[var(--color-background-highlighted)] text-[var(--color-text-default-on-light)] shadow-[var(--shadow-s)]'
      : 'border-[var(--color-border-low-emphasis)] bg-[var(--color-paper-background)] text-[var(--color-text-default-on-light)] shadow-[var(--shadow-xs)] hover:-translate-y-0.5 hover:shadow-[var(--shadow-s)]',
  );
}

function getSummaryLabelClass(isActive: boolean) {
  return isActive
    ? 'text-[var(--color-text-default-on-light)]'
    : 'text-[var(--color-text-low-emphasis)]';
}

function getSummaryValueClass(isActive: boolean) {
  return cn(
    'text-2xl font-semibold',
    isActive
      ? 'text-[var(--color-text-default-on-light)]'
      : 'text-[var(--color-text-default-on-light)]',
  );
}

function getChangeClass(value: number | null | undefined) {
  if (value === null || value === undefined) {
    return 'font-semibold text-[var(--color-text-hint)]';
  }

  if (value > 0) {
    return 'font-semibold text-[var(--color-success-text)]';
  }

  if (value < 0) {
    return 'font-semibold text-[var(--color-alert-text)]';
  }

  return 'font-semibold text-[var(--color-text-hint)]';
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
    Am5.color(getCssToken('--color-link', '#0055cc')),
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
