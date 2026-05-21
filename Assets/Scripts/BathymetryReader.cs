using UnityEngine;
using System.Collections.Generic;
using BitMiracle.LibTiff.Classic;
using System.IO;
using System;
using System.Linq;
using Newtonsoft.Json;
using Unity.VisualScripting;

public class BathymetryReader : MonoBehaviour {

    string readingDir;
    string writingDir;

    List<float> depths;

    [SerializeField] float maxDepth = -10000;

    [SerializeField] float seaLevel = 0;
    [SerializeField] bool runAnalysis = false;
    public void Start() {
        if(!runAnalysis) return;
        depths = new List<float>(1000000);
        readingDir = Path.Combine(Application.dataPath,"Data", "Bathymetry");
        writingDir = Path.Combine(Application.dataPath, "Data", "Processed");
        readInAllTiffs(readingDir, writingDir);
    }


    private void writeToBinary(string filePath, DepthDataRecord depthDataRecord) {
        if (string.IsNullOrEmpty(filePath)) {
            return;
        }

        using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None)) {
            using (BinaryWriter writer = new BinaryWriter(fs)) {
                writer.Write(depthDataRecord.West);
                writer.Write(depthDataRecord.North);
                writer.Write(depthDataRecord.Width);
                writer.Write(depthDataRecord.Height);

                if (depthDataRecord.Depths == null) {
                    writer.Write(0);
                    return;
                }
                
                writer.Write(depthDataRecord.Depths.Count);
                
                foreach (float depth in depthDataRecord.Depths) {
                    writer.Write(depth);
                }
            }
        }
    }

    private void readInAllTiffs(string readingDir, string writingDir){
        
        if(!Directory.Exists(readingDir)){
            Debug.Log("The directory chosen is probably wrong: " + readingDir);
            return;
        }
        string[] searchPatterns = {"*.bytes"};

        IEnumerable<string> files = searchPatterns.SelectMany(pattern => Directory.EnumerateFiles(readingDir, pattern));
        foreach(string file in files){
            DepthDataRecord depthDataRecord = readTiff(Path.Combine(readingDir, file));
            string[] fileSplit = file.Split("/");
            string path = Path.Combine(writingDir, fileSplit[fileSplit.Length - 1]);
            writeToBinary(path, depthDataRecord);
            depths.Clear();
            break;
        }
    }


    private (int, int) parseCoords(string fileName){
        string nameWithExt = fileName.Split("_")[1];
        string extractedName = nameWithExt.Split(".")[0];

        string[] northSplit = extractedName.Split("N");
        string northStr = northSplit[0];
        string westStr = northSplit[1].Substring(0,northSplit[1].Length - 1);


        Debug.LogFormat("NORTH: {0}\nWEST: {1}", northStr, westStr);
        int north = int.Parse(northStr);
        int west = int.Parse(westStr);

        return (north, west);
    }


    private DepthDataRecord readTiff(string filePath) {
        DepthDataRecord depthDataRecord = new DepthDataRecord();

        using (Tiff image = Tiff.Open(filePath, "r")) {
            if (image == null) {
                Debug.LogError("Failed to open TIFF file at: " + filePath);
                return depthDataRecord;
            }

            int width = image.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int height = image.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

            int bitsPerSample = image.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();

            int scanlineSize = image.ScanlineSize();
            byte[] buffer = new byte[scanlineSize];

            depthDataRecord.Width = width;
            depthDataRecord.Height = height;
            (int north, int west) = parseCoords(filePath);

            depthDataRecord.North = north;
            depthDataRecord.West = west;

            for (int i = 0; i < height; i++) {
                if (!image.ReadScanline(buffer, i)) {
                    Debug.LogError("Error reading scanline " + i);
                    break;
                }
                
                if (bitsPerSample == 32) {
                    for (int j = 0; j < scanlineSize; j += 4) {
                        float depthValue = System.BitConverter.ToSingle(buffer, j);
                        depths.Add(Math.Clamp(depthValue, maxDepth, seaLevel));
                    }
                }
                else if (bitsPerSample == 16) {
                    for (int j = 0; j < scanlineSize; j += 2) {
                        ushort shortValue = System.BitConverter.ToUInt16(buffer, j);
                        depths.Add(Math.Clamp((float)shortValue, maxDepth, seaLevel));
                    }
                }
                else {
                    for (int j = 0; j < scanlineSize; j++) {
                        depths.Add(Math.Clamp((float)buffer[j] / 255.0f,maxDepth, seaLevel));
                    }
                }
            }
            depthDataRecord.AverageDepth = depths.Average();
            depthDataRecord.Depths = depths;
        }
        return depthDataRecord;
    }
}