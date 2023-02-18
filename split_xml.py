import xml.etree.ElementTree as ET
import tkinter as tk
from tkinter import filedialog, messagebox

class Application(tk.Frame):
    def __init__(self, master=None):
        super().__init__(master)
        self.master = master
        self.master.title("Types.xml Splitter by Borcio#2121")
        self.master.minsize(500, 200)
        self.master.resizable(False, False)
        self.configure(background="#202123")
        self.pack()
        self.output_directory = None
        self.input_file = None
        self.create_widgets()

    def create_widgets(self):
        # Create grid layout for the widgets in the window
        for i in range(2):
            self.grid_columnconfigure(i, weight=1)
            self.grid_rowconfigure(i, weight=1)
        self.grid_rowconfigure(2, weight=1)

        # Create a button to select the input file
        self.input_button = tk.Button(self, text="Select input file", bg="#444654", fg="white", command=self.select_input_file)
        self.input_button.grid(row=0, column=0, sticky="nsew")

        # Create a button to select the output directory
        self.output_button = tk.Button(self, text="Select output directory", bg="#444654", fg="white", command=self.select_output_directory)
        self.output_button.grid(row=1, column=0, sticky="nsew")

        # Create a button to run the program
        self.run_button = tk.Button(self, text="Run", bg="#444654", fg="white", state="disabled", disabledforeground="red", command=self.run_program)
        self.run_button.grid(row=2, column=1, sticky="nsew")

        # Create a button to cancel the program
        self.cancel_button = tk.Button(self, text="Cancel", bg="#444654", fg="white", command=self.cancel_program)
        self.cancel_button.grid(row=2, column=0, sticky="nsew")

        # Create a label to display selected input file path
        self.input_label = tk.Label(self, width=100, height=2, borderwidth=1, relief="solid", background="white")
        self.input_label.grid(row=0, column=1, columnspan=2, sticky="nsew")

        # Create a label to display selected output directory path
        self.output_label = tk.Label(self, width=100, height=2, borderwidth=1, relief="solid", background="white")
        self.output_label.grid(row=1, column=1, columnspan=2, sticky="nsew")

        # Create a label to display the program status (running, finished, etc.)
        self.status_label = tk.Label(self, width=100, height=2, borderwidth=1, relief="solid", background="white", text="Status: waiting for inputs till then run button is disabled")
        self.status_label.grid(row=3, column=0, columnspan=3, sticky="nsew")

        # Make the buttons fill the entire space of the cell
        for child in self.winfo_children():
            child.grid(padx=10, pady=10)

    def select_input_file(self):
        # Open a file dialog to select the input file
        self.input_file = filedialog.askopenfilename()
        self.input_label["text"] = f"Input file: {self.input_file}"
        self.check_inputs()

    def select_output_directory(self):
        # Open a file dialog to select the output directory
        self.output_directory = filedialog.askdirectory()
        self.output_label["text"] = f"Output directory: {self.output_directory}"
        self.check_inputs()

    def check_inputs(self):
        if self.input_file and self.output_directory:
            self.status_label["text"] = "Status: ready to run"
            self.run_button["state"] = "normal"
        else:
            self.status_label["text"] = "Status: waiting for inputs till then run button is disabled"
            self.run_button["state"] = "disabled"

    def cancel_program(self):
        # Ask the user if they want to cancel the program
        if messagebox.askokcancel("Cancel", "Are you sure you want to cancel the program?"):
            self.master.destroy()

    def run_program(self):

        tree = ET.parse(self.input_file)
        root = tree.getroot()

        # Create a dictionary to hold the items for each category
        categories = {}

        # Iterate over the 'type' elements and sort them into categories
        for item in root.findall('type'):
            category = item.find('category')
            if category is not None:
                category_name = category.get('name')
            else:
                category_name = 'other'
            if category_name not in categories:
                categories[category_name] = []
            categories[category_name].append(item)

        # Write each category to a separate file
        for category, items in categories.items():
            self.status_label["text"] = f"Status: writing {category} to file"
            # Create a new XML tree with the root element
            category_root = ET.Element('types')
            # Add new line after opening tag types and before closing tag types
            category_root.text = '\n'
            category_root.tail = '\n'

            # Add each item to the new tree
            for item in items:
                category_root.append(item)

            # Write the new tree to a file
            xml_declaration = '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>\n'
            xml_string = ET.tostring(category_root, encoding='utf-8').decode('utf-8')
            with open(f'{self.output_directory}/types_{category}.xml', 'w', encoding='utf-8') as f:
                f.write(xml_declaration + xml_string)

        self.status_label["text"] = "Status: finished"
        self.cancel_button["text"] = "Close"

root = tk.Tk()
app = Application(master=root)
app.mainloop()
