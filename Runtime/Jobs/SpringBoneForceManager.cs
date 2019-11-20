
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Jobs;
#if !UNITY_2019_3_OR_NEWER
using UnityEngine.Experimental.Animations;
#endif

namespace Unity.Animations.SpringBones.Jobs
{
    public enum SpringBoneForceType
    {
        Directional,
        Wind,
    }
    
    [Serializable]
    public struct SpringBoneForceComponent
    {
        public Matrix4x4 localToWorldMatrix;
        public SpringBoneForceType type;
        public float weight;
        public float strength;
        public float amplitude;
        public float periodInSecond;
        public float spinPeriodInSecond;
        public float timeInSecond;
        public float peakDistance;
        public Vector3 offsetVector;
    }
    
    public class SpringBoneForceManager : MonoBehaviour
    {
        public int maxForces = 16; //maximum number of forces to update in game
        
        private NativeArray<SpringBoneForceComponent> m_forces; //read only
        private static SpringBoneForceManager s_manager;
        private List<ForceProvider> m_activeForces;

        public NativeArray<SpringBoneForceComponent> Forces => m_forces;

        public int ActiveForceCount => m_activeForces?.Count ?? 0;

        public static SpringBoneForceManager GetManager()
        {
            if (s_manager == null)
            {
                var gameObject = new GameObject("__SpringBoneForceManager__")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                
                s_manager = gameObject.AddComponent<SpringBoneForceManager>();
            }

            return s_manager;
        } 
        
        private void Initialize()
        {
            //NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace;
            
            if (m_forces.IsCreated == false)
            {
                m_forces = new NativeArray<SpringBoneForceComponent>(maxForces, Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
            }
        }

        private void Cleanup()
        {
            m_forces.Dispose();
        }

        void OnEnable()
        {
            Initialize();
        }
        
        void OnDisable()
        {
            Cleanup();
        }

        public void RegisterForce(ForceProvider f)
        {
            if (m_activeForces == null)
            {
                m_activeForces = new List<ForceProvider>(maxForces);
            }

            if (m_activeForces.Count == maxForces)
            {
                throw new OutOfMemoryException("Number of forces in scene exceeded max");
            }
            
            m_activeForces.Add(f);
        }

        public void UnRegisterForce(ForceProvider f)
        {
            m_activeForces.Remove(f);
        }
        
        private void UpdateForceArray()
        {
            if (m_activeForces == null)
            {
                return;
            }
            
            for (var i = 0; i < m_activeForces.Count; ++i)
            {
                if (m_activeForces[i] is ForceVolume)
                {
                    var force = m_activeForces[i] as ForceVolume;
                    var t = force.transform;
                    
                    m_forces[i] = new SpringBoneForceComponent
                    {
                        type = SpringBoneForceType.Directional,
                        strength = force.strength,
                    };
                }
                else if (m_activeForces[i] is WindVolume)
                {
                    var force = m_activeForces[i] as WindVolume;
                    var t = transform;

                    m_forces[i] = new SpringBoneForceComponent
                    {
                        type = SpringBoneForceType.Wind,
                        localToWorldMatrix = Matrix4x4.TRS(t.position, t.rotation, t.localScale),
                        strength = force.strength,
                        weight = force.weight,
                        amplitude = force.amplitude,
                        periodInSecond = force.period,
                        spinPeriodInSecond = force.spinPeriod,
                        timeInSecond = Time.time,
                        peakDistance = force.peakDistance,
                        offsetVector = force.OffsetVector,
                    };
                }
            }
        }

        private void Update()
        {
            UpdateForceArray();
        }
    }
}