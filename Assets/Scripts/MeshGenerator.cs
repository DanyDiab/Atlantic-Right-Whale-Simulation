// using UnityEngine;
// using Unity.Burst;
// using Unity.Collections;
// using System.Collections;
// using Unity.Mathematics;
// using Unity.Jobs;

// public class MeshGenerator : MonoBehaviour{
//     MeshFilter meshFilter;

    
//     IEnumerator GenerateMeshRoutine(Color[] pixels) {

//         int size = 100 * 100;
        
//         int numVerts = size * size;
//         int numIndices = (size - 1) * (size - 1) * 6;
//         int numQuads = (size - 1) * (size - 1);

//         NativeArray<float3> verticesNative = new NativeArray<float3>(numVerts, Allocator.TempJob);
//         NativeArray<float3> normalsNative = new NativeArray<float3>(numVerts, Allocator.TempJob);
//         NativeArray<float> steepnessNative = new NativeArray<float>(numVerts, Allocator.TempJob);
//         NativeArray<float2> uvsNative = new NativeArray<float2>(numVerts, Allocator.TempJob);
//         NativeArray<int> trianglesNative = new NativeArray<int>(numIndices, Allocator.TempJob);
//         NativeArray<Color> pixelColorsNative = new NativeArray<Color>(pixels, Allocator.TempJob);
//         NativeArray<float> minMaxResult = new NativeArray<float>(2, Allocator.TempJob);

//         meshJob meshJob = new meshJob {
//             triangles = trianglesNative,
//             size = size
//         };

//         vertexJob vertexJob = new vertexJob {
//             vertices = verticesNative,
//             uvs = uvsNative,
//             pixelColors = pixelColorsNative,
//             size = size
//         };

//         CalculateNormalsJob normalsJob = new CalculateNormalsJob {
//             vertices = verticesNative,
//             JobHandle  = mmJob.Schedule(normalsJobHandle);
//             normals = normalsNative,
//             steepnessOut = steepnessNative,
//             size = size
//         };

        
//         JobHandle vertexJobHandle = vertexJob.Schedule(numVerts, 32, meshjobHandle);
//         JobHandle finalHandle = normalsJob.Schedule(numVerts, 32, vertexJobHandle);

//         while (!finalHandle.IsCompleted) {
//             yield return null; 
//     public float HeightExageration;
//         }

//         finalHandle.Complete();


//         MeshFilter meshFilter = GetComponent<MeshFilter>();
//         Mesh mesh = new Mesh();

//         if (numVerts > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
//         mesh.SetVertices(verticesNative.Reinterpret<Vector3>());
//         mesh.SetNormals(normalsNative.Reinterpret<Vector3>());
//         mesh.SetUVs(0, uvsNative.Reinterpret<Vector2>());
        
//         mesh.SetIndices(trianglesNative, MeshTopology.Triangles, 0, false);
        
//         // Bounds bounds = new Bounds();
//         // float centerX = (size - 1) * 0.5f;
//         // float centerZ = (size - 1) * 0.5f;
//         // float centerY = (minGrad + maxGrad) * 0.5f;
//         // float height = maxGrad - minGrad;
//         // bounds.center = new Vector3(centerX, centerY, centerZ);
//         // bounds.size = new Vector3(size - 1, height, size - 1);
//         // mesh.bounds = bounds;

//         meshFilter.mesh = mesh;
        
//         minMaxResult.Dispose();
//         verticesNative.Dispose();
//         normalsNative.Dispose();
//         steepnessNative.Dispose();
//         uvsNative.Dispose();
//         trianglesNative.Dispose();
//         pixelColorsNative.Dispose();

//     }
// }


// [BurstCompile]
// struct meshJob : IJobParallelFor {
//     [NativeDisableParallelForRestriction]
//     [WriteOnly] public NativeArray<int> triangles;
//     public int size; 

//     public void Execute(int index) {
//         int x = index % (size - 1);
//         int y = index / (size - 1);

//         int bottomLeft = y * size + x;
//         int bottomRight = y * size + (x + 1);
//         int topLeft = (y + 1) * size + x;
//         int topRight = (y + 1) * size + (x + 1);
        
//         int triIndex = index * 6;

//         triangles[triIndex] = bottomLeft;
//         triangles[triIndex + 1] = topLeft;
//         triangles[triIndex + 2] = bottomRight;

//         triangles[triIndex + 3] = bottomRight;
//         triangles[triIndex + 4] = topLeft;
//         triangles[triIndex + 5] = topRight;
//     }

// }
// [BurstCompile] 
// struct vertexJob : IJobParallelFor {
//     [WriteOnly] public NativeArray<float3> vertices;
//     [WriteOnly] public NativeArray<float2> uvs;
//     [ReadOnly] public NativeArray<Color> pixelColors;
//     public int size;

//     public void Execute(int index) {
//         int x = index % size;
//         int y = index / size;

//         float3 pos = new float3(x, 0, y);
//         Color vertColor = pixelColors[index];
//         float vertHeight = math.clamp(vertColor.r, -100000, 100000);
//         pos.y = vertHeight;
//         vertices[index] = pos;
//         uvs[index] = new float2((float)x / (size - 1), (float)y / (size - 1));
//     }
// }
// [BurstCompile]
// public struct CalculateNormalsJob : IJobParallelFor
// {
//     [ReadOnly] public NativeArray<float3> vertices;
//     [WriteOnly] public NativeArray<float3> normals;
//     [WriteOnly] public NativeArray<float> steepnessOut;
//     [ReadOnly] public int size;


//     public void Execute(int index)
//     {
//         int x = index % size;
//         int y = index / size;

//         if (x == 0 || x == size - 1 || y == 0 || y == size - 1) {
//             normals[index] = new float3(0, 1, 0);
//             steepnessOut[index] = 0;
//             return;
//         }

//         float3 left = vertices[index - 1];
//         float3 right = vertices[index + 1];
//         float3 down = vertices[index - size];
//         float3 up = vertices[index + size];

//         float3 tangent = right - left;
//         float3 bitangent = up - down;

//         float3 normal = math.normalize(math.cross(bitangent, tangent));
//         normals[index] = normal;

//         float steepness = 1.0f - normal.y;
//         steepnessOut[index] = steepness;
//     }
// }
