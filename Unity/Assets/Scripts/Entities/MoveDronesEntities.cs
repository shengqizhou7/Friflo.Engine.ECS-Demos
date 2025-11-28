using UnityEngine;
using System;
using Entities.Drones;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class MoveDronesEntities : DroneSystemBase
{
    private DronesEntities drones;
    
    private Shape shape;

    private RenderParams rp;
    private Matrix4x4[] instData;
    private Vector4[] colorData;
    private MaterialPropertyBlock propertyBlock;
    private Color defaultColor = Color.gray;
    
    private EntityQuery entitiesQuery; 
    private ComponentTypeHandle<LocalTransform> transformTypeHandle;
    private ComponentTypeHandle<CubeColor> cubeColorTypeHandle;
    private EntityManager entityManager;

    private NativeArray<float4x4> transformMatrices;
 
    void Start()
    {
        entityCount = 1024;
        drones = new DronesEntities();
        Debug.Log($"初始化前DOTS世界中的总实体数: {World.DefaultGameObjectInjectionWorld.EntityManager.UniversalQuery.CalculateEntityCount()}");
        Debug.Log($"初始化前PGD世界中的总实体数: {PGDGameContext.GetWorld().Query().EntityCount}");
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

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        entitiesQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<DroneTag>(),
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.Exclude<DroneDisabled>());
        
        transformTypeHandle = entityManager.GetComponentTypeHandle<LocalTransform>(true);
        cubeColorTypeHandle = entityManager.GetComponentTypeHandle<CubeColor>(true);
        
        transformMatrices = new NativeArray<float4x4>(drones.maxDroneCount, Allocator.Persistent);
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
            default: Debug.LogError($"Unknown shape: {shape}"); break;
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

    private void RenderEntities()
    {
        if (World.DefaultGameObjectInjectionWorld == null || !World.DefaultGameObjectInjectionWorld.IsCreated)
        {
            return;
        }
        
        transformTypeHandle = entityManager.GetComponentTypeHandle<LocalTransform>(true);
        cubeColorTypeHandle = entityManager.GetComponentTypeHandle<CubeColor>(true);

        int maxEntitiesToRender = Math.Min(entityCount, instData.Length);
        int count = 0;
        
        using var chunks = entitiesQuery.ToArchetypeChunkArray(Allocator.Temp);

        for (int c = 0; c < chunks.Length && count < maxEntitiesToRender; c++)
        {
            var chunk = chunks[c];
            
            var transforms = chunk.GetNativeArray(ref transformTypeHandle);
            bool chunkHasCubeColor = chunk.Has(ref cubeColorTypeHandle);
            NativeArray<CubeColor> cubeColors = default;
            if (chunkHasCubeColor)
            {
                cubeColors = chunk.GetNativeArray(ref cubeColorTypeHandle);
            }
            int chunkEntityCount = chunk.Count;
            
            int entitiesToProcess = Math.Min(chunkEntityCount, maxEntitiesToRender - count);

            for (int i = 0; i < entitiesToProcess; i++)
            {
                var transform = transforms[i];

                Matrix4x4 matrix = Matrix4x4.TRS(
                    transform.Position,
                    transform.Rotation,
                    Vector4.one);
                instData[count] = matrix;

                Vector4 color = defaultColor;
                if (chunkHasCubeColor)
                {
                    color = cubeColors[i].Value;
                }
                colorData[count] = color;
                
                count++;
            }
        }

        propertyBlock.SetVectorArray("_BaseColor", colorData);
        rp.matProps = propertyBlock;
        
        if (count > 0)
        {
            Graphics.RenderMeshInstanced(rp,  _mesh, 0, instData, count);
        }

    }
    void Update()
    {
        var deltaTime = Time.deltaTime * 1000;
        drones.UpdateTransforms(deltaTime);
        
        RenderEntities();
        
        fpsSamples[sampleIndex++] = (int)(1.0f / Time.deltaTime);
        if (sampleIndex >= fpsSampleCount) sampleIndex = 0;
        
        UpdateFps();
    }

    public override void BuildNeighborRelations() => drones.BuildNeighborRelations();
    
    public override void PlotHotspotGraph() => drones.GenerateHotspotGraph(defaultColor);
    
    // 切换实现时触发
    public override void CleanupResources()
    {
        if (entitiesQuery != null && World.DefaultGameObjectInjectionWorld != null &&
            World.DefaultGameObjectInjectionWorld.IsCreated)
        {
            entitiesQuery.Dispose();
        }

        if (transformMatrices.IsCreated)
        {
            transformMatrices.Dispose();
        }

        drones.CleanupOnSwitchImpl();
    }
}