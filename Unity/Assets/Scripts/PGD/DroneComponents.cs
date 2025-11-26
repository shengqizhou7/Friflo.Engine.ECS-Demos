using UnityEngine;
using Vector3 = System.Numerics.Vector3;

namespace PGD.Drones
{
    public struct Disabled : ITag {}
    
    public struct ColorToBeUpdated : ITag {}

    public struct Start : IComponent
    {
        public Vector3 Value;
    }

    public struct Target : IComponent
    {
        public Vector3 Value;
    }

    public struct CubeColor : IComponent
    {
        public Vector4 Value;

        public CubeColor(Color color)
        {
            Value = color;
        }
    }

    public struct HitCounter : ILookup<int>
    {
        public int counts;
        public int GetLookup() => counts;
    }
    
    public struct NeighborOf : IEntityRelation
    {
        public IEntity Target;
        public float Distance;
        public IEntity GetGenericKey() => Target;
    }
}