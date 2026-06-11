using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Stopwatch = System.Diagnostics.Stopwatch;
using System.Linq;
using System;
using UnityEngine.UIElements;

using UnityMesh = UnityEngine.Mesh;
using UnityEditor;

namespace MeshGeneration
{

    public class Mesh : MonoBehaviour
    {
        [SerializeField] int chunkSize = 10000;

        [Header("General")]
        [Tooltip("Parent of all the mesh chunks")]

        [SerializeField] GameObject parent;
        [SerializeField] ProcessingSettings processingSettings;

        [SerializeField] Material meshMaterial;

        [Header("Reload Meshes")]
        [Tooltip("Press this to reload all the meshes from the binary files found in Assets/Data/Processed/(Area)")]
        [SerializeField] bool reloadMesh;

        FileUtilities fileUtil;

        string byteFileDir;

        void Start()
        {
            fileUtil = new FileUtilities();
            string areaPath = processingSettings.AreaToFilePath();
            byteFileDir = Path.Combine(Application.dataPath, "Data", "Processed", areaPath);
            startMeshPipeline();
        }

        void Update()
        {
            if(!reloadMesh) return;
            
            clearOldChunks();
            startMeshPipeline();
            reloadMesh = false;

        }

        void clearOldChunks()
        {
            foreach(Transform child in parent.GetComponentsInChildren<Transform>(true))
            {
                if(child == parent.transform) continue;

                Destroy(child.gameObject);
            }
        }


        void startMeshPipeline(){
            List<DepthDataRecord> depthDataRecords = traversePath(byteFileDir);

            generateAllMeshes(depthDataRecords);
        }




        List<DepthDataRecord> traversePath(string path)
        {

            int numToRun = processingSettings.numToRun;
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }
            if (!Directory.Exists(path))
            {
                return null;
            }

            string[] files = Directory.GetFiles(path, "*.bytes", SearchOption.TopDirectoryOnly);
            List<DepthDataRecord> depthDataRecords = new List<DepthDataRecord>(files.Length);
            int count = 0;
            foreach (string file in files)
            {
                if (count >= numToRun && numToRun != -1) continue;
                DepthDataRecord depthDataRecord = fileUtil.binToDepthRecord(file);
                depthDataRecords.Add(depthDataRecord);
                count++;
            }
            return depthDataRecords;
        }


        void generateAllMeshes(List<DepthDataRecord> records)
        {
            foreach (DepthDataRecord record in records)
            {
                Vector2 chunkPos = record.ChunkPosition;
                int west = Mathf.FloorToInt(chunkPos.x);
                int north = Mathf.FloorToInt(chunkPos.y);

                UnityMesh chunkMesh = generateMeshData(record);
                GameObject chunkObject = new GameObject("TerrainChunk_W" + west + "_N" + north);
                chunkObject.transform.SetParent(parent.transform);
                MeshFilter meshFilter = chunkObject.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = chunkObject.AddComponent<MeshRenderer>();
                meshRenderer.material = meshMaterial;

                meshFilter.mesh = chunkMesh;


                chunkObject.transform.position = new Vector3(north, 0, -west);
            }
        }

        UnityMesh generateMeshData(DepthDataRecord record){
            List<Vector3> positions = new List<Vector3>();

            UnityMesh mesh = new UnityMesh{
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };


            
            List<float> depths = record.tiffData.Data;

            int height = record.tiffData.Height;
            int width = record.tiffData.Width;
            float distanceBetweenPointsX = chunkSize / (width - 1);
            float distanceBetweenPointsZ = chunkSize / (height - 1);


            int depthsCount = depths.Count;

            positions.Capacity = positions.Count + depths.Count;
            Vector3 curr = new Vector3();
            for(int i = 0; i < depthsCount; i++){
                float x = (i / height) * distanceBetweenPointsX;
                float z = (i % width) * distanceBetweenPointsZ;

                float y = depths[i];

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
                int x = i % (width - 1);
                int y = i / (height - 1);

                int startingIndex = (y * width) + x;

                int v1 = startingIndex;
                int v2 = startingIndex + 1;
                int v3 = startingIndex + height;

                int v4 = v2;
                int v5 = v2 + width;
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
