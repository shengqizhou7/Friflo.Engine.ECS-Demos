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
    
        private static IECSWorld world = PGDGameContext.GetWorld();
        private static IQuery query = PGDGameContext.GetWorld().Query().WithAllComponents(IComponents.Get<PGDTransform, PGDPosition, Start, Target>()).WithoutAnyTags(ITags.Get<Disabled>());
        private IEntity selectedEntity;

        public static void ClearNeighborRelations()
        {
            // // 清除旧关系
            // foreach (var entity in query.Entities)
            // {
            //     if (entity.HasRelationType<NeighborOf>())
            //     {
            //         // 先复制到列表，避免在遍历时修改集合
            //         var relations = entity.GetRelations<NeighborOf>();
            //         var relationList = new System.Collections.Generic.List<NeighborOf>();
            //         
            //         foreach (var rel in relations)
            //         {
            //             relationList.Add(rel);
            //         }
            //         
            //         // 现在可以安全地删除
            //         foreach (var rel in relationList)
            //         {
            //             entity.RemoveRelation<NeighborOf>(rel.Target);
            //         }
            //     }
            // }
            
            // 清除旧关系实现2
            int n = 0;
            var queryRelation = world.QueryRelation<NeighborOf>();
            queryRelation.ForEachEntity((ref NeighborOf neighborOf, IEntity entity) =>
            {
                entity.RemoveRelation<NeighborOf>(neighborOf.Target);
                n++;
            });
            
            Debug.Log($"清除了 {n} 个实体的邻居关系。残留 { world.QueryRelation<NeighborOf>().EntityCount } 个关系");
            
            ClearAllColors();
        }

        public static void ClearAllColors()
        {
            var cq = world.GetCommandQueue();
            // var queryColors = query.WithAllComponents(IComponents.Get<CubeColor>()); 
            foreach (var entity in query.Entities)
            {
                if (entity.HasComponent<CubeColor>())
                {
                    cq.RemoveComponent<CubeColor>(entity.Id);

                }
            }
            cq.Apply();
        }
        
        // 建立邻居关系
        public void BuildNeighborRelations()
        {
            // var query = world.Query().WithAllComponents(IComponents.Get<PGDTransform, PGDPosition, Start, Target>()).WithoutAnyTags(ITags.Get<Disabled>());
            // var query = GetQuery();

            ClearNeighborRelations();
            // ClearAllColors();

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
            
            // TODO: ColorUpdateSystem应该初始禁用，在这里开始启用
            world.RegisterSystem(new ColorUpdateSystem());
        }


        
        //  // 选中一个实体，高亮它和邻居
        // public void SelectEntity(IEntity entity)
        // {
        //     if (entity == null || entity.IsDeleted()) return;
            
        //     // 清除之前的高亮
        //     ClearHighlight();
            
        //     selectedEntity = entity;
            
        //     // 高亮选中的实体
        //     entity.AddComponent(new CubeColor(selectedColor));
        //     // if (!entity.HasComponent<CubeColor>())
        //     // {
        //     //     entity.AddComponent(new CubeColor(selectedColor));
        //     // }
        //     // else
        //     // {
        //     //     ref var color = ref entity.GetComponent<CubeColor>();
        //     //     color.Value = new Vector4(selectedColor.r, selectedColor.g, selectedColor.b, selectedColor.a);
        //     // }
            
        //     // 高亮所有邻居
        //     if (entity.HasRelationType<NeighborOf>())
        //     {
        //         var neighbors = entity.GetRelations<NeighborOf>();
        //         foreach (var neighbor in neighbors)
        //         {
        //             if (!neighbor.Target.IsDeleted())
        //             {
        //                 if (!neighbor.Target.HasComponent<CubeColor>())
        //                 {
        //                     neighbor.Target.AddComponent(new CubeColor(neighborColor));
        //                 }
        //                 else
        //                 {
        //                     ref var nColor = ref neighbor.Target.GetComponent<CubeColor>();
        //                     nColor.Value = new Vector4(neighborColor.r, neighborColor.g, neighborColor.b, neighborColor.a);
        //                 }
        //             }
        //         }
                
        //         Debug.Log($"高亮了 {neighbors.Count} 个邻居");
        //     }
        // }
        
        // // 清除高亮
        // public void ClearHighlight()
        // {
        //     var query = world.Query().WithAllComponents(IComponents.Get<CubeColor>());
        //     foreach (var entity in query.Entities)
        //     {
        //         ref var color = ref entity.GetComponent<CubeColor>();
        //         color.Value = new Vector4(normalColor.r, normalColor.g, normalColor.b, normalColor.a);
        //     }
        // }
        
        // // 鼠标点击选择实体
        // void Update()
        // {
        //     if (Input.GetMouseButtonDown(0))
        //     {
        //         Ray ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);
        //         if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        //         {
        //             Debug.Log("调用了");
        //             // 这里需要根据hit位置找到最近的实体
        //             IEntity clickedEntity = FindNearestEntity(hit.point);
        //             if (clickedEntity != null)
        //             {
        //                 SelectEntity(clickedEntity);
        //             }
        //         }
        //     }
            
        //     // 按空格清除高亮
        //     if (Input.GetKeyDown(KeyCode.Space))
        //     {
        //         ClearHighlight();
        //     }
        // }
        
        // private IEntity FindNearestEntity(Vector3 position)
        // {
        //     var query = world.Query().WithAllComponents(IComponents.Get<PGDTransform, PGDPosition, Start, Target>()).WithoutAnyTags(ITags.Get<Disabled>());
        //     IEntity nearest = new IEntity();
        //     float minDistance = float.MaxValue;
            
        //     foreach (var entity in query.Entities)
        //     {
        //         ref var trans = ref entity.GetComponent<PGDTransform>();
        //         Vector3 entityPos = new Vector3(trans.mtr.Translation.X, trans.mtr.Translation.Y, trans.mtr.Translation.Z);
                
        //         float distance = Vector3.Distance(position, entityPos);
        //         if (distance < minDistance && distance < 2f)  // 2米范围内
        //         {
        //             minDistance = distance;
        //             nearest = entity;
        //         }
        //     }
            
        //     return nearest;
        // }

    }
}