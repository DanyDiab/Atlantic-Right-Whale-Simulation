using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Stopwatch = System.Diagnostics.Stopwatch;
using System.Linq;
using System;
public class Mesh : MonoBehaviour {

    string byteFileDir;
    List<float> depths;

    void Start(){
        byteFileDir = Path.Combine(Application.dataPath,"Data", "Processed");
        depths = new List<float>();

        Stopwatch timer = new Stopwatch();
        timer.Start();
        traversePath(byteFileDir);
        timer.Stop();

        long elapsedMs = timer.ElapsedMilliseconds;
        Debug.Log(elapsedMs);
   
        float deepest = depths.Min();
        float avg = 0.0f;
        Debug.Log("the deepest point is: " + deepest);
        int realPoints = 0;
        foreach(float depth in depths){
            if(depth >= 0.0f) continue;

            avg += depth;
            realPoints++;

        }
        Debug.Log("average Depth: " + avg / realPoints);
        Debug.Log("number Points:" + depths.Count);
        // int seaLevelCount = 0;
        // for(int i = 0; i < depths.Count; i++){
        //     float currElem = depths[i];
        //     if(Mathf.Approximately(currElem,0.0f)){
        //         seaLevelCount++;
        //         continue;
        //     Debug.Log(depths[i]);
        // }
        // Debug.Log(seaLevelCount + " Sea Level Points (> 0)");
    }


    DepthDataRecord readInByteFile(string filePath) {
        DepthDataRecord record = new DepthDataRecord();

        if (string.IsNullOrEmpty(filePath)) {
            return record;
        }

        if (!File.Exists(filePath)) {
            return record;
        }

        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
            using (BinaryReader reader = new BinaryReader(fs)) {
                record.West = reader.ReadInt32();
                record.North = reader.ReadInt32();
                record.Width = reader.ReadInt32();
                record.Height = reader.ReadInt32();

                int count = reader.ReadInt32();
                int byteCount = count * 4;

                byte[] rawBytes = reader.ReadBytes(byteCount);
                float[] depthsArray = new float[count];

                Buffer.BlockCopy(rawBytes, 0, depthsArray, 0, byteCount);

                record.Depths = new List<float>(depthsArray);
            }
        }

        return record;
    }

    void traversePath(string path) {
        if (string.IsNullOrEmpty(path)) {
            return;
        }
        if (!Directory.Exists(path)) {
            return;
        }

        string[] files = Directory.GetFiles(path, "*.bytes", SearchOption.TopDirectoryOnly);

        foreach (string file in files) {
            DepthDataRecord depthDataRecord = readInByteFile(file);
        }
    }



    void generateMeshData(List<float> depths){
        int posCount = depths.Count * 3;

        List<float> positions = new List<float>(posCount);

        // return positions;
    }


}