using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class BackscatterRenderer : MonoBehaviour {

    FileUtilities fileUtil;

    [SerializeField] GameObject meshParent;

    [Header("Scriptable Objects")]
    [SerializeField] ProcessingSettings processingSettings;
    
    [Header("Runtime Controls")]
    [SerializeField] bool reloadTextures = false;

    void Start() {
        fileUtil = new FileUtilities();
        loadAndAssignTextures();
    }

    void Update() {
        if(!reloadTextures) return;
        
        loadAndAssignTextures();
        reloadTextures = false;
    }

    void loadAndAssignTextures() {
        string area = processingSettings.AreaToFilePath();
        string dir = Path.Combine(Application.dataPath, "Data", "Processed", "Backscatter", area);

        if(!Directory.Exists(dir)) {
            Debug.LogError("The backscatter directory is missing: " + dir);
            return;
        }

        Renderer[] meshRenderers;
        if(meshParent != null) {
            meshRenderers = meshParent.GetComponentsInChildren<Renderer>(true);
        } else {
            meshRenderers = GetComponentsInChildren<Renderer>(true);
        }

        int numRenderers = meshRenderers.Length;
        if(numRenderers == 0) {
            Debug.LogWarning("No terrain renderers found to apply textures to.");
            return;
        }

        string[] binSearchPattern = {"*.bytes"};
        IEnumerable<string> binFilesEnum = binSearchPattern.SelectMany(pattern => Directory.EnumerateFiles(dir, pattern));
        
        string[] bins = binFilesEnum.ToArray();
        int numFiles = bins.Length;

        for(int i = 0; i < bins.Count(); i++) {
            if(i >= numRenderers) break;

            string binFile = bins[i];
            Renderer renderer = meshRenderers[i];

            GeoTiffData chunkData = fileUtil.binToTiffData(binFile);

            if(chunkData == null || chunkData.Data == null) continue;

            List<float> vals = chunkData.Data;
            MaterialPropertyBlock block = new MaterialPropertyBlock();

            Texture2D dataTexture = new Texture2D(vals.Count, 1, TextureFormat.RFloat, false);
            dataTexture.filterMode = FilterMode.Point;
            dataTexture.wrapMode = TextureWrapMode.Clamp;

            for (int j = 0; j < vals.Count; j++) {
                Color pixelColor = new Color(vals[j], 0.0f, 0.0f, 0.0f);
                dataTexture.SetPixel(j, 0, pixelColor);
            }

            dataTexture.Apply();

            block.SetFloat("TotalElements", vals.Count);
            block.SetTexture("_Data", dataTexture);

            renderer.SetPropertyBlock(block);
        }
    }
}