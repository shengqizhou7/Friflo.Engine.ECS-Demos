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
    
    public Color defaultColor = Color.gray;
    
    void Start()
    {
        entityCount = 1024;
        drones = new DronesPGD();
        Debug.Log($"初始化前PGD世界中的总实体数: {PGDGameContext.GetWorld().Query().EntityCount}"); // TODO: 为什么初始化前世界中的总实体数=3
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
            case Shape.Ring:	drones.SetTargetRings(500, 24, 1.2f, 1);	break;
            case Shape.Rings:	drones.SetTargetRings(500, 20, 1.2f, 10);	break;
        }
    }
    
    public override void SetTargetPlane() => SetShape(Shape.Plane);
    public override void SetTargetCube() => SetShape(Shape.Cube);
    public override void SetTargetRing() => SetShape(Shape.Ring);
    public override void SetTargetRings() => SetShape(Shape.Rings);
    
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

        int n = 0;
        drones.transQuery.ForEachEntity(((ref PGDTransform transform, IEntity entity) =>
        {
            // 检查实体是否有自定义颜色组件
            if (entity.HasComponent<CubeColor>())
            {
                var cubeColor = entity.GetComponent<CubeColor>();
                colorData[n] = cubeColor.Value;
            }
            else
            {
                // 使用默认颜色
                colorData[n] = defaultColor;
            }
            
            instData[n] = transform.mtr.AsUnityMatrix4x4();
            n++;
        }));
        
        // int renderCount = n;
        
        // // 只传递实际需要渲染的颜色数据
        // Vector4[] actualColors = new Vector4[renderCount];
        // for (int i = 0; i < renderCount; i++)
        // {
        //     actualColors[i] = colorData[i];
        // }
        
        propertyBlock.SetVectorArray("_BaseColor", colorData);
        rp.matProps = propertyBlock;
        
        // if (renderCount > 0)
        // {
            Graphics.RenderMeshInstanced(rp, _mesh, 0, instData, entityCount);
        // }
    }

    public override void CleanupResources()
    {
        drones.CleanupOnSwitchImpl();
    }

    public override void BuildNeighborRelations() => drones.BuildNeighborRelations();

    public override void ClearNeighborRelations() => drones.ClearNeighborRelations();

    public override void ClearAllColors() => drones.ClearAllColors();

    public override void ClearHitCounters() => drones.ClearHitCounters();

    public override void PlotHotspotGraph() => drones.PlotHotspotGraph();
}