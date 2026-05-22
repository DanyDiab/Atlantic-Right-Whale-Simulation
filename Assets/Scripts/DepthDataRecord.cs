using System.Collections.Generic;
using UnityEngine;

public class DepthDataRecord {
    public int West { get; set; }
    public int North { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public float AverageDepth { get; set; }
    public List<float> Depths { get; set; }
}
