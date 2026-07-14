using System.Collections.Generic;
using UnityEngine;

public static class ChunksToBounds
{

    static MeshFilter[] meshs;
    

// traverses on the first call, then caches the result
    static MeshFilter[] traverseAndCache(GameObject meshParent){
        if(meshs != null){
            return meshs;
        }

        meshs = meshParent.GetComponentsInChildren<MeshFilter>();
        return meshs;
    }
    public static List<Bounds> GetBoundsFromChunks(GameObject meshParent){
        MeshFilter[] meshs = traverseAndCache(meshParent);
        List<Bounds> bounds = new List<Bounds>(meshs.Length);

        foreach(MeshFilter meshFilter in meshs){

            Bounds meshBounds = meshFilter.mesh.bounds;

            bounds.Add(meshBounds);
        }

        return bounds;
    }
}