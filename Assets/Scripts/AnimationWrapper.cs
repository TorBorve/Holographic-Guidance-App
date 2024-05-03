using System.Xml.Serialization;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Tutorials
{
    [Serializable]
    public class AnimationWrapper
    {
        /// <summary>
        /// The animation entity's name that will be visible to users
        /// </summary>
        [XmlAttribute]
        public string Name { get; set; }

        /// <summary>
        /// The description that will be visible to users
        /// </summary>
        [XmlAttribute]
        public string Description { get; set; }

        /// <summary>
        /// Speed at which the animation runs, relative to the speed at which it was recorded. 
        /// The user can change this value between 0 and 2 (standing still up to double speed)
        /// </summary>
        [XmlAttribute]
        public double Speed { get; set; } = 1;


        /// <summary>
        /// Determines where the animation should start relative to the complete duration of the animation
        /// (StartFrame == 0.5 means the animation begins from the middle.)
        /// </summary>
        [XmlAttribute]
        public double StartFrame { get; set; } = 0;

        /// <summary>
        /// Determines where the animation should end relative to the complete duration of the animation
        /// (StartFrame == 0.5 means the animation ends from the middle.)
        /// </summary>
        [XmlAttribute]
        public double EndFrame { get; set; } = 1;

        /// <summary>
        /// Determines whether the left hand should be visible
        /// </summary>
        [XmlAttribute]
        public bool LeftHand { get; set; } = true;

        /// <summary>
        /// Determines whether the right hand should be visible
        /// </summary>
        [XmlAttribute]
        public bool RightHand { get; set; } = true;

        /// <summary>
        /// localPosition.x value of the animation specific point of reference. 
        /// </summary>
        [XmlAttribute]
        public double position_x { get; set; } = 0;
        /// <summary>
        /// localPosition.y value of the animation specific point of reference. 
        /// </summary>
        [XmlAttribute]
        public double position_y { get; set; } = 0;
        /// <summary>
        /// localPosition.z value of the animation specific point of reference. 
        /// </summary>
        [XmlAttribute]
        public double position_z { get; set; } = 0;
        /// <summary>
        /// localRotation.x value of the animation specific point of reference. 
        /// </summary>
        [XmlAttribute]
        public double rotation_x { get; set; } = 0;
        /// <summary>
        /// localRotation.y value of the animation specific point of reference. 
        /// </summary>
        [XmlAttribute]
        public double rotation_y { get; set; } = 0;
        /// <summary>
        /// localRotation.z value of the animation specific point of reference. 
        /// </summary>
        [XmlAttribute]
        public double rotation_z { get; set; } = 0;
        /// <summary>
        /// localRotation.w value of the animation specific point of reference. 
        /// </summary>
        [XmlAttribute]
        public double rotation_w { get; set; } = 1;


        /// <summary>
        /// contains the actual animation object. This will never be directly stored locally or online. The corresponding data will be stored in a blob file separately
        /// </summary>
        [XmlIgnore]
        InputAnimation animation;

        /// <summary>
        /// if animation has not been filled yet, the public Animation access field will first either load the animation from a local blob file, or if that fails will download it from the cloud.
        /// </summary>
        [XmlIgnore]
        public InputAnimation Animation
        {
            get
            {
                if (animation == null)
                {
                    //try
                    //{
                        animation = FileHandler.LoadAnimationFromLocalBlobFile(Name);
                    //}
                    //catch (Exception e)
                    //{
                    //    Debug.Log(e.Message + "Animation " + Name + " could not be loaded.");
                    //    return null;
                    //}
                    if (animation == null)
                    {
                        Debug.LogError("Animation is null");
                    }
                }

                return animation;
            }

            set => animation = value;
        }
    }

}
