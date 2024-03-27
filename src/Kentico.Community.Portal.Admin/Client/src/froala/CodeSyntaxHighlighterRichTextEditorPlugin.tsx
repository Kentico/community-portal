import { type RichTextEditorPluginProps } from '@kentico/xperience-admin-components';
import React, { useEffect } from 'react';

// Any required properties must extend 'RichTextEditorPluginProps'
export interface CodeSyntaxHighlighterRichTextEditorPluginProps
  extends RichTextEditorPluginProps {}

// The component must be named 'PluginName' + 'RichTextEditorPlugin'
// Xperience uses this convention when loading requested plugins
export const CodeSyntaxHighlighterRichTextEditorPlugin = ({
  froalaEditorConfigurator,
  froalaEditorRef,
  _inputRef,
}: CodeSyntaxHighlighterRichTextEditorPluginProps): React.JSX.Element => {
  useEffect(() => {
    (function highlightCode() {
      const iconName = 'syntaxhHighlightIcon';
      const commandName = 'syntaxHighlight';
      const buttonName = 'Highlight Code';

      // Defines an icon
      // eslint-disable-next-line @typescript-eslint/naming-convention
      froalaEditorConfigurator.defineIcon(iconName, { SVG_KEY: 'insertMore' });

      froalaEditorConfigurator.registerCommand(commandName, {
        title: buttonName,
        icon: iconName,
        callback: () => {
          // Fix for eslint
          const ref = froalaEditorRef as React.MutableRefObject<FroalaEditor>;

          ref.current.editor.format.apply('code', {
            class: 'language-csharp',
          });
          ref.current.editor.format.apply('pre', {
            class: 'language-csharp',
          });
        },
      });
    })();
  }, [froalaEditorConfigurator, froalaEditorRef]);

  return <></>;
};

interface FroalaEditor {
  editor: {
    format: {
      // eslint-disable-next-line @typescript-eslint/naming-convention
      apply: (...args: unknown[]) => void;
    };
  };
}
