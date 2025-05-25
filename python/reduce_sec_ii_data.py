import math
import os
import pandas as pd
import tkinter as tk
from tkinter.filedialog import askopenfile


fd_df = None

def get_row_col_from_index(ind, num_col):
    grid_size = math.ceil(math.sqrt(num_col))
    row = math.floor(ind/grid_size)
    col = ind - row * grid_size
    return row, col


if __name__ == "__main__":
    # load full_data.csv
    full_data_filepath = askopenfile(mode='r',
                                     title="Select the full_data.csv file",
                                     filetypes=[("CSV Files", ".csv")])

    # calculate rough size of window
    fd_df = pd.read_csv(full_data_filepath, index_col=0) # or None
    columns = fd_df.keys()
    column_checkboxes = []
    intvars = []
    ar_user_intvars = []
    ar_user_checkboxes = []
    vr_user_intvars = []
    vr_user_checkboxes = []
    num_columns = len(columns)
    print(fd_df[fd_df['AR_VR']=='AR'])
    ar_users = fd_df[fd_df['AR_VR']=='AR']['User'].unique().tolist()
    vr_users = fd_df[fd_df['AR_VR'] == 'VR']['User'].unique().tolist()
    num_ar_users = len(ar_users)
    num_vr_users = len(vr_users)


    root = tk.Tk()

    def close_method():
        root.destroy()
        quit()

    root.protocol("WM_DELETE_WINDOW", close_method)

    label = tk.Label(root, text="Select Columns for Inclusion")
    label.pack(side=tk.TOP)

    # Checkbox Container
    checkbox_frame = tk.Frame(root)
    checkbox_frame.pack(side=tk.TOP)

    for i in range(0, num_columns):
        r, c = get_row_col_from_index(i, num_columns)

        # create int variable
        c_intvar = tk.IntVar(root, value=1)
        intvars.append(c_intvar)

        checkbox = tk.Checkbutton(checkbox_frame, text=columns[i], variable=c_intvar)
        checkbox.grid(row=r, column=c)
        column_checkboxes.append(checkbox)

    label2 = tk.Label(root, text="Select AR Users for Inclusion")
    label2.pack(side=tk.TOP)

    # AR User Container
    ar_user_frame = tk.Frame(root)
    ar_user_frame.pack(side=tk.TOP)

    for i in range(0, num_ar_users):
        r, c = get_row_col_from_index(i, num_ar_users)

        # create int variable
        u_intvar = tk.IntVar(root, value=1)
        ar_user_intvars.append(u_intvar)

        user_checkbox = tk.Checkbutton(ar_user_frame, text=ar_users[i], variable=u_intvar)
        user_checkbox.grid(row=r, column=c)
        ar_user_checkboxes.append(user_checkbox)

    label3 = tk.Label(root, text="Select VR Users for Inclusion")
    label3.pack(side=tk.TOP)

    # VR User Container
    vr_user_frame = tk.Frame(root)
    vr_user_frame.pack(side=tk.TOP)

    for i in range(0, num_vr_users):
        r, c = get_row_col_from_index(i, num_vr_users)

        # create int variable
        u_intvar = tk.IntVar(root, value=1)
        vr_user_intvars.append(u_intvar)

        user_checkbox = tk.Checkbutton(vr_user_frame, text=vr_users[i], variable=u_intvar)
        user_checkbox.grid(row=r, column=c)
        vr_user_checkboxes.append(user_checkbox)


    # First Timestamp Only Button
    first_timestamp_intvar = tk.IntVar(root, value=0)
    first_timestamp_only_checkbox = tk.Checkbutton(root, text="First Timestamp of Each Trial Only", variable=first_timestamp_intvar)
    first_timestamp_only_checkbox.pack(side=tk.TOP)



    # Run Button
    run_button = tk.Button(root, text="Run")
    run_button.pack(side=tk.TOP)


    def run_button_function():
        global fd_df
        # Remove de-selected columns
        for column_ind in range(0, num_columns):
            if intvars[column_ind].get() == 0: # remove this column
                fd_df = fd_df.drop(columns[column_ind], axis=1)

        # Remove rows containing de-selected AR users
        selected_ar_users = []
        for i_var in range(0, num_ar_users):
            if ar_user_intvars[i_var].get() == 1:  # keep this user
                selected_ar_users.append(ar_users[i_var])
        print(selected_ar_users)

        # Remove rows containing de-selected vr users
        selected_vr_users = []
        for i_var in range(0, num_vr_users):
            if vr_user_intvars[i_var].get() == 1:  # keep this user
                selected_vr_users.append(vr_users[i_var])
        print(selected_vr_users)
        fd_df = fd_df[((fd_df['AR_VR'] == "AR") & (fd_df['User'].isin(selected_ar_users))) | ((fd_df['AR_VR'] == "VR") & (fd_df['User'].isin(selected_vr_users)))]

        # If selected, only include rows where Time = 0
        if first_timestamp_intvar.get() == 1:
            fd_df = fd_df[fd_df["Time"] == 0]

        # write to file
        dirname = os.path.dirname(full_data_filepath.name)
        filename = os.path.splitext(os.path.basename(full_data_filepath.name))[0]
        save_path = f"{dirname}/{filename}_reduced.csv"
        fd_df.to_csv(save_path)#, index=False)
        print(f"File saved to {save_path}")

    run_button.configure(command=run_button_function)

    root.mainloop()