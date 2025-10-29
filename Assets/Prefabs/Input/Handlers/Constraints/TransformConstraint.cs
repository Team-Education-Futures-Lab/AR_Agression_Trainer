// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.UI
{
    /// <summary>
    /// Base class for all constraints
    /// </summary>
    public abstract class TransformConstraint : MonoBehaviour
    {
        #region Properties

        [SerializeField]
        [EnumFlags]
        [Tooltip("What type of manipulation this constraint applies to. Defaults to One Handed and Two Handed.")]
        private ManipulationHandFlags handType = ManipulationHandFlags.OneHanded | ManipulationHandFlags.TwoHanded;

        /// <summary>
        /// Whether this constraint applies to one hand manipulation, two hand manipulation or both
        /// </summary>
        public ManipulationHandFlags HandType
        {
            get => handType;
            set => handType = value;
        }

        [SerializeField]
        [EnumFlags]
        [Tooltip("What type of manipulation this constraint applies to. Defaults to Near and Far.")]
        private ManipulationProximityFlags proximityType = ManipulationProximityFlags.Near | ManipulationProximityFlags.Far;

        /// <summary>
        /// Whether this constraint applies to near manipulation, far manipulation or both
        /// </summary>
        public ManipulationProximityFlags ProximityType
        {
            get => proximityType;
            set => proximityType = value;
        }

        [SerializeField]
        [Tooltip("Execution order priority of this constraint. Lower numbers will be executed before higher numbers.")]
        private int executionOrder = 0;

        /// <summary>
        /// Execution order priority of this constraint. Lower numbers will be executed before higher numbers.
        /// </summary>
        public int ExecutionPriority
        {
            get => executionOrder;
            set
            {
                executionOrder = value;

                // Notify all ConstraintManagers to re-sort these priorities.
                foreach (var mgr in gameObject.GetComponents<ConstraintManager>())
                {
                    mgr.RefreshPriorities();
                }
            }
        }

        protected MixedRealityTransform worldPoseOnManipulationStart;

        public abstract TransformFlags ConstraintType { get; }

        #endregion Properties

        #region Public Methods

        /// <summary>
        /// Intended to be called on manipulation started
        /// </summary>
        public virtual void Initialize(MixedRealityTransform worldPose)
        {
            worldPoseOnManipulationStart = worldPose;
        }

        /// <summary>
        /// Abstract method for applying constraints to transforms during manipulation
        /// </summary>
        public abstract void ApplyConstraint(ref MixedRealityTransform transform);


        #endregion Public Methods

        #region MonoBeaviour
        protected void OnEnable()
        {
            var managers = gameObject.GetComponents<ConstraintManager>();
            foreach (var manager in managers)
            {
                manager.AutoRegisterConstraint(this);
            }
        }

        protected void OnDisable()
        {
            var managers = gameObject.GetComponents<ConstraintManager>();
            foreach (var manager in managers)
            {
                manager.AutoUnregisterConstraint(this);
            }
        }

        #endregion

        #region Deprecated

        /// <summary>
        /// Intended to be called on manipulation started
        /// </summary>
        [System.Obsolete("Deprecated: Pass MixedRealityTransform instead of MixedRealityPose.")]
        public virtual void Initialize(MixedRealityPose worldPose)
        {
            Initialize(new MixedRealityTransform(worldPose.Position, worldPose.Rotation, Vector3.one));
        }

        /// <summary>	
        /// Transform that we intend to apply constraints to	
        /// </summary>	
        [System.Obsolete("Deprecated: Get component transform instead.")]
        public Transform TargetTransform { get; set; } = null;

        #endregion
    }

        // Utility helper for ConstraintManager
    public static class ConstraintUtils
    {
        /// <summary>
        /// Inserts a constraint into the list while maintaining the order defined by the comparer.
        /// </summary>
        public static void AddWithPriority<T>(ref List<T> list, T item, IComparer<T> comparer)
        {
            int insertIndex = list.BinarySearch(item, comparer);
            if (insertIndex < 0)
            {
                insertIndex = ~insertIndex;
            }
            list.Insert(insertIndex, item);
        }
    }

    public class ConstraintExecOrderComparer : IComparer<TransformConstraint>
    {
        public int Compare(TransformConstraint x, TransformConstraint y)
        {
            if (x == null || y == null)
            {
                return 0;
            }
            return x.ExecutionPriority.CompareTo(y.ExecutionPriority);
        }
    }
}