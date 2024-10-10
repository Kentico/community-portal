import { usePageCommand } from '@kentico/xperience-admin-base';
import {
  Button,
  Colors,
  FormItemWrapper,
  Headline,
  HeadlineSize,
  Paper,
  Spacing,
  Stack,
} from '@kentico/xperience-admin-components';
import React, { useState } from 'react';

interface ContactMaintenanceClientProperties extends RefreshCountsResult {
  label: '';
  totalActivities: number;
  totalContacts: number;
}

interface DeleteContactsResult {
  contactsDeleted: number;
  activitiesDeleted: number;
  dateTo?: Date;
}

interface RefreshCountsResult {
  totalContacts: number;
  totalActivities: number;
}

interface ComponentState extends DeleteContactsResult, RefreshCountsResult {
  isLoading: boolean;
}

export const ContactMaintenanceLayoutTemplate = (
  props: ContactMaintenanceClientProperties,
) => {
  const [state, setState] = useState<ComponentState>({
    activitiesDeleted: 0,
    contactsDeleted: 0,
    dateTo: getHTML5DateStringFromDate(getDefaultDate()),
    isLoading: false,
    ...props,
  });
  const { execute: refreshCounts } = usePageCommand<RefreshCountsResult>(
    'REFRESH_COUNTS',
    {
      after: (resp) => {
        if (resp) {
          setState({ ...state, ...resp });
        }
      },
    },
  );
  const { execute: deleteContacts } = usePageCommand<DeleteContactsResult>(
    'DELETE_CONTACTS',
    {
      data: { dateTo: state.dateTo },
      before: () => {
        setState({ ...state, isLoading: true });
      },
      after: (resp) => {
        if (resp) {
          setState({
            ...state,
            activitiesDeleted: resp.activitiesDeleted,
            contactsDeleted: resp.contactsDeleted,
            dateTo: getHTML5DateStringFromDate(
              new Date(Date.parse(resp.dateTo)),
            ),
            isLoading: false,
          });

          refreshCounts().catch((_ex) => {});
        }
      },
    },
  );

  return (
    <div>
      <Headline size={HeadlineSize.L} labelColor={Colors.TextDefaultOnLight}>
        {props.label}
      </Headline>
      <Paper>
        <div style={{ padding: '1rem' }}>
          <div style={{ marginBlockEnd: '1rem' }}>
            <Headline
              size={HeadlineSize.M}
              labelColor={Colors.TextDefaultOnLight}
            >
              Totals
            </Headline>
            <table style={{ marginBlockEnd: '1rem', color: 'InfoText' }}>
              <thead>
                <tr>
                  <th>Contacts</th>
                  <th>Activities</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td>{state.totalContacts}</td>
                  <td>{state.totalActivities}</td>
                </tr>
              </tbody>
            </table>
          </div>
          <div style={{ marginBlockEnd: '1rem' }}>
            <form
              onSubmit={async (e) => {
                e.preventDefault();
                await deleteContacts({ dateTo: state.dateTo });
              }}
            >
              <Stack spacing={Spacing.M}>
                <FormItemWrapper label="Delete up to date">
                  <input
                    type="date"
                    name="toDate"
                    id="toDate"
                    value={state.dateTo}
                    onChange={(e) =>
                      setState({ ...state, dateTo: e.target.value })
                    }
                  />
                </FormItemWrapper>
                <Button
                  label="Delete some contacts!"
                  type="submit"
                  inProgress={state.isLoading}
                />
              </Stack>
            </form>
          </div>
          <div style={{ marginBlockEnd: '1rem' }}>
            <Headline
              size={HeadlineSize.M}
              labelColor={Colors.TextDefaultOnLight}
            >
              Deletion counts
            </Headline>
            <table style={{ marginBlockEnd: '1rem', color: 'InfoText' }}>
              <thead>
                <tr>
                  <th>Contacts</th>
                  <th>Activities</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td>{state.contactsDeleted}</td>
                  <td>{state.activitiesDeleted}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </Paper>
    </div>
  );
};

function getDefaultDate(): Date {
  return new Date(new Date(Date.now()).setMonth(-24));
}

/**
 * See: https://stackoverflow.com/a/58880605/939634
 * @returns
 */
function getHTML5DateStringFromDate(date: Date) {
  return (
    date.getFullYear().toString().padStart(4, '0') +
    '-' +
    (date.getMonth() + 1).toString().padStart(2, '0') +
    '-' +
    date.getDate().toString().padStart(2, '0')
  );
}
