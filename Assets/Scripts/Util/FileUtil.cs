using System;
using System.Collections.Generic;
using BitMiracle.LibTiff.Classic;
using UnityEngine;

public class FileUtilities
{
    public GeoTiffData ReadGeoTiff(string filePath) {
        if (string.IsNullOrEmpty(filePath)) {
            return null;
        }

        using (Tiff image = Tiff.Open(filePath, "r")) {
            if (image == null) {
                UnityEngine.Debug.LogError("Failed to open TIFF file at: " + filePath);
                return null;
            }

            int width = image.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int height = image.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
            int bitsPerSample = image.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();

            FieldValue[] scaleField = image.GetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG);
            if (scaleField == null || scaleField.Length < 2) {
                UnityEngine.Debug.LogError("Failed to read pixel scale tag for: " + filePath);
                return null;
            }

            byte[] scaleBytes = scaleField[1].GetBytes(); 
            double[] pixelScale = new double[3];
            Buffer.BlockCopy(scaleBytes, 0, pixelScale, 0, scaleBytes.Length);

            float[] rawDataArray = new float[width * height];

            bool success = image.IsTiled() ? ReadTiledTiff(image, rawDataArray, width, height, bitsPerSample) : ReadStripedTiff(image, rawDataArray, width, height, bitsPerSample);

            if (!success) {
                return null;
            }

            GeoTiffData result = new GeoTiffData();
            result.Width = width;
            result.Height = height;
            result.PixelScale = pixelScale;
            result.Data = new List<float>(rawDataArray);

            return result;
        }
    }

    bool ReadTiledTiff(Tiff image, float[] rawDataArray, int width, int height, int bitsPerSample) {
        int tileWidth = image.GetField(TiffTag.TILEWIDTH)[0].ToInt();
        int tileHeight = image.GetField(TiffTag.TILELENGTH)[0].ToInt();
        int tileBufferSize = image.TileSize();
        byte[] tileBuffer = new byte[tileBufferSize];

        int bytesPerPixel = bitsPerSample / 8;

        for (int y = 0; y < height; y += tileHeight) {
            for (int x = 0; x < width; x += tileWidth) {
                if (image.ReadTile(tileBuffer, 0, x, y, 0, 0) == -1) {
                    UnityEngine.Debug.LogError($"Error reading tile at X:{x}, Y:{y}");
                    return false;
                }

                for (int row = 0; row < tileHeight; row++) {
                    int pixelY = y + row;
                    if (pixelY >= height) {
                        break; 
                    }

                    for (int col = 0; col < tileWidth; col++) {
                        int pixelX = x + col;
                        if (pixelX >= width) {
                            break; 
                        }

                        int tileBufferIndex = (row * tileWidth + col) * bytesPerPixel;
                        int targetArrayIndex = pixelY * width + pixelX;

                        if (bitsPerSample == 32) {
                            rawDataArray[targetArrayIndex] = System.BitConverter.ToSingle(tileBuffer, tileBufferIndex);
                        } else if (bitsPerSample == 16) {
                            ushort shortValue = System.BitConverter.ToUInt16(tileBuffer, tileBufferIndex);
                            rawDataArray[targetArrayIndex] = (float)shortValue;
                        } else {
                            rawDataArray[targetArrayIndex] = (float)tileBuffer[tileBufferIndex] / 255.0f;
                        }
                    }
                }
            }
        }
        return true;
    }

    bool ReadStripedTiff(Tiff image, float[] rawDataArray, int width, int height, int bitsPerSample) {
        int scanlineSize = image.ScanlineSize();
        byte[] buffer = new byte[scanlineSize];

        for (int i = 0; i < height; i++) {
            if (!image.ReadScanline(buffer, i)) {
                UnityEngine.Debug.LogError("Error reading scanline " + i);
                return false;
            }

            if (bitsPerSample == 32) {
                for (int j = 0; j < scanlineSize; j += 4) {
                    int pixelX = j / 4;
                    if (pixelX < width) {
                        rawDataArray[i * width + pixelX] = System.BitConverter.ToSingle(buffer, j);
                    }
                }
            } else if (bitsPerSample == 16) {
                for (int j = 0; j < scanlineSize; j += 2) {
                    int pixelX = j / 2;
                    if (pixelX < width) {
                        ushort shortValue = System.BitConverter.ToUInt16(buffer, j);
                        rawDataArray[i * width + pixelX] = (float)shortValue;
                    }
                }
            } else {
                for (int j = 0; j < scanlineSize; j++) {
                    if (j < width) {
                        rawDataArray[i * width + j] = (float)buffer[j] / 255.0f;
                    }
                }
            }
        }
        return true;
    }
}