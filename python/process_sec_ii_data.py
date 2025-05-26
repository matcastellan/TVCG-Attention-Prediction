#! /bin/python3
# process data file

import math
import os
import tkinter as tk
import tkinter.ttk
from tkinter.filedialog import askdirectory
from tkinter import messagebox
from typing import List

import numpy as np
import pandas as pd
import torch
import pickle  # to save name dict

from utilities import angle_between_vectors, vec3_to_f3, s2i
from fixation_library import determine_fixations_from_gaze_data

def x_prod(v1, v2):
    x1, y1, z1 = v1
    x2, y2, z2 = v2
    return np.array([y1*z2-z1*y2, z1*x2-x1*z2, x1*y2-y1*x2])

def dist(v):
    return math.sqrt(v[0]**2 + v[1]**2 + v[2]**2)

def point2line_dist(d0, d1, d2):
    d = dist(x_prod((d0-d1), (d0-d2))) / dist(d1-d2)
    return d

def calc_first_hit_times(frame_dicts, sphere_A_r, sphere_B_r):
    n_frames = len(frame_dicts)
    d_A_l: List[float] = []
    d_B_l: List[float] = []
    time_fl: List[float] = []
    for i in range(n_frames):
        # count += 1; if count >= 100: break;
        frame_dict = frame_dicts[i]

        time_f = frame_dict["time_f"] if "time_f" in frame_dict else frame_dict[
            "Time"]
        time_fl.append(time_f)

        s_A = np.array(frame_dict["sphere_A_worldPos"])
        s_B = np.array(frame_dict["sphere_B_worldPos"])
        gaze_dir = np.array(frame_dict["gazeDirCombined"])
        gaze_ori = np.array(frame_dict["gazeOriCombined"])

        scale = 10.0
        d1 = gaze_ori
        d2 = gaze_ori + gaze_dir * scale

        d0 = s_A
        d_A = point2line_dist(d0, d1, d2)
        d_A_l.append(d_A)
        d0 = s_B
        d_B = point2line_dist(d0, d1, d2)
        d_B_l.append(d_B)

    delta_error = 0.1
    first_hitting_time_A, first_leaving_time_A = -1.0, -1.0
    first_hitting_time_B, first_leaving_time_B = -1.0, -1.0

    if len(time_fl) == 0:
        return (-1.0, -1.0, -1.0, -1.0)

    s_t = time_fl[0]
    for i in range(len(d_A_l)):
        if d_A_l[i] <= sphere_A_r+delta_error and first_hitting_time_A == -1:
            first_hitting_time_A = time_fl[i] - s_t
        if first_hitting_time_A != -1.0 and \
            d_A_l[i] >= sphere_A_r+delta_error and \
            first_leaving_time_A == -1.0:
            first_leaving_time_A = time_fl[i] - s_t
        if first_hitting_time_A != -1.0 and first_leaving_time_A != -1.0:
            break
    for i in range(len(d_B_l)):
        if d_B_l[i] <= sphere_B_r+delta_error and first_hitting_time_B == -1:
            first_hitting_time_B = time_fl[i] - s_t
        if first_hitting_time_B != -1.0 and \
            d_B_l[i] >= sphere_B_r+delta_error and \
            first_leaving_time_B == -1.0:
            first_leaving_time_B = time_fl[i] - s_t
        if first_hitting_time_B != -1.0 and first_leaving_time_B != -1.0:
            break

    return (first_hitting_time_A, first_leaving_time_A, first_hitting_time_B, first_leaving_time_B)

vr_trial_type = None
vr_path = None
ar_path = None

vr_values = {
    "ecce": [5, 15, 25],
    "depth": [2, 5, 8],
    "size": [4, 8]
}
ar_values = {
    "ecce": [5, 6, 7],
    "depth": [2, 5, 8],
    "size": [3, 6]
}
def vector3_to_string(vector3):
    return "{" + str(vector3[0]) + ";" + str(vector3[1]) + ";" + str(vector3[2]) + "}"

def string_to_vector3(vec3_string):
    values = vec3_string.strip('{').strip('}').split(';')
    for i in range(len(values)):
        values[i] = float(values[i])
    return values

# Data Classes
class GazeFrame:
    def __init__(self):
        self.time: float = -1.0
        self.condition = -1

        self.gazeDirCombined = [-1.0, -1.0, -1.0]
        self.gazeOriCombined = [-1.0, -1.0, -1.0]

        self.mainCamPos = [-1.0, -1.0, -1.0]
        self.mainCamF = [-1.0, -1.0, -1.0]
        self.mainCamU = [-1.0, -1.0, -1.0]
        self.mainCamR = [-1.0, -1.0, -1.0]

    def __repr__(self):
        return str(self.__dict__)


class SphereCondition:
    def __init__(self, s_id):
        self.s_id: str = s_id
        self.ecce: int = -1
        self.depth: int = -1
        self.size: int = -1
        self.theta: float = -1.0
        self.shift: float = -1.0
        self.arrow: int = -1
        self.gabor: int = -1
        self.rel_pos = [-1.0, -1.0, -1.0]

    def __repr__(self):
        return str(self.__dict__)


class TrialCondition:
    def __init__(self):
        self.cid = -1
        self.sphere_A = SphereCondition("A")
        self.sphere_B = SphereCondition("B")

    def __repr__(self):
        return str(self.__dict__)


class UserTimeAnswer:
    def __init__(self, basic=False):
        self.cid = -1  # condition ID
        self.sti_st: float = -1.0  # stimulus start time
        self.sti_et: float = -1.0  # stimulus end time

        # the calibration direction is stored here
        self.calib_dirX = 0
        self.calib_dirY = 0
        self.calib_dirZ = 0

        # two types:
        #   reg for standard gabor test
        #   basic for gabor_simple_text
        self.ans_A: int = -1  # stimulus answer values
        self.ans_A_et: float = -1.0  # stimulus end times
        if not basic:
            self.ans_B: int = -1
            self.ans_B_et: float = -1.0

        self.ans_correct: int = -1  # whether user answered correctly

    def __repr__(self):
        return str(self.__dict__)


################################################################################
# some parse code
def parse_gaze_log(gaze_fp):
    file = open(gaze_fp, 'r')
    lines = file.readlines()

    gaze_data = []
    for line in lines:
        tokens = line.split(" ")
        tok_len_adjust = 2 if len(tokens) == 23 else 0
        temp_gaze_frame = GazeFrame()
        temp_gaze_frame.time = float(tokens[2])
        temp_gaze_frame.gazeDirCombined = vec3_to_f3(tokens, 6 - tok_len_adjust)
        temp_gaze_frame.gazeOriCombined = vec3_to_f3(tokens, 10 - tok_len_adjust)
        temp_gaze_frame.mainCamF = vec3_to_f3(tokens, 13 - tok_len_adjust)
        temp_gaze_frame.mainCamU = vec3_to_f3(tokens, 16 - tok_len_adjust)
        temp_gaze_frame.mainCamR = vec3_to_f3(tokens, 19 - tok_len_adjust)
        temp_gaze_frame.mainCamPos = vec3_to_f3(tokens, 22 - tok_len_adjust)

        gaze_data.append(temp_gaze_frame)

    file.close()

    return gaze_data


# parses information about spheres into SphereConditions
def parse_trial_cond_log(trial_fp, basic=False):
    global vr_trial_type, vr_values, ar_values

    # Load file
    file02 = open(trial_fp, 'r')
    lines = file02.readlines()

    # create a list of trials from the trial condition log
    trials = []
    for line in lines:
        tokens = line.split(" ")

        trial = TrialCondition()
        trial.cid = int(tokens[0])

        # populate the Trial Condition with data
        attrs = ("ecce", "depth", "size", "theta", "shift", "arrow", "gabor")
        num_spheres = 1 if basic else 2

        # create two SphereConditions to attach to the Trial Condition,
        # storing information about the spheres in the trial
        for s_i in range(1, num_spheres + 1):  # A = 1, B = 2
            s_id = "A" if s_i == 1 else "B"
            tmp_sphere = SphereCondition(s_id)
            for index in range(7):
                input_str: str = tokens[s_i + num_spheres * index]
                input_val = float(input_str) if index == 4 else int(input_str)
                attr = attrs[index]

                # ecce, depth, and size are stored in trial condition log as
                # enums (e.g., 1, 2, 3). Convert them for storage in the table.
                if attr in vr_values:
                    input_val = vr_values[attr][input_val - 1] if \
                        vr_trial_type else ar_values[attr][input_val - 1]
                setattr(tmp_sphere, attrs[index], input_val)
            rel_pos_ind = 8 if basic else 15 if s_i == 1 else 18
            tmp_sphere.rel_pos = [float(tokens[rel_pos_ind]),
                                  float(tokens[rel_pos_ind + 1]),
                                  float(tokens[rel_pos_ind + 2])]
            if s_i == 1:
                trial.sphere_A = tmp_sphere
            else:
                trial.sphere_B = tmp_sphere

        trials.append(trial)

    return trials


def parse_user_answer(filepath, basic=False):
    file = open(filepath, 'r')
    lines = file.readlines()

    dir_x = 0
    dir_y = 0
    dir_z = 0

    answers = []
    for line in lines:
        tokens = line.split(" ")
        if tokens[0].__contains__("**"):
            # store the calibration direction
            dir_x = float(tokens[3])
            dir_y = float(tokens[4])
            dir_z = float(tokens[5])

        else:
            tokens = [token for token in tokens if len(token) != 0]

            usr_answer = UserTimeAnswer(basic)
            try:
                usr_answer.cid = int(tokens[1])
                usr_answer.sti_st = float(tokens[2])
                usr_answer.sti_et = float(tokens[4])
                usr_answer.calib_dirX = dir_x
                usr_answer.calib_dirY = dir_y
                usr_answer.calib_dirZ = dir_z
                usr_answer.ans_A_et = float(tokens[6])
                if basic:
                    usr_answer.ans_A = s2i(tokens[7])
                    usr_answer.ans_correct = s2i(tokens[8])
                else:
                    usr_answer.ans_B_et = float(tokens[8])
                    usr_answer.ans_A = s2i(tokens[9])
                    usr_answer.ans_B = s2i(tokens[10])
                    usr_answer.ans_correct = s2i(tokens[11])
            except BaseException as err:
                print(f"Unexpected {err=}, {type(err)=}")

            # only include correct answers
            if usr_answer.ans_correct:
                answers.append(usr_answer)

    return answers


def split_user_answer_files(trial_dir):
    # gaze data is split between multiple files, and the time resets with each
    # trial. user answer file is contained in one file, but the time resets
    # with each trial. trial_condition file is contained in one file and
    # contains no reference to time. to parse this information effectively, we
    # really need to consider multiple groups of gaze and user files

    # load user answer file, and count the number of gaze files
    path = None
    gaze_file_count = 0
    for filepath in os.listdir(trial_dir):
        if filepath.endswith("answer.txt"):  # the default file
            path = f"{trial_dir}/{filepath}"
        elif "gaze" in filepath:
            gaze_file_count += 1
    if not path:
        raise ValueError(f"Error: no answer file found in {trial_dir}")

    # parse into multiple files
    files = [[] for i in range(gaze_file_count)]
    current_file = -1
    with open(path, 'r') as f:
        lines = f.readlines()
        for index in range(0, len(lines)):
            line = lines[index]
            if line.startswith("****"): # it's a new answer file
                current_file += 1
            # else:
            files[current_file].append(line)

    # re-save
    for index in range(len(files)):
        filepath = f"{trial_dir}/answer{index}.txt"
        with open(filepath, 'w') as f:
            f.writelines(files[index])

    return gaze_file_count

def get_world_pos(gd, relative_pos):
    return torch.tensor(gd.mainCamPos) + \
           relative_pos[0] * torch.tensor(gd.mainCamR) + \
           relative_pos[1] * torch.tensor(gd.mainCamU) + \
           relative_pos[2] * torch.tensor(gd.mainCamF)


# navigate to the directory where all files of an individual user session
# are contained -- the program will parse correctly
def parse_partial_trial(trial_dir, _user, file_index):
    basic = True if "basic" in trial_dir else False
    trial_type = "basic" if basic else "standard"

    # get gaze index
    gaze_index = []
    for filepath in os.listdir(trial_dir):
        if "gaze" in filepath:
            gaze_index.append(int(filepath.split("gaze_")[1].split(".")[0]))

    gaze_index.sort()

    # get filepath strings
    trial_fp, gaze_fp, user_ans_fp, full_user_ans_fp = None, None, None, None

    for filename in os.listdir(trial_dir):
        filepath = f"{trial_dir}/{filename}"
        # trial condition
        if "trial_recording_time" in filename and not any(
                term in filename for term in
                ("gaze", "debug", "answer", "wrong")):
            trial_fp = filepath
        # gaze data file
        if filename.endswith(f"gaze_{gaze_index[file_index]}.txt"):
            gaze_fp = filepath
        if filename.endswith(f"answer{file_index}.txt"):  # partial answer file
            user_ans_fp = filepath
        if filename.endswith("answer.txt"):  # full answer file
            full_user_ans_fp = filepath

    if not all((trial_fp, gaze_fp, user_ans_fp, full_user_ans_fp)):
        raise ValueError(f"Error: missing files in {trial_dir}")

    gaze_data: List[GazeFrame] = parse_gaze_log(gaze_fp)
    conditions: List[TrialCondition] = parse_trial_cond_log(trial_fp, basic)
    user_answers: List[UserTimeAnswer] = parse_user_answer(user_ans_fp, basic)

    # create column headers
    # non-sphere specific
    cols = ["AR_VR", "User", "Type", "Time",
            "ConditionID", "ConditionStart", "ConditionEnd",
            "CalibDir", "CalibDirX", "CalibDirY", "CalibDirZ",
            "GazeDir", "GazeDirX", "GazeDirY", "GazeDirZ",
            "GazeOrg", "GazeOrgX", "GazeOrgY", "GazeOrgZ",
            "MainCameraPos", "MainCameraPosX", "MainCameraPosY",
            "MainCameraPosZ",
            "MainCameraFor", "MainCameraForX", "MainCameraForY",
            "MainCameraForZ",
            "MainCameraUp", "MainCameraUpX", "MainCameraUpY", "MainCameraUpZ",
            "MainCameraRight", "MainCameraRightX", "MainCameraRightY",
            "MainCameraRightZ", "Fixation"]

    # sphere specific
    for sph in ("A", "B"):
        for header in ("ecce", "depth", "size", "theta", "shift", "arrow",
                       "gabor", "worldPos", "angleWithGaze", "FirstHitTime",
                       "FirstLeaveTime", "fixation"):
            cols.append(f"{sph}_{header}")

    rows = []

    true_fixation_conditions = set()
    for usr_ans in user_answers:  # for each answer (unique, VERIFIED condition)
        # get the associated condition
        _condition = next(c for c in conditions if c.cid == usr_ans.cid)

        # construct a folder for the associated condition
        condition_fp = f"{trial_dir}/trial{_condition.cid}"
        if not os.path.isdir(condition_fp):
            os.mkdir(condition_fp)

        # create the dataframes

        # get the sub-slice of gaze data for this trial
        # for basic - from start of spheres until user answers
        # for standard - non-interactive sphere viewing period
        gaze_data_end_time = usr_ans.ans_A_et if basic else usr_ans.sti_et
        cond_gaze_data = [gd for gd in gaze_data if
                          usr_ans.sti_st <= gd.time <= gaze_data_end_time]

        # sort in order of increasing time
        cond_gaze_data.sort(key=lambda x: x.time)

        # determine fixations based on the gaze data
        fixation_data = determine_fixations_from_gaze_data(cond_gaze_data)

        # construct variables which will be used in multiple rows
        start_time = cond_gaze_data[0].time
        spheres = [_condition.sphere_A, _condition.sphere_B]

        cond_gaze_data_index = 0
        for gd in cond_gaze_data:
            # Full Data
            row = {"AR_VR": "VR" if vr_trial_type else "AR",
                   "User": _user, "Type": trial_type,
                   "Time": gd.time - start_time, "ConditionID": _condition.cid,
                   "ConditionStart": usr_ans.sti_st,
                   "ConditionEnd": usr_ans.ans_A_et if basic else usr_ans.sti_et,
                   "CalibDir": vector3_to_string([usr_ans.calib_dirX, usr_ans.calib_dirY,
                                        usr_ans.calib_dirZ]),
                   "CalibDirX" : usr_ans.calib_dirX,
                   "CalibDirY": usr_ans.calib_dirY,
                   "CalibDirZ": usr_ans.calib_dirZ
                   }

            # add X, Y, Z of all included vectors for flexibility
            dim = ("X", "Y", "Z")
            attrib_dict = {"GazeDir": "gazeDirCombined",
                           "GazeOrg": "gazeOriCombined",
                           "MainCameraPos": "mainCamPos",
                           "MainCameraFor": "mainCamF",
                           "MainCameraUp": "mainCamU",
                           "MainCameraRight": "mainCamR"}
            for col_head, prop in attrib_dict.items():
                col_val = getattr(gd, prop)
                row[col_head] = vector3_to_string(col_val)
                for _i in range(3):
                    row[f"{col_head}{dim[_i]}"] = col_val[_i]

            row["Fixation"] = fixation_data[cond_gaze_data_index]

            # add all sphere attributes for that condition
            for sph in spheres:
                for header_str in (
                    "ecce", "depth", "size", "theta", "shift", "arrow",
                        "gabor"):
                    row[f"{sph.s_id}_{header_str}"] = getattr(sph, header_str)

                world_pos = get_world_pos(gd, sph.rel_pos).tolist()
                row[f"{sph.s_id}_worldPos"] = vector3_to_string(world_pos)
                dim_vars = ["X", "Y", "Z"]
                for _i in range(3):
                    row[f"{sph.s_id}_worldPos{dim_vars[_i]}"] = world_pos[_i]

                # calculate angle between gaze direction vector and
                # gaze origin-sphere_center
                gaze_direction = getattr(gd, "gazeDirCombined")
                gaze_origin = getattr(gd, "gazeOriCombined")
                eye_to_sphere_vector = [
                    world_pos[0] - gaze_origin[0],
                    world_pos[1] - gaze_origin[1],
                    world_pos[2] - gaze_origin[2]]
                sphere_gaze_angle = angle_between_vectors(gaze_direction,
                                                          eye_to_sphere_vector)
                row[f"{sph.s_id}_angleWithGaze"] = sphere_gaze_angle

                row[f"{sph.s_id}_FirstHitTime"] = 0.0
                row[f"{sph.s_id}_FirstLeaveTime"] = 0.0

                if fixation_data[cond_gaze_data_index] and \
                        sphere_gaze_angle <= getattr(sph, "size") / 2:
                    # print(f"Condition {condition.cid}, Time {gd.time},
                    # Sphere {sph.s_id}: sphere_gaze_angle: {sphere_gaze_angle},
                    # radius: {getattr(sph, 'size') / 2},
                    # fixation: {fixation_data[cond_gaze_data_index]}")
                    true_fixation_conditions.add(_condition.cid)
                row[f"{sph.s_id}_fixation"] = True if \
                    fixation_data[cond_gaze_data_index] and \
                    sphere_gaze_angle <= getattr(sph, "size") / 2 else False


            row["FirstHitSphere"] = "N"

            cond_gaze_data_index += 1

            rows.append(row)

    print(f"Fixations found in {len(true_fixation_conditions)} "
          f"conditions: {true_fixation_conditions}")
    _df = pd.DataFrame(rows)
    _df.index.name = "Index"
    return _df


def parse_trial(trial_dir, _user):
    # pre-pre-process the user files, splitting into three files
    gaze_file_count = split_user_answer_files(trial_dir)

    dfs = []
    for _i in range(gaze_file_count):
        dfs.append(parse_partial_trial(trial_dir, _user, _i))

    return pd.concat(dfs, ignore_index=True)


def parse_user(root, _user):
    standard_path = f"{root}/{_user}/standard"
    sp_exists = os.path.exists(standard_path)
    standard = parse_trial(standard_path, _user) if sp_exists else None

    basic_path = f"{root}/{_user}/basic"
    bp_exists = os.path.exists(basic_path)
    basic = parse_trial(basic_path, _user) if bp_exists else None

    dataframe = pd.concat([standard, basic], ignore_index=True) if \
        sp_exists and bp_exists else \
        standard if sp_exists else \
        basic if bp_exists else None
    if dataframe is None:
        raise ValueError("Error: User has no data.")
    return dataframe


# figures out first hit info, and which sphere was hit first.
# additionally, saves some relevant files which are used elsewhere during
# this step.
def calculate_first_hit_info(__df):
    # generate a {name:uid} dictionary (generating UIDs for users)
    name_dic, name_size = generate_name_dic(__df)

    # create a np.array to store first hitting times
    num_conditions = __df["ConditionID"].nunique()
    hitting_time_result = np.ones((num_conditions, name_size, 4))

    # add additional information to base dataframe
    # about first hit sphere
    users = __df['User'].unique()
    for user in users:
        print("Compute first hitting time...")
        for condition in range(num_conditions):
            user_cond_df = __df[
                (__df['User'] == user) & (__df['ConditionID'] == condition)]
            first_hit_times = get_first_hit_times(user_cond_df)

            # Peggy: update hitting_time_result
            hitting_time_result[condition, int(name_dic[user]), :] = np.array(
                first_hit_times)

            print(f"User {user}, condition {condition} \
               has first hit times {first_hit_times}.")

            first_hit_sphere = determine_first_hit(first_hit_times)

            hit_dict = {
                "A_FirstHitTime": first_hit_times[0],
                "A_FirstLeaveTime": first_hit_times[1],
                "B_FirstHitTime": first_hit_times[2],
                "B_FirstLeaveTime": first_hit_times[3],
                "FirstHitSphere": first_hit_sphere
            }

            # set columns in dataframe per user/condition pair
            for key, value in hit_dict.items():
                __df.loc[(__df['User'] == user) & (
                    __df['ConditionID'] == condition), key] = value

    if vr_trial_type:
        np.save("hitting_time_result_VR.npy", hitting_time_result)
    else:
        np.save("hitting_time_result_AR.npy", hitting_time_result)

    # process this again, figuring out for each condition, what percentage
    # of Non-"N" are "A"
    default_value_percentage = -1.0
    __df["PercentPreferringA"] = default_value_percentage

    num_users = __df['User'].nunique()
    for condition_id in range(num_conditions):
        condition_df = __df[__df['ConditionID'] == condition_id].groupby(
            'User').first()
        total_a = (condition_df['FirstHitSphere'] == 'A').sum()
        total_n = (condition_df['FirstHitSphere'] == 'N').sum()
        percent_preferring_a = total_a / (num_users - total_n) if \
            (num_users - total_n) != 0 else 0
        __df.loc[(__df['ConditionID'] == condition_id),
                 "PercentPreferringA"] = percent_preferring_a

    return __df


def calc_radius(depth, size):
    global vr_trial_type
    # r1 is min_depth (2) * tan(minsize/2), where minsize is visual angle
    r1 = 2 * 0.03492 if vr_trial_type else 2 * 0.0261859215

    # r2 is min_depth (2) * tan(max_size/2) / tan(min_size/2)
    r2 = r1 * 0.06993 / 0.03492 if vr_trial_type else \
        r1 * 0.052407779 / 0.02618592156

    # depth_values = [2.0, 5.0, 8.0]
    depth_values = vr_values["depth"] if vr_trial_type else ar_values["depth"]
    size_values = vr_values["size"] if vr_trial_type else ar_values["size"]
    sphere_r = r1 if size == size_values[0] else r2
    sphere_r *= (depth / depth_values[0])

    # if depth == 1 and size == 1:
    #     sphere_r = r1
    # if depth == 1 and size == 2:
    #     sphere_r = r2
    # if depth != 1 and size == 1:
    #     sphere_r = r1*depth/2.0
    # if depth != 1 and size == 2:
    #     sphere_r = r2*depth/2.0
    assert (sphere_r != -1.0)
    return sphere_r


def distance_point_to_line(point, line_start, line_end):
    x1, y1, z1 = line_start
    x2, y2, z2 = line_end
    x0, y0, z0 = point

    # Calculate the direction vector of the line
    dx = x2 - x1
    dy = y2 - y1
    dz = z2 - z1

    # Calculate the vector connecting a point on the line to the given point
    dpx = x0 - x1
    dpy = y0 - y1
    dpz = z0 - z1

    # Calculate the dot product of the direction vector
    # and the connecting vector
    _dot_product = dx * dpx + dy * dpy + dz * dpz

    # Calculate the magnitude of the direction vector
    direction_magnitude = math.sqrt(dx ** 2 + dy ** 2 + dz ** 2)

    # Calculate the projection of the connecting vector
    # onto the direction vector
    projection = _dot_product / direction_magnitude

    # Calculate the coordinates of the projected point on the line
    px = x1 + (projection * dx / direction_magnitude)
    py = y1 + (projection * dy / direction_magnitude)
    pz = z1 + (projection * dz / direction_magnitude)

    # Calculate the distance between the given point
    # and the projected point on the line
    distance = math.sqrt((px - x0) ** 2 + (py - y0) ** 2 + (pz - z0) ** 2)

    return distance


# similar code in test004_visBar.py
# def get_dists(preprocessed_d: str, t_cid: int):
# return (first_hitting_time_A, first_leaving_time_A,
# first_hitting_time_B, first_leaving_time_B)
def get_first_hit_times(_df):

    frame_dicts = []
    for _, row in _df.iterrows():
        frame_dict = {
            'Time': row["Time"],
            'sphere_A_worldPos': string_to_vector3(row['A_worldPos']),
            'sphere_B_worldPos': string_to_vector3(row['B_worldPos']),
            'gazeDirCombined': string_to_vector3(row['GazeDir']),
            'gazeOriCombined': string_to_vector3(row['GazeOrg'])
        }
        frame_dicts.append(frame_dict)

    # calc sphere size
    size_a = _df.iloc[0]["A_size"]
    depth_a = _df.iloc[0]["A_depth"]
    sphere_a_rad = calc_radius(depth_a, size_a)
    size_b = _df.iloc[0]["B_size"]
    depth_b = _df.iloc[0]["B_depth"]
    sphere_b_rad = calc_radius(depth_b, size_b)

    return calc_first_hit_times(frame_dicts, sphere_a_rad, sphere_b_rad)


# Return "A" if the first sphere in the tuple is hit first, "B" if the second,
# and "N" if an error occurred
def determine_first_hit(first_hit_times_tuple):
    a_first_hit_time = first_hit_times_tuple[0]
    b_first_hit_time = first_hit_times_tuple[2]

    # Error States:
    # Return N if either first hit time is 0.0 or -1
    no_sphere_hit = (a_first_hit_time <= 0.0) or (b_first_hit_time <= 0.0)
    a_hit_first = a_first_hit_time < b_first_hit_time

    return "N" if no_sphere_hit else "A" if a_hit_first else "B"


def duplicate_and_switch_ab(__df):
    # duplicate data and switch A/B
    # create a duplicated dataframe
    print("Duplicating dataframe...")
    switched_df = __df.copy()
    # set these trials as "new conditions"
    num_conditions = switched_df["ConditionID"].nunique()
    switched_df["ConditionID"] = switched_df["ConditionID"] + num_conditions
    # switch A and B
    print("Switching A and B for duplicate data...")
    columns_to_switch = ["ecce", "depth", "size", "theta", "shift",
                         "arrow", "gabor", "worldPos", "worldPosX",
                         "worldPosY", "worldPosZ", "angleWithGaze",
                         "fixation", "FirstHitTime", "FirstLeaveTime"]

    for _i in range(len(columns_to_switch)):
        col_type = columns_to_switch[_i]
        a_col = f"A_{col_type}"
        b_col = f"B_{col_type}"
        switched_df[a_col], switched_df[b_col] = \
            switched_df[b_col], switched_df[a_col]
    # swap the percentages
    switched_df.loc[
        switched_df["PercentPreferringA"] >= 0, "PercentPreferringA"] = \
        1.0 - switched_df["PercentPreferringA"]

    # append switched data frame to df
    print("Appending duplicate dataframe...")
    df_combined = pd.concat([__df, switched_df], axis=0)
    return df_combined.reset_index(drop=True)


# generates a name dictionary {name: uid} and saves it to file
def generate_name_dic(_df):
    global vr_trial_type

    _name_dic = dict()
    unique_name_list = _df['User'].unique()
    _name_size = unique_name_list.size
    for i in range(_name_size):
        _name_dic[unique_name_list[i]] = i
    print("unique name dict: ", _name_dic)
    # save name dict in file
    if vr_trial_type:
        with open("user_name_dict_VR.pkl", 'wb') as fp:
            pickle.dump(_name_dic, fp)
        print("name dict is saved in user_name_dict_VR.pkl")
    else:
        with open("user_name_dict_AR.pkl", 'wb') as fp:
            pickle.dump(_name_dic, fp)
        print("name dict is saved in user_name_dict_AR.pkl")
    return _name_dic, _name_size


# provides a simple GUI to check what kind of trial it is - VR or AR
# returns a bool - true if VR, false if AR
def find_trial_locations_and_types():
    root = tk.Tk()

    # Quit entire program if user Xs out
    def close_method():
        root.destroy()
        quit()

    root.protocol("WM_DELETE_WINDOW", close_method)

    def run_button():
        global vr_path, ar_path
        if (vr_path is None) and (ar_path is None):
            messagebox.showerror("No file specified", "At least one trial type (VR or AR) must be specified.")
        else:
            root.destroy() # Close GUI and continue on with code

    def create_load_button(main_frame, trial_type):
        button_controls_frame = tk.Frame(main_frame, padx=25)
        button_controls_frame.pack(side=tk.LEFT)

        button = tk.Button(button_controls_frame, text=f"Load {trial_type}", font=font_type,
                              pady=10)
        button.pack(side=tk.TOP)

        type_loaded = tk.Label(button_controls_frame, text="Not Loaded")
        type_loaded.pack(side=tk.TOP)

        def set_type():
            global vr_path, ar_path
            is_vr = True if trial_type == "VR" else False
            print(f"{trial_type} Trials")

            # Load the file path of the VR/AR trials
            file_path = askdirectory(title=f"Choose {trial_type} trial directory")
            if file_path != "": # empty string means user canceled
                type_loaded.configure(text="Loaded")

                if is_vr:
                    vr_path = file_path
                else:
                    ar_path =file_path


        button.configure(command=set_type)


    font_type = ("Arial", 18, "bold")
    # Title
    label = tk.Label(root, text="Choose trial type", font=font_type)
    label.pack(side=tk.TOP)

    # VR/AR Controls Container
    vr_ar_control_frame = tk.Frame(root)
    vr_ar_control_frame.pack(side=tk.TOP)

    create_load_button(vr_ar_control_frame, "VR")
    create_load_button(vr_ar_control_frame, "AR")

    # Run Button
    run_button = tk.Button(root, text="Run", font=font_type, command=run_button, pady=10)
    run_button.pack(side=tk.TOP)

    root.mainloop()


# expects the following directory structure:
# data_dir
#   -- {User's Name}
#       -- standard
#           -- {all non-"Mono" files}
#       -- basic
#           -- {all "Mono" files}
# def parse_all_data(data_dir):
#     for directory in os.listdir(data_dir):
def parse_all_users(root):
    _df = None
    users = os.listdir(root)
    print("Generating CSV file...")
    for _i in range(len(users)):
        _user = users[_i]
        print(f"Parsing user {_i + 1} ({_user}) of {len(users)}")
        new_df = parse_user(root, _user)
        _df = new_df if _df is None else pd.concat([_df, new_df])

    # add information about calculating the first hit
    _df = calculate_first_hit_info(_df)

    # copy all trials, but switching A and B
    # DON'T UNCOMMENT - this is handled later by condition_augmentation.py
    # _df = duplicate_and_switch_ab(_df)

    _df.index.name = "Index"

    return _df


def preprocess_data(root_fp, _df):
    print("Pre-Processing Data")
    # for each user
    for _user in _df['User'].unique():
        user_df = _df[_df['User'] == _user]
        print("Pre-processing User " + _user)

        # Create pre-process folder
        folder_path = f"{root_fp}/{_user}/preprocess"
        if not os.path.exists(folder_path):
            os.makedirs(folder_path)

        # for each condition of the user
        for _condition in user_df['ConditionID'].unique():
            user_condition_df = user_df[user_df['ConditionID'] == _condition]

            # sort by time
            sorted_df = user_condition_df.sort_values("Time")

            output_string = ""

            # for each timestamp in the sorted df
            for index, row in sorted_df.iterrows():
                # construct your data
                output_data_row = {
                    "Time": row["Time"],
                    "gazeDirCombined": [row["GazeDirX"], row["GazeDirY"],
                                        row["GazeDirZ"]],
                    "gazeOriCombined": [row["GazeOrgX"], row["GazeOrgY"],
                                        row["GazeOrgZ"]],
                    "mainCamPos": [row["MainCameraPosX"], row["MainCameraPosY"],
                                   row["MainCameraPosZ"]],
                    "mainCamF": [row["MainCameraForX"], row["MainCameraForY"],
                                 row["MainCameraForZ"]],
                    "mainCamU": [row["MainCameraUpX"], row["MainCameraUpY"],
                                 row["MainCameraUpZ"]],
                    "mainCamR": [row["MainCameraRightX"],
                                 row["MainCameraRightY"],
                                 row["MainCameraRightZ"]],
                    "sphere_A_worldPos": [row["A_worldPosX"],
                                          row["A_worldPosY"],
                                          row["A_worldPosZ"]],
                    "sphere_B_worldPos": [row["B_worldPosX"],
                                          row["B_worldPosY"],
                                          row["B_worldPosZ"]]
                }
                output_string += f"{str(output_data_row)}\n".replace('\'', '"')

            # save to a condition file
            condition_fp = f"{root_fp}/{_user}/preprocess/{_condition}.txt"
            with open(condition_fp, 'w') as file:
                file.write(output_string)

def load_and_process_data(rootfp, trial_type):
    global vr_trial_type

    vr_trial_type = trial_type == "VR"

    partial_df = parse_all_users(rootfp)

    # create preprocess files, using base dataframe
    preprocess_data(rootfp, partial_df)

    return partial_df

if __name__ == "__main__":
    # check whether AR or VR using GUI
    find_trial_locations_and_types()

    # create or load base dataframe
    vr_df = load_and_process_data(vr_path, "VR") if vr_path else None
    ar_df = load_and_process_data(ar_path, "AR") if ar_path else None

    vr_only = (vr_df is not None) and (ar_df is None)
    vr_and_ar = (ar_df is not None) and (vr_df is not None)

    df = pd.concat([vr_df, ar_df], ignore_index=True) if vr_and_ar else vr_df if vr_only else ar_df

    # rewrite df to CSV
    print("Writing dataframe to file...")
    df.to_csv("full_data.csv")
