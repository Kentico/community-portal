const webpackMerge = require('webpack-merge');
const path = require('path');
const fs = require('fs');
const spawn = require('child_process').spawn;

const baseWebpackConfig = require('@kentico/xperience-webpack-config');

const { cert, key } = getDotnetCertPaths();

module.exports = (opts, argv) => {
  const baseConfig = (webpackConfigEnv, argv) => {
    return baseWebpackConfig({
      orgName: 'kentico-community',
      projectName: 'portal-web-admin',
      webpackConfigEnv,
      argv,
    });
  };

  return new Promise((resolve) => {
    // Skip cert/key creation for prod builds or if the cert/key exist
    // See: https://github.com/dotnet/aspnetcore/issues/58330#issuecomment-2423338006
    if (
      argv.mode === 'production' ||
      (fs.existsSync(cert) && fs.existsSync(key))
    ) {
      console.info('Skipping dev certification creation');
      resolve(buildConfig(baseConfig, opts, argv));

      return;
    }

    // Wait for the certificate to be generated
    console.info('Creating aspnet core dev-certs');
    spawn(
      'dotnet',
      [
        'dev-certs',
        'https',
        '--export-path',
        cert,
        '--format',
        'Pem',
        '--no-password',
      ],
      { stdio: 'inherit' },
    ).on('exit', (code) => {
      resolve(buildConfig(baseConfig, opts, argv));
      if (code) {
        process.exit(code);
      }
    });
  });
};

function buildConfig(baseConfig, opts, argv) {
  const projectConfig = {
    devtool: 'inline-source-map',
    module: {
      rules: [
        {
          test: /\.(js|ts)x?$/,
          exclude: [/node_modules/],
          loader: 'babel-loader',
        },
        {
          test: /\.css$/i,
          use: ['css-loader'],
        },
      ],
    },
    output: {
      clean: true,
      /**
       * Separate chunks will cause Xperience's admin to fail when trying to load the bundle.
       * Example error: Unhandled exception. System.InvalidOperationException: Expected 1 file for source path '/adminresources/kentico-community.portal.web.admin/entry.js' in directory '/adminresources/kentico-community.portal.web.admin', but got '130'.
       * This setting forces the output to be a single chunk
       * https://github.com/webpack/webpack/issues/12464#issuecomment-1911309972
       */
      chunkFormat: false,
    },
    devServer: {
      port: 3019,
      server: {
        type: 'https',
        options: {
          key,
          cert,
        },
      },
    },
  };

  return webpackMerge.merge(projectConfig, baseConfig(opts, argv));
}

/**
 * Retrieves the ASP.NET Core dev certificate paths
 */
function getDotnetCertPaths() {
  const baseFolder =
    process.env.APPDATA !== undefined && process.env.APPDATA !== ''
      ? `${process.env.APPDATA}/ASP.NET/https`
      : `${process.env.HOME}/.aspnet/https`;

  const certificateName = process.env.npm_package_name;

  const cert = path.join(baseFolder, `${certificateName}.pem`);
  const key = path.join(baseFolder, `${certificateName}.key`);

  return { cert, key };
}
