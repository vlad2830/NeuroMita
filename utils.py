import os
import sys
import json
import requests


def clamp(value, min_value, max_value):
    return max(min_value, min(value, max_value))


def load_text_from_file(filename):
    """
    Загружает текст из файла, расположенного в папке 'CrazyMitaPrompts'.

    :param filename: Имя файла или относительный путь к файлу.
    :return: Содержимое файла в виде строки. Если файл не найден, возвращает пустую строку.
    """

    print(f"Загружаю {filename}")
    try:
        # Получаем полный путь к файлу
        filepath = get_resource_path(filename)

        # Если путь не найден, возвращаем пустую строку
        if filepath is None:
            return ""

        # Убедимся, что путь в правильном формате
        filepath = os.path.normpath(filepath)

        # Проверяем, существует ли файл
        if not os.path.exists(filepath):
            print(f"Файл не найден: {filepath}")
            return ""

        # Читаем файл
        with open(filepath, 'r', encoding='utf-8') as file:
            return file.read()
    except Exception as e:
        print(f"Ошибка при чтении файла {filename}: {e}")
        return ""


def get_resource_path(filename):
    """
    Возвращает полный путь к файлу в папке 'CrazyMitaPrompts'.

    :param filename: Имя файла или относительный путь к файлу.
    :return: Полный путь к файлу или None, если папка 'CrazyMitaPrompts' не найдена.
    """
    # Определяем базовый путь
    if getattr(sys, 'frozen', False):
        # Если программа "заморожена" (например, с помощью PyInstaller)
        base_path = os.path.dirname(sys.executable)
    else:
        # Если программа запущена как скрипт
        base_path = os.path.dirname(__file__)

    # Формируем путь к папке
    promts_path = os.path.join(base_path)

    # Проверяем, существует ли папка 'CrazyMitaPrompts'
    if not os.path.isdir(promts_path):
        print(f"Ошибка: Папка не найдена по пути: {promts_path}")
        return None

    # Возвращаем полный путь к файлу
    return os.path.join(promts_path, filename)


def load_json_file(filepath):
    try:
        with open(filepath, "r", encoding="utf-8") as file:
            return json.load(file)
    except FileNotFoundError:
        print(f"Файл {filepath} не найден.")
        return {}


def save_combined_messages(combined_messages, output_folder="SavedMessages"):
    os.makedirs(output_folder, exist_ok=True)
    file_name = f"combined_messages.json"
    file_path = os.path.join(output_folder, file_name)
    with open(file_path, 'w', encoding='utf-8') as file:
        json.dump(combined_messages, file, ensure_ascii=False, indent=4)
    print(f"Сообщения сохранены в файл: {file_path}")


def calculate_cost_for_combined_messages(self, combined_messages, cost_input_per_1000):
    token_count = self.count_tokens(combined_messages)
    cost = (token_count / 1000) * cost_input_per_1000
    return f"Токенов {token_count} Цена {cost}"


def count_tokens(self, messages):
    return sum(
        len(self.tokenizer.encode(msg["content"])) for msg in messages if isinstance(msg, dict) and "content" in msg)


def SH(s, placeholder="***", percent=0.20):
    """
    Сокращает строку, оставляя % символов в начале и % в конце.
    Средняя часть заменяется на placeholder.

    :param percent:
    :param s: Исходная строка.
    :param placeholder: Заполнитель для скрытой части строки (по умолчанию "***").
    :return: Сокращенная строка.
    """
    if not s:
        return s

    length = len(s)
    # Вычисляем 20% от длины строки
    visible_length = max(1, int(length * percent))  # Минимум 1 символ

    # Берем начало и конец строки
    start = s[:visible_length]
    end = s[-visible_length:]

    # Собираем результат
    return f"{start}{placeholder}{end}"
