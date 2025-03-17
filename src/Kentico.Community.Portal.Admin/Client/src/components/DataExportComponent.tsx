import {
  ActionComponentProps,
  usePageCommand,
} from '@kentico/xperience-admin-base';
import { ProgressBar } from '@kentico/xperience-admin-components';
import React, { useEffect, useState } from 'react';

type ExportResultFailure = { errorMessage: string; fileData: null };
type ExportResultSuccess = { fileData: string; errorMessage: null };
type ExportResult = ExportResultSuccess | ExportResultFailure;

type ExportStatus = 'EXPORTING' | 'COMPLETE' | 'FAILURE';
type ComponentState = {
  status: ExportStatus;
  percentComplete: number;
  errorMessage: string;
};

interface DataExportComponentProps extends ActionComponentProps {
  fileNamePrefix: string;
  commandName: string;
}

export const DataExportComponent = (props: DataExportComponentProps) => {
  const [state, setState] = useState<ComponentState>({
    status: 'EXPORTING',
    percentComplete: 0,
    errorMessage: '',
  });

  useEffect(() => {
    let currentProgress = 0;
    const target = 100;
    const interval = 500;

    const intervalId = setInterval(() => {
      // End if we've reached the limit
      if (
        currentProgress >= target - 0.01 ||
        state.status === 'FAILURE' ||
        state.status === 'COMPLETE'
      ) {
        currentProgress = target;
        clearInterval(intervalId);
        props.onActionExecuted();

        if (state.status !== 'FAILURE') {
          props.unloadComponent();
        }

        return;
      }

      // Calculate the next increment as a percentage of the remaining distance to target
      const increment = (target - currentProgress) * 0.1;
      currentProgress += increment;

      setState((s) => ({
        ...s,
        percentComplete: currentProgress.toFixed(2),
      }));
    }, interval);

    return () => clearInterval(intervalId);
  }, [state.status]);

  usePageCommand<ExportResult>(props.commandName, {
    executeOnMount: true,
    after: (resp) => {
      if (!resp) {
        return;
      }

      if (('error' in resp && resp.errorMessage) || !resp.fileData) {
        setState((s) => ({
          ...s,
          status: 'FAILURE',
          errorMessage: resp.errorMessage,
        }));

        return;
      }

      setState((s) => ({
        ...s,
        status: 'COMPLETE',
        percentComplete: 100,
        errorMessage: '',
      }));

      const binaryString = atob(resp.fileData);
      const binaryLength = binaryString.length;
      const bytes = new Uint8Array(binaryLength);
      for (let i = 0; i < binaryLength; i++) {
        bytes[i] = binaryString.charCodeAt(i);
      }

      // Create a Blob from the binary data
      const blob = new Blob([bytes], { type: 'application/octet-stream' }); // Change MIME type if necessary

      // Create a download link
      const downloadUrl = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = downloadUrl;
      link.download = `${props.fileNamePrefix}_${getDashCaseDateSuffix()}.csv`; // Set the desired file name and extension here
      link.click();

      // Clean up the URL object
      URL.revokeObjectURL(downloadUrl);
    },
  });

  return (
    <div
      style={{
        paddingBlock: '0.5rem',
        minHeight: '0.25rem',
        color: 'var(--color-text-default-on-light)',
      }}
    >
      {showProgressBar(state) ? (
        <ProgressBar completed={state.percentComplete} />
      ) : (
        <span>Failure: {state.errorMessage}</span>
      )}
    </div>
  );
};

function getDashCaseDateSuffix() {
  const date = new Date();
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0'); // Months are zero-based
  const day = String(date.getDate()).padStart(2, '0');

  return `${year}-${month}-${day}`;
}

function showProgressBar(state: ComponentState) {
  return state.status === 'EXPORTING' || state.status === 'COMPLETE';
}
