using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections.Generic;

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

        private Dictionary<TrackedHandJoint, MixedRealityPose> _lefHandPoses = null;
        private Dictionary<TrackedHandJoint, MixedRealityPose> _rightHandPoses = null;

        private Transform _leftRecordingHand = null;
        private Transform _rightRecordingHand = null;

        private float _guidanceSpeed = 0f;
        private static readonly float MAX_GUIDANCE_SPEED = 0.75f;
        private static readonly float MAX_ACCELERATION = 1 / 1f;
        private static readonly float MIN_ACCELERATION = -1 / 0.3f;

        private float _estimatedTime = 0f;

        private LogToUIText _debugger = null;

        public void SetDebugger(LogToUIText debugger)
        {
            _debugger = debugger;
        }

        public void SetRecordingHand(Handedness handedness, Transform hand)
        {
            Debug.Log("Set recording hand in HoloGuider: " + handedness.ToString());
            if (handedness == Handedness.Left)
            {
                _leftRecordingHand = hand;
            } else
            {
                _rightRecordingHand = hand;
            }
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
            try {
                _recordingData = RecordingData.FromInputAnimation(inputAnimation);
                Debug.Log("New recording data set");
                _debugger?.logInfo("New recording data set");
            } catch (Exception e) {
                _debugger?.logError(e.Message);
                Debug.LogError(e.Message);
            }
        }

        private void UpdateTrackedHandState()
        {
            _leftHand = HandJointUtils.FindHand(Handedness.Left);
            _rightHand = HandJointUtils.FindHand(Handedness.Right);
            if (_leftHand != null)
            {
                _lefHandPoses = TransformHand(_leftHand, _leftRecordingHand);
            }
            if (_rightHand != null)
            {
                _rightHandPoses = TransformHand(_rightHand, _rightRecordingHand);
            }
            _leftHand = null;
            _rightHand = null;
        }

        private Dictionary<TrackedHandJoint, MixedRealityPose> TransformHand(IMixedRealityHand hand, Transform transform)
        {
            var handPoses = new Dictionary<TrackedHandJoint, MixedRealityPose>();

            Vector3 Pos_world_to_QR = transform.position;
            Quaternion Rot_world_to_QR = transform.rotation;

            for (int i = 1; i < Enum.GetNames(typeof(TrackedHandJoint)).Length; ++i)
            {
                if (hand.TryGetJoint((TrackedHandJoint)i, out var jointPose))
                {
                    Vector3 globPos = jointPose.Position;
                    Quaternion globalRot = jointPose.Rotation;

                    Vector3 relativeToQRPos = Quaternion.Inverse(Rot_world_to_QR) * (globPos - Pos_world_to_QR);
                    Quaternion relativeToQRRot = Quaternion.Inverse(Rot_world_to_QR) * globalRot;
                    MixedRealityPose relativeTOQRPose = new MixedRealityPose(relativeToQRPos, relativeToQRRot);
                    handPoses[(TrackedHandJoint)i] = relativeTOQRPose;
                }
            }
            return handPoses;
        }

        public float UpdateTime(float time)
        {
            if (_recordingData == null) {
                string msg = "Recording data is null";
                Debug.LogError(msg);
                _debugger.logError(msg);
                return time;
            }
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
                if (_rightHandPoses != null && _rightHandPoses.TryGetValue(TrackedHandJoint.Wrist, out MixedRealityPose jointPose))
                {
                    Vector3 trackedPos = jointPose.Position;
                    DataPoint currentDataPoint = _recordingData.InterpolateDataAtTime(_estimatedTime);
                    FingerAndWristData currentFingerData = new FingerAndWristData(currentDataPoint.rightHand);
                    FingerAndWristData recFingerData = new FingerAndWristData(_rightHandPoses);
                    float dist = FingerAndWristData.Distance(currentFingerData, recFingerData);

                    float max_hand_precision_tolerance = 0.1f;
                    float max_hand_precision_speed = 0.05f;
                    float min_hand_precision_tolerance = 0.2f;
                    float min_hand_precision_speed = 1f;

                    float rec_speed = currentDataPoint.rightSpeed;
                    float alpha_speed = Math.Min(Math.Max((rec_speed - max_hand_precision_speed) / (min_hand_precision_speed - max_hand_precision_speed), 0f), 1f);
                    float required_precision = alpha_speed * (min_hand_precision_tolerance - max_hand_precision_tolerance) + max_hand_precision_tolerance;

                    if (_estimatedTime < 1f)
                    {
                        required_precision = min_hand_precision_tolerance;
                    }
                    //required_precision = 0.15f;

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

                    string msg = "Dist: " + dist.ToString() + ", \nreq_dist: " + required_precision.ToString() + ", Accel: " + acceleration.ToString();
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

                float secAhead = 0.3f;
                float visualizeTime = updatedEstimatedTime + secAhead * _guidanceSpeed;
                visualizeTime = Math.Min(visualizeTime, _recordingData.GetEndTime());
                return visualizeTime;
            }

        }
    }
}