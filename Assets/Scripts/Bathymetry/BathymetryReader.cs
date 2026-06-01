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
                writer.Write(depthDataRecord.Width);
                writer.Write(depthDataRecord.Height);
                
                writer.Write(depthDataRecord.ChunkPosition.x);
                writer.Write(depthDataRecord.ChunkPosition.y);
                writer.Write(depthDataRecord.AverageDepth);

                writer.Write(depthDataRecord.Depths.Count);
                
                foreach (float depth in depthDataRecord.Depths) {
                    writer.Write(depth);
                }
            }
        }
    }

    private void generateChunkOffsets(List<DepthDataRecord> records, List<string> fileNames){

        Vector2Int min = new Vector2Int(int.MaxValue, int.MaxValue);

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

            coordsList.Add(coords);
        }

        for(int i = 0; i < numRecords; i++){
            DepthDataRecord record = records[i];

            Vector2Int coord = coordsList[i];

            Vector2Int normalized = (coord - min) / 10;

            record.ChunkPosition = normalized;

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

        using (Tiff image = Tiff.Open(filePath, "r")) {
            if (image == null) {
                UnityEngine.Debug.LogError("Failed to open TIFF file at: " + filePath);
                return depthDataRecord;
            }

            int width = image.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int height = image.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
            int bitsPerSample = image.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();

            FieldValue[] scaleField = image.GetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG);
            
            if (scaleField == null || scaleField.Length < 2) {
                UnityEngine.Debug.LogError("Failed to read pixel scale tag for: " + filePath);
                return depthDataRecord;
            }

            byte[] scaleBytes = scaleField[1].GetBytes(); 
            double[] pixelScale = new double[3];
            Buffer.BlockCopy(scaleBytes, 0, pixelScale, 0, scaleBytes.Length);

            int scanlineSize = image.ScanlineSize();
            byte[] buffer = new byte[scanlineSize];

            depthDataRecord.Width = width;
            depthDataRecord.Height = height;

            List<float> localDepths = new List<float>(width * height);

            for (int i = 0; i < height; i++)
            {
                if (!image.ReadScanline(buffer, i))
                {
                    UnityEngine.Debug.LogError("Error reading scanline " + i);
                    break;
                }

                if (bitsPerSample == 32)
                {
                    for (int j = 0; j < scanlineSize; j += 4)
                    {
                        float depthValue = System.BitConverter.ToSingle(buffer, j);
                        localDepths.Add(Math.Clamp(depthValue, processingSettings.MaxDepth, processingSettings.SeaLevel));
                    }
                }
                else if (bitsPerSample == 16)
                {
                    for (int j = 0; j < scanlineSize; j += 2)
                    {
                        ushort shortValue = System.BitConverter.ToUInt16(buffer, j);
                        localDepths.Add(Math.Clamp((float)shortValue, processingSettings.MaxDepth, processingSettings.SeaLevel));
                    }
                }
                else
                {
                    for (int j = 0; j < scanlineSize; j++)
                    {
                        localDepths.Add(Math.Clamp((float)buffer[j] / 255.0f, processingSettings.MaxDepth, processingSettings.SeaLevel));
                    }
                }
            }

            depthDataRecord.Depths = patcher.patchChunk(localDepths, width,height);
        }

        return depthDataRecord;
    }
}