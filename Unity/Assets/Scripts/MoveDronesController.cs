using UnityEngine;
using System;
using System.Collections;
using PGD.Drones;
using TMPro;
using UnityEngine.UI;
using Unity.Entities;

public enum ImplementationType
{
    PGD,
    Entities,
    OOP
}

public class MoveDronesController : MonoBehaviour
{
    [SerializeField] protected TMP_Text count;
    [SerializeField] protected TMP_Text fpsText;
    [SerializeField] protected Material material;
    [SerializeField] protected Mesh mesh;
    
    [Header("实现选择")]
    [SerializeField] public static ImplementationType implementationType;
    
    public static IDroneSystem _currentSystem;

    private void Awake()
    {
        Application.targetFrameRate = 120;
        QualitySettings.vSyncCount = 0;
        GameObject editorPlane = GameObject.Find("Editor Plane");
        if (editorPlane != null)
        {
            editorPlane.SetActive(false);
        }
        
        // 禁用 Unity Entities 系统（如果需要）
        var world = World.DefaultGameObjectInjectionWorld;
        if (world != null)
        {
            var EntitiesSystem = world.GetOrCreateSystemManaged<Entities.Drones.DroneUpdateSystem>();
            if (EntitiesSystem != null)
            {
                EntitiesSystem.Enabled = false;
            }
            
            var colorPropagationSystem = world.GetOrCreateSystemManaged<Entities.Drones.ColorPropagationSystem>();
            if (colorPropagationSystem != null)
            {
                colorPropagationSystem.Enabled = false;
            }
        }
    }

    private void Start()
    {
        SwitchImplementationType(implementationType);
    }

    public void SwitchImplementationType(ImplementationType newType)
    {
        _currentSystem?.DisableSystem();
        CleanupSceneObjects();
        implementationType = newType;
        _currentSystem = DroneSystemFactory.CreateSystem(newType, gameObject);
        InitializeSystem(_currentSystem);
        _currentSystem.EnableSystem();
        NeighborManager._currentSystem =  _currentSystem;
    }

    private void InitializeSystem(IDroneSystem system)
    {
        system.Count = count;
        system.FpsText = fpsText;
        system.Material = material;
        system.Mesh = mesh;
    }

    public void ApplyImplementationChoice()
    {
        SwitchImplementationType(implementationType);
        
        GC.Collect();
        Resources.UnloadUnusedAssets();
    }

    private void CleanupSceneObjects()
    {
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj != null && obj.activeInHierarchy &&
                obj.transform.parent == null &&
                (obj.name.StartsWith("Entity_") ||
                 obj.name.StartsWith("Instance") ||
                 obj.name.StartsWith("Drone_")) &&
                obj != this.gameObject &&
                !obj.GetComponent<MoveDronesController>() &&
                !obj.GetComponent<Canvas>() &&
                !obj.GetComponent<Button>() &&
                !obj.GetComponent<Camera>())
            {
                Destroy(obj);
            }
        }
    }

    public void SetTargetPlane() => _currentSystem?.SetTargetPlane();
    public void SetTatgetCube() => _currentSystem?.SetTargetCube();
    public void SetTatgetRing() => _currentSystem?.SetTargetRing();
    public void SetTatgetRings() => _currentSystem?.SetTargetRings();
    public void IncreaseCount() => _currentSystem?.IncreaseCount();
    public void DecreaseCount() => _currentSystem?.DecreaseCount();

    public void SetImplementation(int index)
    {
        implementationType = (ImplementationType)index;
        ApplyImplementation();
    }

    private void OnValidate()
    {
        if (Application.isPlaying && _currentSystem != null)
        {
            ApplyImplementation();
        }
    }

    private void ApplyImplementation()
    {
        StartCoroutine(AddSystemLater());
    }

    private IEnumerator AddSystemLater()
    {
        yield return null;
        ApplyImplementationChoice();
    }
}