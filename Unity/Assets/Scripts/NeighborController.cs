using PGD;
using UnityEngine;
using UnityEngine.UI; 

public class NeighborController : MonoBehaviour
{
    private static NeighborController instance;
    
    public static float neighborDistance = 2f; //TODO: 是否应该在不同排列场景中设置不同的数值？
    public static bool relationBuilt; // 是否已构建过邻居关系
    [SerializeField] private Button hotSpotButton;
    [SerializeField] private Button relationButton;

    private IEntity selectedEntity;
    public static IDroneSystem _currentSystem;

    private void Awake()
    {
        instance = this;
        UpdateHotSpotButtonState();
    }

    public static void UpdateHotSpotButtonState()
    {
        bool inECS = MoveDronesController.implementationType != ImplementationType.OOP; // OOP实现中禁用两个按钮
        instance.hotSpotButton.interactable = inECS & relationBuilt;
        instance.relationButton.interactable = inECS & !relationBuilt;
    }

    // 建立邻居关系
    public void BuildNeighborRelations() => _currentSystem?.BuildNeighborRelations();
    
    // 清除邻居关系
    public void ClearNeighborRelations() => _currentSystem?.ClearNeighborRelations();

    // 清除颜色组件和待更新颜色的标签
    public void ClearAllColors() => _currentSystem?.ClearAllColors();

    // 生成命中热点图
    public void PlotHotspotGraph() => _currentSystem?.PlotHotspotGraph();
    
    // 清除记录命中次数的Lookup组件
    public void ClearHitCounters() => _currentSystem?.ClearHitCounters();

    public void checkEntity()
    {
        Debug.Log($"当前DOTS世界中的总实体数: {Unity.Entities.World.DefaultGameObjectInjectionWorld.EntityManager.UniversalQuery.CalculateEntityCount()}");
        Debug.Log($"当前PGD世界中的总实体数: {PGDGameContext.GetWorld().Query().EntityCount}");
    }
}
