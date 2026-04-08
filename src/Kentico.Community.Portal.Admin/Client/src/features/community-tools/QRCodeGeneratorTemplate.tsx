import {
  Button,
  Colors,
  Headline,
  HeadlineSize,
  Input,
  Spacing,
} from '@kentico/xperience-admin-components';
import React, { useEffect, useRef, useState } from 'react';
import { QRCodeCanvas } from 'qrcode.react';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '../../components/ui/card';
import './qr-code-generator.css';

interface QRCodeGeneratorClientProperties {
  logoUrl: string;
}

const qrCodeSize = 300;
const logoSize = 52;
const logoBackdropSize = 80;
const qrMarginModules = 4;
const fallbackLogoUrl = '/manifest/android-chrome-192x192.png';
const downloadPathPrefixLength = 10;

function toSnakeCase(value: string): string {
  return value
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_+|_+$/g, '');
}

function getDownloadFileName(content: string): string {
  try {
    const url = new URL(content);
    const domain = toSnakeCase(url.hostname.replace(/^www\./, '')) || 'url';
    const pathPrefix =
      toSnakeCase(
        decodeURIComponent(url.pathname)
          .replace(/^\/+|\/+$/g, '')
          .slice(0, downloadPathPrefixLength),
      ) || 'root';

    return `kentico-qr-${domain}-${pathPrefix}.png`;
  } catch {
    return 'kentico-qr-content-value.png';
  }
}

function loadImage(src: string): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const image = new Image();
    image.crossOrigin = 'anonymous';
    image.onload = () => resolve(image);
    image.onerror = () => reject(new Error(`Unable to load image: ${src}`));
    image.src = src;
  });
}

async function buildPaddedLogoDataUrl(logoSrc: string): Promise<string> {
  const logoImage = await loadImage(logoSrc);
  const logoCanvas = document.createElement('canvas');
  logoCanvas.width = logoBackdropSize;
  logoCanvas.height = logoBackdropSize;

  const context = logoCanvas.getContext('2d');

  if (!context) {
    throw new Error('Unable to create logo canvas.');
  }

  context.clearRect(0, 0, logoCanvas.width, logoCanvas.height);
  context.fillStyle = '#ffffff';
  context.beginPath();
  context.arc(
    logoCanvas.width / 2,
    logoCanvas.height / 2,
    logoBackdropSize / 2,
    0,
    Math.PI * 2,
  );
  context.fill();

  context.drawImage(
    logoImage,
    (logoBackdropSize - logoSize) / 2,
    (logoBackdropSize - logoSize) / 2,
    logoSize,
    logoSize,
  );

  return logoCanvas.toDataURL('image/png');
}

export const QRCodeGeneratorTemplate = (
  props: QRCodeGeneratorClientProperties,
) => {
  const [content, setContent] = useState('');
  const [paddedLogoSrc, setPaddedLogoSrc] = useState('');
  const canvasRef = useRef<HTMLCanvasElement>(null);

  const hasValue = content.trim().length > 0;
  const logoSrc = props.logoUrl || fallbackLogoUrl;

  useEffect(() => {
    let isActive = true;

    void buildPaddedLogoDataUrl(logoSrc)
      .then((nextLogoSrc) => {
        if (isActive) {
          setPaddedLogoSrc(nextLogoSrc);
        }
      })
      .catch(() => {
        if (isActive) {
          setPaddedLogoSrc('');
        }
      });

    return () => {
      isActive = false;
    };
  }, [logoSrc]);

  const handleDownload = () => {
    const canvas = canvasRef.current;
    if (!canvas) {
      return;
    }

    const dataUrl = canvas.toDataURL('image/png');
    const link = document.createElement('a');
    link.href = dataUrl;
    link.download = getDownloadFileName(content);
    link.click();
  };

  return (
    <div className="qr-code-generator space-y-6 px-4 pb-8 pt-4 sm:px-6">
      <Headline
        size={HeadlineSize.L}
        labelColor={Colors.TextDefaultOnLight}
        spacingBottom={Spacing.M}
      >
        QR Code Generator
      </Headline>

      <section className="grid gap-6 xl:grid-cols-[minmax(0,1.5fr)_minmax(360px,1fr)]">
        <Card className="qr-code-generator__surface border-0 shadow-lg">
          <CardHeader className="space-y-4">
            <div className="space-y-1">
              <CardDescription className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-text-hint)]">
                Instructions
              </CardDescription>
              <CardTitle className="text-2xl">How to use</CardTitle>
            </div>

            <div className="rounded-[var(--radius-l)] border border-dashed border-[var(--color-border-default)] bg-[var(--color-background-highlighted)] p-4">
              <ol className="list-decimal space-y-2 pl-5 text-sm text-[var(--color-text-low-emphasis)]">
                <li>Enter the URL or text to encode in the field below</li>
                <li>
                  A QR code with the Kentico logo will be generated
                  automatically — use a short URL for the cleanest result
                </li>
                <li>
                  Click{' '}
                  <span className="font-semibold text-[var(--color-text-default-on-light)]">
                    Download PNG
                  </span>{' '}
                  to save the image to your device
                </li>
              </ol>
            </div>
          </CardHeader>

          <CardContent>
            <Input
              type="text"
              label="Content"
              placeholder="https://community.kentico.com"
              name="content"
              value={content}
              onChange={(e) => setContent(e.target.value)}
            />
          </CardContent>
        </Card>

        <Card className="qr-code-generator__surface border-0 shadow-lg">
          <CardHeader className="space-y-1">
            <CardDescription className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-text-hint)]">
              Preview
            </CardDescription>
            <CardTitle className="text-2xl">Generated QR code</CardTitle>
          </CardHeader>

          <CardContent className="flex flex-col items-center gap-4">
            {hasValue ? (
              <>
                <div className="qr-code-generator__preview-frame">
                  <QRCodeCanvas
                    ref={canvasRef}
                    value={content}
                    size={qrCodeSize}
                    level="H"
                    marginSize={qrMarginModules}
                    imageSettings={
                      paddedLogoSrc
                        ? {
                            src: paddedLogoSrc,
                            width: logoBackdropSize,
                            height: logoBackdropSize,
                            excavate: true,
                            crossOrigin: 'anonymous',
                          }
                        : undefined
                    }
                    className="qr-code-generator__canvas"
                  />
                </div>
                <Button
                  label="Download PNG"
                  onClick={() => void handleDownload()}
                />
              </>
            ) : (
              <div className="qr-code-generator__preview-frame flex h-[332px] w-full items-center justify-center rounded-[var(--radius-l)] border-2 border-dashed border-[var(--color-border-default)] p-4 text-center text-sm text-[var(--color-text-hint)]">
                <p>Your QR code will appear here once you enter content</p>
              </div>
            )}
          </CardContent>
        </Card>
      </section>
    </div>
  );
};
