from utilities import angle_between_vectors


# Fixation dispersion algorithm based on covariance (CDT)
# NOTE: The algorithm is designed for gaze location on a 2D X/Y plane
def cdt(gaze_data):
    print(gaze_data)


# Engbert and Mergenthaler, 2006 (EM)
def em(gaze_data):
    print(gaze_data)


# Identification by dispersion threshold (IDT)
def idt(gaze_data):
    print(gaze_data)


# Identification by Kalman filter (IKF)
def ikf(gaze_data):
    print(gaze_data)


# Identification by minimal spanning tree (IMST)
def imst(gaze_data):
    print(gaze_data)


# Identification by hidden Markov model (IHMM)
def ihmm(gaze_data):
    print(gaze_data)


# Identification by velocity threshold (IVT)
def ivt(gaze_data):
    fixation_list = [False]
    for _i in range(1, len(gaze_data)):
        gaze_current = gaze_data[_i]
        gaze_previous = gaze_data[_i - 1]
        angle_diff = angle_between_vectors(gaze_current.gazeDirCombined,
                                           gaze_previous.gazeDirCombined)
        time_diff = gaze_current.time - gaze_previous.time
        fixation = True if angle_diff / time_diff < 45 else False
        fixation_list.append(fixation)
    return fixation_list


# Nyström and Holmqvist, 2010 (NH)
def nh(gaze_data):
    print(gaze_data)


# Binocular-individual threshold (BIT)
def bit(gaze_data):
    print(gaze_data)


# Larsson, Nyström and Stridh, 2013 (LNS)
def lns(gaze_data):
    print(gaze_data)


# this can be easily re-written to support
# a number of fixation detection algorithms
def determine_fixations_from_gaze_data(gaze_data):
    # algorithms for fixation
    ivt_fixation_list = ivt(gaze_data)

    fixation_lists = [ivt_fixation_list]  # results from all algorithms
    averaged_fixation_data = []  # final result list to return

    # for each gaze_data time point
    for j in range(len(gaze_data)):
        true_count = 0

        # for each algorithm, at that time point,
        # average their values
        for i in range(len(fixation_lists)):
            fixation_value = 1 if fixation_lists[i][j] else 0
            true_count += fixation_value

        average_fixation_value = float(true_count) / float(len(fixation_lists))

        # what percentage of fixations need to be true in the algorithm in order
        # to register as a true fixation
        threshold = 0.5

        append_value = average_fixation_value > threshold
        averaged_fixation_data.append(append_value)

    return averaged_fixation_data
