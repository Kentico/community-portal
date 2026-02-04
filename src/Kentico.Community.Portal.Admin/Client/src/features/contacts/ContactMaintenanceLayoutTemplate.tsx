import { usePageCommand } from '@kentico/xperience-admin-base';
import React, { useState } from 'react';
import { Button } from '../../components/ui/button';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '../../components/ui/card';
import { Input } from '../../components/ui/input';
import { Label } from '../../components/ui/label';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '../../components/ui/table';

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
    <div className="contact-maintenance-wrapper space-y-6 p-6 !text-foreground">
      <div className="space-y-2">
        <h1 className="!text-3xl !font-bold !tracking-tight !text-foreground !m-0">
          {props.label}
        </h1>
        <p className="!text-sm !text-muted-foreground !m-0">
          Manage contact data and activities
        </p>
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        <Card className="!bg-card !border-border">
          <CardHeader className="!pb-3">
            <CardTitle className="!text-xl !font-semibold !text-card-foreground !m-0">
              Current Totals
            </CardTitle>
            <CardDescription className="!text-sm !text-muted-foreground !m-0">
              Total number of contacts and activities in the system
            </CardDescription>
          </CardHeader>
          <CardContent className="!pt-0">
            <Table>
              <TableHeader>
                <TableRow className="!border-b !border-border">
                  <TableHead className="!text-muted-foreground !font-medium">
                    Contacts
                  </TableHead>
                  <TableHead className="!text-muted-foreground !font-medium">
                    Activities
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                <TableRow className="!border-b-0">
                  <TableCell className="!text-2xl !font-bold !text-foreground">
                    {state.totalContacts.toLocaleString()}
                  </TableCell>
                  <TableCell className="!text-2xl !font-bold !text-foreground">
                    {state.totalActivities.toLocaleString()}
                  </TableCell>
                </TableRow>
              </TableBody>
            </Table>
          </CardContent>
        </Card>

        <Card className="!bg-card !border-border">
          <CardHeader className="!pb-3">
            <CardTitle className="!text-xl !font-semibold !text-card-foreground !m-0">
              Deletion History
            </CardTitle>
            <CardDescription className="!text-sm !text-muted-foreground !m-0">
              Number of items deleted in the last operation
            </CardDescription>
          </CardHeader>
          <CardContent className="!pt-0">
            <Table>
              <TableHeader>
                <TableRow className="!border-b !border-border">
                  <TableHead className="!text-muted-foreground !font-medium">
                    Contacts
                  </TableHead>
                  <TableHead className="!text-muted-foreground !font-medium">
                    Activities
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                <TableRow className="!border-b-0">
                  <TableCell className="!text-2xl !font-bold !text-[hsl(var(--destructive))]">
                    {state.contactsDeleted.toLocaleString()}
                  </TableCell>
                  <TableCell className="!text-2xl !font-bold !text-[hsl(var(--destructive))]">
                    {state.activitiesDeleted.toLocaleString()}
                  </TableCell>
                </TableRow>
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      </div>

      <Card className="!bg-card !border-border">
        <CardHeader className="!pb-3">
          <CardTitle className="!text-xl !font-semibold !text-card-foreground !m-0">
            Delete Contacts
          </CardTitle>
          <CardDescription className="!text-sm !text-muted-foreground !m-0">
            Remove contacts and their associated activities created before a
            specific date
          </CardDescription>
        </CardHeader>
        <CardContent className="!pt-4">
          <form
            onSubmit={async (e) => {
              e.preventDefault();
              await deleteContacts({ dateTo: state.dateTo });
            }}
            className="space-y-4"
          >
            <div className="space-y-2">
              <Label
                htmlFor="toDate"
                className="!text-sm !font-medium !text-foreground"
              >
                Delete up to date
              </Label>
              <Input
                type="date"
                name="toDate"
                id="toDate"
                value={state.dateTo}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                  setState({ ...state, dateTo: e.target.value })
                }
                className="max-w-sm !bg-background !border-input !text-foreground"
              />
            </div>
            <Button
              type="submit"
              inProgress={state.isLoading}
              variant="destructive"
              className="!bg-[hsl(var(--destructive))] !text-[hsl(var(--destructive-foreground))] hover:!bg-[hsl(var(--destructive)/.9)]"
            >
              Delete Contacts
            </Button>
          </form>
        </CardContent>
      </Card>
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
