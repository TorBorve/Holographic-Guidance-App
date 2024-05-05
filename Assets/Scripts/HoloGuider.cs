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
        private static readonly float MAX_ACCELERATION = 1 / 1.5f;
        private static readonly float MIN_ACCELERATION = -1 / 1f;

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

        public float UpdateTime(float time)
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
                    visalizeTime += 1.0f * Time.deltaTime;
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
            //return time;
            return visalizeTime;
        }

        private float FollowingUpdate()
        {
            {
                float updatedEstimatedTime = _estimatedTime;
                if (_rightHand != null && _rightHand.TryGetJoint(TrackedHandJoint.Wrist, out MixedRealityPose jointPose))
                {
                    Vector3 trackedPos = jointPose.Position;
                    DataPoint currentDataPoint = _recordingData.InterpolateDataAtTime(_estimatedTime);
                    FingerAndWristData currentFingerData = new FingerAndWristData(currentDataPoint.rightHand);
                    FingerAndWristData recFingerData = new FingerAndWristData(_rightHand);
                    float dist = FingerAndWristData.Distance(currentFingerData, recFingerData);

                    float max_hand_precision_tolerance = 0.1f;
                    float max_hand_precision_speed = 0.05f;
                    float min_hand_precision_tolerance = 0.4f;
                    float min_hand_precision_speed = 1f;

                    float rec_speed = currentDataPoint.rightSpeed;
                    float alpha_speed = Math.Min(Math.Max((rec_speed - max_hand_precision_speed) / (min_hand_precision_speed - max_hand_precision_speed), 0f), 1f);
                    float required_precision = alpha_speed * (min_hand_precision_tolerance - max_hand_precision_tolerance) + max_hand_precision_tolerance;

                    float acceleration = 0f;
                    if (dist < required_precision) // Then accelerate
                    {
                        float max_accel_dist = 1f / 2 * required_precision;
                        float alpha_acceleration = (dist - required_precision) / (max_accel_dist - required_precision);
                        alpha_acceleration = Math.Min(Math.Max(alpha_acceleration, 0), 1);
                        _debugger.logInfo("Accelerating, alpha: " + alpha_acceleration.ToString());
                        acceleration = MAX_ACCELERATION * alpha_acceleration;
                    } else // Then decelerate
                    {
                        float min_accel_dist = 2 * required_precision;
                        // dist == requride => alpha = 0, dist = min_accel_dist -> alpha = 1
                        float alpha_acceleration = (dist - required_precision) / (min_accel_dist - required_precision);
                        alpha_acceleration = Math.Min(Math.Max(alpha_acceleration, 0), 1);
                        _debugger.logInfo("Decelerating, alpha: " + alpha_acceleration.ToString());
                        acceleration = MIN_ACCELERATION * alpha_acceleration;
                    }

                    string msg = "Dist: " + dist.ToString() + ", req_dist: " + required_precision.ToString() + ", Accel: " + acceleration.ToString();
                    _debugger.logInfo(msg);
                    Debug.Log(msg);


                    // Sigmoid for smoth acceleration, alpha = 1 => max_acceleration, alpha = 0 => max_decelleration
                    //float alpha = (float)(1 - 1 / (1 + Math.Exp(-20 * (dist - 0.30f))));
                    //float acceleration = MIN_ACCELERATION + alpha * (MAX_ACCELERATION - MIN_ACCELERATION);
                    // _debugger.logInfo("Acc: " + acceleration.ToString());
                    _guidanceSpeed += Time.deltaTime * acceleration;
                    _guidanceSpeed = Math.Max(0, Math.Min(_guidanceSpeed, MAX_GUIDANCE_SPEED));
                    updatedEstimatedTime += _guidanceSpeed * Time.deltaTime;
                }
                else
                {
                    _debugger.logWarn("Hand not tracked");
                    _guidanceSpeed = Math.Max(_guidanceSpeed + Time.deltaTime * MIN_ACCELERATION, 0f);
                    updatedEstimatedTime += _guidanceSpeed * Time.deltaTime;
                }
                _estimatedTime = updatedEstimatedTime;

                float secAhead = 0.1f;
                float visualizeTime = updatedEstimatedTime + secAhead * _guidanceSpeed;
                visualizeTime = Math.Min(visualizeTime, _recordingData.GetEndTime());
                return visualizeTime;
            }

        }
    }
}