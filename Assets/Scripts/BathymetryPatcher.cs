using System;
using System.Collections.Generic;
using Supercluster.KDTree;
using UnityEngine;

public class BathymetryPatcher{

    int numNeighbors = 4;
    ProcessingSettings settings;
    

    private KDTree<float, int> generateTree(List<float> depths, int width, int height)
    {

        int depthsCount = depths.Count;
        Func<float[], float[], double> L2Metric = (x, y) => {
            float dx = x[0] - y[0];
            float dy = x[1] - y[1];
            return (dx * dx) + (dy * dy);
        };

        int validPointCount = 0;
        for (int i = 0; i < depthsCount; i++) {
            if (depths[i] <= settings.SeaLevel) {
                validPointCount++;
            }
        }

        float[][] validPoints = new float[validPointCount][];
        int[] validIndices = new int[validPointCount];

        int currentIndex = 0;
        for (int i = 0; i < depthsCount; i++) {
            if (depths[i] > settings.SeaLevel) continue;

            float x = (i % width);
            float y = (i / width);

            validPoints[currentIndex] = new float[] { x, y };
            validIndices[currentIndex] = i;
            currentIndex++;
        }

        KDTree<float, int> tree = new KDTree<float, int>(dimensions: 2,points: validPoints, nodes: validIndices, metric: L2Metric);
        return tree;

    }

// add to Kd Tree
// grab nearest X neighbors using KNN
// do inverse distance weighting on nearest points to estimate missing data point
    public List<float> patchChunk(List<float> depths, int width, int height, ProcessingSettings processingSettings){

        settings = processingSettings;

        KDTree<float, int> tree = generateTree(depths,width,height);

        Dictionary<float, int> seen = new Dictionary<float, int>();
        float[] target = new float[2];

        int badCheck = 0;
        for(int i = 0; i < depths.Count; i++)
        {
            // skip valid points
            if (!seen.ContainsKey(depths[i]))
            {
                seen[depths[i]] = 1;
            }
            else
            {
                seen[depths[i]]++;
            }
            if(depths[i] < settings.SeaLevel) continue;

            float x = i % width;
            float y = i / width;

            target[0] = x;
            target[1] = y;

            Tuple<float[], int>[] nearest = tree.NearestNeighbors(target, numNeighbors);

            float numerator = 0.0f;
            float denominator = 0.0f;
            bool exactMatch = false;
            for (int j = 0; j < nearest.Length; j++) {
                float[] neighborPosition = nearest[j].Item1;
                int originalIndex = nearest[j].Item2;
                
                float knownDepth = depths[originalIndex];
                
                float dx = target[0] - neighborPosition[0];
                float dy = target[1] - neighborPosition[1];
                float squaredDistance = (dx * dx) + (dy * dy);

                if (squaredDistance <= 0.0f) {
                    badCheck++;
                    depths[i] = knownDepth;
                    exactMatch = true;
                    break; 
                }

                float weight = 1.0f / squaredDistance; 
                numerator += knownDepth * weight;
                denominator += weight;
            }

            if(!exactMatch && !Mathf.Approximately(denominator, 0.0f))
            {
                depths[i] = numerator / denominator;
            }

        }

        int unqiue = seen.Keys.Count;

        Debug.LogFormat("Unqiue: {0}", unqiue);
        Debug.Log(badCheck);

        return depths;


    }
    
}