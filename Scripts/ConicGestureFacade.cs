using System.Collections.Generic;
using ConicUnity;

#if XR_HANDS_1_1_OR_NEWER
using UnityEngine.XR.Hands;
using UnityEngine.UI;
#endif

namespace UnityEngine.XR.Interaction.Toolkit.Samples.Hands
{
    /// <summary>
    /// Behavior that provides events for when an <see cref="XRHand"/> starts and ends a gesture. The gesture is
    /// detected if the index finger is extended and the middle, ring, and little fingers are curled in.
    /// </summary>
    public class ConicGestureFacade : MonoBehaviour
    {
        [SerializeField]
        private ConicGestureObserver conicGestureObserver;
        
        [SerializeField]
        [Tooltip("Which hand to check for the gesture.")]
#if XR_HANDS_1_1_OR_NEWER
        Handedness m_Handedness;
#else
        int m_Handedness;
#endif

        public static event System.Action CutGestureStarted;
        public static event System.Action CutGestureEnded;
        public static event System.Action ShootingGestureStarted;
        public static event System.Action ShootingGestureEnded;

#if XR_HANDS_1_1_OR_NEWER
        XRHandSubsystem m_Subsystem;
        bool m_IsCutting;
        bool m_IsShooting;

        static readonly List<XRHandSubsystem> s_Subsystems = new List<XRHandSubsystem>();
#endif

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void OnEnable()
        {
#if XR_HANDS_1_1_OR_NEWER
            SubsystemManager.GetSubsystems(s_Subsystems);
            if (s_Subsystems.Count == 0)
                return;

            m_Subsystem = s_Subsystems[0];
            m_Subsystem.updatedHands += OnUpdatedHands;
#else
            Debug.LogError("Script requires XR Hands (com.unity.xr.hands) package. Install using Window > Package Manager or click Fix on the related issue in Edit > Project Settings > XR Plug-in Management > Project Validation.", this);
#endif
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void OnDisable()
        {
#if XR_HANDS_1_1_OR_NEWER
            if (m_Subsystem == null)
                return;

            m_Subsystem.updatedHands -= OnUpdatedHands;
            m_Subsystem = null;
#endif
        }

#if XR_HANDS_1_1_OR_NEWER
        void OnUpdatedHands(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags, XRHandSubsystem.UpdateType updateType)
        {
            var wasCutting = m_IsCutting;
            var wasShooting = m_IsShooting;
            switch (m_Handedness)
            {
                case Handedness.Left:
                    if (!HasUpdateSuccessFlag(updateSuccessFlags, XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints))
                        return;

                    var leftHand = subsystem.leftHand;

                    m_IsCutting = IsCuttingGesture(leftHand);
                    m_IsShooting = IsShootingGesture(leftHand);

                    break;

                case Handedness.Right:
                    if (!HasUpdateSuccessFlag(updateSuccessFlags, XRHandSubsystem.UpdateSuccessFlags.RightHandJoints))
                        return;

                    var rightHand = subsystem.rightHand;

                    m_IsCutting = IsCuttingGesture(rightHand);
                    m_IsShooting = IsShootingGesture(rightHand);

                    break;
            }

            if (m_IsCutting && !wasCutting && !m_IsShooting)
                StartCutGesture();
            else if (!m_IsCutting && wasCutting || m_IsShooting)
                EndCutGesture();

            if (m_IsShooting && !wasShooting && !m_IsCutting)
                StartShootingGesture();
            else if (!m_IsShooting && wasShooting || m_IsCutting)
                EndShootingGesture();
        }

        private ConicUnity.ConicGestureObserver.ConicHandInternal ConvertHandToInternalType(ConicHand hand) 
        {
            return new ConicUnity.ConicGestureObserver.ConicHandInternal(hand.indexTipPose, hand.indexDistalPose, hand.indexIntermediatePose, hand.indexProximalPose, hand.indexMetacarpalPose,
                    hand.middleTipPose, hand.middleDistalPose, hand.middleIntermediatePose, hand.middleProximalPose, hand.middleMetacarpalPose,
                    hand.ringTipPose, hand.ringDistalPose, hand.ringIntermediatePose, hand.ringProximalPose, hand.ringMetacarpalPose,
                    hand.littleTipPose, hand.littleDistalPose, hand.littleIntermediatePose, hand.littleProximalPose, hand.littleMetacarpalPose,
                    hand.thumbTipPose, hand.thumbDistalPose, hand.thumbProximalPose, hand.thumbMetacarpalPose,
                    hand.palmPose, hand.wristPose);
        }

        private bool IsDetectingJointPoses(XRHand hand) 
        {
            if (!hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out var _)) 
            {
                return false;
            }

            return true;
        }

        bool IsCuttingGesture(XRHand hand) 
        {
            if (!IsDetectingJointPoses(hand))
            {
                return false;
            }

            ConicHand conicHand = new ConicHand(hand);

            return conicGestureObserver.IsCuttingGestureInternal(ConvertHandToInternalType(conicHand));
        }

        bool IsShootingGesture(XRHand hand) 
        {
            if (!IsDetectingJointPoses(hand))
            {
                return false;
            }

            ConicHand conicHand = new ConicHand(hand);

            return conicGestureObserver.IsShootingGestureInternal(ConvertHandToInternalType(conicHand));
        }

        bool HasUpdateSuccessFlag(XRHandSubsystem.UpdateSuccessFlags successFlags, XRHandSubsystem.UpdateSuccessFlags successFlag)
        {
            return (successFlags & successFlag) == successFlag;
        }

        void StartCutGesture()
        {
            m_IsCutting = true;
            CutGestureStarted?.Invoke();
        }

        void EndCutGesture()
        {
            m_IsCutting = false;
            CutGestureEnded?.Invoke();
        }

        void StartShootingGesture()
        {
            m_IsShooting = true;
            ShootingGestureStarted?.Invoke();
        }

        void EndShootingGesture()
        {
            m_IsShooting = false;
            ShootingGestureEnded?.Invoke();
        }

        struct ConicHand 
        {
            public Pose indexTipPose;
            public Pose indexDistalPose;
            public Pose indexIntermediatePose;
            public Pose indexProximalPose;
            public Pose indexMetacarpalPose;
            public Pose middleTipPose;
            public Pose middleDistalPose;
            public Pose middleIntermediatePose;
            public Pose middleProximalPose;
            public Pose middleMetacarpalPose;
            public Pose ringTipPose;
            public Pose ringDistalPose;
            public Pose ringIntermediatePose;
            public Pose ringProximalPose;
            public Pose ringMetacarpalPose;
            public Pose littleTipPose;
            public Pose littleDistalPose;
            public Pose littleIntermediatePose;
            public Pose littleProximalPose;
            public Pose littleMetacarpalPose;
            public Pose thumbTipPose;
            public Pose thumbDistalPose;
            public Pose thumbProximalPose;
            public Pose thumbMetacarpalPose;
            public Pose palmPose;
            public Pose wristPose;

            public ConicHand(XRHand hand) {
                this.indexTipPose = hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out var indexTipPose) ? indexTipPose : Pose.identity;
                this.indexDistalPose = hand.GetJoint(XRHandJointID.IndexDistal).TryGetPose(out var indexDistalPose) ? indexDistalPose : Pose.identity;
                this.indexIntermediatePose = hand.GetJoint(XRHandJointID.IndexIntermediate).TryGetPose(out var indexIntermediatePose) ? indexIntermediatePose : Pose.identity;
                this.indexProximalPose = hand.GetJoint(XRHandJointID.IndexProximal).TryGetPose(out var indexProximalPose) ? indexProximalPose : Pose.identity;
                this.indexMetacarpalPose = hand.GetJoint(XRHandJointID.IndexMetacarpal).TryGetPose(out var indexMetacarpalPose) ? indexMetacarpalPose : Pose.identity;
                this.middleTipPose = hand.GetJoint(XRHandJointID.MiddleTip).TryGetPose(out var middleTipPose) ? middleTipPose : Pose.identity;
                this.middleDistalPose = hand.GetJoint(XRHandJointID.MiddleDistal).TryGetPose(out var middleDistalPose) ? middleDistalPose : Pose.identity;
                this.middleIntermediatePose = hand.GetJoint(XRHandJointID.MiddleIntermediate).TryGetPose(out var middleIntermediatePose) ? middleIntermediatePose : Pose.identity;
                this.middleProximalPose = hand.GetJoint(XRHandJointID.MiddleProximal).TryGetPose(out var middleProximalPose) ? middleProximalPose : Pose.identity;
                this.middleMetacarpalPose = hand.GetJoint(XRHandJointID.MiddleMetacarpal).TryGetPose(out var middleMetacarpalPose) ? middleMetacarpalPose : Pose.identity;
                this.ringTipPose = hand.GetJoint(XRHandJointID.RingTip).TryGetPose(out var ringTipPose) ? ringTipPose : Pose.identity;
                this.ringDistalPose = hand.GetJoint(XRHandJointID.RingDistal).TryGetPose(out var ringDistalPose) ? ringDistalPose : Pose.identity;
                this.ringIntermediatePose = hand.GetJoint(XRHandJointID.RingIntermediate).TryGetPose(out var ringIntermediatePose) ? ringIntermediatePose : Pose.identity;
                this.ringProximalPose = hand.GetJoint(XRHandJointID.RingProximal).TryGetPose(out var ringProximalPose) ? ringProximalPose : Pose.identity;
                this.ringMetacarpalPose = hand.GetJoint(XRHandJointID.RingMetacarpal).TryGetPose(out var ringMetacarpalPose) ? ringMetacarpalPose : Pose.identity;
                this.littleTipPose = hand.GetJoint(XRHandJointID.LittleTip).TryGetPose(out var littleTipPose) ? littleTipPose : Pose.identity;
                this.littleDistalPose = hand.GetJoint(XRHandJointID.LittleDistal).TryGetPose(out var littleDistalPose) ? littleDistalPose : Pose.identity;
                this.littleIntermediatePose = hand.GetJoint(XRHandJointID.LittleIntermediate).TryGetPose(out var littleIntermediatePose) ? littleIntermediatePose : Pose.identity;
                this.littleProximalPose = hand.GetJoint(XRHandJointID.LittleProximal).TryGetPose(out var littleProximalPose) ? littleProximalPose : Pose.identity;
                this.littleMetacarpalPose = hand.GetJoint(XRHandJointID.LittleMetacarpal).TryGetPose(out var littleMetacarpalPose) ? littleMetacarpalPose : Pose.identity;
                this.thumbTipPose = hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out var thumbTipPose) ? thumbTipPose : Pose.identity;
                this.thumbDistalPose = hand.GetJoint(XRHandJointID.ThumbDistal).TryGetPose(out var thumbDistalPose) ? thumbDistalPose : Pose.identity;
                this.thumbProximalPose = hand.GetJoint(XRHandJointID.ThumbProximal).TryGetPose(out var thumbProximalPose) ? thumbProximalPose : Pose.identity;
                this.thumbMetacarpalPose = hand.GetJoint(XRHandJointID.ThumbMetacarpal).TryGetPose(out var thumbMetacarpalPose) ? thumbMetacarpalPose : Pose.identity;
                this.palmPose = hand.GetJoint(XRHandJointID.Palm).TryGetPose(out var palmPose) ? palmPose : Pose.identity;
                this.wristPose = hand.GetJoint(XRHandJointID.Wrist).TryGetPose(out var wristPose) ? wristPose : Pose.identity;
            } 
        }
#endif
    }
}
