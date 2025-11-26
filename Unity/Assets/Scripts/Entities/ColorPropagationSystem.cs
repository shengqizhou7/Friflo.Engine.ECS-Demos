using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Entities.Drones
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class ColorPropagationSystem : SystemBase
    {
        private EntityQuery _droneQuery;
        private EntityQuery _colorUpdateQuery;
        private EntityManager _entityManager;

        private Color[] targetColors =
        {
            new(0.2f, 0.8f, 0.9f, 1f),
            new(1.0f, 0.4f, 0.4f, 1f),
            new(0.2f, 0.6f, 1.0f, 1f),
            new(0.3f, 0.9f, 0.4f, 1f),
            new(1.0f, 0.85f, 0.2f, 1f),
        };

        private int _currentColorIndex;
        private Color CurrentTargetColor => targetColors[_currentColorIndex];
        private Color? _propagatingColor;

        protected override void OnCreate()
        {
            _entityManager = EntityManager;
            _droneQuery = GetEntityQuery(
                ComponentType.ReadOnly<DroneTag>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.Exclude<DroneDisabled>());
            _colorUpdateQuery = GetEntityQuery(
                ComponentType.ReadOnly<ColorToBeUpdated>(),
                ComponentType.ReadOnly<NeighborOf>());
            RequireForUpdate(_droneQuery);
        }

        protected override void OnUpdate()
        {
            // Debug.Log("DOTS ColorPropagationSystem running");
            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                Ray ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);
                Entity clickedEntity = FindNearestEntityOnRay(ray);
                
                if (clickedEntity != Entity.Null && _entityManager.Exists(clickedEntity))
                {
                    _propagatingColor = CurrentTargetColor;
                    SelectEntity(clickedEntity);
                    _currentColorIndex = (_currentColorIndex + 1) % targetColors.Length;
                }
            }

            if (_propagatingColor.HasValue)
            {
                if (_colorUpdateQuery.CalculateEntityCount() > 0)
                {
                    UpdateNeighborColors(_propagatingColor.Value);
                    Debug.Log($"DOTS剩余{_colorUpdateQuery.CalculateEntityCount()}个方块待染色");
                }
                else
                {
                    _propagatingColor = null;
                }
            }
        }
        
        private void SelectEntity(Entity entity)
        {
            if (!_propagatingColor.HasValue)
            {
                Debug.LogError("propagatingColor 未设置！");
                return;
            }
            
            Color colorToUse = _propagatingColor.Value;

            if (_entityManager.HasComponent<CubeColor>(entity) &&
                _entityManager.GetComponentData<CubeColor>(entity).Value.Equals(colorToUse))
            {
                return;
            }
            
            ApplyCubeColor(entity, colorToUse);
            
            var neighbors = _entityManager.GetBuffer<NeighborOf>(entity);
            NativeArray<Entity> neighborEntities = new NativeArray<Entity>(neighbors.Length, Allocator.Temp);
            for (int i = 0; i < neighbors.Length; i++)
            {
                neighborEntities[i] = neighbors[i].Target;
            }

            NativeArray<Entity> drones = _droneQuery.ToEntityArray(Allocator.Temp);
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var drone in drones)
            {
                if (!_entityManager.HasComponent<ColorToBeUpdated>(drone))
                {
                    ecb.AddComponent<ColorToBeUpdated>(drone);
                }
            }
            ecb.RemoveComponent<ColorToBeUpdated>(entity);

            drones.Dispose();
            ecb.Playback(_entityManager);
            ecb.Dispose();

            RecordHitCounts(entity);
            foreach (var neighborEntity in neighborEntities)
            {
                RecordHitCounts(neighborEntity);
            }
            neighborEntities.Dispose();
        }

        private void ApplyCubeColor(Entity entity, Color color)
        {
            var cubeColor = new CubeColor(color);
            if (_entityManager.HasComponent<CubeColor>(entity))
            {
                _entityManager.SetComponentData(entity, cubeColor);
            }
            else
            {
                _entityManager.AddComponentData(entity, cubeColor);
            }
        }

        private void UpdateNeighborColors(Color color)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var entity in _colorUpdateQuery.ToEntityArray(Allocator.Temp))
            {
                var neighbors = _entityManager.GetBuffer<NeighborOf>(entity);
                foreach (var neighbor in neighbors)
                {
                    if (_entityManager.HasComponent<CubeColor>(neighbor.Target) &&  _entityManager.GetComponentData<CubeColor>(neighbor.Target).Value.Equals(color))
                    {
                        ecb.AddComponent(entity, new CubeColor(color));
                        ecb.RemoveComponent<ColorToBeUpdated>(entity);
                        break;
                    }
                }
            }
            ecb.Playback(_entityManager);
            ecb.Dispose();
        }

        private Entity FindNearestEntityOnRay(Ray ray)
        {
            using var entities = _droneQuery.ToEntityArray(Allocator.Temp);
            Entity nearest = Entity.Null;
            float bestScore = float.MaxValue;

            foreach (var entity in entities)
            {
                var transform = _entityManager.GetComponentData<LocalTransform>(entity);
                Vector3 entityPos = new Vector3(transform.Position.x, transform.Position.y, transform.Position.z);

                Vector3 toPoint = entityPos - ray.origin;
                float alongRay = Vector3.Dot(toPoint, ray.direction.normalized);
                if (alongRay < 0f)
                {
                    continue;
                }

                Vector3 closestPoint = ray.origin + ray.direction.normalized * alongRay;
                float perpDistance = Vector3.Distance(closestPoint, entityPos);

                float dynamicRadius = 0.6f + alongRay * 0.01f;
                if (perpDistance > dynamicRadius)
                {
                    continue;
                }

                float score = perpDistance + alongRay * 1f;
                if (score < bestScore)
                {
                    bestScore = score;
                    nearest = entity;
                }
            }
            return nearest;
        }
        
        private void RecordHitCounts(Entity entity)
        {
            if (!_entityManager.Exists(entity))
            {
                return;
            }

            if (_entityManager.HasComponent<HitCounter>(entity))
            {
                var counter = _entityManager.GetComponentData<HitCounter>(entity);
                counter.counts += 1;
                _entityManager.SetComponentData(entity, counter);
            }
            else
            {
                _entityManager.AddComponentData(entity, new HitCounter { counts = 1 });
            }
        }
    }
}

