import json
import os
import datetime
import shutil


class HistoryManager:
    """ В работе, пока неактивно"""

    def __init__(self, character_name="Common", history_file_name=""):

        self.character_name = character_name

        self.history_dir = f"Histories\\{character_name}"
        self.history_file_path = os.path.join(self.history_dir, f"{character_name}_history.json")

        os.makedirs(self.history_dir, exist_ok=True)

        if self.history_file_path != "":
            self.load_history()

    def load_history(self):
        """Загружаем историю из файла, создаем пустую структуру, если файл пуст или не существует."""
        try:
            with open(self.history_file_path, 'r', encoding='utf-8') as f:
                data = json.load(f)
                print("Загрузка истории")

                if self.history_format_correct(data):

                    print("Историю получится заполнить")
                    return data

                else:
                    print("Ошибка загрузки истории, копия сохранена в резерв, текущая сброшена")
                    self.save_history_separate()
                    return self._default_history()

        except (json.JSONDecodeError, FileNotFoundError):
            # Если файл пуст или не существует, возвращаем структуру по умолчанию
            print("Ошибка загрузки истории")
            return self._default_history()

    def history_format_correct(self, data):
        # Проверяем, что все ключи присутствуют и имеют правильный тип
        checks = [
            (isinstance(data.get('fixed_parts'), list), "fixed_parts должен быть списком"),
            (isinstance(data.get('messages'), list), "messages должен быть списком"),
            #(isinstance(data.get('temp_context'), list), "temp_context должен быть списком"),
            (isinstance(data.get('variables'), dict), "variables должен быть словарем")
        ]

        # Проверяем все условия
        if all(check[0] for check in checks):
            return True
        else:
            # Выводим сообщения об ошибках для тех условий, которые не выполнены
            for condition, error_message in checks:
                if not condition:
                    print(f"Ошибка: {error_message}")
            return False

    def save_history(self, data):
        """Сохраняем историю в файл с явной кодировкой utf-8."""
        # Убедимся, что структура данных включает 'messages', 'currentInfo' и 'MitaSystemMessages'
        history_data = {
            'fixed_parts': data.get('fixed_parts', []),
            'messages': data.get('messages', []),
            'temp_context': data.get('temp_context', []),
            'variables': data.get('variables', {})
        }
        # Проверяем, существует ли папка SavedHistories, и создаём её, если нет
        os.makedirs(self.history_dir, exist_ok=True)
        with open(self.history_file_path, 'w', encoding='utf-8') as f:
            json.dump(history_data, f, ensure_ascii=False, indent=4)

    def save_history_separate(self):
        """Нужно, чтобы история сохранилась отдельно"""
        print("save_chat_history")
        # Папка для сохранения историй
        target_folder = f"Histories\\{self.character_name}\\Saved"
        # Проверяем, существует ли папка SavedHistories, и создаём её, если нет
        os.makedirs(target_folder, exist_ok=True)

        # Формируем имя файла с таймингом
        timestamp = datetime.datetime.now().strftime("%d.%m.%Y_%H.%M")
        target_file = f"chat_history_{timestamp}.json"

        # Полный путь к новому файлу
        target_path = os.path.join(target_folder, target_file)

        # Копируем файл
        shutil.copy(self.history_file_path, target_path)
        print(f"Файл сохранён как {target_path}")

    def clear_history(self):
        print("Сброс файла истории")

        self.save_history(self._default_history())

    def _default_history(self):
        print("Созданная пустая история")
        """Создаем структуру истории по умолчанию."""
        return {
            'fixed_parts': [],
            'messages': [],
            'temp_context': [],
            'variables': {}
        }
