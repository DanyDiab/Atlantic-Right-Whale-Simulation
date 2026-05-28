using UnityEngine;

public class Noise
{
    
    ProcessingSettings settings;

    public Noise(ProcessingSettings settings)
    {
        this.settings = settings;
    }
    public float addNoise(float depth, float[] pos, int[] size)
    {

        int numOctaves = 8;

        float frequency = settings.noiseFrequency;
        float amplitude = settings.noiseAmplitude;

        float lacunarity = 2.2f;
        float persistence = .55f;

        float totalAmplitude = 0f;

        float accumulatedNoise = 0f;
        for(int i = 0; i < numOctaves; i++)
        {
            totalAmplitude += amplitude;

            float normalizedX = pos[0] / size[0] * frequency;
            float normalizedY = pos[1] / size[1] * frequency;
            float noise = ((Mathf.PerlinNoise(normalizedX, normalizedY) * 2) - 1) * amplitude;


            accumulatedNoise += noise;

            amplitude *= persistence;
            frequency *= lacunarity;
            
        }

        // float finalNoise = accumulatedNoise / totalAmplitude;

        return depth + accumulatedNoise;
    }
}