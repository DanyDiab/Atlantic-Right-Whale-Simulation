using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Stopwatch = System.Diagnostics.Stopwatch;
using System.Linq;
using System;
using UnityEngine.UIElements;

using UnityMesh = UnityEngine.Mesh;

namespace MeshGeneration{

    public class Mesh : MonoBehaviour {

        string byteFileDir;
        [SerializeField] MeshFilter mf;

        void Start(){
            byteFileDir = Path.Combine(Application.dataPath,"Data", "Processed");

            Stopwatch timer = new Stopwatch();
            timer.Start();
            List<DepthDataRecord> depthDataRecords = traversePath(byteFileDir);
            timer.Stop();

            long elapsedMs = timer.ElapsedMilliseconds;
            Debug.Log("took " + elapsedMs + " ms to generate depths records");

            timer.Reset();
            timer.Start();
            UnityMesh mesh = generateMeshData(depthDataRecords);
            timer.Stop();

            elapsedMs = timer.ElapsedMilliseconds;
            Debug.Log("took " + elapsedMs + " ms to generate mesh");

            mf.mesh = mesh;

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

        List<DepthDataRecord> traversePath(string path) {
            if (string.IsNullOrEmpty(path)) {
                return null;
            }
            if (!Directory.Exists(path)) {
                return null;
            }

            string[] files = Directory.GetFiles(path, "*.bytes", SearchOption.TopDirectoryOnly);
            List<DepthDataRecord> depthDataRecords = new List<DepthDataRecord>(files.Length);
            foreach (string file in files) {
                DepthDataRecord depthDataRecord = readInByteFile(file);
                depthDataRecords.Add(depthDataRecord);
            }
            return depthDataRecords;
        }



        UnityMesh generateMeshData(List<DepthDataRecord> records){
            List<Vector3> positions = new List<Vector3>();

            UnityMesh mesh = new UnityMesh();

            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            int totalWidth, totalHeight = totalWidth = 0;
            int undefinedCount = 0;
            foreach(DepthDataRecord record in records){
                List<float> depths = record.Depths;
                float averageDepth = record.AverageDepth;
                int west = record.West;
                int north = record.North;
                int height = record.Height;
                int width = record.Width;
                
                totalHeight += height;
                totalWidth += width;

                int depthsCount = depths.Count;

                positions.Capacity = positions.Count + depths.Count;
                Vector3 curr = new Vector3();
                for(int i = 0; i < depthsCount; i++){
                    float x = (i / height) - west;
                    float z = (i % width) - north;

                    float y = depths[i];

                    if(y >= 0){
                        y = averageDepth;
                        undefinedCount++;
                    }
                    
                    curr.x = x;
                    curr.y = y;
                    curr.z = z;

                    positions.Add(curr);
                }

            }
            Debug.LogFormat("our data is not defined" + undefinedCount);




            //generate indicies
            int numTrianglesPerCol = (totalHeight - 1) * 2;
            int numTriangles = numTrianglesPerCol * (totalWidth - 1);

            int numQuads = numTriangles / 2;

            List<int> triangles = new List<int>(numTriangles);
            for(int i = 0; i < numQuads; i++){
                int x = i % (totalWidth - 1);
                int y = i / (totalHeight - 1);

                int startingIndex = (y * totalWidth) + x;

                int v1 = startingIndex;
                int v2 = startingIndex + 1;
                int v3 = startingIndex + totalHeight;

                int v4 = v2;
                int v5 = v2 + totalWidth;
                int v6 = v3;

                triangles.Add(v1);
                triangles.Add(v2);
                triangles.Add(v3);
                triangles.Add(v4);
                triangles.Add(v5);
                triangles.Add(v6);

            }

            mesh.SetVertices(positions);
            mesh.SetTriangles(triangles, 0);
            return mesh;
        }
    }
}