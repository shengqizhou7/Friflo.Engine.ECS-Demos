using System;
using UnityEngine;

namespace PGD.Drones
{
    public class DronesPGD
    {
        public IECSWorld world;
        public readonly int maxDroneCount = 255 * 1024;

        private readonly IQuery<Start, PGDPosition> startPositionQuery;
        private readonly IQuery<Target> targetQuery;
        private readonly IQuery<PGDTransform, PGDPosition, Start, Target> activeQuery;
        public readonly IQuery<PGDTransform> transQuery;
        private readonly IQuery allQuery;
        private readonly CommandQueue commandQueue;

        internal DronesPGD()
        {
            world = PGDGameContext.GetWorld();
            startPositionQuery = world.Query<Start, PGDPosition>().WithoutAnyTags(ITags.Get<Disabled>());
            targetQuery = world.Query<Target>().WithoutAnyTags(ITags.Get<Disabled>());
            activeQuery = world.Query<PGDTransform, PGDPosition, Start, Target>().WithoutAnyTags(ITags.Get<Disabled>());
            transQuery = world.Query<PGDTransform>().WithoutAnyTags(ITags.Get<Disabled>());
            allQuery = world.Query<PGDTransform, PGDPosition, Start, Target>(); // TODO: initialize前world中有三个未知实体，所以allQuery = world.Query（）会把这三个实体也查询到，影响SetEntityCount方法
            commandQueue = world.GetCommandQueue();
            commandQueue.EnableReuse = true;
            
            world.RegisterSystem(new DroneUpdateTransformSystem());
        }

        public void Initialize()
        {
            Debug.Log("batch批量创建实体");
            
            var batch = world.GetBatchBuilder(false);
            batch.AddComponent(new PGDPosition())
            .AddComponent(new PGDTransform())
            .AddComponent(new Start())
            .AddComponent(new Target())
            .AddComponent(new CubeColor())
            .AddTag<Disabled>();

            for (int n = 0; n < maxDroneCount; n++)
            {
                batch.CreateEntity();
                // world.CreateEntity(new PGDPosition(), new Start(), new Target(), new PGDTransform(), ITags.Get<Disabled>());
            }
            
            batch.ReleaseBatch();
        }

        public void SetEntityCount(int count)
        {
            CleanupWithinPGD();
            
            int i = 0;
            int n = 0;
            foreach (var entity in allQuery.Entities)
            {
                if (i++ < count)
                {
                    commandQueue.RemoveTag<Disabled>(entity.Id);
                    n++;
                }
                else
                {
                    commandQueue.AddTag<Disabled>(entity.Id);
                    // commandQueue.AddComponent<PGDPosition>(entity.Id);
                }
            }
            commandQueue.Apply();
        }

        public void SetStart(float duration)
        {
            DroneUpdateTransformSystem.Elapsed = 0;
            DroneUpdateTransformSystem.Duration = duration;

            startPositionQuery.ForEachEntity((ref Start start,ref PGDPosition position, IEntity entity) => {
                start.Value = position.vec3;
            });
            
            CleanupWithinPGD(); // TODO: 增减cube数量时，由于会先调用SetEntityCount，CleanUp会调用两次
        }

        public void SetTargetPlane(float duration, float distance)
        {
            SetStart(duration);
            int rowCount = (int)Math.Sqrt(targetQuery.EntityCount);
            float offset = distance * rowCount / 2;

            int x = 0, n = 0;
            targetQuery.ForEachEntity((ref Target target, IEntity entity) => {
                target.Value.X = distance * x - offset;
                target.Value.Y = -distance;
                target.Value.Z = distance * (n++ / rowCount) - offset;
                x = (x + 1) % rowCount;
            });
        }

        public void SetTargetCube(float duration, float distance)
        {
            SetStart(duration);
            int edgeCount = (int)Math.Pow(targetQuery.EntityCount, 1.0f / 3.0f);
            int edgeCount2 = edgeCount * edgeCount;
            float offset = distance * edgeCount / 2;

            int x = 0, n = 0;
            targetQuery.ForEachEntity((ref Target target, IEntity entity) => {
                target.Value.X = distance * x - offset;
                target.Value.Y = distance * ((n / edgeCount2) % edgeCount) - distance - offset;
                target.Value.Z = distance * ((n++ / edgeCount) % edgeCount) - offset;
                x = (x + 1) % edgeCount;
            });
        }
        
        public void SetTargetRings(float duration, int radius, float distance, int count)
        {
            SetStart(duration);
            var     entityCount = targetQuery.EntityCount;
            int     ringCount   = Math.Max(1, entityCount / count);
            float   ringCountF  = ringCount;
            int n = 0;
            targetQuery.ForEachEntity(((ref Target target, IEntity entity) =>
            {
                var pos = (n++ % ringCount) / ringCountF * Math.PI * 2;
                var rot = System.Numerics.Matrix4x4.CreateRotationY((float)pos);
                var y = distance * (n++ / ringCount);
                var v = new System.Numerics.Vector3(radius, y, 0);
                target.Value = System.Numerics.Vector3.Transform(v, rot);
            })); 
        }
        
        // 建立邻居关系
        public void BuildNeighborRelations()
        {
            if (NeighborController.relationBuilt) return;
            
            ClearNeighborRelations();

            // 建立新关系
            foreach (var entityA in activeQuery.Entities)
            {
                foreach (var entityB in activeQuery.Entities)
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

                    if (distance <= NeighborController.neighborDistance)
                    {
                        entityA.AddRelation(new NeighborOf { Target = entityB, Distance = distance }, out _);
                    }
                }
            }
            Debug.Log($"共 {activeQuery.Entities.Count} 个实体建立了邻居关系，当前共有 {world.QueryRelation<NeighborOf>().EntityCount} 条关系");
            
            world.RegisterSystem(new ColorPropagationSystem()); // 玩家点击添加关系按钮时注册颜色变换系统
            NeighborController.relationBuilt = true; // 标志已建立邻居关系
            NeighborController.UpdateHotSpotButtonState();
        }
        
        // 清除邻居关系
        public void ClearNeighborRelations()
        {
            if (!NeighborController.relationBuilt) return;
            
            int n = 0;
            var queryRelation = world.QueryRelation<NeighborOf>();
            queryRelation.ForEachEntity((ref NeighborOf neighborOf, IEntity entity) =>
            {
                entity.RemoveRelation<NeighborOf>(neighborOf.Target);
                n++;
            });
            
            Debug.Log($"清除了 {n} 个实体的邻居关系。残留 { world.QueryRelation<NeighborOf>().EntityCount } 个关系");
            
            NeighborController.relationBuilt = false;
            NeighborController.UpdateHotSpotButtonState();
        }

        // 生成命中热点图
        public void PlotHotspotGraph()
        {
            var cq = world.GetCommandQueue();
            
            foreach (var entity in activeQuery.Entities)
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
                    1 => new Color(0.95f, 0.78f, 0.15f, 1f),
                    >= 2 and <= 3 => new Color(1.0f, 0.42f, 0.1f, 1f),
                    >= 4 => new Color(0.95f, 0.08f, 0.0f, 1f),
                    _ => new Color(0.25f, 0.25f, 0.25f, 1f)
                };

                foreach (var entityId in buckets.Ids)
                {
                    cq.AddComponent(entityId, new CubeColor { Value = color });
                }
            }
            
            cq.Apply();
        }

        // 清除颜色组件和待更新颜色的标签
        public void ClearAllColors()
        {
            var cq = world.GetCommandQueue();
            foreach (var entity in activeQuery.Entities)
            {
                cq.RemoveComponent<CubeColor>(entity.Id);
                cq.RemoveTag<ColorToBeUpdated>(entity.Id);
            }
            
            Debug.Log($"清除了{activeQuery.EntityCount}个实体的CubeColor和ColorToBeUpdated");
            cq.Apply();
        }
        
        // 清除记录命中次数的Lookup组件
        public void ClearHitCounters()
        {
            var cq = world.GetCommandQueue();
            var hitQuery = world.Query<HitCounter>();
            foreach (var entity in hitQuery.Entities)
            {
                cq.RemoveComponent<HitCounter>(entity.Id);
            }
            cq.Apply();
        }

        
        // PGD实现内，切换排布阵列/增删实体时清理资源
        public void CleanupWithinPGD()
        {
            ClearNeighborRelations(); // 清除邻居关系
            ClearAllColors(); // 清理颜色和颜色待更新标签
            ClearHitCounters(); // 清除命中计数器lookup

            // TODO: FindSystem的bool参数含义
            // 删除颜色更新系统
            var colorUpdateSystem = world.FindSystem<ColorPropagationSystem>(false);
            if (colorUpdateSystem != null && colorUpdateSystem.Activated)
            {
                world.RemoveSystem(colorUpdateSystem);
            }
        }

        // 切换实现方式时清理资源
        public void CleanupOnSwitchImpl()
        {
            CleanupWithinPGD();
            world.DestroyEntity(allQuery); // 清理PGD实体
            
            var droneUpdateTransformSystem = world.FindSystem<DroneUpdateTransformSystem>(false);
            if (droneUpdateTransformSystem != null && droneUpdateTransformSystem.Activated)
            {
                world.RemoveSystem(droneUpdateTransformSystem);
            }
        }
    }
}