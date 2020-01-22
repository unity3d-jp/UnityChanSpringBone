using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Animations.SpringBones.Jobs
{
    [ExecuteInEditMode]
    [AddComponentMenu("Animation Rigging/SpringBone/SpringBone Renderer")]
    [HelpURL("https://to.do/url")]
    public class SpringBoneRenderer : MonoBehaviour
    {
    #if UNITY_EDITOR
        public enum BoneShape
        {
            Line,
            Pyramid,
            Box
        };

        public struct TransformPair
        {
            public Transform first;
            public Transform second;
        };

        public BoneShape boneShape = BoneShape.Pyramid;

        public bool drawBones = true;
        public bool drawTripods = false;

        [Range(0.01f, 5.0f)]
        public float boneSize = 1.0f;

        [Range(0.01f, 5.0f)]
        public float tripodSize = 1.0f;

        public Color boneColor = new Color(0f, 0f, 1f, 0.5f);

        [SerializeField]
        private Transform[] m_Transforms;

        private TransformPair[] m_Bones;

        private Transform[] m_Tips;

        public Transform[] transforms
        {
            get { return m_Transforms; }
            set
            {
                m_Transforms = value;
                ExtractBones();
            }
        }

        public TransformPair[] bones { get => m_Bones; }

        public Transform[] tips { get => m_Tips; }

        public delegate void OnAddBoneRendererCallback(SpringBoneRenderer renderer);
        public delegate void OnRemoveBoneRendererCallback(SpringBoneRenderer renderer);

        public static OnAddBoneRendererCallback onAddSpringBoneRenderer;
        public static OnRemoveBoneRendererCallback onRemoveSpringBoneRenderer;

        void OnEnable()
        {
            ExtractBones();
            onAddSpringBoneRenderer?.Invoke(this);
        }

        void OnDisable()
        {
            onRemoveSpringBoneRenderer?.Invoke(this);
        }

        void Reset()
        {
            ClearBones();
        }

        void ClearBones()
        {
            m_Bones = null;
            m_Tips = null;
        }

        public void ExtractBones()
        {
            if (m_Transforms == null || m_Transforms.Length == 0)
            {
                ClearBones();
                return;
            }

            var transformsHashSet = new HashSet<Transform>(m_Transforms);

            var bonesList = new List<TransformPair>(m_Transforms.Length);
            var tipsList = new List<Transform>(m_Transforms.Length);

            for (int i = 0; i < m_Transforms.Length; ++i)
            {
                bool hasValidChildren = false;

                var t  = m_Transforms[i];
                if (t == null)
                    continue;

                if (t.childCount > 0)
                {

                    for (var k = 0; k < t.childCount; ++k)
                    {
                        var childTransform = t.GetChild(k);

                        if (transformsHashSet.Contains(childTransform))
                        {
                            bonesList.Add(new TransformPair() { first = t, second = childTransform });
                            hasValidChildren = true;
                        }
                    }
                }

                if (!hasValidChildren)
                {
                    tipsList.Add(t);
                }
            }

            m_Bones = bonesList.ToArray();
            m_Tips = tipsList.ToArray();
        }
    #endif // UNITY_EDITOR
    }
}

//namespace Unity.Animations.SpringBones.Jobs
//{
//    [ExecuteInEditMode]
//    public class SpringBoneVisualizer : MonoBehaviour
//    {
//        public bool showInRuntime;
//        public bool showBoneNames;
//        public Color color;
//
//        private List<SpringBone> m_bones;
//        private Vector3[] m_boneLinePoints;
//
//        private void OnEnable()
//        {
//            m_bones = new List<SpringBone>();
//            GetComponentsInChildren(m_bones);
//        }
//
//        // Update is called once per frame
//        void OnDrawGizmos()
//        {
//            if (showInRuntime)
//            {
//                if (Application.isEditor)
//                {
//                    GetComponentsInChildren(m_bones);
//                }
//
//                Gizmos.color = color;
//
//                if (m_boneLinePoints == null || m_boneLinePoints.Length != m_bones.Count * 2)
//                {
//                    m_boneLinePoints = new Vector3[m_bones.Count * 2];
//                }
//
//                for (int boneIndex = 0, pointIndex = 0; boneIndex < m_bones.Count; boneIndex++, pointIndex +=2)
//                {
//                    var bone = m_bones[boneIndex];
//                    var origin = bone.transform.position;
////                    var pivotForward = -bone.GetPivotTransform().right;
////                    m_boneLinePoints[pointIndex] = origin;
////                    m_boneLinePoints[pointIndex + 1] = origin + pivotForward;
//                    m_boneLinePoints[pointIndex] = origin;
//                    m_boneLinePoints[pointIndex + 1] = bone.ComputeChildPosition();
//                    Gizmos.DrawLine(m_boneLinePoints[pointIndex], m_boneLinePoints[pointIndex + 1]);
//                    Gizmos.DrawWireSphere(m_boneLinePoints[pointIndex+1], bone.radius);
//                }
//
//                #if UNITY_EDITOR
//                if (showBoneNames)
//                {
//                    foreach (var bone in m_bones)
//                    {
//                        UnityEditor.Handles.Label(bone.transform.position, bone.name, "BoldLabel");
//                    }
//                }
//                #endif
//            }
//        }
//    }
//}