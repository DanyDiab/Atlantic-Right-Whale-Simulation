# NARW Simulation Documentation

This markdwon file contains documetation on Dany's work on the NARW summer 26 project. This will include information on both the systems I have engineered, as well as systems that I have used and worked with, such as Algoryx (AGX).

Information contained will include design ideas, as well as how to operate the system.

**Last Update: 06/29/26**

## Document Layout

This document will be comprised into 2 major sections. Those of which being 

1. Systems I created and how they operate, and why they are made the way they are.
2. Systems I have **not** created but had to work with. (mainly AGX)


## My Systems

### Organization / Layout

In terms of my pipeline, there are 2 main components:

1. **Bathymetry**

    "Bathymetry is the study of underwater depth of ocean floors (seabed topography), river floors, or lake floors. In other words, bathymetry is the underwater equivalent to hypsometry or topography." (Wikipedia)

2. **Backscatter**
   
    "In physics, backscatter (or backscattering) is the reflection of waves, particles, or signals back to the direction from which they came."

Each of these 2 Main Components is then subdivided into 2 tasks that the pipeline handles, I like to call these the **Preprocess** and **Runtime** parts:

1. **Preprocess:** 
   
   Reading in real data in the form of a geoTIFF, applying some preprocessing, and store the preprocessed data into a binary file for the next step of the pipeline to make use of.
2. **Runtime** 
   
   Reading in the Binary File that the previous section, read it in and apply it to the runtime enviornment.


The main reason for needing 2 different sections is due to the fact of having expensive operations that can take several minutes. Rather than processing each time, we can preprocess once and cache the result. **NOTE** Runtime operations, like the name implies shouldb e ran in realtime. If any operation in the runtime section is too slow, it should be moved to the preprocess step. 


### Bathymetry Preprocess Step

The Bathymetry Preprocess step contains 3 steps, those being 

1. **Reading:** Read in geoTIFF data from the CHS dataset (https://data.chs-shc.ca/dashboard/map). Each chunk of bathymetry is converted to a ```DepthDataRecord```, The definition can be found in ```./Assets/Scripts/Bathymetry/DepthDataRecord.cs```. This contains a reference to a ```GeoTiffData```, which can be found at ```./Assets/Scripts/Util/GeoTiffData.cs```. This contains all necessary and important information from reading in the geoTiff. At this stage we also determine the chunk positions relative to each other. This is done in the following manner:
    1. Find the minimum X and Z coordinate of the chunks. 
    2. Use the minimum X and Z as an origin point. 
   
2. **Patching:** Real data is messy. Real data is not defined everywhere. This is the key motivation for this section of the pipeline. There are many points in the Bathymetry that are undefined, and if not processed, would break the plausiblity of the environment, as they lead in spikes in the data. How the pipeline handles missing data points is an algorithm called Inverse Distance Weighting (IDW).
    Essentially, we take the nearest X (typically 4 is chosen) known points and take a weighted average of them. We use $1 / dP$, where $dP$  is the distance to the unknown point. 
    - Note that instead of adding the exact depth at a certain point, the system adds multiple octaves of perlin noise to break up regularity. To ensure that our new interpolated point still goes through our control points (the known points), we mask the noise based on the distance to a known point. If we are within X units of the known point, do not add any noise, enforcing control point crossover, and ensuring a smooth continous surface.
3. **Writing:** After Reading and Patching, we can then write the processed data into a binary (.bytes) file for runtime usage. The binary file is organized like this:    
   
    | Field Name | Data Type | Size (Bytes) |
    | :--- | :--- | :--- |
    | Width | `float` | 4 |
    | Height | `float` | 4 |
    | chunkPosition X | `float` | 4 |
    | chunkPosition Y | `float` | 4 |
    | chunkGlobalStartPosition X | `float` | 4 |
    | chunkGlobalStartPosition Y | `float` | 4 |
    | pixelScale X | `double` | 8 |
    | pixelScale Y | `double` | 8 |
    | dataCount | `int` | 4 |
    | dataPoints | `List<float>` | 4 * dataCount |

### Bathymetry Runtime Step

The Bathymetry runtime step involves the construction of a mesh and displaying chunks of data. IT is realtively simple and follows a conventional data process for mesh construction.

This step as mentioned previously will use the data from the process step to display. 

First, we read in the binary file containing the preprocessed data. The organization of this binary file can be found in the previous section. These Binary files can and should be represent using the ```DepthDataRecord``` struct

Then for each DepthDataRecord, we first construct a unity mesh. Then we position it using the relative chunks position, as well as the chunk size which is defined in the ScriptableObject ```./Assets/Scripts/ProcessingSettings.cs```. We then parent this game object to a set Gameobject parent.


### Backscatter Preprocess Step

This is where things get complicated, as this is where I faced some roadblocks. 

The backscatter pipeline is still underworks and will likely to change later on in the work of this project. 

Generally, the backscatter (BS) pipeline contains 5 steps.

Those being:

1. **Reading:**
2. **Simple Preprocessing**
3. **Cropping**
4. **Projecting**
5. **Writing to Binary**


#### Reading

Reading involves reading in the BS data from the CHS dataset and converting to a ```List<GeoTiffData>```. The reading is comprised of 2 main files that need to be read in.
1. GeoTiff file
2. JSON file

The GeoTiff data contains the raw Backscatter intesity values, however it is missing some key data that is needed for the full picture, such as min and max of the intensity values. This is needed to be passed along to the tiff reader, as it assumes a range of values to clip/enforce values to stay within. 

#### Simple Preprocessing

After the files are read in and processed into a ```List<GeoTiffData>```, we only grab the first file. (THIS WILL BE CHANGED). The reason for this was to simplify the processing. The Bay of Fundy (where the backscatter originated from, was one massive backscatter file, so assuming one file, while not good practice, was feasible for the data, and simplified the preprocessing).

In this master file, we normalize the values from the range defined in the JSON file, to a 0-1 range. This step ensures the data is easier to work with mapping directly to intensity values, where 1.0 represents the max and 0.0 represents the minimum, no matter what data is processed.

#### Cropping 

Before continuing, we should understand the key motivation and purpose of the next two steps. The key motivation for this step, while it is to reduce the compuation time, the key insight is that the Bathymetry data is in Geographical Latitude and Longitude, and the backscatter is in UTM. We need a way to spatially relate these two datasets. They need to be brought into the same coordinate space. This can be done using projections. we should also note that projecting is an **expensive** operation, especially on large datasets. This will be gone into more detail in the next section, **Projection**.

Now armed with infomation of why we need to project datasets, and that it is computationally expensive, we should then decide which coordinate space we should unify both datasets into. After some initial thoughts, it made more sense to normalize the backscatter into geographical latitude and longitude. 

**Why? (Skip this part if wanted)**

While this may seem counter intuitive, why would I pick to normalize into geographical coordintes rather than use UTM. After all, UTM measures in **METERS**, and lat/long is in degrees. Surely working with meters would be easier than working with degrees. This too was my initial thought process. So I commenced converting the Bathymetry data into UTM, and this is where complications and my assumptions from earlier in the project came back around. Earlier in the project when handling the Bathymetry files, I noted that the chunks of bathymetry are 10KM^2 of data, and each is 10 "arbitrary units" apart. This way I could map the bathymetry data onto a flat surface Later on I came to learn that these arbitrary units were degrees of Latitude and Longitude, so my assumption was **warping** the space. When converting into UTM, now I was a real projection, rather than my made up system, so now each chunk, rather than being arbitraily placed 10KM apart, they were uniquely unevenly spaced. Some chunks were 7.8KM apart, others 7.9. At this point, I decided that converting to UTM was **feasible**, but it would be like running into a brick wall. If I wanted to continue down this path, I would likely need to **redesign** my entire system from the ground up. Hence, the decision was taken, I will continue to make my assumption of .1 Degree of Lat/Long = 10KM, and I will convert backscatter into Lat/Long.

**Back to the pipeline**

After the 0-1 Normalization is complete, we begin processing and chunking the backscatter data. As mentioned above, the Bay of Funday BS data from the CHS dataset is one massive dataset. To reduce the computation time, one realization and key finding needed is that instead of preproccesing the entire dataset, only preprocess the parts that are needed, so what is needed, what parts of this entire file do I need? 

I need only the parts that I will be made into mesh chunks. So, this first step of chunking, is grabbing the bathymetry data and projecting them into UTM.
Then from here, I am able to determine where in the massive master BS array each chunk would be using the resoluiton of the data (spatial distance between points). 

I then only save these cropped BS chunks. Which could then be projected. 

Overall instead of projecting roughly ~30 x 30 Chunks of data, we only process the number of chunks wanted, typically only around 5. (However because the clipping requires projecting into UTM space, it is really 2X Projections, where X is the number of chunks we want to show) Which is much less than ~900. 

#### Projection

This is the final preprocessing step for the Backscatter pipeline (excluduing the writing to a binary file, as that is trivial).

Projecting from one coordinate space into another is a complex and expensive operation. 

For our purposes, we need to handle **UTM** <=> **Geographical Lat/Long**. As well as being able to project, we should be able to **upsample** as we project to maintain a uniform resolution across datasets.

Projection comes with 2 distinct steps. A forward and backward pass. The reason for having 2 distinct steps is to ensure points defined uniformly across a grid, effectivly combating projection warping on individual points. For example, say you have a list of data points in lat/long which are spaced 10 meters apart (If this doesn't make sense, read the why section in the Cropping step). when Projecting, we would like to keep points to be evenly spaced apart. However, when projecting, points do not end up exactly in the sapce spot, this is due to warping. We first do a **forward pass**, where we convert all of our known points into the new coordinate space, ignoring the warping effect. Then afterwards, we do a **backward pass**. In this backward pass, instead of looping over each point that was just projected, we loop over each **TARGET** point. The target points are the uniform spaced points that we define based on the resolution. foreach target point, we interpolate the nearest X neighbors together, essentially taking an average for our target point. It is worth noting, that we upsample in the backward pass, based on the target resolution.

We then have our chunked, upsampled, projected backscatter data (what a mouthfull) that is ready to be writting into a binary file. 


#### Writing To Binary

The process of writing to a binary file is trivial. It follows the following structure:

| Field Name | Data Type | Size (Bytes) |
| :--- | :--- | :--- |
| Width | `float` | 4 |
| Height | `float` | 4 |
| dataCount | `int` | 4 |
| dataPoints | `List<float>` | 4 * dataCount |



### Backscatter Runtime Step

THIS IS STILL BEING WORKED ON!


## References

https://en.wikipedia.org/wiki/Bathymetry
https://en.wikipedia.org/wiki/Backscatter