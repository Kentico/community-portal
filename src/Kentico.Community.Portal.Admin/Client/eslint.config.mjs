import globals from 'globals';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import js from '@eslint/js';
import { FlatCompat } from '@eslint/eslintrc';
import tsParser from '@typescript-eslint/parser';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const compat = new FlatCompat({
  baseDirectory: __dirname,
  recommendedConfig: js.configs.recommended,
  allConfig: js.configs.all,
});

export default [
  {
    ignores: ['**/webpack.config.js'],
  },
  ...compat.extends('plugin:@typescript-eslint/recommended', 'prettier'),
  {
    languageOptions: {
      globals: {
        ...globals.browser,
      },

      parser: tsParser,
      ecmaVersion: 'latest',
      sourceType: 'module',

      parserOptions: {
        project: './tsconfig.json',
      },
    },

    rules: {
      '@typescript-eslint/no-invalid-void-type': 'off',

      '@typescript-eslint/no-misused-promises': [
        2,
        {
          checksVoidReturn: {
            attributes: false,
          },
        },
      ],

      quotes: [
        'error',
        'single',
        {
          avoidEscape: true,
        },
      ],

      'jsx-quotes': ['error', 'prefer-double'],
      eqeqeq: ['error', 'always'],
      'no-var': 'error',
      'no-unused-vars': 'off',
      'no-console': 'error',
      'no-debugger': 'error',

      '@typescript-eslint/no-unused-vars': [
        'error',
        {
          args: 'after-used',
          argsIgnorePattern: '^_',
        },
      ],

      'sort-imports': [
        'error',
        {
          ignoreDeclarationSort: true,
        },
      ],

      '@typescript-eslint/no-explicit-any': 'error',
      '@typescript-eslint/no-require-imports': 'error',

      '@typescript-eslint/naming-convention': [
        'error',
        {
          selector: 'default',
          format: ['PascalCase'],

          filter: {
            regex: '^((data|aria|xp|invalid|--[a-z]+)[-].+)|[0-9]+$',
            match: false,
          },
        },
        {
          selector: 'variable',
          format: ['PascalCase', 'camelCase'],
        },
        {
          selector: 'variable',
          format: ['UPPER_CASE'],

          filter: {
            regex: '^__[A-Za-z_]+__$',
            match: true,
          },

          leadingUnderscore: 'requireDouble',
          trailingUnderscore: 'requireDouble',
        },
        {
          selector: 'function',
          format: ['camelCase'],
        },
        {
          selector: 'parameter',
          format: ['camelCase'],
          leadingUnderscore: 'allow',
        },
        {
          selector: 'enum',
          format: ['PascalCase'],
        },
        {
          selector: 'objectLiteralProperty',
          format: ['camelCase', 'PascalCase'],

          filter: {
            regex: '^((data|aria|xp|invalid|--[a-z]+)[-].+)|[0-9]+$',
            match: false,
          },
        },
        {
          selector: 'objectLiteralMethod',
          format: ['camelCase'],
        },
        {
          selector: 'typeProperty',
          format: ['camelCase'],

          filter: {
            regex: '^(data)[-].+$',
            match: false,
          },
        },
        {
          selector: 'typeLike',
          format: ['PascalCase'],
        },
        {
          selector: 'classMethod',
          format: ['camelCase'],
        },
        {
          selector: 'classProperty',
          format: ['camelCase'],
        },
        {
          selector: 'interface',
          format: ['PascalCase'],

          custom: {
            regex: '^I[A-Z]',
            match: false,
          },
        },
      ],

      '@typescript-eslint/await-thenable': 'warn',
      '@typescript-eslint/prefer-enum-initializers': 'error',
      '@typescript-eslint/no-non-null-assertion': 'error',
    },
  },
];
