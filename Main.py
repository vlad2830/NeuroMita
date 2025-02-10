### main.py

from gui import ChatGUI
import os


#pyinstaller --onefile --name NeuroMita --add-data "Promts/*;Promts" --add-data "Promts/**/*;Promts" Main.py

# + кинуть промты рядом!
#pyinstaller --onefile --add-data "Promts/*;Promts" --add-data "Promts/**/*;Promts" Main.py
def main():

    gui = ChatGUI()
    gui.run()



if __name__ == "__main__":
    main()