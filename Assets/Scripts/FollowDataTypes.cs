
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using System;
using System.Threading.Tasks;

namespace Tutorials
{
    public class DataPoint
    {
        public float timeStamp;
        public bool leftIsTracked;
        public bool rightIsTracked;
        public Dictionary<TrackedHandJoint, MixedRealityPose> leftHand;
        public Dictionary<TrackedHandJoint, MixedRealityPose> rightHand;

        public DataPoint() : this(0f) { }

        public DataPoint(float time)
        {
            timeStamp = time;
            leftIsTracked = false;
            rightIsTracked = false;
            leftHand = new Dictionary<TrackedHandJoint, MixedRealityPose>();
            rightHand = new Dictionary<TrackedHandJoint, MixedRealityPose>();
        }

        public static DataPoint FromKeyframe(InputRecordingBuffer.Keyframe keyframe)
        {
            DataPoint dataPoint = new DataPoint(keyframe.Time);
            dataPoint.leftHand = FromTFDictionary(keyframe.LeftJointsTransformData);
            dataPoint.rightHand = FromTFDictionary(keyframe.RightJointsTransformData);
            return dataPoint;
        }

        private static Dictionary<TrackedHandJoint, MixedRealityPose> FromTFDictionary(Dictionary<TrackedHandJoint, TransformData> tfDict)
        {
            var MRPoseDict = new Dictionary<TrackedHandJoint, MixedRealityPose>();
            foreach (TrackedHandJoint jointType in tfDict.Keys)
            {
                TransformData tfData = tfDict[jointType];
                UnityEngine.Vector3 position = new UnityEngine.Vector3(tfData.posx, tfData.posy, tfData.posz);
                UnityEngine.Quaternion rotation = new UnityEngine.Quaternion(tfData.rotx, tfData.roty, tfData.rotz, tfData.rotw);
                MixedRealityPose MRPose = new MixedRealityPose(position, rotation);
                MRPoseDict.Add(jointType, MRPose);
            }
            return MRPoseDict;
        }
    }

    public class RecordingData
    {
        private List<DataPoint> dataPoints;

        public RecordingData()
        {
            dataPoints = new List<DataPoint>();
        }

        public RecordingData(List<DataPoint> dataPoints)
        {
            this.dataPoints = new List<DataPoint>(dataPoints);
        }

        public void AddDataPoint(DataPoint dataPoint)
        {
            if (dataPoints.Count > 0 && dataPoint.timeStamp < dataPoints[
                dataPoints.Count - 1].timeStamp)
            {
                throw new Exception("DataPoint is out of date.");
            }
            dataPoints.Add(dataPoint);
        }

        public DataPoint GetDataPointIndex(int i)
        {
            return dataPoints[i];
        }

        public int Count()
        {
            return dataPoints.Count();
        }

        public DataPoint InterpolateDataAtTime(float time)
        {
            throw new Exception("Not Implemented");
        }

        public static async Task<RecordingData> FromInputAnimationAsync(InputAnimation inputAnimation)
        {
            return FromInputAnimation(inputAnimation);
        }

        public static RecordingData FromInputAnimation(InputAnimation inputAnimation)
        {
            Keyframe[] leftTrackedCurve = inputAnimation.handTrackedCurveLeft.keys;
            Keyframe[] rightTrackedCurve = inputAnimation.handTrackedCurveRight.keys;

            int num_data_points = leftTrackedCurve.Count();
            Debug.Assert(leftTrackedCurve.Count() == rightTrackedCurve.Count());
            List<DataPoint> dataPoints = new List<DataPoint>(new DataPoint[num_data_points]);
            for (int i = 0; i < num_data_points; i++)
            {
                DataPoint dataPoint = new DataPoint(leftTrackedCurve[i].time);
                Debug.Assert(leftTrackedCurve[i].time == rightTrackedCurve[i].time);

                dataPoint.leftIsTracked = leftTrackedCurve[i].value > 0.5f;
                dataPoint.rightIsTracked = rightTrackedCurve[i].value > 0.5f;
                dataPoints[i] = dataPoint;
            }

            Dictionary<TrackedHandJoint, InputAnimation.PoseCurves> handJointCurvesLeft = inputAnimation.handJointCurvesLeft;
            Dictionary<TrackedHandJoint, InputAnimation.PoseCurves> handJointCurvesRight = inputAnimation.handJointCurvesRight;
            int num_joints = Enum.GetNames(typeof(TrackedHandJoint)).Length;
            for (int j = 1; j < num_joints; ++j)
            {
                { 
                    if (!handJointCurvesLeft.TryGetValue((TrackedHandJoint)j, out var curves))
                    {
                        throw new Exception("Joint Not present in data: " + (TrackedHandJoint)j);
                    }
                    Keyframe[] PosXCurve = curves.PositionX.keys;
                    Debug.Assert(PosXCurve.Count() == num_data_points);
                    Keyframe[] PosYCurve = curves.PositionY.keys;
                    Debug.Assert(PosYCurve.Count() == num_data_points);
                    Keyframe[] PosZCurve = curves.PositionZ.keys;
                    Debug.Assert(PosZCurve.Count() == num_data_points);
                    Keyframe[] RotXCurve = curves.RotationX.keys;
                    Debug.Assert(RotXCurve.Count() == num_data_points);
                    Keyframe[] RotYCurve = curves.RotationY.keys;
                    Debug.Assert(RotYCurve.Count() == num_data_points);
                    Keyframe[] RotZCurve = curves.RotationZ.keys;
                    Debug.Assert(RotZCurve.Count() == num_data_points);
                    Keyframe[] RotWCurve = curves.RotationW.keys;
                    Debug.Assert(RotWCurve.Count() == num_data_points);

                    for (int i = 0; i < num_data_points; i++)
                    {
                        float time = PosXCurve[i].time;
                        Debug.Assert(dataPoints[i].timeStamp == time);
                        Vector3 jointPos = new Vector3(PosXCurve[i].value, PosYCurve[i].value, PosZCurve[i].value);
                        Quaternion jointRot = new Quaternion(RotXCurve[i].value, RotYCurve[i].value, RotZCurve[i].value, RotWCurve[i].value);
                        MixedRealityPose jointPose = new MixedRealityPose(jointPos, jointRot);
                        dataPoints[i].leftHand.Add((TrackedHandJoint)j, jointPose);
                    }
                }
                {
                    if (!handJointCurvesRight.TryGetValue((TrackedHandJoint)j, out var curves))
                    {
                        throw new Exception("Joint Not present in data: " + (TrackedHandJoint)j);
                    }
                    Keyframe[] PosXCurve = curves.PositionX.keys;
                    Debug.Assert(PosXCurve.Count() == num_data_points);
                    Keyframe[] PosYCurve = curves.PositionY.keys;
                    Debug.Assert(PosYCurve.Count() == num_data_points);
                    Keyframe[] PosZCurve = curves.PositionZ.keys;
                    Debug.Assert(PosZCurve.Count() == num_data_points);
                    Keyframe[] RotXCurve = curves.RotationX.keys;
                    Debug.Assert(RotXCurve.Count() == num_data_points);
                    Keyframe[] RotYCurve = curves.RotationY.keys;
                    Debug.Assert(RotYCurve.Count() == num_data_points);
                    Keyframe[] RotZCurve = curves.RotationZ.keys;
                    Debug.Assert(RotZCurve.Count() == num_data_points);
                    Keyframe[] RotWCurve = curves.RotationW.keys;
                    Debug.Assert(RotWCurve.Count() == num_data_points);

                    for (int i = 0; i < num_data_points; i++)
                    {
                        float time = PosXCurve[i].time;
                        Debug.Assert(dataPoints[i].timeStamp == time);
                        Vector3 jointPos = new Vector3(PosXCurve[i].value, PosYCurve[i].value, PosZCurve[i].value);
                        Quaternion jointRot = new Quaternion(RotXCurve[i].value, RotYCurve[i].value, RotZCurve[i].value, RotWCurve[i].value);
                        MixedRealityPose jointPose = new MixedRealityPose(jointPos, jointRot);
                        dataPoints[i].rightHand.Add((TrackedHandJoint)j, jointPose);
                    }
                }
            }

            RecordingData recordingData = new RecordingData(dataPoints);
            return recordingData;
        }

        public static RecordingData FromRecordingBuffer(InputRecordingBuffer recordingBuffer)
        {
            RecordingData recordingData = new RecordingData();
            foreach (InputRecordingBuffer.Keyframe keyframe in recordingBuffer)
            {
                DataPoint dataPoint = DataPoint.FromKeyframe(keyframe);
                recordingData.AddDataPoint(dataPoint);
            }
            return recordingData;
        }
    }
}