import * as Am5 from '@amcharts/amcharts5';
import Am5themesAnimated from '@amcharts/amcharts5/themes/Animated';
import * as Am5XY from '@amcharts/amcharts5/xy';
import React, {
  useDeferredValue,
  useId,
  useLayoutEffect,
  useState,
} from 'react';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '../../components/ui/card';
import { Input } from '../../components/ui/input';
import './membership-stats.css';

type MemberModerationStatusCount = {
  key: string;
  label: string;
  value: number;
};

type MemberEmailDomainCount = {
  domain: string;
  count: number;
};

type MemberContributionLeader = {
  memberId: number;
  displayName: string;
  email: string;
  count: number;
  editUrl: string;
  viewUrl: string;
};

type MemberContributionLeaderboard = {
  key: string;
  label: string;
  members: MemberContributionLeader[];
};

type MembershipStatsOverview = {
  enabledMembers: number;
  moderatedMembers: number;
  uniqueEnabledEmailDomains: number;
  uniqueModeratedEmailDomains: number;
};

type MembershipStatsDashboard = {
  overview: MembershipStatsOverview;
  moderationStatuses: MemberModerationStatusCount[];
  enabledEmailDomains: MemberEmailDomainCount[];
  moderatedEmailDomains: MemberEmailDomainCount[];
  leaderboards: MemberContributionLeaderboard[];
};

interface MembershipStatsClientProperties {
  stats: MembershipStatsDashboard;
  title: string;
  description: string;
}

type RankedChartDatum = {
  label: string;
  value: number;
};

export const MembershipStatsLayoutTemplate = (
  props: MembershipStatsClientProperties,
) => {
  const stats = props.stats;

  return (
    <div className="membership-stats-dashboard space-y-6 pb-8">
      <section className="grid gap-6 xl:grid-cols-[minmax(0,2fr)_minmax(320px,1fr)]">
        <Card className="membership-stats-surface border-0 shadow-lg">
          <CardHeader className="space-y-4">
            <div className="space-y-2">
              <CardTitle className="text-4xl font-semibold tracking-tight">
                {props.title}
              </CardTitle>
              <CardDescription className="max-w-2xl text-base text-[var(--color-text-low-emphasis)]">
                {props.description}
              </CardDescription>
            </div>

            <div className="grid gap-4 md:grid-cols-2">
              <OverviewStatCard
                label="Enabled members"
                value={stats.overview.enabledMembers}
                caption="Current enabled member accounts with a valid email domain."
              />
              <OverviewStatCard
                label="Moderated members"
                value={stats.overview.moderatedMembers}
                caption="Disabled members currently marked spam, flagged, or archived."
              />
              <OverviewStatCard
                label="Enabled email domains"
                value={stats.overview.uniqueEnabledEmailDomains}
                caption="Distinct email domains across enabled members."
              />
              <OverviewStatCard
                label="Moderated email domains"
                value={stats.overview.uniqueModeratedEmailDomains}
                caption="Distinct domains represented among moderated disabled members."
              />
            </div>
          </CardHeader>
        </Card>

        <Card className="membership-stats-surface border-0 shadow-lg">
          <CardHeader className="space-y-2 pb-3">
            <CardDescription className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-text-hint)]">
              Moderation snapshot
            </CardDescription>
            <CardTitle className="text-2xl">
              Active moderation statuses
            </CardTitle>
            <CardDescription className="text-sm text-[var(--color-text-low-emphasis)]">
              Counts reflect disabled member accounts only, grouped by the
              current moderation flag.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            {stats.moderationStatuses.map((item) => (
              <div
                key={item.key}
                className="rounded-[var(--radius-l)] border border-[var(--color-border-low-emphasis)] bg-[var(--color-paper-background)] px-4 py-4 shadow-[var(--shadow-xs)]"
              >
                <div className="flex items-center justify-between gap-4">
                  <span className="text-sm font-medium text-[var(--color-text-low-emphasis)]">
                    {item.label}
                  </span>
                  <span className="text-2xl font-semibold text-[var(--color-text-default-on-light)]">
                    {formatNumber(item.value)}
                  </span>
                </div>
              </div>
            ))}
          </CardContent>
        </Card>
      </section>

      <section className="grid gap-6 xl:grid-cols-2">
        <SearchableDomainDistributionCard
          title="Unique enabled member email domains"
          description="Every distinct domain found across enabled member accounts, ordered by volume."
          items={stats.enabledEmailDomains}
          emptyMessage="No enabled member email domains were found."
        />
        <DomainDistributionCard
          title="Unique moderated member email domains"
          description="Distinct domains represented among disabled members with a moderation flag."
          items={stats.moderatedEmailDomains}
          emptyMessage="No moderated member email domains were found."
        />
      </section>

      <section className="grid gap-6 2xl:grid-cols-2">
        {stats.leaderboards.map((leaderboard) => (
          <LeaderboardCard key={leaderboard.key} leaderboard={leaderboard} />
        ))}
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

const DomainDistributionCard = (props: {
  title: string;
  description: string;
  items: MemberEmailDomainCount[];
  emptyMessage: string;
}) => {
  const chartData = props.items.slice(0, 10).map((item) => ({
    label: item.domain,
    value: item.count,
  }));

  return (
    <Card className="membership-stats-surface border-0 shadow-lg">
      <CardHeader className="space-y-2">
        <CardTitle className="text-2xl">{props.title}</CardTitle>
        <CardDescription className="text-sm text-[var(--color-text-low-emphasis)]">
          {props.description}
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-5">
        <RankedHorizontalBarChart
          data={chartData}
          emptyMessage={props.emptyMessage}
        />
        <div className="membership-stats-scroll space-y-3">
          {props.items.length === 0 ? (
            <EmptyState message={props.emptyMessage} />
          ) : (
            props.items.map((item) => (
              <div
                key={item.domain}
                className="rounded-[var(--radius-l)] border border-[var(--color-border-low-emphasis)] bg-[var(--color-paper-background)] px-4 py-3 shadow-[var(--shadow-xs)]"
              >
                <div className="flex items-center justify-between gap-4">
                  <span className="font-medium text-[var(--color-text-default-on-light)]">
                    {item.domain}
                  </span>
                  <span className="text-sm font-semibold text-[var(--color-text-hint)]">
                    {formatNumber(item.count)}
                  </span>
                </div>
              </div>
            ))
          )}
        </div>
      </CardContent>
    </Card>
  );
};

const SearchableDomainDistributionCard = (props: {
  title: string;
  description: string;
  items: MemberEmailDomainCount[];
  emptyMessage: string;
}) => {
  const [searchText, setSearchText] = useState('');
  const deferredSearchText = useDeferredValue(searchText);
  const normalizedSearchText = deferredSearchText.trim().toLowerCase();
  const filteredItems = normalizedSearchText
    ? props.items.filter((item) =>
        item.domain.toLowerCase().includes(normalizedSearchText),
      )
    : props.items;
  const chartData = props.items.slice(0, 10).map((item) => ({
    label: item.domain,
    value: item.count,
  }));
  const hasResults = filteredItems.length > 0;

  return (
    <Card className="membership-stats-surface border-0 shadow-lg">
      <CardHeader className="space-y-2">
        <CardTitle className="text-2xl">{props.title}</CardTitle>
        <CardDescription className="text-sm text-[var(--color-text-low-emphasis)]">
          {props.description}
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-5">
        <RankedHorizontalBarChart
          data={chartData}
          emptyMessage={props.emptyMessage}
        />
        <div className="membership-stats-toolbar">
          <Input
            aria-label="Filter enabled email domains"
            className="membership-stats-search"
            onChange={(event) => setSearchText(event.target.value)}
            placeholder="Search domains"
            type="search"
            value={searchText}
          />
          <p className="membership-stats-results text-sm text-[var(--color-text-low-emphasis)]">
            {formatNumber(filteredItems.length)} result
            {filteredItems.length === 1 ? '' : 's'}
          </p>
        </div>
        {props.items.length === 0 ? (
          <EmptyState message={props.emptyMessage} />
        ) : !hasResults ? (
          <EmptyState message="No enabled member email domains match the current search." />
        ) : (
          <div className="membership-stats-scroll membership-stats-table-scroll">
            <div className="membership-stats-table-shell overflow-hidden rounded-[var(--radius-l)] border border-[var(--color-border-low-emphasis)] bg-[var(--color-paper-background)] shadow-[var(--shadow-xs)]">
              <table className="membership-stats-table w-full border-collapse text-left">
                <thead>
                  <tr>
                    <th scope="col">Domain</th>
                    <th scope="col" className="text-right">
                      Members
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {filteredItems.map((item) => (
                    <tr key={item.domain}>
                      <td className="font-medium text-[var(--color-text-default-on-light)]">
                        {item.domain}
                      </td>
                      <td className="text-right text-sm font-semibold text-[var(--color-text-default-on-light)]">
                        {formatNumber(item.count)}
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
  );
};

const LeaderboardCard = (props: {
  leaderboard: MemberContributionLeaderboard;
}) => {
  const chartData = props.leaderboard.members.slice(0, 10).map((item) => ({
    label: item.displayName,
    value: item.count,
  }));

  return (
    <Card className="membership-stats-surface border-0 shadow-lg">
      <CardHeader className="space-y-2">
        <CardTitle className="text-2xl">{props.leaderboard.label}</CardTitle>
        <CardDescription className="text-sm text-[var(--color-text-low-emphasis)]">
          Ranked enabled members by total associated records.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-5">
        <RankedHorizontalBarChart
          data={chartData}
          emptyMessage="No member associations were found for this content type."
        />
        {props.leaderboard.members.length === 0 ? (
          <EmptyState message="No member associations were found for this content type." />
        ) : (
          <div className="membership-stats-scroll membership-stats-table-scroll">
            <div className="membership-stats-table-shell overflow-hidden rounded-[var(--radius-l)] border border-[var(--color-border-low-emphasis)] bg-[var(--color-paper-background)] shadow-[var(--shadow-xs)]">
              <table className="membership-stats-table w-full border-collapse text-left">
                <thead>
                  <tr>
                    <th scope="col">Member</th>
                    <th scope="col">Count</th>
                    <th scope="col" className="w-[96px] text-right">
                      Actions
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {props.leaderboard.members.map((item, index) => (
                    <tr key={`${props.leaderboard.key}-${item.memberId}`}>
                      <td>
                        <a
                          href={item.editUrl}
                          className="membership-stats-member-link"
                        >
                          <span className="membership-stats-rank">
                            {index + 1}.
                          </span>{' '}
                          <span>{item.displayName}</span>
                        </a>
                        <div className="mt-1 text-sm text-[var(--color-text-low-emphasis)]">
                          {item.email}
                        </div>
                      </td>
                      <td className="text-sm font-semibold text-[var(--color-text-default-on-light)]">
                        {formatNumber(item.count)}
                      </td>
                      <td>
                        <div className="flex items-center justify-end gap-2">
                          <ActionIconLink
                            href={item.editUrl}
                            label={`Edit ${item.displayName}`}
                            icon="edit"
                          />
                          <ActionIconLink
                            href={item.viewUrl}
                            label={`View ${item.displayName}`}
                            icon="view"
                          />
                        </div>
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
  );
};

const ActionIconLink = (props: {
  href: string;
  label: string;
  icon: 'edit' | 'view';
}) => {
  return (
    <a
      href={props.href}
      aria-label={props.label}
      className="membership-stats-icon-link"
      target={props.icon === 'view' ? '_blank' : undefined}
      rel={props.icon === 'view' ? 'noreferrer' : undefined}
      title={props.label}
    >
      {props.icon === 'edit' ? <EditIcon /> : <ViewIcon />}
    </a>
  );
};

const EditIcon = () => {
  return (
    <svg viewBox="0 0 20 20" fill="none" aria-hidden="true">
      <path
        d="M13.958 3.542a1.768 1.768 0 0 1 2.5 2.5L7.5 15H5v-2.5l8.958-8.958Z"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path
        d="M11.875 5.625 14.375 8.125"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
};

const ViewIcon = () => {
  return (
    <svg viewBox="0 0 20 20" fill="none" aria-hidden="true">
      <path
        d="M2.5 10s2.727-4.167 7.5-4.167S17.5 10 17.5 10s-2.727 4.167-7.5 4.167S2.5 10 2.5 10Z"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <circle
        cx="10"
        cy="10"
        r="2.083"
        stroke="currentColor"
        strokeWidth="1.5"
      />
    </svg>
  );
};

const EmptyState = (props: { message: string }) => {
  return (
    <div className="rounded-[var(--radius-l)] border border-dashed border-[var(--color-border-default)] bg-[var(--color-background-highlighted)] px-4 py-6 text-sm text-[var(--color-text-low-emphasis)]">
      {props.message}
    </div>
  );
};

const RankedHorizontalBarChart = (props: {
  data: RankedChartDatum[];
  emptyMessage: string;
}) => {
  const chartId = useId().replace(/:/g, '-');

  useLayoutEffect(() => {
    if (props.data.length === 0) {
      return;
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
      maxWidth: 180,
    });
    yAxis.get('renderer').grid.template.setAll({ forceHidden: true });
    yAxis.data.setAll(props.data);

    const series = chart.series.push(
      Am5XY.ColumnSeries.new(root, {
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
    series.data.setAll(props.data);

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

  if (props.data.length === 0) {
    return <EmptyState message={props.emptyMessage} />;
  }

  return (
    <div
      id={chartId}
      className="membership-stats-chart membership-stats-chart-md"
    />
  );
};

function formatNumber(value: number) {
  return new Intl.NumberFormat().format(value);
}

function getChartTokens() {
  return {
    textLow: getCssToken('--color-text-low-emphasis', '#525252'),
    borderLow: getCssToken('--color-border-low-emphasis', '#dfdfdf'),
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
