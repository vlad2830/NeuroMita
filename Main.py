### main.py

from gui import ChatGUI


# Установка
#pyinstaller --onefile --name NeuroMita --add-data "Promts/*;Promts" --add-data "Promts/**/*;Promts" Main.py

# Удаление всех пакетов
#pip freeze | % { pip uninstall -y $_.Split('==')[0] }

# При установке не забудь ffmpeg поставить питоновский

def main():
    gui = ChatGUI()
    gui.run()


if __name__ == "__main__":
    main()
