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
        private readonly IQuery<PGDTransform, PGDPosition, Start, Target> transPosQuery;
        public readonly IQuery<PGDTransform> transQuery;
        private readonly IQuery allQuery;
        private readonly CommandQueue commandQueue;

        internal DronesPGD()
        {
            world = PGDGameContext.GetWorld();
            startPositionQuery = world.Query<Start, PGDPosition>().WithoutAnyTags(ITags.Get<Disabled>());
            targetQuery = world.Query<Target>().WithoutAnyTags(ITags.Get<Disabled>());
            transPosQuery = world.Query<PGDTransform, PGDPosition, Start, Target>().WithoutAnyTags(ITags.Get<Disabled>());
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
            
            CleanUp();
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
                target.Value.Y = distance * ((n++ / edgeCount2) % edgeCount) - distance - offset;
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

        // 切换实现方法/排布阵列时清理资源
        public void CleanUp()
        {
            NeighborManager.ClearNeighborRelations(); // 清除邻居关系
            NeighborManager.ClearAllColors(); // 清理颜色和颜色待更新标签
            NeighborManager.ClearHitCounters(); // 清除命中计数器lookup

            // TODO: FindSystem的bool参数含义
            // 删除颜色更新系统
            var colorUpdateSystem = world.FindSystem<ColorUpdateSystem>(false);
            if (colorUpdateSystem != null && colorUpdateSystem.Activated)
            {
                world.RemoveSystem(colorUpdateSystem);
            }
        }
    }
}