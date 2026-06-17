using UnityEngine;
using System.Collections.Generic;
using System;
using NUnit.Framework;
using Stopwatch = System.Diagnostics.Stopwatch;
using System.Linq;

public class RasterProjector
{
    // hard coded UTM zone and north for now :')
    int zone;
    bool isNorth;

    float targetResolution;
    int numNeighbors;
    KNN kNN;

    public RasterProjector()
    {
        targetResolution = .10f;
        kNN = new KNN();
        isNorth = true;
        zone = 20;
        numNeighbors = 4;
    }
    
    // 1. Get Bounding Box
    // Determine new Array Length
    // iterate over target array, filling in data points
    public GeoTiffData convert(GeoTiffData geoTiffData)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        Vector4 bbox = getBoundingBox(geoTiffData);

        float xLength = bbox[1] - bbox[0];
        float yLength = bbox[2] - bbox[3];


        int arrLenX = Mathf.CeilToInt(xLength / targetResolution);
        int arrLenY = Mathf.CeilToInt(yLength / targetResolution);

        
        int height = geoTiffData.Height;
        int width = geoTiffData.Width;
    
        double[] pixelScale = geoTiffData.PixelScale;

        int geoPosSize = height * width;
        int geoDataSize = arrLenX * arrLenY;

        List<Vector3> geoPosArr = new List<Vector3>(geoPosSize);

        float[] geoDataArr = new float[geoDataSize];

        List<float> rawData = geoTiffData.Data;
        Debug.Assert(rawData.Count == geoPosSize, "The data and the height * width dont match for backscatter projection");
// forward pass
// convert points from UTM to lat long
        for(int y = 0; y < height; y++){
            for(int x = 0; x < width; x++){
                int idx = y * width + x;

                float val = rawData[idx];

                double xScaledPos = x * pixelScale[0];
                double yScaledPos = y * pixelScale[1];

                Vector2 utm = new Vector2((float)xScaledPos, (float)yScaledPos);

                Vector2 geo = CoordinateProjector.UTMToGeo(utm, 20, true);

                Vector3 geoDepth = new Vector3(geo.x,val, geo.y);
                geoPosArr.Add(geoDepth);
            }
        }

        Vector2 startCoordsUTM = geoTiffData.startCoordsMeters;

        Vector2 geoStart = CoordinateProjector.UTMToGeo(startCoordsUTM,20,true);

        // backward pass
        // populate geoArr with interpolate nearest neighbors to ensure neat positions are kept
        for(int y = 0; y < arrLenY; y++){
            for(int x = 0; x < arrLenX; x++){
                int idx = y * arrLenX + x;
                
                float xScaledPos = (x * targetResolution) + geoStart.x;
                float yScaledPos = (y * targetResolution) + geoStart.y;

                Vector2 geoPos = new Vector2(xScaledPos, yScaledPos);
                // grab 4 nearest neighbors in geo space
                
                Tuple<float[], int>[] nearest = kNN.nearestNeighbors(geoTiffData,geoPos,numNeighbors);

                float intensitySum = 0.0f;
                 foreach(Tuple<float[], int> val in nearest){
                    float[] nieghborPos =  val.Item1;
                    int originalIndex = val.Item2;

                    float intensity = rawData[originalIndex];
                    intensitySum += intensity;
                }

                float avg = intensitySum / numNeighbors;
                
                geoDataArr[idx] = avg;
            }
        }
        geoTiffData.startCoordsMeters = geoStart;
        geoTiffData.Data = geoDataArr.ToList();
        stopwatch.Stop();
        Debug.LogFormat("backscatter processing took {0} ms", stopwatch.ElapsedMilliseconds);
        return geoTiffData;
    }


// gets the bounding box in lat long
// x min, x max, y min, y max
    Vector4 getBoundingBox(GeoTiffData tiffData)
    {
        int width = tiffData.Width;
        int height = tiffData.Height;

        double[] pixelScale = tiffData.PixelScale;

        Vector2 startingCoords = tiffData.startCoordsMeters;

        float maxX = startingCoords.y + (float)(height * pixelScale[1]);
        float maxY =  startingCoords.x + (float)(width * pixelScale[0]);
        

        Vector2 endCoords = new Vector2(maxX, maxY);

        Vector2 startCoordsGeo = CoordinateProjector.UTMToGeo(startingCoords,zone,isNorth);
        Vector2 EndCoordsGeo = CoordinateProjector.UTMToGeo(endCoords,zone,isNorth);

        Vector4 bbox = new Vector4(startCoordsGeo.x, EndCoordsGeo.x, startCoordsGeo.y, EndCoordsGeo.y);

        return bbox;
        
    }
}