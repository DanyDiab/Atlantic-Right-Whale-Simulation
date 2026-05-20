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

    int width, height;
    List<float> depths;

    [SerializeField] float maxDepth = -10000;

    [SerializeField] float seaLevel = 0;
    [SerializeField] bool runAnalysis = false;
    public void Start() {
        if(!runAnalysis) return;
        depths = new List<float>(1000000);
        width = 10;
        height = 10;
        readingDir = Path.Combine(Application.dataPath,"Data", "Bathymetry");
        writingDir = Path.Combine(Application.dataPath, "Data");
        readInAllTiffs(readingDir, writingDir);
    }


    private void writeDepthsToBinary(string filePath) {
        if (string.IsNullOrEmpty(filePath)) {
            return;
        }

        using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None)) {
            using (BinaryWriter writer = new BinaryWriter(fs)) {
                writer.Write(depths.Count);
                
                foreach (float depth in depths) {
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
            readTiff(Path.Combine(readingDir, file));
            string[] fileSplit = file.Split("/");
            string path = Path.Combine(writingDir, fileSplit[fileSplit.Length - 1]);
            writeDepthsToBinary(path);
            depths.Clear();
            break;
        }
    }




    private void readTiff(string filePath) {
        using (Tiff image = Tiff.Open(filePath, "r")) {
            if (image == null) {
                Debug.LogError("Failed to open TIFF file at: " + filePath);
                return;
            }

            width = image.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            height = image.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

            int samplesPerPixel = image.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToInt();
            int bitsPerSample = image.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
            Debug.Log(width);
            Debug.Log(height);

            int scanlineSize = image.ScanlineSize();
            byte[] buffer = new byte[scanlineSize];

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
        }
    }
}