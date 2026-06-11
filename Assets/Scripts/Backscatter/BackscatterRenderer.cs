using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class BackscatterRenderer : MonoBehaviour
{

    readonly string inFile;
    [SerializeField] ProcessingSettings processingSettings;
    FileUtilities fileUtil;
    GeoTiffData gt; 
    void Start()
    {
        string area = processingSettings.AreaToFilePath();
        string dir = Path.Combine(Application.dataPath, "Data", "Processed", "Backscatter", area);

        fileUtil = new FileUtilities();

        List<GeoTiffData> data = readInTiffs(dir);
        
        Dictionary<float, int> seen = new Dictionary<float, int>();
        foreach(float val in data[0].Data){
            if (seen.ContainsKey(val))
            {
                seen[val]++;
            }
            else
            {
                seen[val] = 1;
            }
        }



        var keys = seen.Keys;

        foreach(float key in keys)
        {
            Debug.LogFormat("Key : {0}\nCount : {1}",key, seen[key]);
        }

    }


    List<GeoTiffData> readInTiffs(string dir){
        
        string[] binSearchPattern = {"*.bytes"};
        
        IEnumerable<string> binFiles = binSearchPattern.SelectMany(pattern => Directory.EnumerateFiles(dir, pattern));

        string[] bins = binFiles.ToArray();

        int count = binFiles.Count();
        List<GeoTiffData> geoTiffs = new List<GeoTiffData>(count);

        for(int i = 0; i < count; i++){
            string binFile = bins[i];

            GeoTiffData tiffData = fileUtil.binToTiffData(binFile);
            geoTiffs.Add(tiffData);
        }
        return geoTiffs;
    }



    void Update()
    {
        
    }
}
