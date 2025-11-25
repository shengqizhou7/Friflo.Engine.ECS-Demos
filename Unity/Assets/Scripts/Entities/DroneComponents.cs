using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Entities.Drones
{
    public struct DroneTag : IComponentData
    {
        
    }
    
    public struct DronePosition : IComponentData
    {
        public float3 Value;
    }
    
    public struct DroneStart : IComponentData
    {
        public float3 Value;
    }
    
    public struct DroneTarget : IComponentData
    {
        public float3 Value;
    }
    
    public struct DroneAnimation : IComponentData
    {
        public float Duration;
        public float Elapsed;
    }
    
    public struct DroneDisabled : IComponentData
    {
    }
    
    public struct CubeColor : IComponentData
    {
        public Vector4 Value;

        public CubeColor(Color color)
        {
            Value = color;
        }
    }

    public struct ColorToBeUpdated : IComponentData
    {
    }

    public struct HitCounter : IComponentData
    {
        public int counts;
    }

    public struct NeighborOf : IBufferElementData
    {
        public Entity Target;
        public float Distance;
    }
}