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
      webpackConfigEnv: webpackConfigEnv,
      argv: argv,
    });
  };

  return new Promise((resolve) => {
    //resolve(buildConfig(baseConfig, opts, argv));
    // Ensure the certificate and key exist
    if (!fs.existsSync(cert) || !fs.existsSync(key)) {
      // Wait for the certificate to be generated
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
    } else {
      resolve(buildConfig(baseConfig, opts, argv));
    }
  });
};

function buildConfig(baseConfig, opts, argv) {
  const projectConfig = {
    module: {
      rules: [
        {
          test: /\.(js|ts)x?$/,
          exclude: [/node_modules/],
          loader: 'babel-loader',
        },
      ],
    },
    output: {
      clean: true,
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
