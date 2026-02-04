import { usePageCommand } from '@kentico/xperience-admin-base';
import {
  Button,
  Card,
  Colors,
  Headline,
  HeadlineSize,
  Input,
  Paper,
  Spacing,
} from '@kentico/xperience-admin-components';
import React, { useState } from 'react';

interface MigrationManagementClientProperties {
  commandName: string;
  availableMigrations: MigrationState[];
}

type MigrationState = {
  name: string;
  displayName: string;
  migratableItemsCount: number;
};

interface MigrationResult {
  name: string;
  displayName: string;
  successes: string[];
  failures: string[];
  migratableItemsCount: number;
}

type MigrateCommandParams = {
  migrationName: string;
  migrateItemsCount: int;
};

type MigrationsState = {
  migrations: Map<string, MigrationResult>;
  activeMigration: string | undefined;
};

export const MigrationManagementLayoutTemplate = (
  props: MigrationManagementClientProperties,
) => {
  const [state, setState] = useState<MigrationsState>(() => ({
    activeMigration: undefined,
    migrations: initalizeMigrations(props.availableMigrations),
  }));

  const { execute } = usePageCommand<MigrationResult, MigrateCommandParams>(
    props.commandName,
    {
      after(resp) {
        if (!resp) {
          return;
        }

        const updatedMigrations = new Map(state.migrations);
        updatedMigrations.set(resp.name, resp);

        setState((s) => ({
          ...s,
          migrations: updatedMigrations,
          activeMigration: undefined,
        }));
      },
    },
  );

  const runMigration = async (migrationName: string, count: number) => {
    setState((s) => ({ ...s, activeMigration: migrationName }));

    await execute({ migrationName, migrateItemsCount: count });
  };

  return (
    <div>
      <Headline
        size={HeadlineSize.L}
        labelColor={Colors.TextDefaultOnLight}
        spacingBottom={Spacing.M}
      >
        Migration Management
      </Headline>
      <Paper>
        {Array.from(state.migrations).map(([name, result]) => {
          const [inputValue, setInputValue] = useState<number>(
            result.migratableItemsCount,
          );

          const handleInputChange = (
            event: React.ChangeEvent<HTMLInputElement>,
          ) => {
            const value = parseInt(event.target.value || '0', 10);
            setInputValue(value);
          };
          return (
            <Card headline={result.displayName} key={name}>
              {!result.successes && !result.failures ? (
                <></>
              ) : (
                <dl>
                  <dt>Migratable items: {result.migratableItemsCount}</dt>
                  <dt>Successes: {result.successes.length}</dt>
                  <dd>
                    <ul>
                      {result.successes.map((success, index) => (
                        <li key={index}>{success}</li>
                      ))}
                    </ul>
                  </dd>

                  <dt>Failures: {result.failures.length}</dt>
                  <dd>
                    <ul>
                      {result.failures.map((failure, index) => (
                        <li key={index}>{failure}</li>
                      ))}
                    </ul>
                  </dd>
                </dl>
              )}
              <div style={{ maxWidth: '200px', marginBlockEnd: '10px' }}>
                <Input
                  type={'number'}
                  key={name}
                  label="Items to migrate"
                  name={result.name}
                  max={result.migratableItemsCount}
                  value={inputValue}
                  onChange={handleInputChange}
                />
              </div>
              <Button
                label="Run"
                disabled={state.activeMigration || !result.migratableItemsCount}
                inProgress={state.activeMigration === name}
                onClick={() => runMigration(name, inputValue)}
              />
            </Card>
          );
        })}
      </Paper>
    </div>
  );
};

function initalizeMigrations(
  migrations: MigrationState[],
): Map<string, MigrationResult> {
  return new Map(
    migrations.map(({ name, displayName, migratableItemsCount }) => [
      name,
      { successes: [], failures: [], migratableItemsCount, displayName, name },
    ]),
  );
}
