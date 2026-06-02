using UnityEngine;
using System.Collections.Generic;
using BitMiracle.LibTiff.Classic;
using System.IO;
using System;
using System.Linq;
using Unity.VisualScripting;
using Unity.ProjectAuditor.Editor;

public class BathymetryReader : MonoBehaviour {

    string readingDir;
    string writingDir;
    [SerializeField] ProcessingSettings processingSettings;
    BathymetryPatcher patcher;
    FileUtilities fileUtil;

    [SerializeField] bool reloadReader = false;

    char[] fileDelims = {'/', '\\'};


    void Update()
    {
        if(!reloadReader) return;

        startPipeline();

        reloadReader = false;
    }

    public void Start() {
        patcher = new BathymetryPatcher(processingSettings);
        fileUtil = new FileUtilities();
        startPipeline();

    }


    void startPipeline()
    {
        string path = processingSettings.AreaToFilePath();

        readingDir = Path.Combine(Application.dataPath,"Data", "Bathymetry", path);
        writingDir = Path.Combine(Application.dataPath, "Data", "Processed", path);
        if (!Directory.Exists(writingDir))
        {
            Directory.CreateDirectory(writingDir);
        }
        readInAllTiffs(readingDir, writingDir);        
    }


    private void writeToBinary(string filePath, DepthDataRecord depthDataRecord) {
        using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None)) {
            using (BinaryWriter writer = new BinaryWriter(fs)) {
                writer.Write(depthDataRecord.tiffData.Width);
                writer.Write(depthDataRecord.tiffData.Height);
                
                writer.Write(depthDataRecord.ChunkPosition.x);
                writer.Write(depthDataRecord.ChunkPosition.y);

                writer.Write(depthDataRecord.tiffData.Data.Count);
                
                foreach (float depth in depthDataRecord.tiffData.Data) {
                    writer.Write(depth);
                }
            }
        }
    }

    private void generateChunkOffsets(List<DepthDataRecord> records, List<string> fileNames){

        Vector2Int min = new Vector2Int(int.MaxValue, int.MaxValue);
        Vector2Int max = new Vector2Int(int.MinValue, int.MinValue);

        int numRecords = records.Count;
        List<Vector2Int> coordsList = new List<Vector2Int>(numRecords);
        for(int i = 0; i < numRecords; i++){
            string filename = fileNames[i];

            Vector2Int coords = parseCoords(filename);

            if(coords.x < min.x){
                min.x = coords.x;
            }
            
            if(coords.y < min.y){
                min.y = coords.y;
            }

            if(coords.x > max.x)
            {
                max.x = coords.x;
            }
            
            if(coords.y > max.y)
            {
                max.y = coords.y;
            }

            coordsList.Add(coords);
        }

        for(int i = 0; i < numRecords; i++){

            DepthDataRecord record = records[i];
            Vector2Int coord = coordsList[i];

            Vector2Int normalized = (coord - min) / 10;
            record.ChunkPosition = normalized;

            Vector2 chunkCoord = coord / 10;
            record.tiffData.startCoordsMeters = chunkCoord;

            records[i] = record;
        }

    }
    private void readInAllTiffs(string readingDir, string writingDir){
        
        if(!Directory.Exists(readingDir)){
            Debug.Log("The directory chosen is probably wrong: " + readingDir);
            return;
        }

        int numToRun = processingSettings.numToRun;

        string[] searchPatterns = {"*.bytes"};

        IEnumerable<string> files = searchPatterns.SelectMany(pattern => Directory.EnumerateFiles(readingDir, pattern));
        
        int numFiles = files.Count();

        List<string> fileNames = new List<string>(numFiles);
        List<DepthDataRecord> records = new List<DepthDataRecord>(numFiles);

        int count = 0;
        foreach(string file in files){
            DepthDataRecord depthDataRecord = readTiff(Path.Combine(readingDir, file));

            string[] fileSplit = file.Split(fileDelims);
            string name = fileSplit[fileSplit.Length - 1];

            fileNames.Add(name);
            records.Add(depthDataRecord);
            count++;
            if(numToRun != -1 && count >= numToRun) break;
        }

        generateChunkOffsets(records, fileNames);

        for(int i = 0; i < records.Count; i++){
            DepthDataRecord record = records[i];
            string fileName = fileNames[i];

            string path = Path.Combine(writingDir, fileName);
            writeToBinary(path, record);
        }

    }


    private Vector2Int parseCoords(string fileName){
        string nameWithExt = fileName.Split("_")[1];
        string extractedName = nameWithExt.Split(".")[0];

        string[] northSplit = extractedName.Split("N");
        string northStr = northSplit[0];
        string westStr = northSplit[1].Substring(0,northSplit[1].Length - 1);


        int north = int.Parse(northStr);
        int west = int.Parse(westStr);

        return new Vector2Int(west, north);
    }


    private DepthDataRecord readTiff(string filePath) {
        DepthDataRecord depthDataRecord = new DepthDataRecord();

        if (string.IsNullOrEmpty(filePath)) {
            return depthDataRecord;
        }
        
        GeoTiffData data = fileUtil.ReadGeoTiff(filePath, new float[]{processingSettings.MaxDepth, processingSettings.SeaLevel});
        depthDataRecord.tiffData = data;
        depthDataRecord.tiffData.Data = patcher.patchChunk(data.Data, data.Width, data.Height);

        return depthDataRecord;
    }
}