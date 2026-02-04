import { usePageCommand } from '@kentico/xperience-admin-base';
import {
  Box,
  Colors,
  Headline,
  HeadlineSize,
  LabelWithTooltip,
  MenuItem,
  Select,
  Spacing,
  Stack,
  TextWithLabel,
} from '@kentico/xperience-admin-components';
import React, { useState } from 'react';
import { BarChart, TimeSeriesEntry } from './BarChart';

type StatsTotal = {
  label: string;
  value: number;
};

type StatsDatum = {
  label: string;
  dataEntries: TimeSeriesEntry[];
};

type StatsData = {
  data: StatsDatum[];
  totals: StatsTotal[];
  totalsStartDate?: string;
};

interface StatsClientProperties {
  stats: StatsData;
  totalsTitle: string;
  allowedSelectRange: number[];
  defaultSelectedRange: number;
}

export const StatsLayoutTemplate = (props: StatsClientProperties) => {
  const [statsData, setStatsData] = useState({
    ...(props.stats ?? { data: [], totals: [], totalsStartDate: undefined }),
  });
  const [selectedRange, setSelectedRange] = useState(
    props.defaultSelectedRange,
  );

  const totalsDate = props.stats.totalsStartDate
    ? new Date(props.stats.totalsStartDate).toLocaleDateString()
    : '';

  const { execute: loadData } = usePageCommand<StatsData, number>('LOADDATA', {
    after(commandResult) {
      if (commandResult) {
        setStatsData(commandResult);
      }
    },
  });

  async function onSelect(e: string | undefined) {
    if (!e) {
      return;
    }

    setSelectedRange(e);

    await loadData(parseInt(e, 10));
  }

  return (
    <Stack spacing={Spacing.L}>
      <Box spacingY={Spacing.XS}>
        <LabelWithTooltip
          label="Time range (months)"
          tooltipText="The totals and charts below will update to reflect the selected time range"
        />
      </Box>
      <Box spacingY={Spacing.S}>
        <Select placeholder="Select a time frame" onChange={(e) => onSelect(e)}>
          {props.allowedSelectRange.map((i) => (
            <MenuItem
              key={i}
              primaryLabel={`${i} months`}
              value={i}
              selected={i === selectedRange}
            />
          ))}
        </Select>
      </Box>
      <Box spacingY={Spacing.L}>
        <h1 style={{ margin: '0 0 1rem 0' }}>
          <Headline
            size={HeadlineSize.M}
            labelColor={Colors.TextDefaultOnLight}
          >
            {props.totalsTitle}{' '}
            {totalsDate ? `(since ${totalsDate})` : '(all time)'}
          </Headline>
        </h1>

        <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          {statsData.totals.map((t) => (
            <TextWithLabel key={t.label} label={t.label} value={t.value} />
          ))}
        </div>
      </Box>

      {statsData.data.map((d) => {
        const id = toDashCase(d.label);
        return (
          <Box key={id}>
            <BarChart id={id} data={d.dataEntries} chartName={d.label} />
          </Box>
        );
      })}
    </Stack>
  );
};

function toDashCase(str: string) {
  return str
    .replace(/([a-z])([A-Z])/g, '$1-$2') // Add a dash between lowercase and uppercase letters
    .replace(/\s+/g, '-') // Replace spaces with dashes
    .replace(/_/g, '-') // Replace underscores with dashes
    .toLowerCase(); // Convert the entire string to lowercase
}
