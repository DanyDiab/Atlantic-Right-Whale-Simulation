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
    [SerializeField] Chunks globalChunks;

    void Start() {
        string area = processingSettings.AreaToFilePath();
        string dir = Path.Combine(Application.dataPath, "Data", "Processed", "Backscatter", area);

        fileUtil = new FileUtilities();

        readInTiffs(dir);
        assignTexture();
    }

    void assignTexture() {
        if(globalChunks.chunks == null) return;

        Renderer[] meshRenderers;
        if(meshParent != null) {
            meshRenderers = meshParent.GetComponentsInChildren<Renderer>(true);
        } else {
            meshRenderers = GetComponentsInChildren<Renderer>(true);
        }

        int numRenderers = meshRenderers.Length;

        for(int i = 0; i < globalChunks.chunks.Count; i++) {
            if(i >= numRenderers) break;

            ChunkData chunk = globalChunks.chunks[i];
            Renderer renderer = meshRenderers[i];

            if(chunk.BackscatterData == null || chunk.BackscatterData.Data == null) continue;

            List<float> vals = chunk.BackscatterData.Data;
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

    void readInTiffs(string dir) {
        if(!Directory.Exists(dir)) {
            Debug.LogError("The directory chosen is probably wrong: " + dir);
            return;
        }

        string[] binSearchPattern = {"*.bytes"};
        IEnumerable<string> binFiles = binSearchPattern.SelectMany(pattern => Directory.EnumerateFiles(dir, pattern));
        string[] bins = binFiles.ToArray();
        int count = bins.Length;

        if(globalChunks.chunks == null) {
            globalChunks.chunks = new List<ChunkData>(count);
        }

        for(int i = 0; i < count; i++) {
            string binFile = bins[i];

            if(i < globalChunks.chunks.Count) {
                if(globalChunks.chunks[i].BackscatterData == null) {
                    GeoTiffData tiffData = fileUtil.binToTiffData(binFile);
                    globalChunks.chunks[i].BackscatterData = tiffData;
                }
            } else {
                GeoTiffData tiffData = fileUtil.binToTiffData(binFile);
                ChunkData newChunk = new ChunkData(null, tiffData);
                globalChunks.chunks.Add(newChunk);
            }
        }
    }
}