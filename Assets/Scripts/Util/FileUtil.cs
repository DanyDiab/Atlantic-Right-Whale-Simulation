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

            int scanlineSize = image.ScanlineSize();
            byte[] buffer = new byte[scanlineSize];

            List<float> rawData = new List<float>(width * height);

            for (int i = 0; i < height; i++) {
                if (!image.ReadScanline(buffer, i)) {
                    UnityEngine.Debug.LogError("Error reading scanline " + i);
                    break;
                }

                if (bitsPerSample == 32) {
                    for (int j = 0; j < scanlineSize; j += 4) {
                        float depthValue = System.BitConverter.ToSingle(buffer, j);
                        rawData.Add(depthValue);
                    }
                } else if (bitsPerSample == 16) {
                    for (int j = 0; j < scanlineSize; j += 2) {
                        ushort shortValue = System.BitConverter.ToUInt16(buffer, j);
                        rawData.Add((float)shortValue);
                    }
                } else {
                    for (int j = 0; j < scanlineSize; j++) {
                        rawData.Add((float)buffer[j] / 255.0f);
                    }
                }
            }

            GeoTiffData result = new GeoTiffData();
            result.Width = width;
            result.Height = height;
            result.PixelScale = pixelScale;
            result.Data = rawData;

            return result;
        }
    }
}