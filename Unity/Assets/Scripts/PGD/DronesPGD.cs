using System;

namespace PGD.Drones
{
    public class DronesPGD
    {
        public IECSWorld world;
        public readonly int maxDroneCount = 256 * 1024;

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
            allQuery = world.Query();
            commandQueue = world.GetCommandQueue();
            commandQueue.EnableReuse = true;
        }

        public void Initialize()
        {
            // var batch = world.GetBatchBuilder();
            // batch.AddComponent(new PGDPosition())
            // .AddComponent(new PGDTransform())
            // .AddComponent(new Start())
            // .AddComponent(new Target())
            // .AddTag<Disabled>();
            
            for (int n = 0; n < maxDroneCount; n++)
            {
                // batch.CreateEntity();
                world.CreateEntity(new PGDPosition(), new Start(), new Target(), new PGDTransform(), ITags.Get<Disabled>());
            }
            

            // batch.ReleaseBatch();
        }

        public void SetEntityCount(int count)
        {
            int i = 0;
            foreach (var entity in allQuery.Entities)
            {
                if (i++ < count)
                {
                    commandQueue.RemoveTag<Disabled>(entity.Id);
                }
                else
                {
                    commandQueue.AddTag<Disabled>(entity.Id);
                    commandQueue.AddComponent<PGDPosition>(entity.Id);
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
    }
}