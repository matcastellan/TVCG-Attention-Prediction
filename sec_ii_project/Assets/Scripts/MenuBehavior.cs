using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Xml.Serialization;
using System.IO;
using UnityEngine.SceneManagement;
using System.Text;

public class MenuBehavior : MonoBehaviour
{

    public static bool menuUsed = false;
    public static bool csv_loaded = false;
    public static float progress = 0;
    public static Datafile df;

    // Buttons
    private Button Btn_ImportCSV;
    private Button Btn_SaveDataFile;
    private Button Btn_LoadDataFile;
    private Button Btn_ExploreTrials;
    private Button Btn_ExportAnswerCSV;

    private TMPro.TextMeshProUGUI progress_title, progress_value;

    private bool button_click_sound_active = true;

    private GameObject importButtonCheckmark;

    public static bool importCSVCoroutineFinished = false;

    public GameObject prog_title, prog_val;



    // Start is called before the first frame update
    void Start()
    {
        GameObject importButton = GameObject.Find("Import_CSV_Btn");
        Btn_ImportCSV = importButton.GetComponent<Button>();
        importButtonCheckmark = importButton.transform.Find("Checkmark").gameObject;

        Btn_ImportCSV = importButton.GetComponent<Button>();
        Btn_SaveDataFile = GameObject.Find("Save_Data_File_Btn").GetComponent<Button>();
        Btn_LoadDataFile = GameObject.Find("Load_Data_File_Btn").GetComponent<Button>();
        Btn_ExploreTrials = GameObject.Find("Explore_Trials_Btn").GetComponent<Button>();
        Btn_ExportAnswerCSV = GameObject.Find("Export Answer CSV").GetComponent<Button>();

        progress_title = prog_title.GetComponent<TMPro.TextMeshProUGUI>();
        progress_value = prog_val.GetComponent<TMPro.TextMeshProUGUI>();

        prog_title.SetActive(false);
        prog_val.SetActive(false);
    }

    public void ImportCSV()
    {
        df = new Datafile();
        string path = EditorUtility.OpenFilePanel("Choose a CSV file", "", "csv");
        StartCoroutine(CSVParser.ParseCSVCoroutine(path, df));
        StartCoroutine(LoadAsynchronously());
        Btn_ImportCSV.interactable = false;
        Btn_SaveDataFile.interactable = false;
        Btn_LoadDataFile.interactable = false;
        Btn_ExploreTrials.interactable = false;
        Btn_ExportAnswerCSV.interactable = false;
        button_click_sound_active = false;
    }

    public void OnCSVImported()
    {
        Debug.Log("CSV loaded.");

        csv_loaded = true;
        importButtonCheckmark.SetActive(true);

        Btn_ImportCSV.interactable = true;
        Btn_SaveDataFile.interactable = true;
        Btn_LoadDataFile.interactable = true;
        Btn_ExploreTrials.interactable = true;
        Btn_ExportAnswerCSV.interactable = true;
        button_click_sound_active = true;
    }

    public void SaveDataFile()
    {
        string path = Application.dataPath + "/SaveFiles/save.xml";
        XmlSerializer serializer = new XmlSerializer(typeof(Datafile));
        StreamWriter writer = new StreamWriter(path);
        serializer.Serialize(writer.BaseStream, df);
        writer.Close();
    }

    public void LoadDataFile()
    {
        string path = Application.dataPath + "/SaveFiles/save.xml";
        XmlSerializer serializer = new XmlSerializer(typeof(Datafile));
        StreamReader reader = new StreamReader(path);
        Datafile deserialized = (Datafile)serializer.Deserialize(reader.BaseStream);
        reader.Close();
        df = deserialized;
    }

    public void ExploreTrials()
    {
        menuUsed = true;
        SceneManager.LoadScene("GazeVisualizer", LoadSceneMode.Single);
    }

    public void ExportAnswerCSV()
    {
        // Choose a save location
        string path = EditorUtility.OpenFolderPanel("Choose a save location", "", "");

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


    IEnumerator LoadAsynchronously()
    {
        prog_title.SetActive(true);
        prog_val.SetActive(true);

        while (!importCSVCoroutineFinished)
        {
            progress_value.text = $"{(progress * 100):00.000}%";
            yield return null;
        }
        prog_title.SetActive(false);
        prog_val.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (importCSVCoroutineFinished)
        {
            OnCSVImported();
            importCSVCoroutineFinished = false;
        }
    }
}
