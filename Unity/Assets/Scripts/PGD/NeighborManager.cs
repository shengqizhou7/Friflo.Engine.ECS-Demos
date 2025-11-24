using PGD;
using UnityEngine;
using System.Collections.Generic;

namespace PGD.Drones
{
    public struct NeighborOf : IEntityRelation
    {
        public IEntity Target;
        public float Distance;
        public IEntity GetGenericKey() => Target;
    }
    
    // public class NeighborManager : PGDSystem<PGDTransform, PGDPosition, Start, Target>
    public class NeighborManager : MonoBehaviour
    {
        private float neighborDistance = 2f; //TODO: 是否应该在不同排列场景中设置不同的数值？
        public static bool relationBuilt; // 是否已构建过邻居关系
    
        private static IECSWorld world = PGDGameContext.GetWorld();
        private static IQuery query = PGDGameContext.GetWorld().Query().WithAllComponents(IComponents.Get<PGDTransform, PGDPosition, Start, Target>()).WithoutAnyTags(ITags.Get<Disabled>());
        private IEntity selectedEntity;

        // 清除邻居关系
        public static void ClearNeighborRelations()
        {
            int n = 0;
            var queryRelation = world.QueryRelation<NeighborOf>();
            queryRelation.ForEachEntity((ref NeighborOf neighborOf, IEntity entity) =>
            {
                entity.RemoveRelation<NeighborOf>(neighborOf.Target);
                n++;
            });
            
            Debug.Log($"清除了 {n} 个实体的邻居关系。残留 { world.QueryRelation<NeighborOf>().EntityCount } 个关系");
            
            relationBuilt = false;
            GameObject.Find("MoveDrones")?.transform.Find("HotSpot").gameObject.SetActive(false); // 禁用HotSpot按钮
        }

        // 清除颜色组件和待更新颜色的标签
        public static void ClearAllColors()
        {
            var cq = world.GetCommandQueue();
            // var queryColors = query.WithAllComponents(IComponents.Get<CubeColor>()); 
            foreach (var entity in query.Entities)
            {
                if (entity.HasComponent<CubeColor>())
                {
                    cq.RemoveComponent<CubeColor>(entity.Id);
                    cq.RemoveTag<ColorToBeUpdated>(entity.Id);
                }
            }
            cq.Apply();
        }
        
        // 清除记录命中次数的Lookup组件
        public static void ClearHitCounters()
        {
            var cq = world.GetCommandQueue();
            var hitQuery = world.Query<HitCounter>();
            foreach (var entity in hitQuery.Entities)
            {
                cq.RemoveComponent<HitCounter>(entity.Id);
            }
            cq.Apply();
        }
        
        // 建立邻居关系
        public void BuildNeighborRelations()
        {
            if (relationBuilt) return;
            
            ClearNeighborRelations();

            // 建立新关系
            foreach (var entityA in query.Entities)
            {
                foreach (var entityB in query.Entities)
                {
                    if (entityA.Id == entityB.Id) continue;
                    
                    ref var transA = ref entityA.GetComponent<PGDTransform>();
                    ref var transB = ref entityB.GetComponent<PGDTransform>();

                    // 提取位置
                    Vector3 posA = new Vector3(transA.mtr.Translation.X, transA.mtr.Translation.Y,
                        transA.mtr.Translation.Z);
                    Vector3 posB = new Vector3(transB.mtr.Translation.X, transB.mtr.Translation.Y,
                        transB.mtr.Translation.Z);

                    float distance = Vector3.Distance(posA, posB);

                    if (distance <= neighborDistance)
                    {
                        entityA.AddRelation(new NeighborOf { Target = entityB, Distance = distance }, out _);
                    }
                }
            }
            Debug.Log($"共 {query.Entities.Count} 个实体建立了邻居关系，当前共有 {world.QueryRelation<NeighborOf>().EntityCount} 条关系");
            
            world.RegisterSystem(new ColorUpdateSystem()); // 玩家点击添加关系按钮时注册颜色变换系统
            relationBuilt = true; // 标志已建立邻居关系
            GameObject.Find("MoveDrones")?.transform.Find("HotSpot").gameObject.SetActive(true); // 启用HotSpot按钮
        }

        // 生成命中热点图
        public void GenerateHotspotGraph()
        {
            var cq = world.GetCommandQueue();
            
            foreach (var entity in query.Entities)
            {
                cq.AddComponent(entity.Id, new CubeColor { Value = Color.gray });
                cq.RemoveTag<ColorToBeUpdated>(entity.Id);
            }
            
            var hitsLookup = world.ComponentLookup<HitCounter, int>();

            foreach (var hits in hitsLookup.Values)
            {
                var buckets = hitsLookup[hits];
                Color color = hits switch
                {
                    1 => new Color(0.95f, 0.78f, 0.15f, 1f),               // cool green
                    >= 2 and <= 3 => new Color(1.0f, 0.42f, 0.1f, 1f),  // warm amber
                    >= 4 and <= 5 => new Color(0.95f, 0.08f, 0.0f, 1f),    // hot orange
                    >= 6 => new Color(0.95f, 0.08f, 0.0f, 1f),            // lava red
                    _ => new Color(0.25f, 0.25f, 0.25f, 1f)               // default gray
                };

                foreach (var entityId in buckets.Ids)
                {
                    cq.AddComponent(entityId, new CubeColor { Value = color });
                }
            }

            cq.Apply();
        }
    }
}