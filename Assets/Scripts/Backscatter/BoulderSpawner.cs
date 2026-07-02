using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class BoulderSpawner : MonoBehaviour
{
    
    [SerializeField] GameObject boulderPrefab;
    [SerializeField] GameObject boulderParent;
    [SerializeField] GameObject meshParent;

    int minNumBoulders = 0;

    int maxNumBoulders = 100;

    float minScale = 10f;
    float maxScale = 30f;

    [SerializeField] ProcessingSettings processingSettings;

    List<GameObject> activeBoulders;
    FileUtilities fileUtil;
    bool shouldSpawnBoulders;

    void Start(){
        fileUtil = new FileUtilities();
        shouldSpawnBoulders = true;
    }

    void Update(){
        if(shouldSpawnBoulders)

        ReadBackscatterAndSpawnBoulders();

        shouldSpawnBoulders = false;
    }


    public void ReadBackscatterAndSpawnBoulders() {
        string area = processingSettings.AreaToFilePath();
        string bsDir = Path.Combine(Application.dataPath, "Data", "Processed", "Backscatter", area);
        string bathyDir = Path.Combine(Application.dataPath, "Data", "Processed", area);

        if (!Directory.Exists(bsDir)) {
            Debug.LogError("The backscatter directory is missing: " + bsDir);
            return;
        }

        if (!Directory.Exists(bathyDir)) {
            Debug.LogError("The bathymetry directory is missing: " + bathyDir);
            return;
        }

        Renderer[] meshRenderers;
        if (meshParent != null) {
            meshRenderers = meshParent.GetComponentsInChildren<Renderer>(true);
        } else {
            meshRenderers = GetComponentsInChildren<Renderer>(true);
        }

        int numRenderers = meshRenderers.Length;
        if (numRenderers == 0) {
            Debug.LogWarning("No terrain renderers found to apply textures to.");
            return;
        }

        string[] binSearchPattern = { "*.bytes" };
        string[] bins = binSearchPattern.SelectMany(pattern => Directory.EnumerateFiles(bsDir, pattern)).ToArray();
        int numFiles = bins.Length;

        for (int i = 0; i < numFiles; i++) {
            if (i >= numRenderers) {
                break;
            }

            string binFile = bins[i];
            string fileName = Path.GetFileName(binFile);
            string bathyFile = Path.Combine(bathyDir, fileName);

            if (!File.Exists(bathyFile)) {
                Debug.LogWarning("Missing matching bathymetry file for: " + fileName);
                continue;
            }

            Renderer renderer = meshRenderers[i];
            GeoTiffData chunkData = fileUtil.binToTiffData(binFile);

            if (chunkData == null || chunkData.Data == null) {
                continue;
            }

            if (chunkData.Data.Count(x => x > 1.0f) == chunkData.Data.Count()) {
                Debug.LogWarning("Renderer: this entire grabbed chunk is invalid!");
                continue;
            }

            List<float> vals = chunkData.Data;
            if (vals.Count == 0) {
                continue;
            }

            float chunkBSAverage = vals.Average();

            DepthDataRecord bathyRecord = readTiff(bathyFile);

            Vector3 minBounds = renderer.bounds.min;
            Vector3 maxBounds = renderer.bounds.max;
            
            Vector2 chunkMinBounds2D = new Vector2(minBounds.x, minBounds.z);
            Vector2 chunkMaxBounds2D = new Vector2(maxBounds.x, maxBounds.z);

            spawnBoulders(chunkMinBounds2D, chunkMaxBounds2D, chunkBSAverage, bathyRecord);
        }
    }

    public void spawnBoulders(Vector2 chunkMinBounds, Vector2 chunkMaxBounds, float chunkBSAverage, DepthDataRecord bathy) {
        int numBouldersToSpawn = Mathf.RoundToInt(Mathf.Lerp(minNumBoulders, maxNumBoulders, chunkBSAverage));

        GeoTiffData tiffData = bathy.tiffData;
        int width = tiffData.Width;
        int height = tiffData.Height;

        for (int i = 0; i < numBouldersToSpawn; i++) {
            float randX = Random.Range(0f, 1f);
            float randZ = Random.Range(0f, 1f);

            float chosenX = Mathf.Lerp(chunkMinBounds.x, chunkMaxBounds.x, randX);
            float chosenZ = Mathf.Lerp(chunkMinBounds.y, chunkMaxBounds.y, randZ);

            int pixelX = Mathf.RoundToInt(randX * (width - 1));
            int pixelY = Mathf.RoundToInt(randZ * (height - 1));
            int index = (pixelY * width) + pixelX;
            
            Vector3 chosenPos = new Vector3(chosenX, tiffData.Data[index], chosenZ);

            float randScale = Random.Range(0f, 1f);
            float chosenScale = Mathf.Lerp(minScale, maxScale, randScale);
            Vector3 scaleVec = new Vector3(chosenScale, chosenScale, chosenScale);

            GameObject obj = Instantiate(boulderPrefab, chosenPos, Quaternion.identity, boulderParent.transform);
            obj.transform.localScale = scaleVec;

            activeBoulders.Add(obj);
        }
    }


    public void clearBoulders(){
        // negative index for efficient array removal
        for(int i = activeBoulders.Count - 1; i >= 0; i--){
            Destroy(activeBoulders[i]);
            activeBoulders.RemoveAt(i);
        }        
    }


    private DepthDataRecord readTiff(string filePath) {
        DepthDataRecord depthDataRecord = new DepthDataRecord();

        if (string.IsNullOrEmpty(filePath)) {
            return depthDataRecord;
        }
        
        GeoTiffData data = fileUtil.ReadGeoTiff(filePath, new float[]{processingSettings.MaxDepth, processingSettings.SeaLevel});
        depthDataRecord.tiffData = data;

        return depthDataRecord;
    }
}