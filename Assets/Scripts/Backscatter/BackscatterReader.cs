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

    void readInAllTiffs(string dir)
    {
        if(!Directory.Exists(dir)){
            Debug.Log("The directory chosen is probably wrong: " + dir);
            return;
        }

        string[] searchPatterns = {"*.bytes"};

        
        IEnumerable<string> files = searchPatterns.SelectMany(pattern => Directory.EnumerateFiles(dir, pattern));


        foreach(string file in files)
        {
            GeoTiffData tiffData = fileUtil.ReadGeoTiff(file);
        }
    }

}