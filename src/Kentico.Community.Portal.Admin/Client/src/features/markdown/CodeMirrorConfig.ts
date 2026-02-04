import { SQLDialect } from '@codemirror/lang-sql';
import {
  LanguageDescription,
  LanguageSupport,
  StreamLanguage,
  type StreamParser,
} from '@codemirror/language';

export function getCodeMirrorLanguages() {
  function legacy<T>(parser: StreamParser<T>) {
    return new LanguageSupport(StreamLanguage.define<T>(parser));
  }
  function sql(dialectName: string) {
    return import('@codemirror/lang-sql').then((m) =>
      m[dialectName] instanceof SQLDialect
        ? m.sql({ dialect: m[dialectName] })
        : undefined,
    );
  }

  return [
    /*@__PURE__*/ LanguageDescription.of({
      name: 'CSharp',
      alias: ['csharp', 'cs'],
      extensions: ['cs'],
      load() {
        return import('@codemirror/legacy-modes/mode/clike').then((m) =>
          legacy(m.csharp),
        );
      },
    }),
    /*@__PURE__*/ LanguageDescription.of({
      name: 'CSS',
      extensions: ['css'],
      load() {
        return import('@codemirror/lang-css').then((m) => m.css());
      },
    }),
    /*@__PURE__*/ LanguageDescription.of({
      name: 'HTML',
      alias: ['xhtml'],
      extensions: ['html', 'htm', 'handlebars', 'hbs'],
      load() {
        return import('@codemirror/lang-html').then((m) => m.html());
      },
    }),
    /*@__PURE__*/ LanguageDescription.of({
      name: 'JavaScript',
      alias: ['ecmascript', 'js', 'node'],
      extensions: ['js', 'mjs', 'cjs'],
      load() {
        return import('@codemirror/lang-javascript').then((m) =>
          m.javascript(),
        );
      },
    }),
    /*@__PURE__*/ LanguageDescription.of({
      name: 'JSON',
      alias: ['json5'],
      extensions: ['json', 'map'],
      load() {
        return import('@codemirror/lang-json').then((m) => m.json());
      },
    }),
    /*@__PURE__*/ LanguageDescription.of({
      name: 'JSX',
      extensions: ['jsx'],
      load() {
        return import('@codemirror/lang-javascript').then((m) =>
          m.javascript({ jsx: true }),
        );
      },
    }),
    /*@__PURE__*/ LanguageDescription.of({
      name: 'Markdown',
      extensions: ['md', 'markdown', 'mkd'],
      load() {
        return import('@codemirror/lang-markdown').then((m) => m.markdown());
      },
    }),
    /*@__PURE__*/ LanguageDescription.of({
      name: 'SCSS',
      extensions: ['scss'],
      load() {
        return import('@codemirror/lang-sass').then((m) => m.sass());
      },
    }),
    /*@__PURE__*/ LanguageDescription.of({
      name: 'SQL',
      extensions: ['sql'],
      load() {
        return sql('StandardSQL');
      },
    }),
    /*@__PURE__*/ LanguageDescription.of({
      name: 'TSX',
      extensions: ['tsx'],
      load() {
        return import('@codemirror/lang-javascript').then((m) =>
          m.javascript({ jsx: true, typescript: true }),
        );
      },
    }),
    /*@__PURE__*/ LanguageDescription.of({
      name: 'TypeScript',
      alias: ['ts'],
      extensions: ['ts', 'mts', 'cts'],
      load() {
        return import('@codemirror/lang-javascript').then((m) =>
          m.javascript({ typescript: true }),
        );
      },
    }),
    /*@__PURE__*/ LanguageDescription.of({
      name: 'XML',
      alias: ['rss', 'wsdl', 'xsd'],
      extensions: ['xml', 'xsl', 'xsd', 'svg'],
      load() {
        return import('@codemirror/lang-xml').then((m) => m.xml());
      },
    }),
    /*@__PURE__*/ LanguageDescription.of({
      name: 'YAML',
      alias: ['yml'],
      extensions: ['yaml', 'yml'],
      load() {
        return import('@codemirror/lang-yaml').then((m) => m.yaml());
      },
    }),

    /*@__PURE__*/ LanguageDescription.of({
      name: 'PowerShell',
      extensions: ['ps1', 'psd1', 'psm1'],
      load() {
        return import('@codemirror/legacy-modes/mode/powershell').then((m) =>
          legacy(m.powerShell),
        );
      },
    }),
    /*@__PURE__*/ LanguageDescription.of({
      name: 'Shell',
      alias: ['bash', 'sh', 'zsh'],
      extensions: ['sh', 'ksh', 'bash'],
      filename: /^PKGBUILD$/,
      load() {
        return import('@codemirror/legacy-modes/mode/shell').then((m) =>
          legacy(m.shell),
        );
      },
    }),
    /*@__PURE__*/ LanguageDescription.of({
      name: 'Text',
      alias: [],
      extensions: ['txt'],
      load() {
        return import('@codemirror/legacy-modes/mode/properties').then((m) =>
          legacy(m.properties),
        );
      },
    }),
  ];
}
