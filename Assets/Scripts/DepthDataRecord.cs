using System.Collections.Generic;
using UnityEngine;

public class DepthDataRecord {
    public int Width { get; set; }
    public int Height { get; set; }

    public Vector2Int ChunkPosition {get; set;}
    public float AverageDepth { get; set; }
    public List<float> Depths { get; set; }
}
