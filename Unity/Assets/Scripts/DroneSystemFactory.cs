using System.Collections.Generic;
using System;
using UnityEngine;

public static class DroneSystemFactory
{
    private static readonly Dictionary<ImplementationType, Type> _systemMap = new()
    {
        { ImplementationType.Entities, typeof(MoveDronesEntities) },
        { ImplementationType.PGD, typeof(MoveDronesPGD) }
    };
    private static ImplementationType _oldType = default;

    public static IDroneSystem CreateSystem(ImplementationType type, GameObject owner)
    {
        if (!_systemMap.TryGetValue(type, out var componentType))
        {
            throw new ArgumentException($"Unsupported system type {type}");
        }

        if (_systemMap.TryGetValue(_oldType, out var oldComponentType))
        {
            var existingComponent = owner.GetComponent(oldComponentType);
            UnityEngine.Object.Destroy(existingComponent);
        }

        var newComponent = owner.AddComponent(componentType) as IDroneSystem;
        if (newComponent == null)
        {
            throw new InvalidOperationException($"Failed to create system of type {type}");
        }

        _oldType = type;
        return newComponent;
    }
}