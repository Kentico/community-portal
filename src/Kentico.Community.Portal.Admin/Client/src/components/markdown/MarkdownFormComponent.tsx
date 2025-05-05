/* eslint-disable no-console */
import React, { useState } from 'react';
import { FormComponentProps } from '@kentico/xperience-admin-base';
import {
  Button,
  ButtonColor,
  FormItemWrapper,
} from '@kentico/xperience-admin-components';
import { Milkdown, MilkdownProvider, useEditor } from '@milkdown/react';
import { Crepe } from '@milkdown/crepe';
import { listener, listenerCtx } from '@milkdown/kit/plugin/listener';
import CrepeStyles from '@milkdown/crepe/theme/common/style.css';
import FrameStyles from '@milkdown/crepe/theme/frame.css';
import { EditorStatus } from '@milkdown/kit/core';
import { getCodeMirrorLanguages } from './CodeMirrorConfig';

let crepeEditor: Crepe | undefined = undefined;
let crepeReadonly: Crepe | undefined = undefined;

type EditorMode = 'edit' | 'readonly';
type EditorType = 'markdown' | 'editor';
type ComponentState = { type: EditorType };

export const MarkdownFormComponent = (props: FormComponentProps) => {
  const defaultValue =
    typeof props.value === 'string' && !!props.value ? props.value : '';

  const mode: EditorMode = props.disabled === true ? 'readonly' : 'edit';

  const [state, setState] = useState<ComponentState>({
    type: 'editor',
  });

  const updateContentDebounced = debounce((oldValue, newValue) => {
    if (newValue !== oldValue) {
      if (props.onChange && typeof newValue === 'string') {
        props.onChange(newValue);
      }
    }
  }, 250);

  const updatedContent = (oldValue?: string, newValue?: string) => {
    if (props.onChange && newValue) {
      props?.onChange(newValue);
    }
  };

  return (
    <div className={'markdown-editor-container'} onClick={blockEvents}>
      <EditorStyles
        styles={[
          (CrepeStyles as string[]).toString(),
          (FrameStyles as string[]).toString(),
          customStyles,
        ]}
      />

      <FormItemWrapper
        {...props}
        childrenWrapperClassnames={
          mode === 'readonly'
            ? 'markdown-editor-form-item disabled'
            : 'markdown-editor-form-item'
        }
        disabled={mode === 'readonly'}
      >
        {state.type === 'markdown' ? (
          <RawEditor
            {...{
              defaultValue,
              UpdateContent: updatedContent,
              mode,
            }}
          />
        ) : (
          <MilkdownProvider>
            {mode === 'edit' ? (
              <MilkdownEditor
                {...{
                  defaultValue,
                  UpdateContent: updateContentDebounced,
                  mode: 'edit',
                }}
              />
            ) : (
              <MilkdownEditorReadOnly
                {...{
                  defaultValue,
                  UpdateContent: updateContentDebounced,
                  mode: 'readonly',
                }}
              />
            )}
          </MilkdownProvider>
        )}
      </FormItemWrapper>
      <ToggleEditor
        type={state.type}
        OnToggle={() =>
          setState((s) => ({
            ...s,
            type: s.type === 'editor' ? 'markdown' : 'editor',
          }))
        }
      />
    </div>
  );
};

const ToggleEditor = ({
  type,
  OnToggle: onToggle,
}: {
  type: EditorType;
  OnToggle: () => void;
}) => {
  return (
    <Button
      label={type === 'markdown' ? 'View editor' : 'View markdown'}
      className={'toggle-editor'}
      onClick={onToggle}
      color={ButtonColor.Secondary}
    />
  );
};

const EditorStyles = ({ styles }: { styles: string[] }) => {
  return (
    <>
      {styles.map((s, i) => (
        <style key={i}>{s}</style>
      ))}
    </>
  );
};

interface EditorProps {
  defaultValue: string;
  UpdateContent: (oldValue: string, newValue: string) => void;
  mode: EditorMode;
}

const RawEditor = ({
  defaultValue,
  UpdateContent: updateContent,
  mode,
}: EditorProps) => {
  return (
    <textarea
      className={'raw-editor'}
      rows={15}
      onChange={(e) => updateContent(undefined, e.target.value)}
      value={defaultValue}
      disabled={mode === 'readonly'}
    ></textarea>
  );
};

const MilkdownEditor = ({
  defaultValue,
  UpdateContent: updateContent,
}: EditorProps) => {
  useEditor((root) => {
    if (crepeEditor) {
      return crepeEditor;
    }
    crepeEditor = new Crepe({
      root,
      defaultValue,
      featureConfigs: {
        placeholder: {
          text: 'Add some markdown',
        },
        ['code-mirror']: {
          languages: getCodeMirrorLanguages(),
        },
      },
    });

    crepeEditor.editor
      .config((ctx) => {
        ctx
          .get(listenerCtx)
          .markdownUpdated((_, newValue, oldValue) =>
            updateContent(oldValue, newValue),
          );
      })
      .use(listener)
      .onStatusChange((s) => {
        if (s === EditorStatus.Destroyed) {
          crepeEditor?.destroy().catch((err) => console.error(err));
          crepeEditor = undefined;
        }
      });

    return crepeEditor;
  }, []);

  return <Milkdown />;
};

const MilkdownEditorReadOnly = ({ defaultValue }: EditorProps) => {
  useEditor((root) => {
    if (crepeReadonly) {
      return crepeReadonly;
    }
    crepeReadonly = new Crepe({
      root,
      defaultValue,
      featureConfigs: {
        placeholder: {
          text: '',
        },
        ['code-mirror']: {
          languages: getCodeMirrorLanguages(),
        },
      },
    });

    crepeReadonly.setReadonly(true);

    crepeReadonly.editor.onStatusChange((s) => {
      if (s === EditorStatus.Destroyed) {
        crepeReadonly?.destroy().catch((err) => console.error(err));
        crepeReadonly = undefined;
      }
    });

    return crepeReadonly;
  }, []);

  return <Milkdown />;
};

function debounce(
  func: (...args: unknown[]) => void,
  delay: number,
): (...args: unknown[]) => void {
  let timeoutId: NodeJS.Timeout | undefined;

  return (...args) => {
    clearTimeout(timeoutId);
    timeoutId = setTimeout(() => {
      func.apply(this, args);
    }, delay);
  };
}

function blockEvents(e: Event) {
  e.stopPropagation();
  e.preventDefault();
}

const customStyles = `
.markdown-editor-container {
  color: var(--color-border-default);
  background: var(--color-paper-background);

  .markdown-editor-form-item {
      min-width: 100%;
      min-height: 300px;
      padding: 0.5rem;

      border: 1px solid var(--color-input-border);
      border-radius: 20px;

      &:has(textarea) {
        padding: 0;
        border: none;
      }

      &.disabled {
          color: var(--color-text-disabled);
          background-color: var(--color-background-disabled);

          .milkdown {
              color: var(--color-text-disabled);
              background-color: var(--color-background-disabled);
          }

          .raw-editor {
            border: none;
          }
      }

      .raw-editor {
          border: 1px solid var(--color-input-border);
          border-radius: 20px;
          box-sizing: border-box;
          padding: 0.5rem;
          padding-inline-start: 4rem;
          width: 100%;
          resize: none;
          overflow: auto;

          
      }

      .milkdown {
          .milkdown-block-handle {
              left: -4px !important;
          }
          .ProseMirror.editor {
              padding: 0;
              padding-inline-start: 4rem;
          }
      }

      .milkdown-slash-menu {
        z-index: 1;
      }
  }   
 
  .toggle-editor {
    margin-block-start: 1rem;
  } 
}
`;
