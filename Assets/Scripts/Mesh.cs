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

        [Header("General")]
        [Tooltip("Parent of all the mesh chunks")]

        [SerializeField] GameObject parent;

        [SerializeField] Material meshMaterial;
        [Header("File Settings")]

        [Tooltip("number of files to create into a mesh, set to -1 to process all")]
        [SerializeField] int numToRun = 1;
        string byteFileDir;

        void Start(){
            byteFileDir = Path.Combine(Application.dataPath,"Data", "Processed");

            Stopwatch timer = new Stopwatch();
            List<DepthDataRecord> depthDataRecords = traversePath(byteFileDir);

            long elapsedMs = timer.ElapsedMilliseconds;

            timer.Start();
            generateAllMeshes(depthDataRecords);
            timer.Stop();

            elapsedMs = timer.ElapsedMilliseconds;
            Debug.Log("took " + elapsedMs + " ms to generate mesh");
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
                    record.Width = reader.ReadInt32();
                    record.Height = reader.ReadInt32();
                    
                    int chunkX = reader.ReadInt32();
                    int chunkY = reader.ReadInt32();
                    record.ChunkPosition = new Vector2Int(chunkX, chunkY);
                    
                    record.AverageDepth = reader.ReadSingle();

                    int count = reader.ReadInt32();

                    if (count == 0) {
                        return record;
                    }

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
            int count = 0;
            foreach (string file in files) {
                if(count >= numToRun && numToRun != -1) continue;
                DepthDataRecord depthDataRecord = readInByteFile(file);
                depthDataRecords.Add(depthDataRecord);
                count++;
            }
            return depthDataRecords;
        }


        void generateAllMeshes(List<DepthDataRecord> records){
            foreach(DepthDataRecord record in records){
                Vector2Int chunkPos = record.ChunkPosition;
                int west = chunkPos.x * 9000;
                int north = chunkPos.y * 9000;

                UnityMesh chunkMesh = generateMeshData(record);
                GameObject chunkObject = new GameObject("TerrainChunk_" + west + "_" + north);
                MeshFilter meshFilter = chunkObject.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = chunkObject.AddComponent<MeshRenderer>();
                meshRenderer.material = meshMaterial;
                
                meshFilter.mesh = chunkMesh;

                
                chunkObject.transform.position = new Vector3(west,0,north);
            }
        }

        UnityMesh generateMeshData(DepthDataRecord record){
            List<Vector3> positions = new List<Vector3>();

            UnityMesh mesh = new UnityMesh{
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };


            int totalWidth, totalHeight = totalWidth = 0;
            int undefinedCount = 0;

            List<float> depths = record.Depths;
            float averageDepth = record.AverageDepth;
            int height = record.Height;
            int width = record.Width;
            float distanceBetweenPoints = 10000 / height;
            totalHeight += height;
            totalWidth += width;

            int depthsCount = depths.Count;

            positions.Capacity = positions.Count + depths.Count;
            Vector3 curr = new Vector3();
            for(int i = 0; i < depthsCount; i++){
                float x = (i / height) * distanceBetweenPoints;
                float z = (i % width) * distanceBetweenPoints;

                float y = depths[i];

                if(y >= -5){
                    y = averageDepth;
                    undefinedCount++;
                }
                
                curr.x = x;
                curr.y = y;
                curr.z = z;

                positions.Add(curr);
            }





            //generate indicies
            int numTrianglesPerCol = (height - 1) * 2;
            int numTriangles = numTrianglesPerCol * (width - 1);


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