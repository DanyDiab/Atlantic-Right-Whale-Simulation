using System.Collections.Generic;
using System.IO;
using System.Linq;
using BitMiracle.LibTiff.Classic;
using UnityEngine;

public class BackscatterReader : MonoBehaviour
{
    string path;

    [SerializeField] ProcessingSettings processingSettings;


    void Start()
    {
        string area = processingSettings.AreaToFilePath();
        path = Path.Combine(Application.dataPath, "Data", "Backscatter", area);

        readInAllTiffs(path);

    }

    void readInAllTiffs(string dir)
    {
        if(!Directory.Exists(dir)){
            Debug.Log("The directory chosen is probably wrong: " + dir);
            return;
        }

        string[] searchPatterns = {"*.bytes"};

        
        IEnumerable<string> files = searchPatterns.SelectMany(pattern => Directory.EnumerateFiles(dir, pattern));


        foreach(string file in files)
        {
            readInBSTiff(file);
        }
    }

    void readInBSTiff(string path)
    {
        using (Tiff image = Tiff.Open(path, "r")) {
            if (image == null) {
                UnityEngine.Debug.LogError("Failed to open TIFF file at: " + path);
                return;
            }

            int width = image.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int height = image.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

            Debug.LogFormat("W: {0}\nH: {1}", width,height);
            int bitsPerSample = image.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();

        }
    }
}