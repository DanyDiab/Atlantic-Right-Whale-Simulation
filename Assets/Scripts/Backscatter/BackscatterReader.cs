using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BitMiracle.LibTiff.Classic;
using UnityEngine;


public class BackscatterReader : MonoBehaviour
{
    string path;
    
    FileUtilities fileUtil;

    [SerializeField] ProcessingSettings processingSettings;


    void Start()
    {
        fileUtil = new FileUtilities();
        string area = processingSettings.AreaToFilePath();
        path = Path.Combine(Application.dataPath, "Data", "Backscatter", area);

        readInAllTiffs(path);

    }

    List<GeoTiffData> readInAllTiffs(string dir)
    {
        List<GeoTiffData> geoTiffs = new List<GeoTiffData>();

        if(!Directory.Exists(dir)){
            Debug.LogError("The directory chosen is probably wrong: " + dir);
            return geoTiffs;
        }

        string[] searchPatterns = {"*.bytes"};

        
        IEnumerable<string> files = searchPatterns.SelectMany(pattern => Directory.EnumerateFiles(dir, pattern));

        foreach(string file in files)
        {
            GeoTiffData tiffData = fileUtil.ReadGeoTiff(file);
            geoTiffs.Add(tiffData);
        }

        return geoTiffs;
    }

}