using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace Tutorials
{
    public class Follower : MonoBehaviour
    {
        private Recorder _recorder;
        private Player _player;

        private bool followMode = false;
        private float timeKeeper = 0f;

        public Follower() { }
        public void SetFollower(Player player, Recorder recorder)
        {
            _recorder = recorder;
            _player = player;
        }
        public void playAnimation()
        {
            followMode = true;
            _player.StartFollowAnimation();
            _recorder.StartRecording();
        }
        public void Update()
        {
            if (timeKeeper == 0)
            {
                timeKeeper = 2.0f;
            }
            InputRecordingBuffer.Keyframe key;
            if (followMode)
            {
                key = _recorder.GetLatestKeyframe();
                IDictionary<TrackedHandJoint, TransformData>  joints = _player.GetAnimationByTime(timeKeeper);
                Vector3 john = joints[TrackedHandJoint.Wrist].GetPosition();
                Vector3 vect = key.RightJoints[TrackedHandJoint.Wrist].Position;

                float distance = Vector3.Distance(john, vect);
                Debug.Log(distance);
                if (timeKeeper == 0)
                {
                    timeKeeper = 5.0f;
                }
                if (distance < 0.20)
                {
                    timeKeeper += 0.1f;
                }
                Debug.Log(timeKeeper);
            }
            
            _player.setLocalTime(timeKeeper);
        }

    }
}