### main.py

from gui import ChatGUI


# Установка
#pyinstaller --onefile --name NeuroMita --add-data "Prompts/*;Prompts" --add-data "Prompts/**/*;Prompts" Main.py
#pyinstaller --onefile --name NeuroMita --add-data "CrazyMitaPrompts/*;CrazyMitaPrompts" --add-data "CrazyMitaPrompts/**/*;CrazyMitaPrompts" Main.py

# Удаление всех пакетов
#pip freeze | % { pip uninstall -y $_.Split('==')[0] }

# При установке не забудь ffmpeg поставить питоновский

def main():
    gui = ChatGUI()
    gui.run()


if __name__ == "__main__":
    main()
