import { usePageCommand } from '@kentico/xperience-admin-base';
import {
  Button,
  DateTimeRangeInput,
} from '@kentico/xperience-admin-components';
import * as Am5 from '@amcharts/amcharts5';
import Am5themesAnimated from '@amcharts/amcharts5/themes/Animated';
import * as Am5Timeline from '@amcharts/amcharts5/timeline';
import * as Am5XY from '@amcharts/amcharts5/xy';
import React, { useId, useLayoutEffect, useState } from 'react';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '../../components/ui/card';
import './contact-activity-path.css';

type DateRangeValue = {
  from: Date;
  to: Date;
};

type ContactActivityPathQuery = {
  rangeStart: Date;
  rangeEnd: Date;
};

type ContactActivityPathContact = {
  displayName: string;
  email: string;
};

type ContactActivityPathOverview = {
  totalActivities: number;
  activityTypesTracked: number;
  activeDays: number;
  firstActivityAt: string;
  lastActivityAt: string;
};

type ContactActivityPathItem = {
  key: string;
  activityTypeKey: string;
  activityTypeLabel: string;
  dayLabel: string;
  startTime: string;
  endTime: string;
  occurredAtLabel: string;
  detail: string;
};

type ContactActivityPathDashboard = {
  rangeStartDate: string;
  rangeEndDate: string;
  contact: ContactActivityPathContact;
  overview: ContactActivityPathOverview;
  items: ContactActivityPathItem[];
};

interface ContactActivityPathClientProperties {
  stats: ContactActivityPathDashboard;
  title: string;
  description: string;
}

type ChartItem = {
  category: string;
  start: number;
  end: number;
  label: string;
  detail: string;
  tooltipText: string;
  fill: string;
  iconSrc: string;
};

export const ContactActivityPathLayoutTemplate = (
  props: ContactActivityPathClientProperties,
) => {
  const [dashboard, setDashboard] = useState<ContactActivityPathDashboard>(
    props.stats,
  );
  const [selectedRange, setSelectedRange] = useState<DateRangeValue>({
    from: parseDateOnlyString(props.stats.rangeStartDate),
    to: parseDateOnlyString(props.stats.rangeEndDate),
  });
  const [isLoading, setIsLoading] = useState(false);

  const appliedRange = {
    from: parseDateOnlyString(dashboard.rangeStartDate),
    to: parseDateOnlyString(dashboard.rangeEndDate),
  };

  const { execute: loadData } = usePageCommand<
    ContactActivityPathDashboard,
    ContactActivityPathQuery
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
      }

      setIsLoading(false);
    },
  });

  async function applySelectedRange() {
    await loadData({
      rangeStart: selectedRange.from,
      rangeEnd: selectedRange.to,
    });
  }

  const hasPendingRangeChange = !areRangesEqual(selectedRange, appliedRange);

  return (
    <div className="contact-activity-path-dashboard space-y-6 pb-8">
      <section className="grid gap-6 xl:grid-cols-[minmax(0,2fr)_minmax(320px,1fr)]">
        <Card className="contact-activity-path-surface border-0 shadow-lg">
          <CardHeader className="space-y-4">
            <div className="space-y-2">
              <CardTitle className="text-4xl font-semibold tracking-tight">
                {props.title}
              </CardTitle>
              <CardDescription className="max-w-3xl text-base text-[var(--color-text-low-emphasis)]">
                {props.description}
              </CardDescription>
            </div>

            <div className="rounded-[var(--radius-l)] border border-dashed border-[var(--color-border-default)] bg-[var(--color-background-highlighted)] p-4">
              <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-text-hint)]">
                Contact
              </p>
              <p className="mt-3 text-2xl font-semibold text-[var(--color-text-default-on-light)]">
                {dashboard.contact.displayName}
              </p>
              <p className="mt-2 text-sm text-[var(--color-text-low-emphasis)]">
                {dashboard.contact.email ||
                  'No email address is stored for this contact.'}
              </p>
            </div>

            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
              <OverviewStatCard
                label="Activities"
                value={dashboard.overview.totalActivities}
                caption={getRangeCaption(dashboard)}
              />
              <OverviewStatCard
                label="Activity types"
                value={dashboard.overview.activityTypesTracked}
                caption="Distinct tracked activity types in range"
              />
              <OverviewStatCard
                label="Active days"
                value={dashboard.overview.activeDays}
                caption={dashboard.overview.firstActivityAt}
              />
              <OverviewStatCard
                label="Latest activity"
                value={dashboard.items.length}
                caption={dashboard.overview.lastActivityAt}
              />
            </div>
          </CardHeader>
        </Card>

        <Card className="contact-activity-path-surface border-0 shadow-lg">
          <CardContent className="space-y-4 pt-5 text-sm text-[var(--color-text-low-emphasis)]">
            <div className="rounded-[var(--radius-l)] border border-[var(--color-border-low-emphasis)] bg-[var(--color-paper-background)] p-4 shadow-[var(--shadow-xs)]">
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
                    if (value) {
                      setSelectedRange(value);
                    }
                  }}
                />
                <Button
                  className="w-full"
                  disabled={!hasPendingRangeChange || isLoading}
                  inProgress={isLoading}
                  label="Update path"
                  onClick={() => {
                    void applySelectedRange();
                  }}
                  type="button"
                />
              </div>
            </div>

            <div className="rounded-[var(--radius-l)] border border-dashed border-[var(--color-border-default)] bg-[var(--color-background-highlighted)] p-4">
              <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-text-hint)]">
                Reading guide
              </p>
              <p className="mt-2 text-sm leading-6 text-[var(--color-text-low-emphasis)]">
                The path diagram lays activity blocks across time and groups
                them by day so you can see where a contact clustered actions,
                paused, or returned later in the selected window.
              </p>
            </div>
          </CardContent>
        </Card>
      </section>

      <section className="grid gap-6 xl:grid-cols-[minmax(0,1.8fr)_minmax(340px,1fr)]">
        <Card className="contact-activity-path-surface border-0 shadow-lg">
          <CardHeader className="space-y-2">
            <CardDescription className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-text-hint)]">
              Linear process diagram
            </CardDescription>
            <CardTitle className="text-2xl">
              Tracked journey through time
            </CardTitle>
            <CardDescription className="text-sm text-[var(--color-text-low-emphasis)]">
              Each block represents one recorded activity, positioned in
              sequence across the selected range.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <ContactActivityPathChart items={dashboard.items} />
          </CardContent>
        </Card>

        <Card className="contact-activity-path-surface border-0 shadow-lg">
          <CardHeader className="space-y-2 pb-3">
            <CardDescription className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-text-hint)]">
              Sequence
            </CardDescription>
            <CardTitle className="text-2xl">Recorded activity list</CardTitle>
          </CardHeader>
          <CardContent>
            {dashboard.items.length === 0 ? (
              <EmptyState message="No tracked contact activity was found in the selected range." />
            ) : (
              <div className="contact-activity-path-scroll">
                <div className="contact-activity-path-table-shell overflow-hidden rounded-[var(--radius-l)] border border-[var(--color-border-low-emphasis)] bg-[var(--color-paper-background)] shadow-[var(--shadow-xs)]">
                  <table className="contact-activity-path-table w-full border-collapse text-left">
                    <thead>
                      <tr>
                        <th scope="col">Time</th>
                        <th scope="col">Activity</th>
                        <th scope="col">Detail</th>
                      </tr>
                    </thead>
                    <tbody>
                      {dashboard.items.map((item) => (
                        <tr key={item.key}>
                          <td className="whitespace-nowrap text-sm font-medium text-[var(--color-text-default-on-light)]">
                            {item.occurredAtLabel}
                          </td>
                          <td>
                            <div className="font-semibold text-[var(--color-text-default-on-light)]">
                              {item.activityTypeLabel}
                            </div>
                            <div className="mt-1 text-xs text-[var(--color-text-hint)]">
                              {item.dayLabel}
                            </div>
                          </td>
                          <td className="text-sm text-[var(--color-text-low-emphasis)]">
                            {item.detail}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}
          </CardContent>
        </Card>
      </section>
    </div>
  );
};

const OverviewStatCard = (props: {
  label: string;
  value: number;
  caption: string;
}) => {
  return (
    <div className="rounded-[calc(var(--radius-l)+8px)] border border-[var(--color-border-low-emphasis)] bg-[var(--color-paper-background)] p-5 shadow-[var(--shadow-xs)]">
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

const ContactActivityPathChart = (props: {
  items: ContactActivityPathItem[];
}) => {
  const chartId = useId().replace(/:/g, '-');

  useLayoutEffect(() => {
    if (props.items.length === 0) {
      return;
    }

    const root = Am5.Root.new(chartId);
    root.setThemes([Am5themesAnimated.new(root)]);
    root.numberFormatter.setAll({ numberFormat: '#,###' });

    const chartTokens = getChartTokens();
    const palette = getChartPalette();
    const chartItems = props.items.map((item, index) => ({
      category: index % 3 === 1 ? 'b' : 'a',
      start: new Date(item.startTime).getTime(),
      end: new Date(item.endTime).getTime(),
      label: item.activityTypeLabel,
      detail: item.detail,
      tooltipText: `${item.activityTypeLabel}\n${item.occurredAtLabel}\n${item.detail}`,
      fill: palette[index % palette.length],
      iconSrc: getActivityIconDataUri(item.activityTypeKey),
    }));
    const rangeStart = Math.min(...chartItems.map((item) => item.start));
    const rangeEnd = Math.max(...chartItems.map((item) => item.end));
    const zoomPadding = Math.max(
      (rangeEnd - rangeStart) * 0.08,
      1000 * 60 * 60 * 6,
    );

    const chart = root.container.children.push(
      Am5Timeline.CurveChart.new(root, {
        wheelY: 'zoomX',
        pinchZoomX: true,
      }),
    );

    const scrollbarX = chart.set(
      'scrollbarX',
      Am5.Scrollbar.new(root, {
        orientation: 'vertical',
        height: Am5.percent(24),
        position: 'absolute',
        x: Am5.percent(100),
        centerX: Am5.p100,
        dx: -12,
        y: 28,
        centerY: Am5.p0,
      }),
    );
    chart.plotContainer.children.push(scrollbarX);

    const yRenderer = Am5Timeline.AxisRendererCurveY.new(root, {
      axisLocation: -0.12,
    });
    yRenderer.labels.template.setAll({
      forceHidden: true,
    });
    yRenderer.grid.template.set('forceHidden', true);

    const xRenderer = Am5Timeline.AxisRendererCurveX.new(root, {
      points: getWrappedTimelinePoints(),
      yRenderer,
      strokeOpacity: 1,
      strokeWidth: 5,
      stroke: Am5.color(chartTokens.pathStroke),
    });
    xRenderer.labels.template.setAll({
      centerY: Am5.p50,
      fontSize: 11,
      fill: Am5.color(chartTokens.textLow),
      minPosition: 0.01,
    });
    xRenderer.grid.template.setAll({
      strokeOpacity: 0,
    });
    xRenderer.labels.template.setup = (target) => {
      target.set('layer', 30);
      target.set(
        'background',
        Am5.Rectangle.new(root, {
          fill: Am5.color(0xffffff),
          fillOpacity: 0.96,
        }),
      );
    };

    const yAxis = chart.yAxes.push(
      Am5XY.CategoryAxis.new(root, {
        maxDeviation: 0,
        categoryField: 'category',
        renderer: yRenderer,
      }),
    );
    yAxis.data.setAll([{ category: 'a' }, { category: 'b' }]);

    const xAxis = chart.xAxes.push(
      Am5XY.DateAxis.new(root, {
        baseInterval: { timeUnit: 'minute', count: 1 },
        renderer: xRenderer,
        tooltip: Am5.Tooltip.new(root, {}),
      }),
    );

    const series = chart.series.push(
      Am5Timeline.CurveColumnSeries.new(root, {
        xAxis,
        yAxis,
        baseAxis: yAxis,
        categoryYField: 'category',
        openValueXField: 'start',
        valueXField: 'end',
        maskBullets: false,
      }),
    );

    series.columns.template.setAll({
      height: Am5.percent(20),
      strokeOpacity: 0,
    });
    series.columns.template.adapters.add('fill', (_fill, target) => {
      const dataItem = target.dataItem?.dataContext as ChartItem | undefined;
      return dataItem
        ? Am5.color(dataItem.fill)
        : Am5.color(chartTokens.product);
    });
    series.columns.template.adapters.add('stroke', (_stroke, target) => {
      const dataItem = target.dataItem?.dataContext as ChartItem | undefined;
      return dataItem
        ? Am5.color(dataItem.fill)
        : Am5.color(chartTokens.product);
    });

    series.bullets.push((bulletRoot, _series, dataItem) => {
      const context = dataItem.dataContext as ChartItem;
      const container = Am5.Container.new(bulletRoot, {
        centerY: Am5.p100,
      });

      container.children.push(
        Am5.PointedRectangle.new(bulletRoot, {
          width: 42,
          height: 38,
          cornerRadius: 10,
          pointerBaseWidth: 12,
          pointerX: 21,
          pointerY: 42,
          centerX: Am5.p50,
          centerY: Am5.p100,
          fill: Am5.color(context.fill),
          strokeOpacity: 0,
          shadowColor: Am5.color(0x000000),
          shadowBlur: 5,
          shadowOffsetX: 1,
          shadowOffsetY: 3,
          tooltipText: context.tooltipText,
          tooltipY: 0,
        }),
      );

      container.children.push(
        Am5.Picture.new(bulletRoot, {
          centerX: Am5.p50,
          centerY: 31,
          width: 18,
          height: 18,
          src: context.iconSrc,
        }),
      );

      return Am5.Bullet.new(bulletRoot, {
        sprite: container,
      });
    });

    series.data.setAll(chartItems);
    xAxis.zoomToDates(
      new Date(rangeStart - zoomPadding),
      new Date(rangeEnd + zoomPadding),
      0,
    );

    void series.appear(700);
    void chart.appear(700, 100);

    return () => {
      root.dispose();
    };
  }, [chartId, props.items]);

  if (props.items.length === 0) {
    return (
      <EmptyState message="No tracked contact activity was found in the selected range." />
    );
  }

  return (
    <div
      id={chartId}
      className="contact-activity-path-chart contact-activity-path-chart-lg"
    />
  );
};

const EmptyState = (props: { message: string }) => {
  return (
    <div className="rounded-[var(--radius-l)] border border-dashed border-[var(--color-border-default)] bg-[var(--color-background-highlighted)] px-4 py-6 text-sm text-[var(--color-text-low-emphasis)]">
      {props.message}
    </div>
  );
};

function formatNumber(value: number) {
  return new Intl.NumberFormat().format(value);
}

function parseDateOnlyString(value: string) {
  const [year, month, day] = value.split('-').map((part) => parseInt(part, 10));
  return new Date(year, month - 1, day);
}

function areRangesEqual(left: DateRangeValue, right: DateRangeValue) {
  return (
    left.from.getTime() === right.from.getTime() &&
    left.to.getTime() === right.to.getTime()
  );
}

function getRangeCaption(dashboard: ContactActivityPathDashboard) {
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

function getWrappedTimelinePoints() {
  const points = [
    { x: -360, y: 180 },
    { x: 0, y: 180 },
  ];
  const width = 620;
  const height = 440;
  const levelCount = 4;
  const radius = Math.min(width / (levelCount - 1) / 2, height / 2.5);
  const startX = radius;

  for (let index = 0; index < 24; index += 1) {
    const angle = (index / 24) * 90;
    const centerPoint = { y: 180 - radius, x: 0 };

    points.push({
      y: centerPoint.y + radius * Am5.math.cos(angle),
      x: centerPoint.x + radius * Am5.math.sin(angle),
    });
  }

  for (let level = 0; level < levelCount; level += 1) {
    if (level % 2 === 0) {
      points.push({
        y: height / 2 - radius,
        x: startX + (width / (levelCount - 1)) * level,
      });
      points.push({
        y: -height / 2 + radius,
        x: startX + (width / (levelCount - 1)) * level,
      });

      const centerPoint = {
        y: -height / 2 + radius,
        x: startX + (width / (levelCount - 1)) * (level + 0.5),
      };

      if (level < levelCount - 1) {
        for (let step = 0; step < 44; step += 1) {
          const angle = -90 - (step / 44) * 180;
          points.push({
            y: centerPoint.y + radius * Am5.math.cos(angle),
            x: centerPoint.x + radius * Am5.math.sin(angle),
          });
        }
      }
    } else {
      points.push({
        y: -height / 2 + radius,
        x: startX + (width / (levelCount - 1)) * level,
      });
      points.push({
        y: height / 2 - radius,
        x: startX + (width / (levelCount - 1)) * level,
      });

      const centerPoint = {
        y: height / 2 - radius,
        x: startX + (width / (levelCount - 1)) * (level + 0.5),
      };

      if (level < levelCount - 1) {
        for (let step = 0; step < 44; step += 1) {
          const angle = -90 + (step / 44) * 180;
          points.push({
            y: centerPoint.y + radius * Am5.math.cos(angle),
            x: centerPoint.x + radius * Am5.math.sin(angle),
          });
        }
      }

      if (level === levelCount - 1) {
        points.pop();
        points.push({
          y: -radius,
          x: startX + (width / (levelCount - 1)) * level,
        });

        const exitCenterPoint = {
          y: -radius,
          x: startX + (width / (levelCount - 1)) * (level + 0.5),
        };

        for (let step = 0; step < 24; step += 1) {
          const angle = -90 + (step / 24) * 90;
          points.push({
            y: exitCenterPoint.y + radius * Am5.math.cos(angle),
            x: exitCenterPoint.x + radius * Am5.math.sin(angle),
          });
        }

        points.push({ y: 0, x: 1760 });
      }
    }
  }

  return points;
}

function getActivityIconDataUri(activityTypeKey: string) {
  const normalized = activityTypeKey.trim().toLowerCase();

  const iconMarkup =
    activityIconMarkup[normalized] ?? activityIconMarkup.default;

  return `data:image/svg+xml;utf8,${encodeURIComponent(`
    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none">
      ${iconMarkup}
    </svg>
  `)}`;
}

const activityIconMarkup: Record<string, string> = {
  landingpage: `
    <path d="M4 12h9" stroke="white" stroke-width="1.8" stroke-linecap="round"/>
    <path d="m10 8 4 4-4 4" stroke="white" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/>
    <path d="M16 5h4v14h-4" stroke="white" stroke-width="1.8" stroke-linejoin="round"/>
  `,
  pagevisit: `
    <path d="M2.5 12s3.4-5 9.5-5 9.5 5 9.5 5-3.4 5-9.5 5-9.5-5-9.5-5Z" stroke="white" stroke-width="1.8" stroke-linejoin="round"/>
    <circle cx="12" cy="12" r="2.8" stroke="white" stroke-width="1.8"/>
  `,
  click: `
    <path d="m7 4 8 8-4 1 2 6-2.5 1-2-6-3 3Z" stroke="white" stroke-width="1.8" stroke-linejoin="round"/>
    <path d="M16.5 4.5v3" stroke="white" stroke-width="1.8" stroke-linecap="round"/>
    <path d="M20 8h-3" stroke="white" stroke-width="1.8" stroke-linecap="round"/>
  `,
  emailclick: `
    <rect x="3" y="5" width="18" height="14" rx="2" stroke="white" stroke-width="1.8"/>
    <path d="m5 7 7 5 7-5" stroke="white" stroke-width="1.8" stroke-linejoin="round"/>
    <path d="m14 15 5 0" stroke="white" stroke-width="1.8" stroke-linecap="round"/>
  `,
  bizformsubmit: `
    <rect x="5" y="3" width="14" height="18" rx="2" stroke="white" stroke-width="1.8"/>
    <path d="M8.5 8.5h7" stroke="white" stroke-width="1.8" stroke-linecap="round"/>
    <path d="M8.5 12h7" stroke="white" stroke-width="1.8" stroke-linecap="round"/>
    <path d="m8 16 2 2 5-5" stroke="white" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/>
  `,
  datainput: `
    <rect x="4" y="5" width="16" height="14" rx="2" stroke="white" stroke-width="1.8"/>
    <path d="M7.5 9h6" stroke="white" stroke-width="1.8" stroke-linecap="round"/>
    <path d="M7.5 13h9" stroke="white" stroke-width="1.8" stroke-linecap="round"/>
    <path d="M16.5 18.5 20 22" stroke="white" stroke-width="1.8" stroke-linecap="round"/>
  `,
  memberregistration: `
    <circle cx="12" cy="8" r="3" stroke="white" stroke-width="1.8"/>
    <path d="M6 19c1.5-3 4-4.5 6-4.5s4.5 1.5 6 4.5" stroke="white" stroke-width="1.8" stroke-linecap="round"/>
    <path d="M18 6v5" stroke="white" stroke-width="1.8" stroke-linecap="round"/>
    <path d="M15.5 8.5H20.5" stroke="white" stroke-width="1.8" stroke-linecap="round"/>
  `,
  chat: `
    <path d="M5 6.5h14a2 2 0 0 1 2 2v7a2 2 0 0 1-2 2H11l-4 3v-3H5a2 2 0 0 1-2-2v-7a2 2 0 0 1 2-2Z" stroke="white" stroke-width="1.8" stroke-linejoin="round"/>
    <path d="M8 11h8" stroke="white" stroke-width="1.8" stroke-linecap="round"/>
  `,
  default: `
    <circle cx="12" cy="12" r="7" stroke="white" stroke-width="1.8"/>
    <path d="M12 8v4l2.5 2.5" stroke="white" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/>
  `,
};

function getChartTokens() {
  return {
    textLow: getCssToken('--color-text-low-emphasis', '#525252'),
    borderLow: getCssToken('--color-border-low-emphasis', '#dfdfdf'),
    product: getCssToken('--color-product', '#7f09b7'),
    pathStroke: getCssToken('--color-border-default', '#b8b8b8'),
  };
}

function getChartPalette() {
  return [
    getCssToken('--color-product', '#7f09b7'),
    getCssToken('--color-info-background-high-emphasis', '#3d5dff'),
    getCssToken('--color-success-background-high-emphasis', '#007d72'),
    getCssToken('--color-warning-background-high-emphasis', '#db9d00'),
    getCssToken('--color-alert-background-high-emphasis', '#e10007'),
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
