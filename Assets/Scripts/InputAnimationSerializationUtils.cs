using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

namespace Tutorials
{

    /// <summary>
    /// Functions for serializing input animation data to and from binary files.
    /// </summary>
    public static class InputAnimationSerializationUtils
    {
        private static readonly int jointCount = Enum.GetNames(typeof(TrackedHandJoint)).Length;

        public const string Extension = "txt";

        const long Magic = 0x6a8faf6e0f9e42c6;

        public const int VersionMajor = 1;
        public const int VersionMinor = 1;

        /// <summary>
        /// Generate a file name for export.
        /// </summary>
        public static string GetOutputFilename(string baseName = "InputAnimation", bool appendTimestamp = true)
        {
            string filename;
            if (appendTimestamp)
            {
                filename = String.Format("{0}-{1}.{2}", baseName, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"), InputAnimationSerializationUtils.Extension);
            }
            else
            {
                filename = baseName;
            }
            return filename;
        }

        /// <summary>
        /// Write a header for the input animation file format into the stream.
        /// </summary>
        public static void WriteHeader(StreamWriter writer)
        {
            writer.WriteLine(Magic);
        }

        /// <summary>
        /// Write a header for the input animation file format into the stream.
        /// </summary>
        public static void ReadHeader(StreamReader reader)
        {
            long fileMagic;
            if (!long.TryParse(reader.ReadLine(), out fileMagic) || fileMagic != Magic)
            {
                throw new Exception("File is not an input animation file");
            }
        }

        /// <summary>
        /// Serialize an animation curve with tangents as binary data.
        /// </summary>
        public static void WriteFloatCurve(StreamWriter writer, AnimationCurve curve, float startTime)
        {
            writer.Write((int)curve.preWrapMode);
            writer.Write((int)curve.postWrapMode);

            writer.Write(curve.length);
            for (int i = 0; i < curve.length; ++i)
            {
                var keyframe = curve.keys[i];
                writer.Write(keyframe.time - startTime);
                writer.Write(keyframe.value);
                writer.Write(keyframe.inTangent);
                writer.Write(keyframe.outTangent);
                writer.Write(keyframe.inWeight);
                writer.Write(keyframe.outWeight);
                writer.Write((int)keyframe.weightedMode);
            }
        }

        /// <summary>
        /// Deserialize an animation curve with tangents from binary data.
        /// </summary>
        public static void ReadFloatCurve(StreamReader reader, AnimationCurve curve)
        {
            curve.preWrapMode = (WrapMode)int.Parse(reader.ReadLine());
            curve.postWrapMode = (WrapMode)int.Parse(reader.ReadLine());

            int keyframeCount = int.Parse(reader.ReadLine());

            Keyframe[] keys = new Keyframe[keyframeCount];
            for (int i = 0; i < keyframeCount; ++i)
            {
                keys[i].time = float.Parse(reader.ReadLine());
                keys[i].value = float.Parse(reader.ReadLine());
                keys[i].inTangent = float.Parse(reader.ReadLine());
                keys[i].outTangent = float.Parse(reader.ReadLine());
                keys[i].inWeight = float.Parse(reader.ReadLine());
                keys[i].outWeight = float.Parse(reader.ReadLine());
                keys[i].weightedMode = (WeightedMode)int.Parse(reader.ReadLine());
            }

            curve.keys = keys;
        }

        /// <summary>
        /// Serialize an animation curve as binary data, ignoring tangents.
        /// </summary>
        public static void WriteBoolCurve(StreamWriter writer, AnimationCurve curve)
        {
            writer.WriteLine((int)curve.preWrapMode);
            writer.WriteLine((int)curve.postWrapMode);

            writer.WriteLine(curve.length);
            for (int i = 0; i < curve.length; ++i)
            {
                var keyframe = curve.keys[i];
                writer.WriteLine(keyframe.time);
                writer.WriteLine(keyframe.value);
            }
        }

        /// <summary>
        /// Deserialize an animation curve from binary data, ignoring tangents.
        /// </summary>
        public static void ReadBoolCurve(StreamReader reader, AnimationCurve curve)
        {
            curve.preWrapMode = (WrapMode)int.Parse(reader.ReadLine());
            curve.postWrapMode = (WrapMode)int.Parse(reader.ReadLine());

            int keyframeCount = int.Parse(reader.ReadLine());

            Keyframe[] keys = new Keyframe[keyframeCount];
            for (int i = 0; i < keyframeCount; ++i)
            {
                keys[i].time = float.Parse(reader.ReadLine());
                keys[i].value = float.Parse(reader.ReadLine());
                keys[i].outWeight = 1.0e6f;
                keys[i].weightedMode = WeightedMode.Both;
            }

            curve.keys = keys;
        }

        /// <summary>
        /// Serialize an animation curve with tangents as binary data. Only encodes keyframe position and time.
        /// </summary>
        public static void WriteFloatCurveSimple(StreamWriter writer, AnimationCurve curve)
        {
            writer.WriteLine((int)curve.preWrapMode);
            writer.WriteLine((int)curve.postWrapMode);

            writer.WriteLine(curve.length);
            for (int i = 0; i < curve.length; ++i)
            {
                var keyframe = curve.keys[i];
                writer.WriteLine(keyframe.time);
                writer.WriteLine(keyframe.value);
            }
        }

        /// <summary>
        /// Deserialize an animation curve with tangents from binary data. Only decodes keyframe position and time.
        /// </summary>
        /// <remarks>Only use for curves serialized using WriteFloatCurvesSimple</remarks>
        public static void ReadFloatCurveSimple(StreamReader reader, AnimationCurve curve)
        {
            if (curve == null)
            {
                Debug.LogError("ReadFloatCurveSimple got null curve!");
            }
            curve.preWrapMode = (WrapMode)int.Parse(reader.ReadLine());
            curve.postWrapMode = (WrapMode)int.Parse(reader.ReadLine());
            int keyframeCount = int.Parse(reader.ReadLine());
            Keyframe[] keys = new Keyframe[keyframeCount];
            for (int i = 0; i < keyframeCount; ++i)
            {
                keys[i].time = float.Parse(reader.ReadLine());
                keys[i].value = float.Parse(reader.ReadLine());
                keys[i].weightedMode = WeightedMode.Both;
            }
            curve.keys = keys;
        }

        /// <summary>
        /// Serialize an array of animation curves with tangents as binary data.
        /// </summary>
        public static void WriteFloatCurveArray(StreamWriter writer, AnimationCurve[] curves, float startTime)
        {
            foreach (AnimationCurve curve in curves)
            {
                InputAnimationSerializationUtils.WriteFloatCurve(writer, curve, startTime);
            }
        }

        /// <summary>
        /// Deserialize an array of animation curves with tangents from binary data.
        /// </summary>
        public static void ReadFloatCurveArray(StreamReader reader, AnimationCurve[] curves)
        {
            foreach (AnimationCurve curve in curves)
            {
                InputAnimationSerializationUtils.ReadFloatCurve(reader, curve);
            }
        }

        /// <summary>
        /// Serialize an array of animation curves as binary data, ignoring tangents.
        /// </summary>
        public static void WriteBoolCurveArray(StreamWriter writer, AnimationCurve[] curves)
        {
            foreach (AnimationCurve curve in curves)
            {
                InputAnimationSerializationUtils.WriteBoolCurve(writer, curve);
            }
        }

        /// <summary>
        /// Deserialize an array of animation curves from binary data, ignoring tangents.
        /// </summary>
        public static void ReadBoolCurveArray(StreamReader reader, AnimationCurve[] curves)
        {
            foreach (AnimationCurve curve in curves)
            {
                InputAnimationSerializationUtils.ReadBoolCurve(reader, curve);
            }
        }

        /// <summary>
        /// Serialize a list of markers.
        /// </summary>
        public static void WriteMarkerList(StreamWriter writer, List<InputAnimationMarker> markers)
        {
            writer.WriteLine("MARKER_LIST");
            writer.WriteLine(markers.Count);
            foreach (var marker in markers)
            {
                writer.WriteLine(marker.time);
                writer.WriteLine(marker.name);
            }
        }

        /// <summary>
        /// Deserialize a list of markers.
        /// </summary>
        public static void ReadMarkerList(StreamReader reader, List<InputAnimationMarker> markers)
        {
            markers.Clear();
            var header = reader.ReadLine();
            if (header != "MARKER_LIST")
            {
                Debug.LogError("Excepted MARKER_LIST header, got: " + header);
            }
            int count = int.Parse(reader.ReadLine());
            markers.Capacity = count;
            for (int i = 0; i < count; ++i)
            {
                var marker = new InputAnimationMarker();
                marker.time = float.Parse(reader.ReadLine());
                marker.name = reader.ReadLine();
                markers.Add(marker);
            }
        }
    }
}