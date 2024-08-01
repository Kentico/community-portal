import {
  Colors,
  Headline,
  HeadlineSize,
  Paper,
  TextWithLabel,
} from '@kentico/xperience-admin-components';
import React from 'react';
import { BarChart, TimeSeriesEntry } from './BarChart';

type StatsTotals = {
  enabledMembers: number;
  newsletterSubscribers: number;
  blogPosts: number;
  qAndAQuestions: number;
  qAndAAnswers: number;
};

interface StatsClientProperties {
  members: TimeSeriesEntry[];
  subscribers: TimeSeriesEntry[];
  blogPosts: TimeSeriesEntry[];
  questions: TimeSeriesEntry[];
  answers: TimeSeriesEntry[];
  totals: StatsTotals;
}

export const StatsLayoutTemplate = (props: StatsClientProperties) => {
  return (
    <div style={{ padding: '3rem' }}>
      <Paper>
        <div style={{ padding: '1rem' }}>
          <h1>
            <Headline
              size={HeadlineSize.M}
              labelColor={Colors.TextDefaultOnLight}
            >
              Totals
            </Headline>
          </h1>

          <div
            style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}
          >
            <TextWithLabel
              label="Enabled Members"
              value={props.totals.enabledMembers}
            />
            <TextWithLabel
              label="Newsletter Subscribers"
              value={props.totals.newsletterSubscribers}
            />
            <TextWithLabel
              label="Blog Posts"
              value={props.totals.blogPosts}
            />
            <TextWithLabel
              label="Q&A Questions"
              value={props.totals.qAndAQuestions}
            />
            <TextWithLabel
              label="Q&A Answers"
              value={props.totals.qAndAAnswers}
            />
          </div>
        </div>
      </Paper>

      <BarChart id="members" data={props.members} chartName="Members Joined" />

      <BarChart
        id="subscribers"
        data={props.subscribers}
        chartName="Newsletter Subscribers"
      />

      <BarChart
        id="blogPosts"
        data={props.blogPosts}
        chartName="Blog Posts"
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
