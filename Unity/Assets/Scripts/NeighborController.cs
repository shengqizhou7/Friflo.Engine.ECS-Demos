using UnityEngine;
using UnityEngine.UI; 

public class NeighborController : MonoBehaviour
{
    private static NeighborController instance;
    
    public static float neighborDistance = 2f;
    public static bool relationBuilt; // 是否已构建过邻居关系
    public static IDroneSystem currentSystem;
    [SerializeField] private Button hotSpotButton;
    [SerializeField] private Button relationButton;

    private void Awake()
    {
        instance = this;
        UpdateHotSpotButtonState();
    }

    public static void UpdateHotSpotButtonState()
    {
        bool inECS = MoveDronesController.implementationType != ImplementationType.OOP; // OOP实现中禁用两个按钮
        instance.hotSpotButton.interactable = inECS && relationBuilt;
        instance.relationButton.interactable = inECS && !relationBuilt;
    }
    
    public void BuildNeighborRelations() => currentSystem?.BuildNeighborRelations(); // 建立邻居关系
    
    public void PlotHotspotGraph() => currentSystem?.PlotHotspotGraph(); // 生成命中热点图

    public void checkEntity()
    {
        Debug.Log($"当前DOTS世界中的总实体数: {Unity.Entities.World.DefaultGameObjectInjectionWorld.EntityManager.UniversalQuery.CalculateEntityCount()}");
        Debug.Log($"当前PGD世界中的总实体数: {PGDGameContext.GetWorld().Query().EntityCount}");
        Debug.Log($"当前PGD世界中的总关系数: {PGDGameContext.GetWorld().QueryRelation<PGD.Drones.NeighborOf>().EntityCount}");
    }
}
