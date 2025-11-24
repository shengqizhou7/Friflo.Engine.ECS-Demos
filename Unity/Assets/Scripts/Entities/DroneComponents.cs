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
            // Value = new Vector4(color.r, color.g, color.b, color.a);
            Value = color;
        }
    }
}