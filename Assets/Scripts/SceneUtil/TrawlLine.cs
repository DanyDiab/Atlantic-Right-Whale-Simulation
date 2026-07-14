using AGXUnity;
using AGXUnity.Rendering;
using AGXUnity.Utils;
using AGXUnityEditor.Menus;
using Unity.VisualScripting;
using UnityEngine;

public class TrawlLine : ScriptComponent{
    [SerializeField] GameObject lobsterTrap;
    [SerializeField] GameObject buoy;

    float buoyHeight;

    float trapWidth;
    float trapHeight;

    [SerializeField] Material wireMat;
    [SerializeField] ShapeMaterial wireShapeMat;

    int minOnTrawl = 1;
    int maxOnTrawl = 3;

    [SerializeField] float spacingDelta;

    [SerializeField] float depth = 30;

    protected override bool Initialize(){
        getFishingProperties();
        // spawnTrawl(Vector2.zero);
        return base.Initialize();
    }


    private void getFishingProperties()
    {
        buoyHeight = buoy.GetComponentInChildren<AGXUnity.Collide.Capsule>().Height;

        AGXUnity.Collide.Box box = lobsterTrap.GetComponentInChildren<AGXUnity.Collide.Box>();

        trapWidth = box.HalfExtents.x * 2;
        trapHeight = box.HalfExtents.y * 2;

        
    }

// spawnPosition = x,z 
    public GameObject spawnTrawl(Vector2 spawnPosition, GameObject parent){

        Vector3 depthPosition = new Vector3(spawnPosition.x, depth, spawnPosition.y);

        GameObject buoyParent = new GameObject("BUOY");
        parent.AddChild(buoyParent);

        
        GameObject buoyGO = Instantiate(buoy, depthPosition, Quaternion.identity, buoyParent.transform);

        Wire wire = buoyParent.AddComponent<Wire>();
        WireRenderer renderer = buoyParent.AddComponent<WireRenderer>();

        renderer.Material = wireMat;
        wire.Material = wireShapeMat;
        wire.Diameter = .1f;
        WireRouteNode buoyRouteNode = WireRouteNode.Create(Wire.NodeType.BodyFixedNode,buoyGO,Vector3.up * -buoyHeight, Quaternion.identity);
        wire.Route.Add(buoyRouteNode);


        float randT = Random.Range(0.0f,1.0f);
        int numTraps = Mathf.RoundToInt(Mathf.Lerp(minOnTrawl,maxOnTrawl, randT));

        for(int i = 0; i < numTraps; i++){
            float deltaOffset = spacingDelta * i;
            
            Vector3 offset = new Vector3(deltaOffset, -depth -trapHeight,0);
            Vector3 position = offset + depthPosition;

            GameObject trap = Instantiate(lobsterTrap,position,Quaternion.identity,buoyParent.transform);

            Wire.NodeType nodeType = i == numTraps - 1 ? Wire.NodeType.BodyFixedNode : Wire.NodeType.EyeNode;

            Vector3 ropeBoxDelta = new Vector3(0, trapHeight, 0);

            WireRouteNode trapRouteNode = WireRouteNode.Create(nodeType,trap, ropeBoxDelta, Quaternion.identity);
            wire.Route.Add(trapRouteNode);

        }
        GetSimulation().add(wire.Native);

        return buoyParent;
    }

}