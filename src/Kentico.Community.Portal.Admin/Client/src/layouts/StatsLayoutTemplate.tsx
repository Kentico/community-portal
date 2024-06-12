import React from 'react';
import { BarChart, TimeSeriesEntry } from './BarChart';

interface StatsClientProperties {
  members: TimeSeriesEntry[];
  subscribers: TimeSeriesEntry[];
  questions: TimeSeriesEntry[];
  answers: TimeSeriesEntry[];
}

export const StatsLayoutTemplate = (props: StatsClientProperties) => {
  return (
    <div style={{ padding: '3rem' }}>
      <BarChart id="members" data={props.members} chartName="Members Joined" />

      <BarChart
        id="subscribers"
        data={props.subscribers}
        chartName="Newsletter Subscribers"
      />

      <BarChart
        id="questions"
        data={props.questions}
        chartName="Q&A Questions"
      />

      <BarChart id="answers" data={props.answers} chartName="Q&A Answers" />
    </div>
  );
};
