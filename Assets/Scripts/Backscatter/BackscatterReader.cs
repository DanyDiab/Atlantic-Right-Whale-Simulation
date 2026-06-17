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



            int numPointsX = (int)(Mathf.Abs(geoSize.x) / geoPointDistance);
            int numPointsY = (int)(Mathf.Abs(geoSize.y) / geoPointDistance);

            int width = masterBackscatter.Width;
            int height = masterBackscatter.Height;

            int startIndex = (width * numPointsY) + numPointsX;

            double[] pixelSize = depthRecord.tiffData.PixelScale;
            


            List<float> chunkBS = new List<float>(numPointsX * numPointsY);
            
            for (int y = 0; y < numPointsY; y++) {
                int rowStartIndex = startIndex + (y * width);
                
                if (rowStartIndex >= 0 && rowStartIndex + chunkWidth <= masterBackscatter.Data.Count) {
                    List<float> rowData = masterBackscatter.Data.GetRange(rowStartIndex, chunkWidth);
                    chunkBS.AddRange(rowData);
                }
            }

            GeoTiffData chunkTiff = new GeoTiffData();
            chunkTiff.Data = chunkBS;
            chunkTiff.Width = numPointsX;
            chunkTiff.Height = numPointsY;
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

        int rawDataCount = rawData.Count;
        for (int i = 0; i < rawDataCount; i++) {
            float dataPoint = rawData[i];
            float normal = (dataPoint - min) / range;
            normalized.Add(normal);
        }

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