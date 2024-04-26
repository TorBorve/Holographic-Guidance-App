using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using System.Numerics;
using System;

namespace Tutorials
{
    public class DataPoint
    {
        public float timeStamp { get; set; }
        public Dictionary<TrackedHandJoint, MixedRealityPose> leftHand { get; set; }
        public Dictionary<TrackedHandJoint, MixedRealityPose> rightHand { get; set; }


        public DataPoint(float time)
        {
            timeStamp = time;
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