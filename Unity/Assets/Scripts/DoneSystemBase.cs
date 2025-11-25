using TMPro;
using UnityEngine;

public enum Shape
{
    Plane,
    Cube,  
    Ring,
    Rings
}

public interface IDroneSystem
{
    TMP_Text Count { get; set; }
    TMP_Text FpsText { get; set; }
    Material Material { get; set; }
    Mesh Mesh { get; set; }

    void EnableSystem();
    void DisableSystem();
    void SetTargetPlane();
    void SetTargetCube();
    void SetTargetRing();
    void SetTargetRings();
    void IncreaseCount();
    void DecreaseCount();
    void BuildNeighborRelations();
    void ClearNeighborRelations();
    void ClearAllColors();
    void ClearHitCounters();
    void GenerateHotspotGraph();
    void CleanupResources();
}

public abstract class DroneSystemBase : MonoBehaviour, IDroneSystem
{
    [SerializeField] protected TMP_Text _count;
    [SerializeField] protected TMP_Text _fpsText;
    [SerializeField] protected Material _material;
    [SerializeField] protected Mesh _mesh;

    public TMP_Text Count
    {
        get => _count;
        set => _count = value;
    }
    
    public TMP_Text FpsText
    {
        get => _fpsText;
        set => _fpsText = value;
    }
    
    public Material Material
    {
        get => _material;
        set => _material = value;
    }
    
    public Mesh Mesh
    {
        get => _mesh;
        set => _mesh = value;
    }

    public void EnableSystem() => enabled = true;
    public void DisableSystem() => enabled = false;

    protected const int fpsSampleCount = 30;
    protected readonly int[] fpsSamples = new int[fpsSampleCount];
    protected int sampleIndex;
    protected int entityCount;
    
    public abstract void SetTargetPlane();
    public abstract void SetTargetCube();
    public abstract void SetTargetRing();
    public abstract void SetTargetRings();
    public abstract void IncreaseCount();
    public abstract void DecreaseCount();
    public abstract void CleanupResources();
    public abstract void BuildNeighborRelations();
    public abstract void ClearNeighborRelations();
    public abstract void ClearAllColors();
    public abstract void ClearHitCounters();
    public abstract void GenerateHotspotGraph();

    private void HideEditorPlane()
    {
        GameObject editorPlane = GameObject.Find("Editor Plane");
        if (editorPlane != null)
        {
            editorPlane.SetActive(false);
        }
    }

    protected virtual void OnDisable()
    {
        CleanupResources();
    }

    protected void UpdateGuiCount()
    {
        _count.text = $"Count: {entityCount}";
    }

    protected void UpdateFps()
    {
        var sum = 0;
        for (var i = 0; i < fpsSampleCount; i++)
        {
            sum += fpsSamples[i];
        }
        _fpsText.text = $"FPS: {sum / fpsSampleCount}";
    }
}