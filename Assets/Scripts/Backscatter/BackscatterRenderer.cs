using System;
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

    [SerializeField] GameObject meshParent;
    void Start()
    {
        string area = processingSettings.AreaToFilePath();
        string dir = Path.Combine(Application.dataPath, "Data", "Processed", "Backscatter", area);

        fileUtil = new FileUtilities();

        List<GeoTiffData> data = readInTiffs(dir);
        
        assignTexture(data[0]);
    }

    void assignTexture(GeoTiffData data){
        Renderer[] meshRenderer = GetComponentsInChildren<Renderer>(true);

        List<float> vals = data.Data;

        foreach(Renderer renderer in meshRenderer){
            MaterialPropertyBlock block = new MaterialPropertyBlock();

            Texture2D dataTexture = new Texture2D(vals.Count, 1, TextureFormat.RFloat, false);
            dataTexture.filterMode = FilterMode.Point;
            dataTexture.wrapMode = TextureWrapMode.Clamp;


            for (int i = 0; i < vals.Count; i++) {
                Color pixelColor = new Color(vals[i], 0.0f, 0.0f, 0.0f);
                dataTexture.SetPixel(i, 0, pixelColor);
            }
        
            dataTexture.Apply();

            block.SetFloat("TotalElements", data.Data.Count);
            block.SetTexture("_Data", dataTexture);

            renderer.SetPropertyBlock(block);
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
