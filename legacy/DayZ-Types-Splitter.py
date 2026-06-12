import re
import sys
import xml.etree.ElementTree as ET

try:
    # Hardened against XXE / entity-expansion attacks; falls back to stdlib
    # parsing (mitigated since Python 3.7.1) when defusedxml is not installed.
    from defusedxml.ElementTree import parse as safe_parse
except ImportError:
    safe_parse = ET.parse


def split_types(input_file, output_directory, status_callback=None):
    """Split a DayZ types.xml into one file per category.

    Returns a dict {category_name: item_count}.
    Raises ET.ParseError / OSError on bad input - callers handle reporting.
    """
    tree = safe_parse(input_file)
    root = tree.getroot()

    # Sort 'type' elements into categories; types without a category go to 'other'
    categories = {}
    for item in root.findall('type'):
        category = item.find('category')
        category_name = category.get('name') if category is not None else None
        if not category_name:
            category_name = 'other'
        categories.setdefault(category_name, []).append(item)

    for category, items in categories.items():
        if status_callback:
            status_callback(f"Status: writing {category} to file ({len(items)} items)")

        category_root = ET.Element('types')
        category_root.text = '\n'
        category_root.tail = '\n'
        for item in items:
            category_root.append(item)

        # Keep filenames safe even if a category name contains odd characters
        safe_name = re.sub(r'[^\w.-]', '_', category)
        xml_declaration = '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>\n'
        xml_string = ET.tostring(category_root, encoding='utf-8').decode('utf-8')
        with open(f'{output_directory}/types_{safe_name}.xml', 'w', encoding='utf-8') as f:
            f.write(xml_declaration + xml_string)

    return {category: len(items) for category, items in categories.items()}


def run_gui():
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
            self.status_label = tk.Label(self, width=100, height=2, borderwidth=1, relief="solid", background="white", text="Status: Waiting for inputs till then run button is disabled")
            self.status_label.grid(row=3, column=0, columnspan=3, sticky="nsew")

            # Make the buttons fill the entire space of the cell
            for child in self.winfo_children():
                child.grid(padx=10, pady=10)

        def select_input_file(self):
            # Open a file dialog to select the input file, only display xml
            self.input_file = filedialog.askopenfilename(filetypes=[("XML files", "*.xml")])
            self.input_label["text"] = f"Input file: {self.input_file}"
            self.check_inputs()

        def select_output_directory(self):
            # Open a file dialog to select the output directory
            self.output_directory = filedialog.askdirectory()
            self.output_label["text"] = f"Output directory: {self.output_directory}"
            self.check_inputs()

        def check_inputs(self):
            if self.input_file and self.output_directory:
                self.status_label["text"] = "Status: Ready to run"
                self.run_button["state"] = "normal"
            else:
                self.status_label["text"] = "Status: Waiting for inputs till then run button is disabled"
                self.run_button["state"] = "disabled"

        def cancel_program(self):
            # Ask the user if they want to cancel the program
            if messagebox.askokcancel("Cancel", "Are you sure you want to cancel the program?"):
                self.master.destroy()

        def set_status(self, text):
            self.status_label["text"] = text
            self.update_idletasks()

        def run_program(self):
            try:
                counts = split_types(self.input_file, self.output_directory, self.set_status)
            except ET.ParseError as e:
                messagebox.showerror("Parse error", f"Input file is not valid XML:\n{e}")
                self.set_status("Status: failed - invalid XML")
                return
            except OSError as e:
                messagebox.showerror("File error", str(e))
                self.set_status("Status: failed - file error")
                return

            total = sum(counts.values())
            self.set_status(f"Status: finished - {total} types into {len(counts)} files")
            self.cancel_button["text"] = "Close"

    root = tk.Tk()
    try:
        root.iconbitmap('xml.ico')
    except tk.TclError:
        pass
    app = Application(master=root)
    app.mainloop()


def run_cli(input_file, output_directory):
    try:
        counts = split_types(input_file, output_directory, print)
    except ET.ParseError as e:
        print(f"ERROR: input file is not valid XML: {e}", file=sys.stderr)
        return 1
    except OSError as e:
        print(f"ERROR: {e}", file=sys.stderr)
        return 1

    total = sum(counts.values())
    print(f"Done: {total} types split into {len(counts)} files in {output_directory}")
    return 0


if __name__ == '__main__':
    if len(sys.argv) == 3:
        sys.exit(run_cli(sys.argv[1], sys.argv[2]))
    elif len(sys.argv) == 1:
        run_gui()
    else:
        print("Usage:\n  python DayZ-Types-Splitter.py                     # GUI\n  python DayZ-Types-Splitter.py <types.xml> <outdir>  # CLI", file=sys.stderr)
        sys.exit(2)
