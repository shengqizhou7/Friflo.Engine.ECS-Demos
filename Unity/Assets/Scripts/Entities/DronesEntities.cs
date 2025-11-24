using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Entities.Drones
{
    public class DronesEntities
    {
        private readonly World world;
        private readonly EntityManager entityManager;
        public readonly int maxDroneCount = 255 * 1024;

        private readonly EntityQuery allDronesQuery;
        private readonly EntityQuery activeDronesQuery;

        private float duration = 500;

        private ComponentTypeHandle<DroneAnimation> animationTypeHandle;
        private ComponentTypeHandle<DroneStart> startTypeHandle;
        private ComponentTypeHandle<DroneTarget> targetTypeHandle;
        private ComponentTypeHandle<LocalTransform> transformTypeHandle;

        public DronesEntities()
        {
            world = World.DefaultGameObjectInjectionWorld;
            entityManager = world.EntityManager;
            
            allDronesQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<DroneTag>());
            activeDronesQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<DroneTag>(),
                ComponentType.Exclude<DroneDisabled>());
            
            UpdateTypeHandles();

            var system = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<DroneUpdateSystem>();
            if (system != null)
            {
                system.Enabled = true;
            }
        }

        private void UpdateTypeHandles()
        {
            animationTypeHandle = entityManager.GetComponentTypeHandle<DroneAnimation>(false);
            startTypeHandle = entityManager.GetComponentTypeHandle<DroneStart>(true);
            targetTypeHandle = entityManager.GetComponentTypeHandle<DroneTarget>(true);
            transformTypeHandle = entityManager.GetComponentTypeHandle<LocalTransform>(false);
        }

        public void Initialize()
        {
            EntityArchetype droneArchetype = entityManager.CreateArchetype(
                typeof(DroneTag),
                typeof(DroneStart),
                typeof(DroneTarget),
                typeof(LocalTransform),
                typeof(DroneAnimation),
                typeof(DronePosition),
                typeof(DroneDisabled));
            
            NativeArray<Entity> entities = new NativeArray<Entity>(maxDroneCount, Allocator.Temp);
            entityManager.CreateEntity(droneArchetype, entities);

            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                float3 zeroPos = float3.zero;
                
                entityManager.SetComponentData(entity, new DronePosition { Value = zeroPos });
                entityManager.SetComponentData(entity, new DroneStart { Value = zeroPos });
                entityManager.SetComponentData(entity, new DroneTarget { Value = zeroPos });
                entityManager.SetComponentData(entity, new DroneAnimation { Duration = 500, Elapsed = 0 });
                entityManager.SetComponentData(entity, LocalTransform.FromPosition(zeroPos));
            }
            
            entities.Dispose();
        }

        public void SetEntityCount(int count)
        {
            
            NativeArray<Entity> entities = allDronesQuery.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                bool shouldBeEnabled = i < count;
                bool isDisabled  = entityManager.HasComponent<DroneDisabled>(entity);

                if (shouldBeEnabled && isDisabled )
                {
                    entityManager.RemoveComponent<DroneDisabled>(entity);
                }
                else if (!shouldBeEnabled && !isDisabled )
                {
                    entityManager.AddComponent<DroneDisabled>(entity);
                }
            }
            
            entities.Dispose();
        }

        public int GetActiveCount()
        {
            return activeDronesQuery.CalculateEntityCount();
        }

        public NativeArray<Entity> GetActiveEntities(Allocator allocator)
        {
            return activeDronesQuery.ToEntityArray(allocator);
        }

        public void SetTargetPlane(float duration, float distance)
        { 
            this.duration = duration;
            
            NativeArray<Entity> entities = activeDronesQuery.ToEntityArray(Allocator.Temp);
            int entityCount = entities.Length;
            
            DroneShapeSystem.SetTargetPlane(entityManager, entityCount, duration);
            
            entities.Dispose();
        }

        public void SetTargetCube(float duration, float distance)
        {
            this.duration = duration;
            
            NativeArray<Entity> entities = activeDronesQuery.ToEntityArray(Allocator.Temp);
            int entityCount = entities.Length;
            
            DroneShapeSystem.SetTargetCube(entityManager, entityCount, duration);
            
            entities.Dispose();
        }
        
        internal void SetTargetRings(float duration, int ringCnt, float distance, int ringLayers)
        {
            this.duration = duration;

            if (ringLayers <= 1)
            {
                DroneShapeSystem.SetTargetRing(entityManager, GetActiveCount(), duration);
            }
            else
            {
                DroneShapeSystem.SetTargetRings(entityManager, GetActiveCount(), duration, ringLayers);
            }
        }

        public void UpdateTransforms(float deltaTime)
        {
            UpdateTypeHandles();
        }
    }

}