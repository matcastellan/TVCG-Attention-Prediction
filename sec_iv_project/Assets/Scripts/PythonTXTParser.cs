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
    public Vector3 gazeOrigin;
    public Vector3 gazeDirection;
    public Vector3 cameraPos;
    public Quaternion cameraRot;
    public Vector3 sphereApos;
    public Vector3 sphereBpos;
    public Quaternion sphereArot;
    public Quaternion sphereBrot;
    public Vector3 sphereAscale;
    public Vector3 sphereBscale;

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
    public float a_size, b_size, a_depth, b_depth, a_ecce, b_ecce;
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
            if (table.Columns.Contains("Time")) { table.Columns["Time"].DataType = typeof(float); }
            if (table.Columns.Contains("ConditionID")) { table.Columns["ConditionID"].DataType = typeof(int); }
            

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

    public static Quaternion StringToQuat(string str)
    {
        char[] trim_chars = new char[] { '{', '}' };
        string[] coords = str.Trim(trim_chars).Split(";");
        return new Quaternion(float.Parse(coords[0].Replace("(", string.Empty)), float.Parse(coords[1]), float.Parse(coords[2]), float.Parse(coords[3].Replace(")", string.Empty)));
    }

    public static Datafile ParseTrialData(string folderPath)
    {
        // create datafile
        Datafile df = new Datafile();

        // get all user directories
        string[] user_directories = Directory.GetDirectories(folderPath);

        foreach(string user_directory in user_directories)
        {
            // single user
            string trialFilePath = Directory.GetFiles(user_directory).Where(fn => !fn.Contains("gaze") && Path.GetExtension(fn) == ".csv").First();
            string gazeFilePath = Directory.GetFiles(user_directory).Where(fn => fn.Contains("gaze") && Path.GetExtension(fn) == ".csv").First();

            DataTable trialTable = CreateTable(trialFilePath);
            DataTable gazeTable = CreateTable(gazeFilePath);

            

            List<User> userType = df.vrUsers;

            // get the number of trials
            int maxNumTrials = trialTable.AsEnumerable().Max(row => row.Field<int>("ConditionID")) + 1;

            // create user
            User ud = new User();
            ud.name = trialTable.AsEnumerable().First<DataRow>().Field<string>("User");
            if (ud.name == "") { ud.name = Path.GetFileName(user_directory); }

            // add each trial
            for (int i = 0; i < maxNumTrials; ++i)
            {
                Trial tr = new Trial();
                tr.condition = i;



                // set trial data
                DataRow conditionRow = trialTable.AsEnumerable().SingleOrDefault<DataRow>(row => row.Field<int>("ConditionID") == i);
                tr.a_ecce = float.Parse(conditionRow.Field<string>("A_eccentricity"));
                tr.b_ecce = float.Parse(conditionRow.Field<string>("B_eccentricity"));
                tr.a_size = float.Parse(conditionRow.Field<string>("A_size"));
                tr.b_size = float.Parse(conditionRow.Field<string>("B_size"));
                tr.a_depth = float.Parse(conditionRow.Field<string>("A_depth"));
                tr.b_depth = float.Parse(conditionRow.Field<string>("B_depth"));


                // get the sorted, filtered data rows from gaze
                // TODO: Last trial end time logging somehow
                float trialStartTime = conditionRow.Field<float>("Time");
                float trialEndTime = float.Parse(conditionRow.Field<string>("EndTime"));

                EnumerableRowCollection<DataRow> gazeRows = gazeTable.AsEnumerable().Where(row => row.Field<float>("Time") >= trialStartTime && row.Field<float>("Time") <= trialEndTime);
                EnumerableRowCollection<DataRow> sortedGazeRows = gazeRows.OrderBy(row => row["Time"]);
                foreach (DataRow row in sortedGazeRows)
                {
                    TrialData td = new TrialData();
                    td.time = row.Field<float>("Time");
                    td.gazeOrigin = StringToVec3(row.Field<String>("CombinedOriWor"));
                    td.gazeDirection = StringToVec3(row.Field<String>("CombinedDirWor"));
                    td.cameraPos = StringToVec3(row.Field<String>("CamPos"));
                    td.cameraRot = StringToQuat(row.Field<String>("CamRot"));
                    td.sphereApos = StringToVec3(conditionRow.Field<String>("A_worldPos"));
                    td.sphereBpos = StringToVec3(conditionRow.Field<String>("B_worldPos"));
                    td.sphereArot = StringToQuat(conditionRow.Field<String>("A_worldRot"));
                    td.sphereBrot = StringToQuat(conditionRow.Field<String>("B_worldRot"));
                    td.sphereAscale = StringToVec3(conditionRow.Field<String>("A_localScale"));
                    td.sphereBscale = StringToVec3(conditionRow.Field<String>("B_localScale"));

                    tr.trialData.Add(td);
                }

                ud.trials.Add(tr);
            }

            df.vrUsers.Add(ud);
        }

        return df;
    }

}



public class PythonTxtParser
{
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
}