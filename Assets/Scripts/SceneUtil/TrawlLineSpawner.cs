using System.Collections.Generic;
using UnityEngine;

public class TrawlLineSpawner : MonoBehaviour{
    [SerializeField] GameObject trawlLinePrefab;
    [SerializeField] GameObject parent;

    [SerializeField] int numToSpawnPerChunk = 5;
    [SerializeField] TrawlLine TL;
    
    [SerializeField] bool spawn;

    [SerializeField] GameObject meshChunksParent;
    List<GameObject> lines;
    
    void Start()
    {
        lines = new List<GameObject>(numToSpawnPerChunk);
    }

    void Update(){
        if(!spawn) return;

        clearLines();
        spawnLines();
        spawn = false;
    }

    
    // TODO, only spawn lines IF the entire trawl wont go over the edge.
    public void spawnLines(){

        List<Bounds> meshBounds = ChunksToBounds.GetBoundsFromChunks(meshChunksParent);

        lines.Capacity = meshBounds.Count * numToSpawnPerChunk;

        foreach(Bounds meshBound in meshBounds){
            
            float xMin = meshBound.min.x;
            float xMax = meshBound.max.x;

            float zMin = meshBound.min.z;
            float zMax = meshBound.max.z;

            for(int i = 0; i < numToSpawnPerChunk; i++){

                float tX = Random.Range(0.0f,1.0f);
                float tZ = Random.Range(0.0f,1.0f);

                float x = Mathf.Lerp(xMin,xMax,tX);
                float z = Mathf.Lerp(zMin,zMax,tZ);

                Vector2 position = new Vector2(x,z);

                GameObject line = TL.spawnTrawl(position, parent);

                lines.Add(line);
            }
        }
    }



    void clearLines(){

        if(lines.Count == 0) return;
        
        for(int i = lines.Count - 1; i >= 0; i--){
            GameObject currLine = lines[i];

            Destroy(currLine);

            lines.RemoveAt(i);
        }
    }
}