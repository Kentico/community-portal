import { usePageCommand } from '@kentico/xperience-admin-base';
import {
  Button,
  Colors,
  Headline,
  HeadlineSize,
  Paper,
} from '@kentico/xperience-admin-components';
import React, { useState } from 'react';

interface MemberManagementClientProperties {
  incorrectAvatarFiles: string[];
  correctAvatarFiles: string[];
}

interface AvatarPathMigrationResult {
  migratedPaths: string[];
}

export const MemberManagementLayoutTemplate = (
  props: MemberManagementClientProperties,
) => {
  const [correctPaths, setCorrectPaths] = useState<string[]>(
    props.correctAvatarFiles,
  );
  const { execute: migrateAvatars } = usePageCommand<AvatarPathMigrationResult>(
    'MigrateOldAvatarPaths',
    {
      after: (resp) => {
        if (resp) {
          setCorrectPaths([...props.correctAvatarFiles, ...resp.migratedPaths]);
        }
      },
    },
  );

  return (
    <div>
      <Headline size={HeadlineSize.L} labelColor={Colors.TextDefaultOnLight}>
        Member Management
      </Headline>
      <Paper>
        <div style={{ padding: '1rem' }}>
          <div style={{ marginBlockEnd: '1rem' }}>
            <Headline
              size={HeadlineSize.M}
              labelColor={Colors.TextDefaultOnLight}
            >
              Incorrect avatars
            </Headline>
            <table style={{ marginBlockEnd: '1rem', color: 'InfoText' }}>
              <thead>
                <tr>
                  <th>Filename</th>
                </tr>
              </thead>
              <tbody>
                {props.incorrectAvatarFiles.length ? (
                  props.incorrectAvatarFiles.map((p) => {
                    return (
                      <tr key={p}>
                        <td>{p}</td>
                      </tr>
                    );
                  })
                ) : (
                  <tr>
                    <td>No incorrect avatar paths</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <div style={{ marginBlockEnd: '1rem' }}>
            <Headline
              size={HeadlineSize.M}
              labelColor={Colors.TextDefaultOnLight}
            >
              Correct avatars
            </Headline>
            <table style={{ marginBlockEnd: '1rem', color: 'InfoText' }}>
              <thead>
                <tr>
                  <th>Filename</th>
                </tr>
              </thead>
              <tbody>
                {correctPaths.length ? (
                  correctPaths.map((p) => {
                    return (
                      <tr key={p}>
                        <td>{p}</td>
                      </tr>
                    );
                  })
                ) : (
                  <tr>
                    <td>No correct avatar paths</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Button
            label="Migrate old avatar paths"
            disabled={!props.incorrectAvatarFiles.length}
            onClick={() => migrateAvatars()}
          />
        </div>
      </Paper>
    </div>
  );
};
