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
    
    public class NeighborManager
    {
        public float neighborDistance = 5f;
        public Color normalColor = Color.white;
        public Color selectedColor = Color.yellow;
        public Color neighborColor = Color.cyan;
    
        private IECSWorld world = PGDGameContext.GetWorld();
        private IEntity selectedEntity;
    
        // 建立邻居关系
        public void BuildNeighborRelations()
        {
            var query = world.Query().WithAllComponents(IComponents.Get<PGDTransform>());

            // 清除旧关系
            foreach (var entity in query.Entities)
            {
                if (entity.HasRelationType<NeighborOf>())
                {
                    var relations = entity.GetRelations<NeighborOf>();
                    foreach (var rel in relations)
                    {
                        entity.RemoveRelation<NeighborOf, IEntity>(rel.Target);
                    }
                }
            }

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
                        entityB.AddRelation(new NeighborOf { Target = entityA, Distance = distance }, out _);
                    }
                }
            }

            Debug.Log($"共 {query.Entities.Count} 个实体建立了邻居关系");
        }
    }
}