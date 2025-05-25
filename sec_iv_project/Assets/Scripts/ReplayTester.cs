using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.Text;
using System.IO;


public class ReplayTester : MonoBehaviour
{
    public GameObject mainCamera;
    public GameObject stimulus_root_A, stimulus_root_B;//, stimulus_root_A_Large, stimulus_root_B_Large;
    public VisibleRay gazeRay;
    private GameObject gazeRayCone;
    public GameObject gazeRayCone2deg;
    public GameObject gazeRayCone5deg;
    public GameObject gazeRayCone8deg;

    public string filePath;

    public Canvas canvas; // the Canvas which displays information about trial and the controls
    private GameObject control_info;
    private GameObject trial_type_text_go;
    private GameObject trial_checkmark_go;
    private GameObject user_text_go;
    private GameObject user_checkmark_go;
    private GameObject trial_text_go;
    private GameObject choice_text_go;
    private GameObject calibration_text_go;
    private TMP_Text trial_type_text;
    private TMP_Text user_text;
    private TMP_Text trial_text;
    private TMP_Text choice_text;
    private TMP_Text calibration_text;

    private bool calibration = false;

    private float totalTime = 0;
    private float lastTime = 0;

    public int timeScale = 10;
    private int i = 0;

    private List<TrialData> trialdata;

    public const float ar_st_Size_1 = 2f * 2f * 0.0261859215f; //2 * 2 * 0.03492f;//0.08728f;//0.00874f;//0.01303f;
    public const float ar_st_Size_2 = ar_st_Size_1 * 0.052407779f / 0.02618592156f; //0.06993f / 0.03492f;
    public const float vr_st_Size_1 = 2f * 2f * 0.03492f;//0.08728f;//0.00874f;//0.01303f;
    public const float vr_st_Size_2 = vr_st_Size_1 * 0.06993f / 0.03492f;

    private bool vr = true;

    // Variables used for replay controls
    private int trialDataIndex = 0; // the index into the trial data, used to control the position in timeline playback
    private bool playing = false; // whether or not the trial is replaying
    private bool forward = true; // whether playback is going forward or not (rewinding)

    public Datafile df;
    public List<User> current_users;
    public int user_index = 0;
    public int trial_index = 0;

    private bool has_vr_data = false;
    private bool has_ar_data = false;

    public Vector3 ar_camera_position = new Vector3(0f, 0.091f, 0f);
    public Vector3 vr_camera_position = new Vector3(0f, 1.391f, 0f);

    private float[] ga_OrientList = new float[4] { 0f, 45f, 90f, 135f };

    private float trialCount;

    public bool useRayInsteadOfCone = false;
    public enum GazeRayConeVisualAngleSize { TwoDegrees, FiveDegrees, EightDegrees };

    public GazeRayConeVisualAngleSize gazeRayConeVisualAngle;

    private bool cameraHeadPositionTracking = true;

    float angleThreshold; // within 2 degrees, classify as a hit
    float defaultAngleThreshold;

    private bool autoclassify = false; // whether we should autoclassify this trial
    bool autoadvance = false;
    bool autoadvance_user = false;

    public string userFolderPath; // the path where the user's data is

    GameObject trialCameraPlaceholder; // used to set the position/rotation of the camera during the trial for calculations, regardless of the current camera's transform

    string sceneName;

    bool fullAutoadvance = false;
    bool waitaframe = false;

    public GameObject pcA_transform, pcB_transform;

    public void ExportAnswerCSV()
    {
        string path = "";
#if UNITY_EDITOR
        // Choose a save location
        path = EditorUtility.OpenFolderPanel("Choose a save location", "", "");
#endif
        // Create a CSV from the Datafile df
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("AR_VR,User,ConditionID,FirstHitSphere,A_ecce,A_depth,A_size,B_ecce,B_depth,B_size");

        foreach (string trial_type in new HashSet<string> { "VR", "AR" })
        {
            List<User> users = (trial_type == "VR") ? df.vrUsers : df.arUsers;
            if (users.Count == 0) // no AR/VR users
            {
                continue;
            }

            foreach (User user in users)
            {
                foreach (Trial t in user.trials)
                {
                    string sphereChoice = (t.sphereChoice != null) ? t.sphereChoice : "N";
                    sb.AppendLine($"{trial_type},{user.name},{t.condition},{sphereChoice},{t.true_a_ecce},{t.true_a_depth},{t.true_a_size},{t.true_b_ecce},{t.true_b_depth},{t.true_b_size}");
                }
            }

        }
        File.WriteAllText(path + "/answers.csv", sb.ToString());
    }

    void SetUpStimuli(Trial currentTrial, TrialData td)
    {
        // set the appropriate spheres active
        stimulus_root_A.SetActive(true);
        stimulus_root_B.SetActive(true);

        // set position and rotation and scale
        stimulus_root_A.transform.position = td.sphereApos;
        stimulus_root_B.transform.position = td.sphereBpos;

        stimulus_root_A.transform.rotation = td.sphereArot;
        stimulus_root_B.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Vector3.up);

        stimulus_root_A.transform.localScale = new Vector3(td.sphereAscale.x, td.sphereAscale.x, td.sphereAscale.x);
        stimulus_root_B.transform.localScale = new Vector3(td.sphereBscale.x, td.sphereBscale.x, td.sphereBscale.x);

        trialCameraPlaceholder.transform.position = td.cameraPos; // only the position aspect of the transform is used here
        trialCameraPlaceholder.transform.rotation = td.cameraRot;

    }

    // Start is called before the first frame update
    void Start()
    {
        sceneName = SceneManager.GetActiveScene().name;
        
        if (sceneName == "cemeteryTestReplay")
        {
            angleThreshold = 0;
            defaultAngleThreshold = 0;
            
        }
        else
        {
            angleThreshold = 3;
            defaultAngleThreshold = 3;
        }


        trialCameraPlaceholder = new GameObject();

        // load CSV files
        df = CSVParser.ParseTrialData(userFolderPath);

        has_ar_data = df.arUsers.Count > 0;
        has_vr_data = df.vrUsers.Count > 0;

        // Start in VR if VR data exists; else, start in AR
        current_users = (has_vr_data) ? df.vrUsers : df.arUsers;
        vr = has_vr_data;

        trialCount = current_users[0].trials.Count;

        mainCamera.transform.position = (vr) ? vr_camera_position : ar_camera_position;
        mainCamera.GetComponent<Camera>().fieldOfView = (vr) ? 104f : 29f; // TODO: Not accurate to what user saw, but good enough for reconstruction

        Trial currentTrial = current_users[user_index].trials[trial_index];

        trialdata = currentTrial.trialData;

        trial_type_text_go = canvas.transform.Find("TrialTypeText").gameObject;
        trial_type_text = trial_type_text_go.GetComponent<TMP_Text>();
        trial_type_text.text = (vr) ? "VR" : "AR";

        trial_checkmark_go = canvas.transform.Find("TrialTypeCheckmark").gameObject;

        user_text_go = canvas.transform.Find("UserText").gameObject;
        user_text = user_text_go.GetComponent<TMP_Text>();
        user_text.text = $"{current_users[user_index].name}";

        user_checkmark_go = canvas.transform.Find("UserCheckmark").gameObject;

        trial_text_go = canvas.transform.Find("TrialText").gameObject;
        trial_text = trial_text_go.GetComponent<TMP_Text>();
        trial_text.text = $"{trial_index}";

        choice_text_go = canvas.transform.Find("ChoiceText").gameObject;
        choice_text = choice_text_go.GetComponent<TMP_Text>();

        calibration_text_go = canvas.transform.Find("CalibrationText").gameObject;
        calibration_text = calibration_text_go.GetComponent<TMP_Text>();
        calibration_text.text = (calibration) ? "ON" : "OFF";

        control_info = canvas.transform.Find("ControlInfo").gameObject;

        // set static positions (e.g., sphere positions)
        TrialData first_trial_dp = trialdata[0];
        mainCamera.transform.position = first_trial_dp.cameraPos;
        mainCamera.transform.rotation = first_trial_dp.cameraRot;
        SetUpStimuli(currentTrial, first_trial_dp);

        // determine whether we're using the GazeRay or the GazeRayCone

        gazeRay.gameObject.SetActive(useRayInsteadOfCone);
        if (gazeRayConeVisualAngle == GazeRayConeVisualAngleSize.TwoDegrees)
        {
            gazeRayCone = gazeRayCone2deg;
        }
        else if (gazeRayConeVisualAngle == GazeRayConeVisualAngleSize.FiveDegrees)
        {
            gazeRayCone = gazeRayCone5deg;
        }
        else
        {
            gazeRayCone = gazeRayCone8deg;
        }
        gazeRayCone.SetActive(!useRayInsteadOfCone);

        if (!useRayInsteadOfCone)
        {
            gazeRayCone.transform.Find("Dot").gameObject.SetActive(cameraHeadPositionTracking);
            gazeRayCone.transform.Find("Cone").gameObject.SetActive(!cameraHeadPositionTracking);
        }

        // log the first trial information
        UpdateFromTrial();
    }


    bool UserComplete(User user)
    {
        return user.trials.TrueForAll(t => t.sphereChoice != null);
    }

    bool TrialTypeComplete(List<User> users)
    {
        return users.TrueForAll(user => UserComplete(user));
    }

    void CalculateSphereStats(GameObject sphere, TrialData td, string sphereName, GameObject pcTransform)
    {
        //GameObject realSphere = sphere.transform.Find("sphereRoot").Find("smallNearRotating").gameObject;

        // Eccentricity
        float eccentricity = 0;
        if (sceneName == "officeTestReplay")
        {
            eccentricity = Vector3.Angle(pcTransform.transform.position - td.gazeOrigin, td.gazeDirection);
        }
        else if (sceneName == "spaceTestReplay")
        {
            Vector3 center = sphere.transform.TransformPoint(sphere.GetComponent<SphereCollider>().center);
            eccentricity = Vector3.Angle(center - td.gazeOrigin, td.gazeDirection);
        }
        else
        {
            eccentricity = Vector3.Angle(sphere.transform.position - td.gazeOrigin, td.gazeDirection);
        }


        // Depth
        float distance = 0;
        if (sceneName == "officeTestReplay")
        {
            distance = Vector3.Distance(td.gazeOrigin, pcTransform.transform.position);
        }
        else if (sceneName == "spaceTestReplay")
        {
            Vector3 center = sphere.transform.TransformPoint(sphere.GetComponent<SphereCollider>().center);
            distance = Vector3.Distance(td.gazeOrigin, center);
        }
        else
        {
            distance = Vector3.Distance(td.gazeOrigin, sphere.transform.position);
        }
        

            // compute the depth as a double for precision, but return as a float
        float depth = (float)(System.Math.Cos(eccentricity * (System.Math.PI / 180)) * distance);

        // Size
        Physics.SyncTransforms();
        float sizeAngle = SpecialGetVisualAngle(sphere, td.gazeOrigin, pcTransform);

        Debug.Log($"Sphere {sphereName} - eccentricity: {eccentricity}, depth: {depth}, size: {sizeAngle}");

        // Save stats
        if (sphereName == "A")
        {
            current_users[user_index].trials[trial_index].true_a_ecce = eccentricity;
            current_users[user_index].trials[trial_index].true_a_depth = depth;
            current_users[user_index].trials[trial_index].true_a_size = sizeAngle;
        }
        else if (sphereName == "B")
        {
            current_users[user_index].trials[trial_index].true_b_ecce = eccentricity;
            current_users[user_index].trials[trial_index].true_b_depth = depth;
            current_users[user_index].trials[trial_index].true_b_size = sizeAngle;
        }


    }

    float SpecialGetVisualAngle(GameObject sphere, Vector3 gazeOrigin, GameObject pc_Transform)
    {
        float distance = 0;
        if (sceneName == "officeTestReplay")
        {
            distance = Vector3.Distance(gazeOrigin, pc_Transform.transform.position);
        }
        else if (sceneName == "spaceTestReplay")
        {
            Vector3 center = sphere.transform.TransformPoint(sphere.GetComponent<SphereCollider>().center);
            distance = Vector3.Distance(gazeOrigin, center);
        }
        else
        {
            distance = Vector3.Distance(gazeOrigin, sphere.transform.position);
        }
        double sphereColliderRadius = sphere.GetComponent<SphereCollider>().radius;
        double actualRadius = sphereColliderRadius * sphere.transform.localScale.x;
        double visualAngleRadians = 2 * System.Math.Asin(actualRadius / distance);
        return (float)(visualAngleRadians * 180 / System.Math.PI);
    }

    // TODO: Refactor further
    void UpdateFromTrial(bool newTrial = true)
    {

        // Trial Data
        Trial currentTrial = current_users[user_index].trials[trial_index];
        trialdata = currentTrial.trialData;
        TrialData td = trialdata[trialDataIndex];

        // Head Position/Rotation
        Quaternion headRotation = td.cameraRot;

        if (cameraHeadPositionTracking)
        {
            mainCamera.transform.position = td.cameraPos;
            mainCamera.transform.rotation = headRotation;
        }

        // world position is only valid on the first frame, since relative position that it's computed from is only logged once.
        if (newTrial)
        {
            angleThreshold = defaultAngleThreshold + 0;
            SetUpStimuli(currentTrial, td);
            CalculateSphereStats(stimulus_root_A, td, "A", pcA_transform);
            CalculateSphereStats(stimulus_root_B, td, "B", pcB_transform);
        }

        Vector3 gazeDirection = td.gazeDirection;

        if (useRayInsteadOfCone) // gazeRay
        {
            gazeRay.origin = td.gazeOrigin;
            gazeRay.direction = gazeDirection;
        }
        else // gaze ray cone
        {
            gazeRayCone.transform.position = td.gazeOrigin;
            gazeRayCone.transform.rotation = Quaternion.LookRotation(gazeDirection);
            gazeRayCone.transform.Find("Dot").gameObject.SetActive(cameraHeadPositionTracking);
            gazeRayCone.transform.Find("Cone").gameObject.SetActive(!cameraHeadPositionTracking);
        }



        // UI elements
        trial_type_text.text = (vr) ? "VR" : "AR";
        user_text.text = $"{current_users[user_index].name}";
        trial_text.text = $"{trial_index}";

        // Checkmarks
        user_checkmark_go.SetActive(UserComplete(current_users[user_index]));
        trial_checkmark_go.SetActive(TrialTypeComplete(current_users));


        bool sphereChosen = current_users[user_index].trials[trial_index].sphereChoice != null;
        choice_text.text = (sphereChosen) ? current_users[user_index].trials[trial_index].sphereChoice : "";
    }

    void ToggleHeadTracking()
    {
        // Toggle
        cameraHeadPositionTracking = !cameraHeadPositionTracking;

        // Set Keyboard controls
        mainCamera.GetComponent<CameraControls>().enabled = !cameraHeadPositionTracking;

        // Change the gaze ray cone
        if (!useRayInsteadOfCone)
        {
            gazeRayCone.transform.Find("Dot").gameObject.SetActive(cameraHeadPositionTracking);
            gazeRayCone.transform.Find("Cone").gameObject.SetActive(!cameraHeadPositionTracking);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            ExportAnswerCSV();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            ToggleHeadTracking();
        }

        // Toggle Calibration
        if (Input.GetKeyDown(KeyCode.Quote))
        {
            calibration = !calibration;
            calibration_text.text = (calibration) ? "ON" : "OFF";

        }

        // Toggle Info
        if (Input.GetKeyDown(KeyCode.I))
        {
            bool active = !canvas.gameObject.activeInHierarchy;
            canvas.gameObject.SetActive(active);
        }

        // If there's both VR and AR data, you can toggle between them
        if (Input.GetKeyDown(KeyCode.T) && has_ar_data && has_vr_data)
        {
            vr = !vr;
            if (vr)
            {
                Debug.Log("Switching to VR");
            }
            else
            {
                Debug.Log("Switching to AR");
            }
            current_users = (vr) ? df.vrUsers : df.arUsers;
            playing = false;
            trialDataIndex = 0; // start from beginning of trial
            user_index = 0;
            //halfTrialCount = current_users[0].trials.Count / 2;
            trialCount = current_users[0].trials.Count;
            autoclassify = false;
            UpdateFromTrial();

        }

        // Switch users/trials
        bool advance_user = (Input.GetKeyDown(KeyCode.P) || autoadvance_user) && (user_index < current_users.Count - 1);
        bool decrement_user = Input.GetKeyDown(KeyCode.Y) && (user_index > 0);
        user_index += decrement_user ? -1 : advance_user ? 1 : 0;

        bool advance_trial = (Input.GetKeyDown(KeyCode.O) || autoadvance) && (trial_index < trialCount - 1); // halfTrialCount - 1);
        bool decrement_trial = Input.GetKeyDown(KeyCode.U) && (trial_index > 0);
        trial_index += decrement_trial ? -1 : advance_trial ? 1 : 0;

        if (Input.GetKeyDown(KeyCode.Y) || Input.GetKeyDown(KeyCode.U) || Input.GetKeyDown(KeyCode.O) || Input.GetKeyDown(KeyCode.P) || autoadvance || autoadvance_user)
        {
            playing = false;
            trialDataIndex = 0; // start from beginning of trial
            if (autoadvance_user)
            {
                trial_index = 0;
            }
            autoclassify = false;
            UpdateFromTrial();
        }

        // Sphere Choice Controls
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            current_users[user_index].trials[trial_index].sphereChoice = "A";
            //current_users[user_index].trials[trial_index + (int)trialCount].sphereChoice = "B"; // (int)halfTrialCount].sphereChoice = "B"; // set opposite in the reversed trials
            Debug.Log("A logged");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            current_users[user_index].trials[trial_index].sphereChoice = "B";
            //current_users[user_index].trials[trial_index + (int)trialCount].sphereChoice = "A"; //(int)halfTrialCount].sphereChoice = "A"; // set opposite in the reversed trials
            Debug.Log("B logged");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            current_users[user_index].trials[trial_index].sphereChoice = null;
            Debug.Log("Choice cleared");
        }

        if (current_users[user_index].trials[trial_index].sphereChoice != null)
        {
            //Debug.Log(current_users[user_index].trials[trial_index].sphereChoice);
        }


        //  Each Trial Data is a single time point
        // Update the replay visualization
        // TODO: Default timescale should be actual recorded time

        // Playback Controls
        // Toggle Playback
        if (Input.GetKeyDown(KeyCode.K)) { playing = !playing; }

        // Toggle Autoclassify
        // If Enter pressed, or enter has been pressed and it's not the last trial, or enter has been pressed and it's the last trial but no sphere has been chosen
        if (Input.GetKeyDown(KeyCode.KeypadEnter) || (autoadvance && trial_index < trialCount - 1) || ((autoadvance && trial_index == trialCount - 1 && current_users[user_index].trials[trial_index].sphereChoice == null)))
        {
            playing = true;
            forward = true;
            autoclassify = true;
            autoadvance = false;
            autoadvance_user = false;
            fullAutoadvance = true;
        }
        else if ((autoadvance && trial_index == trialCount - 1 && current_users[user_index].trials[trial_index].sphereChoice == null) && autoadvance_user)
        {
            playing = true;
            forward = true;
            autoclassify = true;
            autoadvance = false;
            autoadvance_user = false;
        }

        // Toggle Playback Direction (Rewinding vs Playing Normally)
        forward = Input.GetKeyDown(KeyCode.H) ? false : Input.GetKeyDown(KeyCode.Semicolon) ? true : forward;

        // Step Forward/Back Controls are only valid while not playing
        // TODO: Holding J/L down shuttles back/forth after a second
        if (!playing)
        {
            trialDataIndex += Input.GetKeyDown(KeyCode.J) && trialDataIndex > 0 ? -1 : Input.GetKeyDown(KeyCode.L) && trialDataIndex < trialdata.Count ? 1 : 0;
            UpdateFromTrial(false);
        }

        // Update the Trial if appropriate
        if (i == timeScale)
        {
            i = 0;
            bool trialShouldAdvance = (trialDataIndex < trialdata.Count - 1) && playing && forward; // we have not hit the end, and we are playing forward
            bool trialShouldReverse = (trialDataIndex > 0) && playing && !forward; // we have not hit the beginning, and we're playing in reverse

            trialDataIndex += trialShouldAdvance ? 1 : trialShouldReverse ? -1 : 0;
            UpdateFromTrial(false);
        }
        i++;

        if (waitaframe)
        {
            waitaframe = false;
            playing = true;
            forward = true;
            autoadvance = false;
            autoclassify = true;
            trialDataIndex = 0;
            trial_index = 0;
            user_index += 1;
            UpdateFromTrial(true);
        }
        else if (fullAutoadvance && current_users[user_index].trials.TrueForAll(t => t.sphereChoice != null) && (user_index != current_users.Count - 1))
        {
            waitaframe = true;
        }
        else if (fullAutoadvance && current_users[user_index].trials.TrueForAll(t => t.sphereChoice != null) && (user_index == current_users.Count - 1))
        {
            fullAutoadvance = false;
        }
    }

    bool IsFixation()
    {
        float velocity_threshold = 40;
        if (trialDataIndex > 0)
        {
            TrialData currentTimestamp = trialdata[trialDataIndex];
            TrialData lastTimestamp = trialdata[trialDataIndex - 1];
            float delta_angle = (Vector3.Angle(currentTimestamp.gazeDirection, lastTimestamp.gazeDirection));
            float delta_time = currentTimestamp.time - lastTimestamp.time;
            if (lastTime != delta_time)
            {
                lastTime = delta_time;
                totalTime += delta_time;
            }

            return (delta_angle / delta_time) < velocity_threshold;

        }
        return false;
    }


    bool GazeIntersectedWith(GameObject g, GameObject pc_Transform)
    {
        TrialData td_datapoint = current_users[user_index].trials[trial_index].trialData[trialDataIndex];
        float gazeAngleWithObject = ObjectGazeAngle(td_datapoint, g, pc_Transform);
        float objectSize = SpecialGetVisualAngle(g, td_datapoint.gazeOrigin, pc_Transform);
        
        if (sceneName == "cemeteryTestReplay")
        {
            return gazeAngleWithObject <= (objectSize / 2) + angleThreshold; // ghost uses full object boundaries
        }
        else
        {
            return gazeAngleWithObject <= angleThreshold;
        }
    }


    float ObjectGazeAngle(TrialData tdpoint, GameObject g, GameObject pc_Transform)
    {
        Vector3 lineOfSightToObject = Vector3.zero;
        if (sceneName == "officeTestReplay")
        {
            lineOfSightToObject = pc_Transform.transform.position - tdpoint.gazeOrigin;
        }
        else if (sceneName == "spaceTestReplay")
        {
            Vector3 center = g.transform.TransformPoint(g.GetComponent<SphereCollider>().center);
            lineOfSightToObject = center - tdpoint.gazeOrigin;
        }
        else
        {
            lineOfSightToObject = g.transform.position - tdpoint.gazeOrigin;
        }
        return Vector3.Angle(lineOfSightToObject, tdpoint.gazeDirection);
    }

    void CheckDistanceBetweenGazeAndSpheres(GameObject physicalSphereA, GameObject physicalSphereB)
    {
        // Trial Datapoint
        TrialData td_datapoint = current_users[user_index].trials[trial_index].trialData[trialDataIndex];


        float angleWithA = ObjectGazeAngle(td_datapoint, physicalSphereA, pcA_transform);
        float angleWithB = ObjectGazeAngle(td_datapoint, physicalSphereB, pcB_transform);

        if (angleWithA < angleThreshold && angleWithB < angleThreshold)
        {
            //Debug.Log("This condition has not been handled.");
            //autoclassify = false;
        }
        else if (angleWithA < angleThreshold)
        {
            current_users[user_index].trials[trial_index].sphereChoice = "A";
            autoadvance = true;
            autoclassify = false;
        }
        else if (angleWithB < angleThreshold)
        {
            current_users[user_index].trials[trial_index].sphereChoice = "B";
            autoadvance = true;
            autoclassify = false;
        }
    }

    void AdjustParameters()
    {
        angleThreshold += 1;
    }

    void AutoClassify()
    {
        // This is a standardized method for determining which sphere was paid attention to first.
        // This will be manually reviewed and updated.
        bool gaze_intersected_A = GazeIntersectedWith(stimulus_root_A, pcA_transform);
        bool gaze_intersected_B = GazeIntersectedWith(stimulus_root_B, pcB_transform);
        bool fixation = IsFixation();

        // if there's a fixation and the gaze intersects
        if (gaze_intersected_A && gaze_intersected_B && fixation)
        {
            CheckDistanceBetweenGazeAndSpheres(stimulus_root_A, stimulus_root_B);
        }
        else if (gaze_intersected_A && fixation)
        {
            current_users[user_index].trials[trial_index].sphereChoice = "A";
            autoadvance = true;
            //autoadvance_user = trial_index == trialCount - 1;
            autoclassify = false;
        }
        else if (gaze_intersected_B && fixation)
        {
            current_users[user_index].trials[trial_index].sphereChoice = "B";
            autoadvance = true;
            //autoadvance_user = trial_index == trialCount - 1;
            autoclassify = false;
        }

        // we've reached the end, and no sphere was chosen
        if ((trialDataIndex == trialdata.Count - 1) && (current_users[user_index].trials[trial_index].sphereChoice == null))
        {
            // adjust parameters
            AdjustParameters();

            // try again
            trialDataIndex = 0;
        }
    }

    // collision happens before late update
    private void LateUpdate()
    {
        // If Autoclassify, attempt to autoclassify.
        if (autoclassify)
        {
            AutoClassify();
        }

    }
}
