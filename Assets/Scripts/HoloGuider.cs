using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;

namespace Tutorials
{
    public class HoloGuider
    {
        enum State
        {
            Starting,
            Preview,
            Following,
            Finished,
            Manual
        }
        private RecordingData _recordingData = null;
        private State _state = State.Starting;

        private IMixedRealityHand _leftHand = null;
        private IMixedRealityHand _rightHand = null;

        private float _guidanceSpeed = 0f;
        private static readonly float MAX_GUIDANCE_SPEED = 1f;
        private static readonly float MAX_ACCELERATION = 1/1.5f;
        private static readonly float MIN_ACCELERATION = -1/1f;

        private float _estimatedTime = 0f;

        private LogToUIText _debugger = null;

        public void SetDebugger(LogToUIText debugger)
        {
            _debugger = debugger;
        }

        public void StopGuiding()
        {
            _state = State.Finished;
            _guidanceSpeed = 0f;
        }
        public void StartGuiding()
        {
            _state = State.Starting;
            _guidanceSpeed = 0f;
            _estimatedTime = _recordingData.GetStartTime();
        }

        public void setRecordingData(InputAnimation inputAnimation)
        {
            _recordingData = RecordingData.FromInputAnimation(inputAnimation);
            Debug.Log("New recording data set");
            _debugger?.logInfo("New recording data set");
        }

        private void UpdateTrackedHandState()
        {
            _leftHand = HandJointUtils.FindHand(Handedness.Left);
            _rightHand = HandJointUtils.FindHand(Handedness.Right);
        }

        public float UpdateTime()
        {
            UpdateTrackedHandState();
            float visalizeTime = _estimatedTime;
            switch (_state)
            {
                case State.Starting:
                    if (_recordingData != null)
                    {
                        visalizeTime = _recordingData.GetStartTime();
                        Debug.Log("Changing to Preview State");
                        _debugger.logInfo("Changing to Preview State");
                        _state = State.Preview;
                    }
                    break;
                case State.Preview:
                    visalizeTime += Time.deltaTime;
                    if (visalizeTime >= _recordingData.GetEndTime())
                    {
                        visalizeTime = _recordingData.GetStartTime();
                        Debug.Log("Changing to Following State");
                        _debugger.logInfo("Changing to Following State");
                        _state = State.Following;
                    }
                    _estimatedTime = visalizeTime;
                    break;
                case State.Following:
                    visalizeTime = FollowingUpdate();
                    if (_estimatedTime >= _recordingData.GetEndTime())
                    {
                        _debugger.logInfo("Finished Guiding");
                        _state = State.Starting;
                    }
                    break;
                case State.Finished:
                    break;
                case State.Manual:
                    break;
                default:
                    Debug.LogError("Invalid state in HoloGuider");
                    break;
            }
            return visalizeTime;
        }

        private float FollowingUpdate()
        {
            { 
                DataPoint currentDataPoint = _recordingData.InterpolateDataAtTime(_estimatedTime);
                if (_rightHand != null && _rightHand.TryGetJoint(TrackedHandJoint.Palm, out MixedRealityPose palmPose))
                {
                    _debugger.logInfo("t palm: " + (1000 * palmPose.Position).ToString());
                }
                if (_rightHand != null && _rightHand.TryGetJoint(TrackedHandJoint.ThumbTip, out MixedRealityPose thumbPose))
                {
                    _debugger.logInfo("t thumb: " + (1000 * thumbPose.Position).ToString());
                }
                if (_rightHand != null && _rightHand.TryGetJoint(TrackedHandJoint.PinkyTip, out MixedRealityPose pinkyPose))
                {
                    _debugger.logInfo("t Pinky: " + (1000 * pinkyPose.Position).ToString());
                }
            }
            {
                DataPoint currentDataPoint = _recordingData.InterpolateDataAtTime(_estimatedTime);
                if (currentDataPoint.rightHand.TryGetValue(TrackedHandJoint.Palm, out MixedRealityPose palmPose))
                {
                    _debugger.logInfo("rec palm: " + (1000 * palmPose.Position).ToString());
                }
                if (currentDataPoint.rightHand.TryGetValue(TrackedHandJoint.ThumbTip, out MixedRealityPose thumbPose))
                {
                    _debugger.logInfo("rec thumb: " + (1000 * thumbPose.Position).ToString());
                }
                if (currentDataPoint.rightHand.TryGetValue(TrackedHandJoint.PinkyTip, out MixedRealityPose pinkyPose))
                {
                    _debugger.logInfo("rec Pinky: " + (1000 * pinkyPose.Position).ToString());
                }
            }
            float updatedEstimatedTime = _estimatedTime;
            if (_rightHand != null && _rightHand.TryGetJoint(TrackedHandJoint.Wrist, out MixedRealityPose jointPose))
            {
                Vector3 trackedPos = jointPose.Position;
                DataPoint currentDataPoint = _recordingData.InterpolateDataAtTime(_estimatedTime);
                Vector3 recordedPos = currentDataPoint.rightHand[TrackedHandJoint.Wrist].Position;
                float dist = (trackedPos - recordedPos).magnitude;
                _debugger.logInfo("Track: " + trackedPos.ToString() + "Rec: " + recordedPos.ToString() + " dist: " + dist.ToString());
                
                // Sigmoid for smoth acceleration, alpha = 1 => max_acceleration, alpha = 0 => max_decelleration
                float alpha = (float)(1 - 1 / (1 + Math.Exp(-20 * (dist - 0.30f))));
                float acceleration = MIN_ACCELERATION + alpha * (MAX_ACCELERATION - MIN_ACCELERATION);
                //_debugger.logInfo("Acc: " + acceleration.ToString());
                _guidanceSpeed += Time.deltaTime * acceleration;
                _guidanceSpeed = Math.Max(0, Math.Min(_guidanceSpeed, MAX_GUIDANCE_SPEED));
                updatedEstimatedTime += _guidanceSpeed * Time.deltaTime;
            } else
            {
                _debugger.logWarn("Hand not tracked");
                _guidanceSpeed = Math.Max(_guidanceSpeed + Time.deltaTime * MIN_ACCELERATION, 0f);
                updatedEstimatedTime += _guidanceSpeed * Time.deltaTime;
            }
            _estimatedTime = updatedEstimatedTime;

            float secAhead = 0.3f;
            float visualizeTime = updatedEstimatedTime + secAhead * _guidanceSpeed;
            visualizeTime = Math.Min(visualizeTime, _recordingData.GetEndTime());
            return visualizeTime;
        }

    }
}