using System;
using UnityEngine;

namespace Unity.Animations.SpringBones.Jobs
{
    public struct Intersection
    {
        public Vector3 origin;
        public Vector3 upVector;
        public float radius;
    }
    
    public enum ColliderType : int
    {
        Sphere,
        Panel,
        Capsule
    }
    
    [Serializable]
    public struct SpringColliderComponent
    {
        public int index;
        public int layer;
        public ColliderType type;
        public float radius;
        public float width;
        public float height;
    }
}