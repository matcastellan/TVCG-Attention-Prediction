import datetime
import math


def s2f(i_str: str):
    return float(i_str.strip("(),\n"))


def s2i(i_str: str):
    return int(i_str.strip("(),\n"))


def vec3_to_f3(tokens, s_i):
    return [s2f(tokens[s_i]), s2f(tokens[s_i + 1]), s2f(tokens[s_i + 2])]

def vec4_to_f4(tokens, s_i):
    return [s2f(tokens[s_i]), s2f(tokens[s_i + 1]), s2f(tokens[s_i + 2]), s2f(tokens[s_i + 3])]


def vec3_to_str(tokens):
    return f"({tokens[0]};{tokens[1]};{tokens[2]})"


# grab the date from the first line of the user file as a datetime object
def get_date(filepath):
    file = open(filepath, "r")
    line = file.readline()
    tokens = line.split(" ")
    date_str = " ".join(tokens[8:11]).strip('\n')
    date_obj = datetime.datetime.strptime(date_str, "%m/%d/%Y %I:%M:%S %p")
    return date_obj


# if enum as string was included instead of eccentricity, depth, or size,
#   returns the int associated with that enum
# temporary function - will adjust affected study data to avoid this
def enum_to_int(input_str):
    ret_val = 0 if input_str == "none" \
        else 1 if input_str in (
            "center", "near", "small", "ccw_0", "up", "vertical") \
        else 2 if input_str in ("middle", "big", "ccw_45", "left", "negative") \
        else 3 if input_str in (
            "peripheral", "far", "ccw_90", "down", "horizontal") \
        else 4 if input_str in ("ccw_135", "positive") \
        else 5 if input_str == "ccw_180" \
        else 6 if input_str == "ccw_225" \
        else 7 if input_str == "ccw_270" \
        else 8 if input_str == "ccw_315" \
        else -1
    if ret_val == -1:
        raise ValueError(f"Error: Invalid enum \"{input_str}\"")
    else:
        return ret_val


def dot_product(v1, v2):
    return sum(x * y for x, y in zip(v1, v2))


def magnitude(vector):
    return math.sqrt(sum(x ** 2 for x in vector))


def angle_between_vectors(v1, v2):
    dot = dot_product(v1, v2)
    mag_v1 = magnitude(v1)
    mag_v2 = magnitude(v2)
    cosine_angle = dot / (mag_v1 * mag_v2)
    if cosine_angle > 1:
        cosine_angle = 1
    angle = math.acos(cosine_angle)
    return math.degrees(angle)
