using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Tutorials
{
    public class Follower : MonoBehaviour
    {
        [SerializeField]
        private Recorder _recorder;
        [SerializeField]
        private Player _player;

        [SerializeField]
        private LogToUIText _debugger;

        private bool followMode = false;
        private float timeKeeper = 0f;

        [SerializeField]
        private GameObject debugger;

        public Follower() {}
        public void SetFollower(Player player, Recorder recorder)
        {
        }
        public void playAnimation()
        {
            _debugger.logInfo("Play Animation Called");
            if (followMode)
            {
                if (timeKeeper > 20f)
                {
                    timeKeeper = 0f;
                }
            }
            else
            {
                followMode = true;
                _player.StartFollowAnimation();
                // _recorder.StartRecording();
            }
        }
        public void Update()
        {
            //TextMesh txt = debugger.GetComponent<TextMesh>();
            // InputRecordingBuffer.Keyframe key;
            _debugger.logInfo("Time: " + timeKeeper.ToString());
            if (followMode)
            {
                IDictionary<TrackedHandJoint, TransformData> joints = _player.GetAnimationByTime(timeKeeper);
                Vector3 recordedHandPosition = joints[TrackedHandJoint.Wrist].GetPosition();

                Vector3 trackedHandPosition = GetCurrentWristPosition(Handedness.Right);
                float distance = (trackedHandPosition - recordedHandPosition).magnitude;
                _debugger.logInfo("Tracked pos: " + trackedHandPosition.ToString() + " Rec pos: " + recordedHandPosition.ToString() + " Dist: " + distance.ToString());
                if (distance < 0.05f)
                {
                    timeKeeper += 0.03f;
                }
                _player.setLocalTime(timeKeeper);
            } else
            {
                //txt.text = "Follow mode off";
            }

            
        }

        private Vector3 GetCurrentWristPosition(Handedness handedness)
        {
            var hand = HandJointUtils.FindHand(handedness);
            if (hand == null)
            {
                _debugger.logError("Hand is null");
                return new Vector3(0f, 0f, 0f);
            }
            if (hand.TrackingState != TrackingState.Tracked)
            {
                _debugger.logError("Hand not tracked");
                return new Vector3(0f, 0f, 0f);
            }
            Transform jointTransform;
            if (hand.TryGetJoint(TrackedHandJoint.Wrist, out MixedRealityPose jointPose))
            {
                return jointPose.Position;
            } else
            {
                _debugger.logError("Could not get wrist joint");
            }
            return new Vector3(0f, 0f, 0f);
        }
    }
}