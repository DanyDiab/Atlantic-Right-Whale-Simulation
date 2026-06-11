using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BitMiracle.LibTiff.Classic;
using UnityEngine;
using Newtonsoft.Json.Linq;
public class BackscatterReader : MonoBehaviour
{
    string inPath;
    string outPath;
    
    FileUtilities fileUtil;

    [SerializeField] ProcessingSettings processingSettings;
    RasterProjector rasterProjector;



    void Start()
    {
        rasterProjector = new RasterProjector();
        fileUtil = new FileUtilities();

        string area = processingSettings.AreaToFilePath();
        string outName = area + ".bytes";
        inPath = Path.Combine(Application.dataPath, "Data", "Backscatter", area);
        outPath = Path.Combine(Application.dataPath, "Data", "Processed", "Backscatter", area, outName);

        List<GeoTiffData> data = readInAllTiffs(inPath);
        processBS(data);


        foreach(GeoTiffData gt in data){
            fileUtil.writeGeoTiffToBinary(gt, outPath);
        }
    }

    void processBS(List<GeoTiffData> geoTiffs)
    {
        foreach(GeoTiffData geoTiff in geoTiffs)
        {
            List<float> rawData = geoTiff.Data;

            float min = rawData.Min();
            float max = rawData.Max();

            float range = max - min;
            List<float> normalized = new List<float>(rawData.Count);
            Dictionary<float, int> seen = new Dictionary<float, int>();

            foreach(float dataPoint in rawData)
            {
            
                float normal = (dataPoint - min) / range;
                normalized.Add(normal);
                if (seen.ContainsKey(normal))
                {
                    seen[normal]++;
                }
                else
                {
                    seen[normal] = 1;
                }
            }

            geoTiff.Data = normalized;
            var keys = seen.Keys;

            foreach(float key in keys)
            {
                Debug.LogFormat("Key : {0}\nCount : {1}",key, seen[key]);
            }
            // convert from UTM to lat/long
            // rasterProjector.convert(geoTiff);
        }
    }

    float[] readInJSON(string filePath)
    {
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
    List<GeoTiffData> readInAllTiffs(string dir)
    {
        List<GeoTiffData> geoTiffs = new List<GeoTiffData>();

        if(!Directory.Exists(dir)){
            Debug.LogError("The directory chosen is probably wrong: " + dir);
            return geoTiffs;
        }

        string[] binSearchPattern = {"*.bytes"};
        string[] jsonSearchPattern = {"*.json"};


        IEnumerable<string> binFiles = binSearchPattern.SelectMany(pattern => Directory.EnumerateFiles(dir, pattern));
        
        IEnumerable<string> jsonFiles = jsonSearchPattern.SelectMany(pattern => Directory.EnumerateFiles(dir, pattern));

        for(int i = 0; i < binFiles.Count(); i++)
        {
            string binFile = binFiles.ElementAt(i);
            string jsonFile = jsonFiles.ElementAt(i);

            float[] range = readInJSON(jsonFile);
            GeoTiffData tiffData = fileUtil.ReadGeoTiff(binFile, range);
            geoTiffs.Add(tiffData);
        }

        return geoTiffs;
    }

}