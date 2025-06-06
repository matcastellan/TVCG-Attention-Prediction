# TVCG-Attention-Prediction

## Files
There are two distinct datasets: one for the experiments conducted in Section II (sec_ii_experiments) and another for those in Section IV (sec_iv_experiments).

The dataset for Section II includes a single experiment involving 16 users. In contrast, the dataset for Section IV covers 9 experiments, categorized by scene complexity (simple, normal, motion) and scene type (cemetery, office, space).

Additionally, there are two corresponding project folders: sec_ii_project for Section II and sec_iv_project for Section IV. Each project file is specifically designed to work with its respective dataset.

## Processing Section II data

The raw data for each experiment is organized in a slightly different format. Section II data needs to be processed in order to be used with the Gaze Visualizer. Section IV data does not need to be processed.

For Section II data: 
- install python dependencies:
  - numpy
  - pandas
  - pytorch
- run python/process_section_ii_data.py. 
- Press the “Load VR” button. 
- Navigate to the sec_ii_experiments folder (which should contain subfolders 1-16) and select it.
- Press the Run button
The code should reorganize the data into a single CSV (full_data.csv). This file contains a lot of extra information that is not needed for the Gaze Visualizer.

To speed up import time, run python/reduce_sec_ii_data.py on full_data.csv, and only include the following categories:
1. AR_VR
2. User
3. Time
4. ConditionID
5. CalibDir
6. GazeDir
7. GazeOrg
8. MainCameraPos
9. MainCameraFor
10. MainCameraUp
11. MainCameraRight
12. A_ecce
13. A_depth
14. A_size
15. A_gabor
16. A_worldPos
17. B_ecce
18. B_depth
19. B_size
20. B_gabor
21. B_worldPos

Click “Run”. The file will be saved as “full_data_reduced.csv” in the same folder. This file can be imported into the Gaze Visualizer in sec_ii_project.

## Working with Section II Data
To run the Gaze Visualizer for the Section II experiments:
- Download sec_ii_project.
- Open the project using Unity Hub with Unity 2021.3.18f1. Hit Continue when prompted.
- Import the following packages if they are not automatically imported:
  - ProBuilder 5.0.7
  - TextMeshPro 3.0.6 (Choose to Import TextMeshPro Essentials when prompted)
  - Unity UI 1.0.0
- Open each scene in the Scenes folder, and add it to the Build Settings (File-Build Settings-Add Open Scenes).
  - If you see the TMP Importer window appear, click "Import TMP Essentials"
- Open Scenes/Menu
- Run in editor using the Play Button
- Optional: You can adjust the FOV slider on the camera to make the spheres easier to see, or you can enable Unity Mock HMD under XR Plugin Management (once installed) in Project Settings. Alternatively, if you connect an HTC Vive Pro (or Pro Eye), install Steam VR, and add it to the project, you can set up the camera such that you can view what the user was looking at directly through the headset.

You will need to import the full_data_reduced.csv file created in the "Processing Section II Data" section, and import it using the Import CSV button, which will take a long time. To speed up the loading process next time, click "Save Data File" after import, and in future runs, just click "Load Data File".

Once data has been loaded/imported, click “Explore Trials”. The scene will change. Basic navigation controls are listed at the bottom of the window. Here you can play through trials, and classify them or auto-classify them.
To classify:
- Press Alpha1 for “A”
- Press Alpha2 for “B”
- Press Alpha3 to clear classification for that trial.
- Press Numpad Enter to trigger autoclassification, which can be paused with the “K” key.

When you are finished, hit Esc to go back to the main menu. You can then export the data by pressing “Export Answer CSV”, and choosing a directory, which will save the data as “answers.csv”.

## Working with Section IV Data
Section IV data has a separate project and scenes for the Section IV experiments to facilitate reconstruction, given that certain models must be externally downloaded and re-added to the project due to copyright constraints. 

To run the Gaze Visualizer for the Section IV experiments:
- Unzip sec_iv_project.
- Open the project using Unity Hub with Unity 2021.3.4f1. Hit Continue when prompted.
- Import the following packages if they are not automatically imported:
  - ProBuilder 5.0.7
  - TextMeshPro 3.0.6 (Choose to Import TextMeshPro Essentials when prompted)
  - Unity UI 1.0.0
- For the stimuli, import the following free packages from the Asset Store:
  - Balloon Ghost(with Breakable parts)(V1.0) (Free) https://assetstore.unity.com/packages/3d/animations/balloon-ghost-with-breakable-parts-209499
  - Customize Monitor Interface (V1.0) (Free) https://assetstore.unity.com/packages/p/customize-monitor-interface-123558
  - Fix any broken shaders by setting them to Particles-Standard Unlit
- Download the following asset: Space Rocket 9 Shuttle ($2.50) Rocket Icon Model - TurboSquid 1645562
  - Import the .fbx file into Space Shuttle - source
  - Import the BaseColor.png file into Space Shuttle - textures
  - Using a text editor, edit the .meta file for the .fbx file to have the following GUID: 19925ee165407a84e93347315ea73501
- Open each scene in the Scenes folder, and add it to the Build Settings (File-Build Settings-Add Open Scenes).
  - If you see the TMP Importer window appear, click "Import TMP Essentials"
- Open the relevant scene.
- Select the ReplayController GameObject, go to its "ReplayTester" component, and change the field "User Folder Path" to the location of the relevant dataset in sec_iv_experiments (e.g., C:/....motion/cemetery)
- Run in editor using the Play Button
- Optional: You can adjust the FOV slider on the camera to make the spheres easier to see, or you can enable Unity Mock HMD under XR Plugin Management (once installed) in Project Settings. Alternatively, if you connect an HTC Vive Pro (or Pro Eye), install Steam VR, and add it to the project, you can set up the camera such that you can view what the user was looking at directly through the headset.

Basic navigation controls are listed on the screen. 
To classify:
- Press Alpha1 for “A”
- Press Alpha2 for “B”
- Press Alpha3 to clear classification for that trial.
- Press Numpad Enter to trigger autoclassification, which can be paused with the “K” key.
- To export the data once classification is finished, press F5 and select a directory.




