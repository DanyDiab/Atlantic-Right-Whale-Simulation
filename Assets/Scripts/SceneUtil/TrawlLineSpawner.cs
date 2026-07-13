using AGXUnity;
using AGXUnity.Rendering;
using Unity.VisualScripting;
using UnityEngine;

public class TrawlLineSpawner : ScriptComponent{
    [SerializeField] GameObject lobsterTrap;
    [SerializeField] GameObject buoy;

    float buoyHeight;

    float trapWidth;
    float trapHeight;

    [SerializeField] Material wireMat;
    [SerializeField] ShapeMaterial wireShapeMat;

    int minOnTrawl = 5;
    int maxOnTrawl = 15;

    [SerializeField] float spacingDelta;

    [SerializeField] float depth = 50;

    protected override bool Initialize(){
        getFishingProperties();
        routeWire();
        return base.Initialize();
        
    }


    private void getFishingProperties()
    {
        buoyHeight = buoy.GetComponentInChildren<AGXUnity.Collide.Capsule>().Height;

        AGXUnity.Collide.Box box = lobsterTrap.GetComponentInChildren<AGXUnity.Collide.Box>();

        trapWidth = box.HalfExtents.x * 2;
        trapHeight = box.HalfExtents.y * 2;

        
    }

    public void routeWire(){
        Wire wire = gameObject.AddComponent<Wire>();
        WireRenderer renderer = gameObject.AddComponent<WireRenderer>();

        renderer.Material = wireMat;
        wire.Material = wireShapeMat;
        wire.Diameter = .1f;
        WireRouteNode buoyRouteNode = WireRouteNode.Create(Wire.NodeType.BodyFixedNode,buoy,Vector3.up * -buoyHeight, Quaternion.identity);
        wire.Route.Add(buoyRouteNode);


        int randT = Random.Range(0,1);
        int numTraps = Mathf.RoundToInt(Mathf.Lerp(minOnTrawl,maxOnTrawl, randT));

        for(int i = 0; i < numTraps; i++){
            float deltaOffset = spacingDelta * i;
            
            Vector3 offset = new Vector3(deltaOffset,-depth - trapHeight,0);
            Vector3 position = offset + buoy.transform.position;

            GameObject trap = Instantiate(lobsterTrap,position,Quaternion.identity,gameObject.transform);

            Wire.NodeType nodeType = i == numTraps - 1 ? Wire.NodeType.BodyFixedNode : Wire.NodeType.EyeNode;

            Vector3 ropeBoxDelta = new Vector3(0, trapHeight, 0);

            WireRouteNode trapRouteNode = WireRouteNode.Create(nodeType,trap, ropeBoxDelta, Quaternion.identity);
            wire.Route.Add(trapRouteNode);

        }
        GetSimulation().add(wire.Native);

        Debug.Log(wire.Route.NumNodes);
    }

}