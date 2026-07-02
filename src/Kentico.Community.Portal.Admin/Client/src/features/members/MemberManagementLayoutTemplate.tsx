import { usePageCommand } from '@kentico/xperience-admin-base';
import {
  Button,
  Card,
  Colors,
  Headline,
  HeadlineSize,
  Input,
  Link,
  Paper,
  Spacing,
} from '@kentico/xperience-admin-components';
import React, { useState } from 'react';

interface MemberManagementClientProperties {
  avatarMigrationAnalysis: MemberAvatarMigrationAnalysisResult;
  defaultBatchSize: number;
}

interface MemberAvatarMigrationCandidate {
  memberID: number;
  avatarFileExtension: string;
  avatarPath: string;
  avatarSizeBytes: number;
  existsInContentHub: boolean;
  avatarUrl: string;
}

interface ManagedAvatarFileInventoryItem {
  fileName: string;
  avatarPath: string;
  avatarExtension: string;
  avatarSizeBytes: number;
  lastModifiedUtc: string;
  createdUtc: string;
  isReadOnly: boolean;
  memberID: number | null;
  memberExists: boolean;
  memberUserName: string;
  memberFullName: string;
  memberEmail: string;
  memberAvatarFileExtension: string;
  existsInContentHub: boolean;
}

interface MemberAvatarMigrationAnalysisResult {
  membersWithAvatarFiles: number;
  membersMissingAvatarFiles: number;
  membersAlreadyInContentHub: number;
  membersPendingMigration: number;
  totalAvatarBytes: number;
  candidates: MemberAvatarMigrationCandidate[];
  managedAvatarFiles: ManagedAvatarFileInventoryItem[];
}

interface MigrationResult {
  name: string;
  displayName: string;
  successes: string[];
  failures: string[];
  migratableItemsCount: number;
}

const sectionStyle: React.CSSProperties = {
  marginBlockEnd: '1rem',
};

const tableStyle: React.CSSProperties = {
  width: '100%',
  borderCollapse: 'collapse',
  color: '#1f2937',
  fontSize: '13px',
};

const headCellStyle: React.CSSProperties = {
  borderBottom: '1px solid #d1d5db',
  padding: '8px 10px',
  textAlign: 'left',
  whiteSpace: 'nowrap',
  fontWeight: 700,
};

const bodyCellStyle: React.CSSProperties = {
  borderBottom: '1px solid #e5e7eb',
  padding: '8px 10px',
  verticalAlign: 'top',
};

const mutedTextStyle: React.CSSProperties = {
  color: '#6b7280',
};

const retiredMessage =
  'Legacy avatar filesystem migration and inventory are retired. Member avatars are served from Content Hub assets only.';

const monoTextStyle: React.CSSProperties = {
  fontFamily:
    'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, Liberation Mono, Courier New, monospace',
  fontSize: '12px',
  wordBreak: 'break-all',
};

function formatBytes(size: number): string {
  if (size < 1024) {
    return `${size} B`;
  }

  const units = ['KB', 'MB', 'GB'];
  let value = size / 1024;
  let unitIndex = 0;

  while (value >= 1024 && unitIndex < units.length - 1) {
    value /= 1024;
    unitIndex++;
  }

  return `${value.toFixed(1)} ${units[unitIndex]}`;
}

function formatUtc(utcValue: string): string {
  const date = new Date(utcValue);
  return Number.isNaN(date.getTime()) ? utcValue : date.toLocaleString();
}

const StatusBadge = (props: {
  active: boolean;
  activeLabel: string;
  inactiveLabel: string;
}) => {
  const background = props.active ? '#dcfce7' : '#f3f4f6';
  const foreground = props.active ? '#166534' : '#374151';

  return (
    <span
      style={{
        display: 'inline-block',
        borderRadius: '999px',
        padding: '2px 8px',
        fontSize: '12px',
        fontWeight: 600,
        background,
        color: foreground,
      }}
    >
      {props.active ? props.activeLabel : props.inactiveLabel}
    </span>
  );
};

export const MemberManagementLayoutTemplate = (
  props: MemberManagementClientProperties,
) => {
  const [analysis, setAnalysis] = useState<MemberAvatarMigrationAnalysisResult>(
    props.avatarMigrationAnalysis,
  );
  const [batchSize, setBatchSize] = useState<number>(props.defaultBatchSize);
  const [analyzing, setAnalyzing] = useState<boolean>(false);
  const [migrating, setMigrating] = useState<boolean>(false);
  const [migratingMemberID, setMigratingMemberID] = useState<number | null>(
    null,
  );
  const [migrationResult, setMigrationResult] =
    useState<MigrationResult | null>(null);

  const { execute: analyzeAvatars } =
    usePageCommand<MemberAvatarMigrationAnalysisResult>(
      'AnalyzeMemberAvatarsForContentHubMigration',
      {
        after: (resp) => {
          if (resp) {
            setAnalysis(resp);
          }
          setAnalyzing(false);
        },
      },
    );

  const { execute: migrateAvatars } = usePageCommand<
    MigrationResult,
    { batchSize: number }
  >('MigrateAvatarsToContentHub', {
    after: (resp) => {
      if (resp) {
        setMigrationResult(resp);
      }
      setMigrating(false);
    },
  });

  const { execute: migrateSingleAvatar } = usePageCommand<
    MigrationResult,
    { memberID: number }
  >('MigrateSingleAvatarToContentHub', {
    after: (resp) => {
      if (resp) {
        setMigrationResult(resp);
      }

      setMigratingMemberID(null);
      setAnalyzing(true);
      void analyzeAvatars();
    },
  });

  const visibleCandidates = analysis.candidates.slice(0, 100);
  const visibleManagedFiles = analysis.managedAvatarFiles;

  const summaryMetrics = [
    {
      label: 'Members with avatar files',
      value: analysis.membersWithAvatarFiles.toLocaleString(),
    },
    {
      label: 'Members missing avatar files',
      value: analysis.membersMissingAvatarFiles.toLocaleString(),
    },
    {
      label: 'Members already in Content Hub',
      value: analysis.membersAlreadyInContentHub.toLocaleString(),
    },
    {
      label: 'Members pending migration',
      value: analysis.membersPendingMigration.toLocaleString(),
    },
    {
      label: 'Total avatar data',
      value: `${formatBytes(analysis.totalAvatarBytes)} (${analysis.totalAvatarBytes.toLocaleString()} bytes)`,
    },
    {
      label: 'Managed files found',
      value: visibleManagedFiles.length.toLocaleString(),
    },
  ];

  return (
    <div>
      <Headline
        size={HeadlineSize.L}
        labelColor={Colors.TextDefaultOnLight}
        spacingBottom={Spacing.M}
      >
        Member Management
      </Headline>
      <Paper>
        <div style={{ padding: '1rem' }}>
          <Card headline="Avatar migration analysis">
            <div style={{ ...mutedTextStyle, marginBlockEnd: '12px' }}>
              {retiredMessage}
            </div>
            <div
              style={{
                display: 'grid',
                gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
                gap: '10px',
                marginBlockEnd: '14px',
              }}
            >
              {summaryMetrics.map((metric) => (
                <div
                  key={metric.label}
                  style={{
                    border: '1px solid #e5e7eb',
                    borderRadius: '10px',
                    padding: '10px 12px',
                    background: '#fafafa',
                  }}
                >
                  <div style={{ ...mutedTextStyle, marginBlockEnd: '4px' }}>
                    {metric.label}
                  </div>
                  <div style={{ fontSize: '18px', fontWeight: 700 }}>
                    {metric.value}
                  </div>
                </div>
              ))}
            </div>
            <Button
              label="Re-run analysis"
              inProgress={analyzing}
              disabled={true}
              onClick={() => {
                setAnalyzing(true);
                void analyzeAvatars();
              }}
            />
          </Card>

          <Card headline="Run migration batch">
            <div
              style={{
                display: 'flex',
                gap: '16px',
                alignItems: 'flex-end',
                flexWrap: 'wrap',
              }}
            >
              <div style={{ maxWidth: '220px' }}>
                <Input
                  type={'number'}
                  name={'avatarMigrationBatchSize'}
                  label="Batch size"
                  min={1}
                  max={analysis.membersWithAvatarFiles || 1}
                  value={batchSize}
                  onChange={(event) => {
                    const parsed = parseInt(event.target.value || '1', 10);
                    setBatchSize(
                      Number.isNaN(parsed) ? 1 : Math.max(1, parsed),
                    );
                  }}
                />
              </div>
              <Button
                label="Migration retired"
                inProgress={migrating}
                disabled={true}
                onClick={() => {
                  setMigrating(true);
                  void migrateAvatars({ batchSize });
                }}
              />
            </div>

            {migrationResult ? (
              <div
                style={{
                  marginTop: '12px',
                  display: 'flex',
                  gap: '8px',
                  flexWrap: 'wrap',
                }}
              >
                <StatusBadge
                  active={migrationResult.successes.length > 0}
                  activeLabel={`Successes: ${migrationResult.successes.length}`}
                  inactiveLabel={`Successes: ${migrationResult.successes.length}`}
                />
                <StatusBadge
                  active={migrationResult.failures.length === 0}
                  activeLabel={`Failures: ${migrationResult.failures.length}`}
                  inactiveLabel={`Failures: ${migrationResult.failures.length}`}
                />
                <span style={{ ...mutedTextStyle, paddingTop: '3px' }}>
                  Remaining migratable items:{' '}
                  {migrationResult.migratableItemsCount}
                </span>
              </div>
            ) : (
              <div style={{ ...mutedTextStyle, marginTop: '10px' }}>
                {retiredMessage}
              </div>
            )}
          </Card>

          <div style={sectionStyle}>
            <Card headline="Candidate members (first 100)">
              <div style={{ ...mutedTextStyle, marginBottom: '8px' }}>
                Migration candidates are no longer tracked after retirement of
                legacy member-assets storage.
              </div>
              <div style={{ overflowX: 'auto' }}>
                <table style={tableStyle}>
                  <thead>
                    <tr>
                      <th style={headCellStyle}>Member ID</th>
                      <th style={headCellStyle}>Extension</th>
                      <th style={headCellStyle}>File size</th>
                      <th style={headCellStyle}>In Content Hub</th>
                      <th style={headCellStyle}>Image URL</th>
                      <th style={headCellStyle}>Path</th>
                      <th style={headCellStyle}>Action</th>
                    </tr>
                  </thead>
                  <tbody>
                    {visibleCandidates.length ? (
                      visibleCandidates.map((candidate) => {
                        const rowInProgress =
                          migratingMemberID === candidate.memberID;

                        return (
                          <tr key={candidate.memberID}>
                            <td style={bodyCellStyle}>{candidate.memberID}</td>
                            <td style={bodyCellStyle}>
                              {candidate.avatarFileExtension}
                            </td>
                            <td style={bodyCellStyle}>
                              {formatBytes(candidate.avatarSizeBytes)}
                            </td>
                            <td style={bodyCellStyle}>
                              <StatusBadge
                                active={candidate.existsInContentHub}
                                activeLabel="Yes"
                                inactiveLabel="No"
                              />
                            </td>
                            <td style={bodyCellStyle}>
                              <Link
                                href={candidate.avatarUrl}
                                text={candidate.avatarUrl}
                              />
                            </td>
                            <td style={{ ...bodyCellStyle, ...monoTextStyle }}>
                              {candidate.avatarPath}
                            </td>
                            <td style={bodyCellStyle}>
                              <Button
                                label="Retired"
                                inProgress={rowInProgress}
                                disabled={true}
                                onClick={() => {
                                  setMigratingMemberID(candidate.memberID);
                                  void migrateSingleAvatar({
                                    memberID: candidate.memberID,
                                  });
                                }}
                              />
                            </td>
                          </tr>
                        );
                      })
                    ) : (
                      <tr>
                        <td style={bodyCellStyle} colSpan={7}>
                          No candidate member avatars found.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </Card>
          </div>

          <div style={sectionStyle}>
            <Card
              headline={`Managed avatar files (${visibleManagedFiles.length})`}
            >
              <div style={{ ...mutedTextStyle, marginBottom: '8px' }}>
                Legacy filesystem inventory is retired.
              </div>
              <div style={{ overflowX: 'auto' }}>
                <table style={tableStyle}>
                  <thead>
                    <tr>
                      <th style={headCellStyle}>File</th>
                      <th style={headCellStyle}>Member ID</th>
                      <th style={headCellStyle}>Member</th>
                      <th style={headCellStyle}>Email</th>
                      <th style={headCellStyle}>Member avatar ext</th>
                      <th style={headCellStyle}>Size</th>
                      <th style={headCellStyle}>Extension</th>
                      <th style={headCellStyle}>Created</th>
                      <th style={headCellStyle}>Modified</th>
                      <th style={headCellStyle}>Read-only</th>
                      <th style={headCellStyle}>In Content Hub</th>
                      <th style={headCellStyle}>Path</th>
                    </tr>
                  </thead>
                  <tbody>
                    {visibleManagedFiles.length ? (
                      visibleManagedFiles.map((file) => {
                        const memberDisplay = file.memberExists
                          ? `${file.memberFullName} (${file.memberUserName})`
                          : 'No member record';

                        return (
                          <tr key={file.avatarPath}>
                            <td style={{ ...bodyCellStyle, ...monoTextStyle }}>
                              {file.fileName}
                            </td>
                            <td style={bodyCellStyle}>
                              {file.memberID ?? 'n/a'}
                            </td>
                            <td style={bodyCellStyle}>{memberDisplay}</td>
                            <td style={bodyCellStyle}>
                              {file.memberEmail || 'n/a'}
                            </td>
                            <td style={bodyCellStyle}>
                              {file.memberAvatarFileExtension || 'n/a'}
                            </td>
                            <td style={bodyCellStyle}>
                              {formatBytes(file.avatarSizeBytes)}
                            </td>
                            <td style={bodyCellStyle}>
                              {file.avatarExtension}
                            </td>
                            <td style={bodyCellStyle}>
                              {formatUtc(file.createdUtc)}
                            </td>
                            <td style={bodyCellStyle}>
                              {formatUtc(file.lastModifiedUtc)}
                            </td>
                            <td style={bodyCellStyle}>
                              <StatusBadge
                                active={file.isReadOnly}
                                activeLabel="Yes"
                                inactiveLabel="No"
                              />
                            </td>
                            <td style={bodyCellStyle}>
                              <StatusBadge
                                active={file.existsInContentHub}
                                activeLabel="Yes"
                                inactiveLabel="No"
                              />
                            </td>
                            <td style={{ ...bodyCellStyle, ...monoTextStyle }}>
                              {file.avatarPath}
                            </td>
                          </tr>
                        );
                      })
                    ) : (
                      <tr>
                        <td style={bodyCellStyle} colSpan={12}>
                          No managed avatar files found.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </Card>
          </div>
        </div>
      </Paper>
    </div>
  );
};
