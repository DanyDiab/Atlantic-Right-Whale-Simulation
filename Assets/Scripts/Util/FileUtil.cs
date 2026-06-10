using System;
using System.Collections.Generic;
using System.Linq;
using BitMiracle.LibTiff.Classic;
using UnityEngine;
using System.IO;

public class FileUtilities
{
    public GeoTiffData ReadGeoTiff(string filePath, float[] range) {
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
            FieldValue[] tiepointField = image.GetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG);


            double[] tiePoints = tiepointField[1].ToDoubleArray();

            float startingLong = (float)tiePoints[3];
            float startingLat = (float)tiePoints[4];

            Vector2 startingCoords = new Vector2(startingLong, startingLat);


            FieldValue[] scaleField = image.GetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG);
            if (scaleField == null || scaleField.Length < 2) {
                Debug.LogError("Failed to read pixel scale tag for: " + filePath);
                return null;
            }

            byte[] scaleBytes = scaleField[1].GetBytes(); 
            double[] pixelScale = new double[3];
            Buffer.BlockCopy(scaleBytes, 0, pixelScale, 0, scaleBytes.Length);

            float[] rawDataArray = new float[width * height];

            bool success = image.IsTiled() ? ReadTiledTiff(image, rawDataArray, width, height, bitsPerSample, range) : ReadStripedTiff(image, rawDataArray, width, height, bitsPerSample, range);

            if (!success) {
                return null;
            }

            GeoTiffData result = new GeoTiffData();
            result.Width = width;
            result.Height = height;
            result.startCoordsMeters = startingCoords;
            result.PixelScale = pixelScale;
            result.Data = new List<float>(rawDataArray);

            return result;
        }
    }

    bool ReadTiledTiff(Tiff image, float[] rawDataArray, int width, int height, int bitsPerSample, float[] range) {
        int tileWidth = image.GetField(TiffTag.TILEWIDTH)[0].ToInt();
        int tileHeight = image.GetField(TiffTag.TILELENGTH)[0].ToInt();
        int tileBufferSize = image.TileSize();
        byte[] tileBuffer = new byte[tileBufferSize];

        int bytesPerPixel = bitsPerSample / 8;

        float min = range[0];
        float max = range[1];

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
                            rawDataArray[targetArrayIndex] = Mathf.Clamp(System.BitConverter.ToSingle(tileBuffer, tileBufferIndex), min, max);
                        } else if (bitsPerSample == 16) {
                            ushort shortValue = (ushort) Math.Min(System.BitConverter.ToUInt16(tileBuffer, tileBufferIndex), max);
                            rawDataArray[targetArrayIndex] = shortValue;
                        } else {
                            rawDataArray[targetArrayIndex] = Mathf.Clamp(tileBuffer[tileBufferIndex] / 255.0f, min, max);
                        }
                    }
                }
            }
        }
        return true;
    }

    bool ReadStripedTiff(Tiff image, float[] rawDataArray, int width, int height, int bitsPerSample, float[] range) {
        int scanlineSize = image.ScanlineSize();
        byte[] buffer = new byte[scanlineSize];

        float min = range[0];
        float max = range[1];
        for (int i = 0; i < height; i++) {
            if (!image.ReadScanline(buffer, i)) {
                UnityEngine.Debug.LogError("Error reading scanline " + i);
                return false;
            }

            if (bitsPerSample == 32) {
                for (int j = 0; j < scanlineSize; j += 4) {
                    int pixelX = j / 4;
                    if (pixelX < width) {
                        rawDataArray[i * width + pixelX] = Mathf.Clamp(System.BitConverter.ToSingle(buffer, j), min, max);
                    }
                }
            } else if (bitsPerSample == 16) {
                for (int j = 0; j < scanlineSize; j += 2) {
                    int pixelX = j / 2;
                    if (pixelX < width) {
                        ushort shortValue = (ushort) Math.Min(System.BitConverter.ToUInt16(buffer, j), max);
                        rawDataArray[i * width + pixelX] = shortValue;
                    }
                }
            } else {
                for (int j = 0; j < scanlineSize; j++) {
                    if (j < width) {
                        rawDataArray[i * width + j] = Mathf.Clamp(buffer[j] / 255.0f, min, max);
                    }
                }
            }
        }
        return true;
    }


    public void writeGeoTiffToBinary(GeoTiffData gtData, string filePath){
        string directoryPath = Path.GetDirectoryName(filePath);
    
        if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath)) {
            Directory.CreateDirectory(directoryPath);
        }

        using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None)) {
            using (BinaryWriter writer = new BinaryWriter(fs)){
                writer.Write(gtData.Width);
                writer.Write(gtData.Height);

                writer.Write(gtData.Data.Count);

                foreach(float val in gtData.Data){
                    writer.Write(val);
                }
            }
        }
    }

    public void writeToBinary( DepthDataRecord depthDataRecord, string filePath) {
        string directoryPath = Path.GetDirectoryName(filePath);
    
        if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath)) {
            Directory.CreateDirectory(directoryPath);
        }
        
        using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None)) {
            using (BinaryWriter writer = new BinaryWriter(fs)) {
                writer.Write(depthDataRecord.tiffData.Width);
                writer.Write(depthDataRecord.tiffData.Height);
                
                writer.Write(depthDataRecord.ChunkPosition.x);
                writer.Write(depthDataRecord.ChunkPosition.y);

                writer.Write(depthDataRecord.tiffData.Data.Count);
                
                foreach (float depth in depthDataRecord.tiffData.Data) {
                    writer.Write(depth);
                }
            }
        }
    }

}