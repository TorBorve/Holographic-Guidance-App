# HoloGuidance

### Abstract
The advent of Augmented Reality (AR), Virtual Reality (VR), and Extended Reality (XR) technologies has significantly impacted various sectors, prompting substantial investments in related software and hardware. Among the promising applications of XR is its potential to enhance training in fields such as medicine and industrial engineering. Traditional training methods often fall short in effectively conveying complex physical tasks, leading to inefficiencies and reliance on experienced instructors. This paper introduces an innovative XR-based training application that builds on the foundation of 4D Holographic Tutorials, offering adaptive holographic guidance tailored to the user’s progress. Our contributions include the development of an XR application with adaptive guidance for intricate tasks, an intuitive user interface evaluated through a user study with 20 participants, and a proof-of-concept for sequence matching methods to assess trainee progress. The study reveals a positive user experience, with the application achieving an average rating of 7.15 out of 10 and a "Good" mark on the System Usability Scale (SUS). Future work aims to expand on the application of Dynamic Time Warping (DTW) for improved task assessment, incorporate critical pose snapshots, and enhance task explanation and item tracking within the app.


Research semester project as part of [3D Vision lecture at ETH Zurich](https://cvg.ethz.ch/lectures/3D-vision/),   
Spring 2024  

Tor Børve Rasmussen  
Faye Zhang  
Styrmir Óli Þorsteinsson  
Haoran Xu  

### Impressions
Video Playlist: https://www.youtube.com/playlist?list=PL9ljg0RPLMToyLbPvqVUjwlWdnGND72LN

<a href="https://www.youtube.com/playlist?list=PL9ljg0RPLMToyLbPvqVUjwlWdnGND72LN">
<p align="">
    <img src="https://user-images.githubusercontent.com/2311941/173391780-a71b4cdb-2424-43b2-94c7-88adbf715bb3.png" alt="design" width="500"/>
</p>
</a>

<p align="">
    <img src="https://user-images.githubusercontent.com/2311941/173393117-ee452d04-7036-42cb-8e42-98f8d6ed74b4.png" alt="design" width="500"/>
</p>
<p align="">
    <img src="https://user-images.githubusercontent.com/2311941/173393182-5babfe24-5ff0-4018-bd91-25ccce06f77a.png" alt="design" width="500"/>
</p>

<p align="">
    <img src="https://user-images.githubusercontent.com/2311941/173393240-0f3cc725-edbe-49e5-93ab-379b24478c3a.png" alt="design" width="500"/>
</p>



### Getting started
See [INSTALL file](INSTALL.md)

### Used packages
 * MixedReality/com.microsoft.mixedreality.toolkit.examples-2.7.3.tgz
 * MixedReality/com.microsoft.mixedreality.toolkit.extensions-2.7.3.tgz
 * MixedReality/com.microsoft.mixedreality.toolkit.foundation-2.7.3.tgz
 * MixedReality/com.microsoft.mixedreality.toolkit.standardassets-2.7.3.tgz
 * MixedReality/com.microsoft.mixedreality.toolkit.testutilities-2.7.3.tgz
 * MixedReality/com.microsoft.mixedreality.toolkit.tools-2.7.3.tgz
 * All Mixed Reality packages above are available through the Mixed Reality Feature Tool [Link](https://docs.microsoft.com/en-gb/windows/mixed-reality/develop/unity/welcome-to-mr-feature-tool)
 * For a more extensive list of packages used, see [here](Packages/manifest.json)

### Relevant Scripts Written
 * [Recording](Assets/Scripts/Recorder.cs)
 * [Buffer for Recording](Assets/Scripts/InputRecordingBuffer.cs)
 * [Playback](Assets/Scripts/Player.cs)
 * [Hand Playback](Assets/Scripts/RiggedHandVisualizer.cs)
 * [User Input](Assets/Scripts/InputHandler.cs)
 * [Object Management](Assets/Scripts/ObjectManager.cs)
 * [Point Cloud Management](Assets/Scripts/researchmode)
 * [Location Invariance QR Code](Assets/Scripts/QRCode)
 * [Animation Management](Assets/Scripts/AnimationList.cs)
 * [Animation Management](Assets/Scripts/AnimationWrapper.cs)
 * [Animation Management](Assets/Scripts/InputAnimation.cs)
 * [Countdown](Assets/Scripts/CountdownHandler.cs)
 * [User Interface](Assets/Scripts/FeaturesPanelVisuals.cs)
 * [User Interface](Assets/Scripts/ScrollablePagination.cs)
 * [User Interface](Assets/Scripts/SceneManagerPanel.cs)
 * [User Interface](Assets/Scripts/StepNameHandler.cs)
 * [User Interface](Assets/Scripts/UpdateObjectName.cs)

