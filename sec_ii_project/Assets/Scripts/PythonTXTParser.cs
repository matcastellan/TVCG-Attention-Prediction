using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using System.Data;
using System.Linq;
using System.Collections;


public class TrialData 
{
    public float time;
    public Vector3 mainCamPos;
    public Vector3 mainCamF;
    public Vector3 mainCamU;
    public Vector3 mainCamR;
    public Vector3 gazeOrigin;
    public Vector3 gazeDirection;
    public Vector3 sphereApos;
    public Vector3 sphereBpos;

    public List<string> vector3_as_string;

    public TrialData()
    {

    }

}

public class Trial
{
    public List<TrialData> trialData;
    public int condition;
    public string sphereChoice;
    public float a_size, b_size, a_depth, b_depth;
    public float true_a_ecce, true_b_ecce, true_a_depth, true_b_depth, true_a_size, true_b_size;
    public int a_gabor_orient, b_gabor_orient;
    public Vector3 calibDirection; // the calibration direction the user did at the program launch
    public Trial()
    {
        trialData = new List<TrialData>();
    }
}

public class User
{
    public string name;
    public List<Trial> trials;

    public User()
    {
        trials = new List<Trial>();
    }
}

public class Datafile
{
    public List<User> arUsers;
    public List<User> vrUsers;
    public string name;

    public Datafile()
    {
        arUsers = new List<User>();
        vrUsers = new List<User>();
    }
}



public class CSVParser
{
    public static DataTable CreateTable(string filePath)
    {
        if (File.Exists(filePath))
        {
            // Create table from CSV file
            DataTable table = new DataTable();
            string[] lines = File.ReadAllLines(filePath);

            string[] headers = lines[0].Split(',');
            foreach (string header in headers)
            {
                table.Columns.Add(header);
            }

            // Set types of some columns
            table.Columns["Time"].DataType = typeof(float);
            table.Columns["ConditionID"].DataType = typeof(int);
            

            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = lines[i].Split(',');
                table.Rows.Add(values);
            }            

            return table;
        }
        else
        {
            Debug.LogError("File does not exist: " + filePath);
            return null;
        }
    }

    public static string[] GetUniqueValues(DataTable table, string columnName, string trialType)
    {
        HashSet<string> uniqueValues = new HashSet<string>();

        foreach (DataRow row in table.Rows)
        {
            if (row.Field<string>("AR_VR") == trialType)
            {
                string value = row[columnName].ToString();
                uniqueValues.Add(value);
            }
            
        }

        return uniqueValues.OrderBy(x => x).ToArray();
    }

    public static Vector3 StringToVec3(string str)
    {
        char[] trim_chars = new char[] { '{', '}' };
        string[] coords = str.Trim(trim_chars).Split(";");
        return new Vector3(float.Parse(coords[0].Replace("(", string.Empty)), float.Parse(coords[1]), float.Parse(coords[2].Replace(")", string.Empty)));
    }

    public static Datafile ParseCSV(string filePath)
    {
        // create table
        DataTable table = CreateTable(filePath);

        // create datafile
        Datafile df = new Datafile();

        // for AR, VR trials
        foreach (string trialType in new List<string> { "VR", "AR" }) 
        {
            // find total number of trials per user per AR/VR
            List<User> userType = (trialType == "VR") ? df.vrUsers : df.arUsers; 
            EnumerableRowCollection<DataRow> filteredByTrialType = table.AsEnumerable().Where(row => row.Field<string>("AR_VR") == trialType);

            if (filteredByTrialType.Count<DataRow>() == 0)
            {
                continue;
            }

            int maxNumTrials = filteredByTrialType.Max(row => row.Field<int>("ConditionID")) + 1;

            // find unique user names
            string[] users = GetUniqueValues(table, "User", trialType);

            // for each unique user
            foreach(string user in users)
            {
                // create user
                User ud = new User();
                ud.name = user;
                Debug.Log($"{trialType} user added: {user}");

                // add each trial
                for (int i = 0; i < maxNumTrials; ++i)
                {

                    Trial tr = new Trial();
                    tr.condition = i;

                    // get the sorted, filtered data rows
                    EnumerableRowCollection<DataRow> userTrialRows = table.AsEnumerable().Where(row => row.Field<string>("AR_VR") == trialType && row.Field<string>("User") == user && row.Field<int>("ConditionID") == i);
                    EnumerableRowCollection<DataRow> sortedRows = userTrialRows.OrderBy(row => row["Time"]);

                    // set trial data
                    DataRow first_row = sortedRows.First<DataRow>();
                    tr.a_size = first_row.Field<float>("A_size");
                    tr.b_size = first_row.Field<float>("B_size");
                    tr.a_depth = first_row.Field<float>("A_depth");
                    tr.b_depth = first_row.Field<float>("B_depth");
                    tr.a_gabor_orient = int.Parse(first_row.Field<string>("A_gabor")) - 1;
                    tr.b_gabor_orient = int.Parse(first_row.Field<string>("B_gabor")) - 1;
                    tr.calibDirection = StringToVec3(first_row.Field<String>("CalibDir"));


                    // add the trial timepoint data from these rows
                    foreach (DataRow row in sortedRows)
                    {
                        TrialData td = new TrialData();
                        td.time = row.Field<float>("Time");
                        td.gazeDirection = StringToVec3(row.Field<String>("GazeDir"));
                        td.gazeOrigin = StringToVec3(row.Field<String>("GazeOrg"));
                        td.mainCamPos = StringToVec3(row.Field<String>("MainCameraPos"));
                        td.mainCamF = StringToVec3(row.Field<String>("MainCameraFor"));
                        td.mainCamU = StringToVec3(row.Field<String>("MainCameraUp"));
                        td.mainCamR = StringToVec3(row.Field<String>("MainCameraRight"));
                        td.sphereApos = StringToVec3(row.Field<String>("A_worldPos"));
                        td.sphereBpos = StringToVec3(row.Field<String>("B_worldPos"));

                        tr.trialData.Add(td);
                    }

                    ud.trials.Add(tr);
                }

                // add user to userType
                userType.Add(ud);

            }


        }

        return df;
    }
    public static IEnumerator ParseCSVCoroutine(string filePath, Datafile df)
    {
        float import_progress_goal = 0.15f;  // completing import should be ~15% (not timed, just chosen)

        // Create table from CSV file
        DataTable table = new DataTable();
        IEnumerable<string> lines = File.ReadLines(filePath);

        bool first_line = true; // is this the first line?
        int yield_count = 0; // keeping track of how many rows processed to determine when to yield
        int yield_break_goal = 100; // after how many rows we should yield the coroutine

        // keep track of lines for progress bar
        float current_line = 0;
        float total_lines = lines.Count<string>();

        // for each line
        foreach (string line in lines)
        {
            // for first line, add headers
            if (first_line)
            {
                first_line = false;
                string[] headers = line.Split(',');
                foreach (string header in headers)
                {
                    table.Columns.Add(header);
                }

                // Set types of some columns
                table.Columns["Time"].DataType = typeof(float);
                table.Columns["ConditionID"].DataType = typeof(int);
            }
            else // add row
            {
                string[] values = line.Split(',');
                table.Rows.Add(values);
                yield_count++;
                if (yield_count >= yield_break_goal) // yield control after so many rows imported
                {
                    yield_count = 0;
                    yield return null;
                }
            }

            // update progress bar
            current_line += 1;
            MenuBehavior.progress = current_line / total_lines * import_progress_goal;
        }



        // for AR, VR trials
        foreach (string trialType in new List<string> { "VR", "AR" }) 
        {
            // find total number of trials per user per AR/VR
            List<User> userType = (trialType == "VR") ? df.vrUsers : df.arUsers;
            EnumerableRowCollection<DataRow> filteredByTrialType = table.AsEnumerable().Where(row => row.Field<string>("AR_VR") == trialType);

            if (filteredByTrialType.Count<DataRow>() == 0)
            {
                continue;
            }

            int maxNumTrials = filteredByTrialType.Max(row => row.Field<int>("ConditionID")) + 1;

            // find unique user names
            string[] users = GetUniqueValues(table, "User", trialType);
            int maxNumUsers = users.Length;

            // variables for tracking progress
            float currentTrialCumulative = 0;
            float totaltrials = maxNumUsers * maxNumTrials;
            float total_progress_goal = 1f;

            // for each unique user
            foreach (string user in users)
            {
                Debug.Log(user);
                // create user
                User ud = new User();
                ud.name = user;

                // add each trial
                for (int i = 0; i < maxNumTrials; ++i)
                {

                    Trial tr = new Trial();
                    tr.condition = i;

                    // get the sorted, filtered data rows
                    EnumerableRowCollection<DataRow> userTrialRows = table.AsEnumerable().Where(row => row.Field<string>("AR_VR") == trialType && row.Field<string>("User") == user && row.Field<int>("ConditionID") == i);
                    EnumerableRowCollection<DataRow> sortedRows = userTrialRows.OrderBy(row => row["Time"]);

                    // set trial data
                    DataRow first_row = sortedRows.First<DataRow>();
                    tr.a_size = float.Parse(first_row.Field<string>("A_size"));
                    tr.b_size = float.Parse(first_row.Field<string>("B_size"));
                    tr.a_depth = float.Parse(first_row.Field<string>("A_depth"));
                    tr.b_depth = float.Parse(first_row.Field<string>("B_depth"));
                    tr.a_gabor_orient = int.Parse(first_row.Field<string>("A_gabor")) - 1;
                    tr.b_gabor_orient = int.Parse(first_row.Field<string>("B_gabor")) - 1;
                    tr.calibDirection = StringToVec3(first_row.Field<String>("CalibDir"));

                    // add the trial timepoint data from these rows
                    foreach (DataRow row in sortedRows)
                    {
                        TrialData td = new TrialData();
                        td.time = row.Field<float>("Time");
                        td.gazeDirection = StringToVec3(row.Field<String>("GazeDir"));
                        td.gazeOrigin = StringToVec3(row.Field<String>("GazeOrg"));
                        td.mainCamPos = StringToVec3(row.Field<String>("MainCameraPos"));
                        td.mainCamF = StringToVec3(row.Field<String>("MainCameraFor"));
                        td.mainCamU = StringToVec3(row.Field<String>("MainCameraUp"));
                        td.mainCamR = StringToVec3(row.Field<String>("MainCameraRight"));
                        td.sphereApos = StringToVec3(row.Field<String>("A_worldPos"));
                        td.sphereBpos = StringToVec3(row.Field<String>("B_worldPos"));

                        tr.trialData.Add(td);
                    }

                    ud.trials.Add(tr);

                    // update the progress bar
                    currentTrialCumulative += 1;
                    MenuBehavior.progress = import_progress_goal + (currentTrialCumulative / totaltrials * (total_progress_goal - import_progress_goal));

                    yield return null;
                }

                // add user to userType
                userType.Add(ud);

            }


        }

        // indicate that we're finished
        MenuBehavior.importCSVCoroutineFinished = true;
    }

}



public class PythonTxtParser
{

    public static List<TrialData> ParseTxtFile(string filePath)
    {
        List<TrialData> trial_data = new List<TrialData>();
        if (File.Exists(filePath))
        {
            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            {
                TrialData datapoint = ParseDictionary(line);
                trial_data.Add(datapoint);
            }
        }
        else
        {
            Debug.LogError("File does not exist: " + filePath);
        }

        return trial_data;
    }

    private static string[] SplitStringByCommas(string input)
    {
        List<string> result = new List<string>();
        bool withinArray = false; // Track if within an array
        int startIndex = 0; // Start index of the current segment
        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == '[')
            {
                withinArray = true;
            }
            else if (input[i] == ']')
            {
                withinArray = false;
            }
            else if (input[i] == ',' && !withinArray)
            {
                // Split the segment and add it to the result
                string segment = input.Substring(startIndex, i - startIndex);
                result.Add(segment.Trim());
                // Update the start index for the next segment
                startIndex = i + 1;
            }
        }
        // Add the last segment to the result
        string lastSegment = input.Substring(startIndex).Trim();
        result.Add(lastSegment);
        return result.ToArray();
    }

    private static Vector3 ParseVector3FromString(string v3)
    {
        string[] parts = v3.Trim().TrimStart('[').TrimEnd(']').Split(",");
        return new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
    }

    private static TrialData ParseDictionary(string line)
    {
        TrialData td = new TrialData();
        string edited_line = line.Trim().TrimStart('{').TrimEnd('}');

        // Split the line by commas to separate key-value pairs
        string[] keyValuePairs = SplitStringByCommas(edited_line);


        foreach (string keyValuePair in keyValuePairs)
        {
            string[] parts = keyValuePair.Split(':');
            string key = parts[0].Trim().TrimStart('"').TrimEnd('"');
            string valueString = parts[1].Trim();

            if (key == "Time")
            {
                td.time = float.Parse(valueString);
            }
            else // it's an array
            {
                Vector3 v = ParseVector3FromString(valueString);
                
                switch (key)
                {
                    case "gazeDirCombined":
                        td.gazeDirection = v;
                        break;
                    case "gazeOriCombined":
                        td.gazeOrigin = v;
                        break;
                    case "mainCamPos":
                        td.mainCamPos = v;
                        break;
                    case "mainCamF":
                        td.mainCamF = v;
                        break;
                    case "mainCamU":
                        td.mainCamU = v;
                        break;
                    case "mainCamR":
                        td.mainCamR = v;
                        break;
                    case "sphere_A_worldPos":
                        td.sphereApos = v;
                        break;
                    case "sphere_B_worldPos":
                        td.sphereBpos = v;
                        break;
                }
            }
        }

        return td;
    }

}