import { usePageCommand } from '@kentico/xperience-admin-base';
import {
  Button,
  Colors,
  Headline,
  HeadlineSize,
  Spacing,
} from '@kentico/xperience-admin-components';
import React, { useState } from 'react';
import './web-page-rocket-surgery.css';

interface WebPageRocketSurgeryClientProperties {
  templateConfiguration: string;
  widgetsConfiguration: string;
  isModifiable: boolean;
  canEdit: boolean;
  updateTemplateCommandName: string;
  updateWidgetsCommandName: string;
}

function tryFormatJson(value: string): string {
  if (!value) return '';
  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
}

function isValidJson(value: string): boolean {
  if (!value) return true;
  try {
    JSON.parse(value);
    return true;
  } catch {
    return false;
  }
}

interface JsonViewerProps {
  value: string;
  label: string;
}

const JsonViewer = ({ value, label }: JsonViewerProps) => {
  const formatted = tryFormatJson(value);

  return (
    <div className="rocket-surgery__json-viewer">
      <p className="rocket-surgery__section-label">{label}</p>
      {formatted ? (
        <pre className="rocket-surgery__code-block">{formatted}</pre>
      ) : (
        <p className="rocket-surgery__empty-state">
          No configuration data found.
        </p>
      )}
    </div>
  );
};

export const WebPageRocketSurgeryTemplate = (
  props: WebPageRocketSurgeryClientProperties,
) => {
  const [templateConfig, setTemplateConfig] = useState(
    props.templateConfiguration,
  );
  const [widgetsConfig, setWidgetsConfig] = useState(
    props.widgetsConfiguration,
  );
  const [savedTemplateConfig, setSavedTemplateConfig] = useState(
    props.templateConfiguration,
  );
  const [savedWidgetsConfig, setSavedWidgetsConfig] = useState(
    props.widgetsConfiguration,
  );
  const [isUpdatingTemplate, setIsUpdatingTemplate] = useState(false);
  const [isUpdatingWidgets, setIsUpdatingWidgets] = useState(false);

  const { execute: executeUpdateTemplate } = usePageCommand<
    { templateConfiguration: string },
    { templateConfiguration: string }
  >(props.updateTemplateCommandName, {
    after: (resp) => {
      setIsUpdatingTemplate(false);
      if (resp?.templateConfiguration !== undefined) {
        setSavedTemplateConfig(resp.templateConfiguration);
      }
    },
  });

  const { execute: executeUpdateWidgets } = usePageCommand<
    { widgetsConfiguration: string },
    { widgetsConfiguration: string }
  >(props.updateWidgetsCommandName, {
    after: (resp) => {
      setIsUpdatingWidgets(false);
      if (resp?.widgetsConfiguration !== undefined) {
        setSavedWidgetsConfig(resp.widgetsConfiguration);
      }
    },
  });

  const handleUpdateTemplate = async () => {
    setIsUpdatingTemplate(true);
    await executeUpdateTemplate({ templateConfiguration: templateConfig });
  };

  const handleUpdateWidgets = async () => {
    setIsUpdatingWidgets(true);
    await executeUpdateWidgets({ widgetsConfiguration: widgetsConfig });
  };

  const templateConfigInvalid = !isValidJson(templateConfig);
  const widgetsConfigInvalid = !isValidJson(widgetsConfig);

  return (
    <div className="rocket-surgery">
      <Headline
        size={HeadlineSize.L}
        labelColor={Colors.TextDefaultOnLight}
        spacingBottom={Spacing.M}
      >
        🚀 Rocket Surgery
      </Headline>

      <p className="rocket-surgery__description">
        Advanced tools for viewing and modifying Page Builder template and widget
        configuration stored in the database. Changes apply to the current draft
        only.
      </p>

      {!props.isModifiable && props.canEdit && (
        <div className="rocket-surgery__warning">
          ⚠️ This page does not have a draft. Create a draft first to enable
          editing.
        </div>
      )}

      {/* Section 1: Fix Page Template Configuration */}
      <section className="rocket-surgery__section">
        <Headline size={HeadlineSize.M} spacingBottom={Spacing.S}>
          Fix Page Template Configuration
        </Headline>
        <p className="rocket-surgery__section-description">
          Update the{' '}
          <code>ContentItemCommonDataVisualBuilderTemplateConfiguration</code>{' '}
          column for this page&apos;s current draft. Use this to fix pages that
          were created before page templates were registered.
        </p>

        <JsonViewer
          value={savedTemplateConfig}
          label="Current template configuration (read-only view)"
        />

        {props.canEdit && (
          <div className="rocket-surgery__edit-area">
            <label
              htmlFor="template-config-input"
              className="rocket-surgery__section-label"
            >
              New template configuration JSON
            </label>
            <textarea
              id="template-config-input"
              className={`rocket-surgery__textarea${templateConfigInvalid ? ' rocket-surgery__textarea--invalid' : ''}`}
              value={templateConfig}
              onChange={(e) => setTemplateConfig(e.target.value)}
              rows={8}
              disabled={!props.isModifiable}
              placeholder='{"identifier": "your.template.identifier", "properties": {}}'
            />
            {templateConfigInvalid && (
              <p className="rocket-surgery__validation-error">
                Invalid JSON — please correct before saving.
              </p>
            )}
            <div className="rocket-surgery__actions">
              <Button
                label="Save Template Configuration"
                onClick={() => void handleUpdateTemplate()}
                disabled={
                  !props.isModifiable ||
                  templateConfigInvalid ||
                  isUpdatingTemplate
                }
                inProgress={isUpdatingTemplate}
              />
            </div>
          </div>
        )}
      </section>

      {/* Section 2: View Widget Configuration */}
      <section className="rocket-surgery__section">
        <Headline size={HeadlineSize.M} spacingBottom={Spacing.S}>
          Widget Configuration Viewer
        </Headline>
        <p className="rocket-surgery__section-description">
          Read-only view of the{' '}
          <code>ContentItemCommonDataVisualBuilderWidgets</code> column for this
          page&apos;s current draft.
        </p>

        <JsonViewer
          value={savedWidgetsConfig}
          label="Current widget configuration (read-only view)"
        />
      </section>

      {/* Section 3: Update Widget Configuration */}
      {props.canEdit && (
        <section className="rocket-surgery__section">
          <Headline size={HeadlineSize.M} spacingBottom={Spacing.S}>
            Update Widget Configuration
          </Headline>
          <p className="rocket-surgery__section-description">
            Update the{' '}
            <code>ContentItemCommonDataVisualBuilderWidgets</code> column for
            this page&apos;s current draft. Use this to migrate Page Builder
            configuration between pages via copy/paste.
          </p>
          <p className="rocket-surgery__section-note">
            ℹ️ After saving, run the{' '}
            <strong>Track missing content item usages</strong> scheduled task to
            ensure content item usages are up to date.
          </p>

          <label
            htmlFor="widgets-config-input"
            className="rocket-surgery__section-label"
          >
            Widget configuration JSON
          </label>
          <textarea
            id="widgets-config-input"
            className={`rocket-surgery__textarea${widgetsConfigInvalid ? ' rocket-surgery__textarea--invalid' : ''}`}
            value={widgetsConfig}
            onChange={(e) => setWidgetsConfig(e.target.value)}
            rows={16}
            disabled={!props.isModifiable}
            placeholder="Paste widget configuration JSON here..."
          />
          {widgetsConfigInvalid && (
            <p className="rocket-surgery__validation-error">
              Invalid JSON — please correct before saving.
            </p>
          )}
          <div className="rocket-surgery__actions">
            <Button
              label="Save Widget Configuration"
              onClick={() => void handleUpdateWidgets()}
              disabled={
                !props.isModifiable || widgetsConfigInvalid || isUpdatingWidgets
              }
              inProgress={isUpdatingWidgets}
            />
          </div>
        </section>
      )}
    </div>
  );
};
