import os
import sys
import json
import datetime
import requests

def clamp(value, min_value, max_value):
    return max(min_value, min(value, max_value))

def print_ip_and_country():
    try:
        response = requests.get("https://ipinfo.io/json")
        data = response.json()
        ip = data.get("ip", "Не удалось определить IP")
        country = data.get("country", "Не удалось определить страну")
        print(f"Ваш IP: {ip}")
        print(f"Ваша страна: {country}")
    except Exception as e:
        print(f"Ошибка при получении данных: {e}")

def get_resource_path(filename):
    if getattr(sys, 'frozen', False):
        base_path = os.path.dirname(sys.executable)
    else:
        base_path = os.path.dirname(__file__)
    promts_path = os.path.join(base_path, 'Promts')
    if os.path.isdir(promts_path):
        return os.path.join(promts_path, filename)
    print(f"Ошибка: Папка 'Promts' не найдена рядом с исполнимым файлом.")
    return None

def load_text_from_file(filename):
    try:
        with open(get_resource_path(filename), 'r', encoding='utf-8') as file:
            return file.read()
    except Exception as e:
        print(f"Ошибка при чтении файла {filename}: {e}")
        return ""

def save_combined_messages(combined_messages, output_folder="SavedMessages"):
    os.makedirs(output_folder, exist_ok=True)
    file_name = f"combined_messages.json"
    file_path = os.path.join(output_folder, file_name)
    with open(file_path, 'w', encoding='utf-8') as file:
        json.dump(combined_messages, file, ensure_ascii=False, indent=4)
    print(f"Сообщения сохранены в файл: {file_path}")

def calculate_cost_for_combined_messages(self, combined_messages,cost_input_per_1000):
    token_count = self.count_tokens(combined_messages)
    cost = (token_count / 1000) * cost_input_per_1000
    return f"Токенов {token_count} Цена {cost}"

def count_tokens(self, messages):
    return sum(len(self.tokenizer.encode(msg["content"])) for msg in messages if isinstance(msg, dict) and "content" in msg)