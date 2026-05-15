using UnityEngine;
using System.Collections.Generic;
using BitMiracle.LibTiff.Classic;
using System.IO;
using System;

public class BathymetryReader : MonoBehaviour {

    string dir;

    int width, height;
    List<float> luminances;

    [SerializeField] float maxDepth = -10000;

    [SerializeField] float seaLevel = 0;
    public void Start() {
        luminances = new List<float>(1000000);
        width = 10;
        height = 10;
        dir = Path.Combine(Application.dataPath,"Data", "Bathymetry");
        readTiff(Path.Combine(dir, "NONNA10_4680N06110W.tiff"));

        int seaLevelCount = 0;
        for(int i = 0; i < luminances.Count; i++){
            float currElem = luminances[i];
            if(Mathf.Approximately(currElem,0.0f)){
                seaLevelCount++;
                continue;
            }
            Debug.Log(luminances[i]);
        }
        Debug.Log(seaLevelCount + " Sea Level Points (> 0)");
    }



    private void readTiff(string filePath) {
        using (Tiff image = Tiff.Open(filePath, "r")) {
            if (image == null) {
                Debug.LogError("Failed to open TIFF file at: " + filePath);
                return;
            }

            width = image.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            height = image.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

            int samplesPerPixel = image.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToInt();
            int bitsPerSample = image.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();

            int scanlineSize = image.ScanlineSize();
            byte[] buffer = new byte[scanlineSize];

            for (int i = 0; i < height; i++) {
                if (!image.ReadScanline(buffer, i)) {
                    Debug.LogError("Error reading scanline " + i);
                    break;
                }

                if (bitsPerSample == 32) {
                    for (int j = 0; j < scanlineSize; j += 4) {
                        float depthValue = System.BitConverter.ToSingle(buffer, j);
                        luminances.Add(Math.Clamp(depthValue, maxDepth, seaLevel));
                    }
                }
                else if (bitsPerSample == 16) {
                    for (int j = 0; j < scanlineSize; j += 2) {
                        ushort shortValue = System.BitConverter.ToUInt16(buffer, j);
                        luminances.Add(Math.Clamp((float)shortValue, maxDepth, seaLevel));
                    }
                }
                else {
                    for (int j = 0; j < scanlineSize; j++) {
                        luminances.Add(Math.Clamp((float)buffer[j] / 255.0f,maxDepth, seaLevel));
                    }
                }
            }
        }
    }
}