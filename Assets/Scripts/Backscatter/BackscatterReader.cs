using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BitMiracle.LibTiff.Classic;
using UnityEngine;
using Newtonsoft.Json.Linq;

public class BackscatterReader : MonoBehaviour {

    FileUtilities fileUtil;
    RasterProjector rasterProjector;

    [Header("Scriptable Objects")]
    [SerializeField] ProcessingSettings processingSettings;

    [ContextMenu("Bake Backscatter Data")]
    public void BakeData() {
        rasterProjector = new RasterProjector();
        fileUtil = new FileUtilities();

        string area = processingSettings.AreaToFilePath();
        string bsInPath = Path.Combine(Application.dataPath, "Data", "Backscatter", area);
        string bsOutDir = Path.Combine(Application.dataPath, "Data", "Processed", "Backscatter", area);
        string bathyDir = Path.Combine(Application.dataPath, "Data", "Processed", area);

        if (!Directory.Exists(bsOutDir)) {
            Directory.CreateDirectory(bsOutDir);
        }

        List<GeoTiffData> masterTiffs = readInAllTiffs(bsInPath);

        if (masterTiffs.Count == 0) return;

        GeoTiffData masterBackscatter = processMasterBS(masterTiffs);

        bakeChunksToDisk(masterBackscatter, bathyDir, bsOutDir);

        Debug.Log("Backscatter baking complete. Data is ready for runtime loading.");
    }

    void bakeChunksToDisk(GeoTiffData masterBackscatter, string bathyDir, string bsOutDir) {
        if (!Directory.Exists(bathyDir)) {
            Debug.LogError("Bathymetry directory missing: " + bathyDir);
            return;
        }

        string[] bathyFiles = Directory.GetFiles(bathyDir, "*.bytes", SearchOption.TopDirectoryOnly);
        float chunkSize = processingSettings.chunkSize;
        float geoPointDistance = 0.1f;

        int numFiles = bathyFiles.Length;
        int numToRun = processingSettings.numToRun;

        for (int i = 0; i < numFiles; i++) {
            if(numToRun != -1 && i >= numToRun) break;
            string bathyFile = bathyFiles[i];
            DepthDataRecord depthRecord = fileUtil.binToDepthRecord(bathyFile);
            // need to grab the non normalized position
            Vector2 chunkPos = depthRecord.tiffData.startCoordsMeters;
            int chunkWidth = depthRecord.tiffData.Width;
            int chunkHeight = depthRecord.tiffData.Height;
            Vector2 backScatterPos = masterBackscatter.startCoordsMeters;

            Vector2 geoSize = chunkPos - backScatterPos;

            Debug.LogFormat("Chunk: {0} | Chunk Pos: {1} | Master Pos: {2} | GeoSize (Offset in Degrees): {3}", bathyFile, chunkPos, backScatterPos, geoSize);

            int offsetX = Mathf.RoundToInt((chunkPos.x - backScatterPos.x) / geoPointDistance);
            int offsetY = Mathf.RoundToInt((backScatterPos.y - chunkPos.y) / geoPointDistance);

            int width = masterBackscatter.Width;
            int height = masterBackscatter.Height;
            
            Debug.LogFormat("OffsetX: {0}, OffsetY: {1} | Master Width: {2}, Master Height: {3}", offsetX, offsetY, width, height);

            int startIndex = (width * offsetY) + offsetX;

            double[] pixelSize = depthRecord.tiffData.PixelScale;
            
            float noDataValue = 1.0f;

            List<float> chunkBS = new List<float>(chunkWidth * chunkHeight);
            
            for (int y = 0; y < chunkHeight; y++) {
                int currentY = offsetY + y;

                if (currentY < 0 || currentY >= height) {
                    chunkBS.AddRange(Enumerable.Repeat(noDataValue, chunkWidth));
                    continue; 
                }

                int startX = offsetX;
                int endX = offsetX + chunkWidth;

                if (endX <= 0 || startX >= width) {
                    chunkBS.AddRange(Enumerable.Repeat(noDataValue, chunkWidth));
                    continue; 
                }

                if (startX >= 0 && endX <= width) {
                    int rowStartIndex = (currentY * width) + startX;
                    chunkBS.AddRange(masterBackscatter.Data.GetRange(rowStartIndex, chunkWidth));
                    continue;
                }

                for (int x = 0; x < chunkWidth; x++) {
                    int currentX = startX + x;
                
                    if (currentX >= 0 && currentX < width) {
                        int index = (currentY * width) + currentX;
                        chunkBS.Add(masterBackscatter.Data[index]);
                        continue;
                    }

                    chunkBS.Add(noDataValue);
                    }
                }

            GeoTiffData chunkTiff = new GeoTiffData();
            chunkTiff.Data = chunkBS;
            chunkTiff.Width = chunkWidth;
            chunkTiff.Height = chunkHeight;
            chunkTiff.startCoordsMeters = chunkPos;
            chunkTiff.PixelScale = pixelSize;

            string fileName = Path.GetFileName(bathyFile);
            string outPath = Path.Combine(bsOutDir, fileName);
            fileUtil.writeGeoTiffToBinary(chunkTiff, outPath);

            
        }
    }

    GeoTiffData processMasterBS(List<GeoTiffData> masterTiffs) {
        if (masterTiffs == null || masterTiffs.Count == 0) return null;

        GeoTiffData masterTiff = masterTiffs[0];
        List<float> rawData = masterTiff.Data;

        float min = rawData.Min();
        float max = rawData.Max();
        float range = max - min;
        
        List<float> normalized = new List<float>(rawData.Count);
        int numberNoData = 0;
        int rawDataCount = rawData.Count;
        for (int i = 0; i < rawDataCount; i++) {
            float dataPoint = rawData[i];
            float normal = (dataPoint - min) / range;
            normalized.Add(normal);
            if(normal == max) numberNoData++;
        }

        Debug.LogFormat("There are {0} max points, out of {1} total", numberNoData, rawDataCount);

        masterTiff.Data = normalized;
        rasterProjector.convert(masterTiff);

        return masterTiff;
    }

    float[] readInJSON(string filePath) {
        if (!File.Exists(filePath)) {
            return new float[2] { 0.0f, 0.0f };
        }

        string jsonContent = File.ReadAllText(filePath);
        JObject jsonObject = JObject.Parse(jsonContent);

        JToken intensityRange = jsonObject["productDefaults"]["intensityRange"];
        if (intensityRange == null) {
            return new float[2] { 0.0f, 0.0f };
        }

        string minStr = intensityRange["intensityRangeMin"]?.ToString();
        string maxStr = intensityRange["intensityRangeMax"]?.ToString();

        float min = float.TryParse(minStr, out float parsedMin) ? parsedMin : 0.0f;
        float max = float.TryParse(maxStr, out float parsedMax) ? parsedMax : 0.0f;

        return new float[2] { min, max };
    }
    
    List<GeoTiffData> readInAllTiffs(string dir) {
        if (!Directory.Exists(dir)) {
            Debug.LogError("The directory chosen is probably wrong: " + dir);
            return new List<GeoTiffData>();
        }

        string[] binSearchPattern = {"*.bytes"};
        string[] jsonSearchPattern = {"*.json"};

        IEnumerable<string> binFiles = binSearchPattern.SelectMany(pattern => Directory.EnumerateFiles(dir, pattern));
        IEnumerable<string> jsonFiles = jsonSearchPattern.SelectMany(pattern => Directory.EnumerateFiles(dir, pattern));

        int binCount = binFiles.Count();
        List<GeoTiffData> masterBackscatter = new List<GeoTiffData>(binCount);

        for (int i = 0; i < binCount; i++) {
            string binFile = binFiles.ElementAt(i);
            string jsonFile = jsonFiles.ElementAt(i);

            float[] range = readInJSON(jsonFile);
            GeoTiffData tiffData = fileUtil.ReadGeoTiff(binFile, range);
            
            masterBackscatter.Add(tiffData);
        }

        return masterBackscatter;
    }
}