using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;

public class BathymetryReader : MonoBehaviour {

    string readingDir;
    string writingDir;
    BathymetryPatcher patcher;
    FileUtilities fileUtil;

    [SerializeField] bool reloadReader = false;
    [Header("Scriptable Objects")]
    [SerializeField] ProcessingSettings processingSettings;

    [SerializeField] Chunks globalChunks;

    char[] fileDelims = {'/', '\\'};

    void Update() {
        if(!reloadReader) return;

        startPipeline();

        reloadReader = false;
    }

    public void Start() {
        patcher = new BathymetryPatcher(processingSettings);
        fileUtil = new FileUtilities();
        startPipeline();
    }

    void startPipeline() {
        string path = processingSettings.AreaToFilePath();

        readingDir = Path.Combine(Application.dataPath,"Data", "Bathymetry", path);
        writingDir = Path.Combine(Application.dataPath, "Data", "Processed", path);
        
        if (!Directory.Exists(writingDir)) {
            Directory.CreateDirectory(writingDir);
        }
        
        readInAllTiffs(readingDir, writingDir);        
    }

    private void generateChunkOffsets(List<ChunkData> chunkList, List<string> fileNames) {

        Vector2 min = new Vector2(int.MaxValue, int.MaxValue);

        int numRecords = chunkList.Count;
        List<Vector2> coordsList = new List<Vector2>(numRecords);
        
        for(int i = 0; i < numRecords; i++) {
            string filename = fileNames[i];

            Tuple<Vector2,Vector2> coords = parseCoords(filename);
            Vector2 utm = coords.Item1;
            Vector2 geoCoords = coords.Item2;

            if(utm.x < min.x) {
                min.x = utm.x;
            }
            
            if(utm.y < min.y) {
                min.y = utm.y;
            }

            coordsList.Add(utm);
        }

        for(int i = 0; i < numRecords; i++) {

            ChunkData chunk = chunkList[i];
            Vector2 coord = coordsList[i];

            Vector2 normalized = coord - min;
            chunk.MeshData.ChunkPosition = normalized;

            Vector2 chunkCoord = coord;
            chunk.MeshData.tiffData.startCoordsMeters = chunkCoord;

            chunkList[i] = chunk;
        }

    }
    
    private void readInAllTiffs(string readingDir, string writingDir) {
        
        if(!Directory.Exists(readingDir)) {
            Debug.Log("The directory chosen is probably wrong: " + readingDir);
            return;
        }

        int numToRun = processingSettings.numToRun;

        string[] searchPatterns = {"*.bytes"};

        IEnumerable<string> files = searchPatterns.SelectMany(pattern => Directory.EnumerateFiles(readingDir, pattern));
        
        int numFiles = files.Count();

        List<string> fileNames = new List<string>(numFiles);
        
        if(globalChunks.chunks == null) {
            globalChunks.chunks = new List<ChunkData>(numFiles);
        } else {
            globalChunks.chunks.Clear();
        }

        int count = 0;
        foreach(string file in files) {
            DepthDataRecord depthDataRecord = readTiff(Path.Combine(readingDir, file));
            ChunkData newChunk = new ChunkData(depthDataRecord, null);

            string[] fileSplit = file.Split(fileDelims);
            string name = fileSplit[fileSplit.Length - 1];

            fileNames.Add(name);
            globalChunks.chunks.Add(newChunk);
            count++;
            
            if(numToRun != -1 && count >= numToRun) break;
        }

        generateChunkOffsets(globalChunks.chunks, fileNames);

        for(int i = 0; i < globalChunks.chunks.Count; i++) {
            ChunkData chunk = globalChunks.chunks[i];
            string fileName = fileNames[i];

            string path = Path.Combine(writingDir, fileName);
            fileUtil.writeToBinary(chunk.MeshData, path);
        }

    }

    private Tuple<Vector2, Vector2> parseCoords(string fileName) {
        string nameWithExt = fileName.Split("_")[1];
        string extractedName = nameWithExt.Split(".")[0];

        string[] northSplit = extractedName.Split("N");
        string northStr = northSplit[0];
        string westStr = northSplit[1].Substring(0,northSplit[1].Length - 1);

        int north = int.Parse(northStr);
        int west = int.Parse(westStr);

        float lat = north / 100f;
        float lon = -(west / 100f);

        Vector2 coords = new Vector2(lon, lat);
        Vector2 utm = CoordToUTM.Convert(coords);
        Tuple<Vector2, Vector2> data = new Tuple<Vector2, Vector2>(utm, coords);
        
        return data;
    }

    private DepthDataRecord readTiff(string filePath) {
        DepthDataRecord depthDataRecord = new DepthDataRecord();

        if (string.IsNullOrEmpty(filePath)) {
            return depthDataRecord;
        }
        
        GeoTiffData data = fileUtil.ReadGeoTiff(filePath, new float[]{processingSettings.MaxDepth, processingSettings.SeaLevel});
        depthDataRecord.tiffData = data;
        depthDataRecord.tiffData.Data = patcher.patchChunk(data);

        return depthDataRecord;
    }
}