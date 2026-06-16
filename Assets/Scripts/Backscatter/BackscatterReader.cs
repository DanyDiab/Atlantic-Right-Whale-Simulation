using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BitMiracle.LibTiff.Classic;
using UnityEngine;
using Newtonsoft.Json.Linq;

public class BackscatterReader : MonoBehaviour {
    string inPath;
    string outPath;
    
    FileUtilities fileUtil;
    RasterProjector rasterProjector;

    [Header("Scriptable Objects")]
    [SerializeField] ProcessingSettings processingSettings;
    [SerializeField] Chunks globalChunks;

    void Start() {
        rasterProjector = new RasterProjector();
        fileUtil = new FileUtilities();

        string area = processingSettings.AreaToFilePath();
        string outName = area + ".bytes";
        inPath = Path.Combine(Application.dataPath, "Data", "Backscatter", area);
        outPath = Path.Combine(Application.dataPath, "Data", "Processed", "Backscatter", area, outName);

        readInAllTiffs(inPath);
        processBS();

        foreach(ChunkData chunk in globalChunks.chunks) {
            if(chunk.BackscatterData != null) {
                fileUtil.writeGeoTiffToBinary(chunk.BackscatterData, outPath);
            }
        }
    }

    void processBS() {
        if(globalChunks.chunks == null) return;

        foreach(ChunkData chunk in globalChunks.chunks) {
            GeoTiffData geoTiff = chunk.BackscatterData;
            if(geoTiff == null || geoTiff.Data == null) continue;

            List<float> rawData = geoTiff.Data;

            float min = rawData.Min();
            float max = rawData.Max();

            float range = max - min;
            List<float> normalized = new List<float>(rawData.Count);
            Dictionary<float, int> seen = new Dictionary<float, int>();

            foreach(float dataPoint in rawData) {
            
                float normal = (dataPoint - min) / range;
                normalized.Add(normal);
                
                if (seen.ContainsKey(normal)) {
                    seen[normal]++;
                } else {
                    seen[normal] = 1;
                }
            }

            geoTiff.Data = normalized;
            Dictionary<float, int>.KeyCollection keys = seen.Keys;

            foreach(float key in keys) {
                Debug.LogFormat("Key : {0}\nCount : {1}", key, seen[key]);
            }
            // convert from UTM to lat/long
            // rasterProjector.convert(geoTiff);
        }
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
    
    void readInAllTiffs(string dir) {
        if(!Directory.Exists(dir)) {
            Debug.LogError("The directory chosen is probably wrong: " + dir);
            return;
        }

        string[] binSearchPattern = {"*.bytes"};
        string[] jsonSearchPattern = {"*.json"};

        IEnumerable<string> binFiles = binSearchPattern.SelectMany(pattern => Directory.EnumerateFiles(dir, pattern));
        IEnumerable<string> jsonFiles = jsonSearchPattern.SelectMany(pattern => Directory.EnumerateFiles(dir, pattern));

        int binCount = binFiles.Count();

        if(globalChunks.chunks == null) {
            globalChunks.chunks = new List<ChunkData>(binCount);
        }

        for(int i = 0; i < binCount; i++) {
            string binFile = binFiles.ElementAt(i);
            string jsonFile = jsonFiles.ElementAt(i);

            float[] range = readInJSON(jsonFile);
            GeoTiffData tiffData = fileUtil.ReadGeoTiff(binFile, range);
            
            if(i < globalChunks.chunks.Count) {
                globalChunks.chunks[i].BackscatterData = tiffData;
            } else {
                ChunkData newChunk = new ChunkData(null, tiffData);
                globalChunks.chunks.Add(newChunk);
            }
        }
    }
}