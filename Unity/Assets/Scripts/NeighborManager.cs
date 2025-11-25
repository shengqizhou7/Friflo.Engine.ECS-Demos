using PGD;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; 

namespace PGD.Drones
{
    public class NeighborManager : MonoBehaviour
    {
        private static NeighborManager instance;
        
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
            instance.hotSpotButton.interactable = relationBuilt;
            instance.relationButton.interactable = !relationBuilt;
        }

        // 清除邻居关系
        public void ClearNeighborRelations() => _currentSystem?.ClearNeighborRelations();

        // 清除颜色组件和待更新颜色的标签
        public void ClearAllColors() => _currentSystem?.ClearAllColors();
        
        // 清除记录命中次数的Lookup组件
        public void ClearHitCounters() => _currentSystem?.ClearHitCounters();
        
        // 建立邻居关系
        public void BuildNeighborRelations() => _currentSystem?.BuildNeighborRelations();

        // 生成命中热点图
        public void GenerateHotspotGraph() => _currentSystem?.GenerateHotspotGraph();
    }
}