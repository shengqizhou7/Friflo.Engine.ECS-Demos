using System;
using PGD;
using PGD.Drones;
using UnityEngine;

public class MoveDronesPGD : DroneSystemBase
{
    private DronesPGD drones;
    
    private Shape shape;

    private RenderParams rp;
    private Matrix4x4[] instData;
    private Vector4[] colorData;

    private MaterialPropertyBlock propertyBlock;
    
    public Color neighborhoodColor = Color.cyan;
    
    void Start()
    {
        entityCount = 1024;
        drones = new DronesPGD();
        drones.Initialize();
        drones.SetEntityCount(entityCount);
        drones.SetTargetPlane(500, 1.2f);
        
        UpdateGuiCount();
        _material.enableInstancing = true;
        rp = new RenderParams(_material);
        instData = new Matrix4x4[drones.maxDroneCount];
        colorData = new Vector4[drones.maxDroneCount];
        propertyBlock = new MaterialPropertyBlock();

        GameObject editorPlane = GameObject.Find("Editor Plane");
        if (editorPlane != null)
        {
            editorPlane.SetActive(false);
        }
    }
    
    private void SetShape (Shape shape)
    {
        this.shape = shape;
        switch (shape)
        {
            case Shape.Plane:	drones.SetTargetPlane(500, 1.2f); 		    break;
            case Shape.Cube:	drones.SetTargetCube (500, 1.2f);			break;
            // case Shape.Ring:	drones.SetTargetRings(500, 24, 1.2f, 1);	break;
            // case Shape.Rings:	drones.SetTargetRings(500, 20, 1.2f, 10);	break;
        }
    }
    
    public override void SetTargetPlane()    => SetShape(Shape.Plane);
    public override void SetTargetCube()     => SetShape(Shape.Cube);
    public override void SetTargetRing()     => SetShape(Shape.Ring);
    public override void SetTargetRings()    => SetShape(Shape.Rings);
    
    public override void IncreaseCount() {
        entityCount = Math.Min(drones.maxDroneCount, entityCount * 2);
        drones.SetEntityCount(entityCount);
        SetShape(shape);
        UpdateGuiCount();
    }
    
    public override void DecreaseCount() {
        entityCount = Math.Max(4, entityCount / 2);
        drones.SetEntityCount(entityCount);
        SetShape(shape);
        UpdateGuiCount();
    }
    
    void Update()
    {
        fpsSamples[sampleIndex++] = (int)(1.0f / Time.deltaTime);
        if (sampleIndex >= fpsSampleCount) sampleIndex = 0;
        
        UpdateFps();
        
        // var deltaTime = Time.deltaTime * 1000;
        // drones.UpdateTransforms(deltaTime, default);

        int n = 0;
        drones.transQuery.ForEachEntity(((ref PGDTransform transform, IEntity entity) =>
        {
            colorData[n] = new Vector4(neighborhoodColor.r, neighborhoodColor.g, neighborhoodColor.b, neighborhoodColor.a);
            instData[n] = transform.mtr.AsUnityMatrix4x4();
            n++;
        }));
        
        // int entityCount = n;
        
        // Vector4[] actualColors = new Vector4[entityCount];
        // for (int i = 0; i < entityCount; i++)
        // {
        //     actualColors[i] = colorData[i];
        // }
        propertyBlock.SetVectorArray("_BaseColor", colorData);
        rp.matProps = propertyBlock;
        Graphics.RenderMeshInstanced(rp, _mesh, 0, instData, entityCount);
    }

    public override void CleanupResources()
    {
        
    }
}