using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Linq;

public class ReplayTester : MonoBehaviour
{
    public GameObject mainCamera;
    private GameObject actualSphereA, actualSphereB; // corresponds to stimulus_root
    public GameObject stimulus_root_A, stimulus_root_B, stimulus_root_A_Large, stimulus_root_B_Large;
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

    private bool calibration = true;

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

    float angleThreshold = 3; // within 2 degrees, classify as a hit
    float defaultAngleThreshold = 3;

    private bool autoclassify = false; // whether we should autoclassify this trial
    bool autoadvance = false;
    bool autoadvance_user = false;

    bool fullAutoadvance = false;
    bool waitaframe = false;

    GameObject trialCameraPlaceholder; // used to set the position/rotation of the camera during the trial for calculations, regardless of the current camera's transform

    void SetSpherePosRot(GameObject sphere, Vector3 sphere_pos, TrialData td)
    {
        sphere.transform.position = sphere_pos;
        sphere.transform.rotation = Quaternion.LookRotation(sphere_pos - td.mainCamPos, Vector3.up);
    }

    void SetUpSpheres(Trial currentTrial, TrialData td)
    {
        // set the appropriate spheres active
        stimulus_root_A.SetActive(currentTrial.a_size == 4);
        stimulus_root_B.SetActive(currentTrial.b_size == 4);
        stimulus_root_A_Large.SetActive(currentTrial.a_size != 4);
        stimulus_root_B_Large.SetActive(currentTrial.b_size != 4);

        // figure out which spheres we're using
        actualSphereA = (currentTrial.a_size == 4) ? stimulus_root_A : stimulus_root_A_Large;
        actualSphereB = (currentTrial.b_size == 4) ? stimulus_root_B : stimulus_root_B_Large;

        // set position and rotation
        SetSpherePosRot(actualSphereA, td.sphereApos, td);
        SetSpherePosRot(actualSphereB, td.sphereBpos, td);

        // adjust scale
        trialCameraPlaceholder.transform.position = td.mainCamPos; // only the position aspect of the transform is used here
        trialCameraPlaceholder.transform.rotation = Quaternion.LookRotation(td.mainCamF, td.mainCamU);
        AdjustStimulusScale(actualSphereA, trialCameraPlaceholder.transform, currentTrial.a_depth, currentTrial.a_size, vr);
        AdjustStimulusScale(actualSphereB, trialCameraPlaceholder.transform, currentTrial.b_depth, currentTrial.b_size, vr);
    }

    // Start is called before the first frame update
    void Start()
    {
        trialCameraPlaceholder = new GameObject();

        // load CSV file

        if (MenuBehavior.menuUsed) // use the one imported in menu
        {
            df = MenuBehavior.df;
        }
        else // load it ourselves (debugging purposes only)
        {
            df = CSVParser.ParseCSV(filePath);
        }

        has_ar_data = df.arUsers.Count > 0;
        has_vr_data = df.vrUsers.Count > 0;

        // Start in VR if VR data exists; else, start in AR
        current_users = (has_vr_data) ? df.vrUsers : df.arUsers;
        vr = has_vr_data;

        current_users = current_users
            .OrderBy(p => int.Parse(p.name)) // Convert Name to integer for sorting
            .ToList();

        trialCount = current_users[0].trials.Count;

        mainCamera.transform.position = (vr) ? vr_camera_position : ar_camera_position;
        mainCamera.GetComponent<Camera>().fieldOfView = (vr) ? 104f : 29f;

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

        SetUpSpheres(currentTrial, first_trial_dp);

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

        mainCamera.GetComponent<CameraControls>().enabled = !cameraHeadPositionTracking;

        // log the first trial information
        UpdateFromTrial();
    }

    void AdjustStimulusScale(GameObject sphere, Transform camTrans, float depth, float size, bool vr)
    {
        VisualProperties.SetScaleForTopLevelObjectBasedOnDesiredVisualAngle(camTrans, sphere, size);
    }

    bool UserComplete(User user)
    {
        return user.trials.TrueForAll(t => t.sphereChoice != null);
    }

    bool TrialTypeComplete(List<User> users)
    {
        return users.TrueForAll(user => UserComplete(user));
    }

    void CalculateSphereStats(GameObject sphere, TrialData td, string sphereName)
    {
        // Eccentricity
        float eccentricity = VisualProperties.GetEccentricity(trialCameraPlaceholder.transform, sphere);

        // Depth
        float depth = VisualProperties.GetDepth(trialCameraPlaceholder.transform, sphere);

        // Size
        Physics.SyncTransforms();
        float sizeAngle = VisualProperties.GetVisualAngle(trialCameraPlaceholder.transform, sphere);

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

    Vector3 GetGazeDirection(Trial currentTrial, TrialData td)
    {
        Quaternion headRotation = Quaternion.LookRotation(td.mainCamF, td.mainCamU);

        // get the rotation for calibration, which uses vectors in the user's local coordinate system - hence, a local rotation
        Quaternion calibRotation = Quaternion.Inverse(Quaternion.FromToRotation(Vector3.forward, currentTrial.calibDirection.normalized));

        // compute the offset version of the gaze direction, based on the calibration, by transforming the gaze direction to local space,
        // applying the local quaternion, and then transforming it back

        Vector3 localGaze = headRotation * td.gazeDirection;
        Vector3 localRotatedGaze = calibRotation * localGaze;
        Vector3 worldRotatedGaze = Quaternion.Inverse(headRotation) * localRotatedGaze;

        return (calibration) ? worldRotatedGaze : td.gazeDirection;
    }

    void UpdateFromTrial(bool newTrial=true)
    {

        // Trial Data
        Trial currentTrial = current_users[user_index].trials[trial_index];
        trialdata = currentTrial.trialData;
        TrialData td = trialdata[trialDataIndex];

        // world position is only valid on the first frame, since relative position that it's computed from is only logged once.
        if (newTrial)
        {
            angleThreshold = defaultAngleThreshold + 0;
            SetUpSpheres(currentTrial, td);
            CalculateSphereStats(actualSphereA, td, "A");
            CalculateSphereStats(actualSphereB, td, "B");
        }

        // Head Position/Rotation
        Quaternion headRotation = Quaternion.LookRotation(td.mainCamF, td.mainCamU);
        if (cameraHeadPositionTracking)
        {
            mainCamera.transform.position = td.mainCamPos;
            mainCamera.transform.rotation = headRotation;
        }


        //// Gaze Ray/Cone
        Vector3 gazeDirection = GetGazeDirection(currentTrial, td);

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
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ToggleHeadTracking();
        }

        // If Escape hit and menu used, go back to the main menu
        if (MenuBehavior.menuUsed && Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene("Menu", LoadSceneMode.Single);
        }

        // Toggle Calibration
        if (Input.GetKeyDown(KeyCode.Quote))
        {
            calibration = !calibration;
            calibration_text.text = (calibration) ? "ON" : "OFF";

        }

        // Toggle Info
        if (Input.GetKeyDown(KeyCode.I)) {
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
            Debug.Log("A logged");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            current_users[user_index].trials[trial_index].sphereChoice = "B";
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

        // Playback Controls
        // Toggle Playback
        if (Input.GetKeyDown(KeyCode.K))  {playing = !playing;}

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
            Trial currentTrial = current_users[user_index].trials[trial_index];
            TrialData currentTimestamp = trialdata[trialDataIndex];
            TrialData lastTimestamp = trialdata[trialDataIndex - 1];

            Vector3 currentGazeDirection = GetGazeDirection(currentTrial, currentTimestamp);
            Vector3 lastGazeDirection = GetGazeDirection(currentTrial, lastTimestamp);

            float delta_angle = (Vector3.Angle(currentGazeDirection, lastGazeDirection));
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

    bool GazeIntersectedWith(GameObject g)
    {
        GameObject realSphere = g.transform.Find("sphereRoot").Find("smallNearRotating").gameObject;
        TrialData td_datapoint = current_users[user_index].trials[trial_index].trialData[trialDataIndex];
        float gazeAngleWithObject = ObjectGazeAngle(td_datapoint, realSphere);
        return gazeAngleWithObject <= angleThreshold;
    }


    float ObjectGazeAngle(TrialData tdpoint, GameObject g)
    {
        Vector3 lineOfSightToObject = g.transform.position - tdpoint.gazeOrigin;
        Trial currentTrial = current_users[user_index].trials[trial_index];
        Vector3 gazeDirection = GetGazeDirection(currentTrial, tdpoint);
        return Vector3.Angle(lineOfSightToObject, gazeDirection);
    }

    void CheckDistanceBetweenGazeAndSpheres(GameObject physicalSphereA, GameObject physicalSphereB)
    {
        // Trial Datapoint
        TrialData td_datapoint = current_users[user_index].trials[trial_index].trialData[trialDataIndex];

        
        float angleWithA = ObjectGazeAngle(td_datapoint, physicalSphereA);
        float angleWithB = ObjectGazeAngle(td_datapoint, physicalSphereB);

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
        bool gaze_intersected_A = GazeIntersectedWith(actualSphereA);
        bool gaze_intersected_B = GazeIntersectedWith(actualSphereB);
        bool fixation = IsFixation();

        

        // The actual mesh gameobjects of the spheres
        GameObject physicalSphereA = actualSphereA.transform.Find("sphereRoot").Find("smallNearRotating").gameObject;
        GameObject physicalSphereB = actualSphereB.transform.Find("sphereRoot").Find("smallNearRotating").gameObject;

        // if there's a fixation and the gaze intersects
        if (gaze_intersected_A && gaze_intersected_B && fixation)
        {
            CheckDistanceBetweenGazeAndSpheres(physicalSphereA, physicalSphereB);
        }
        else if (gaze_intersected_A && fixation)
        {
            current_users[user_index].trials[trial_index].sphereChoice = "A";
            autoadvance = true;
            autoclassify = false;
        }
        else if (gaze_intersected_B && fixation)
        {
            current_users[user_index].trials[trial_index].sphereChoice = "B";
            autoadvance = true;
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
