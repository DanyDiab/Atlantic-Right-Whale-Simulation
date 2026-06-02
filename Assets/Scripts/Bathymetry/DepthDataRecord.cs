using System.Collections.Generic;
using UnityEngine;

public class DepthDataRecord {

    public Vector2Int ChunkPosition {get; set;}
    public Vector2 ChunkCoords {get; set;}
    public GeoTiffData tiffData {get; set;}
}
