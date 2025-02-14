from chat_model import ChatModel
from server import ChatServer

from Silero import TelegramBotHandler

import os
import base64
import pickle
from pathlib import Path

import glob

import asyncio
import threading
import tkinter as tk


class ChatGUI:
    def __init__(self):

        self.silero_connected = False
        self.game_connected = False

        self.chat_window = None
        self.token_count_label = None

        self.bot_handler = None
        self.bot_handler_ready = False

        self.api_key = ""
        self.api_url = ""
        self.api_model = ""
        self.makeRequest = False
        self.api_hash = None
        self.api_id = None
        self.phone = None
        try:
            self.config_path = Path.home() / ".myapp_config.bin"  # Путь к файлу в домашней директории
            self.load_api_settings(False)  # Загружаем настройки при инициализации
        except:
            print("Не удалось удачно получить из сис переменных все данные")

        self.model = ChatModel(self, self.api_key, self.api_url, self.api_model, self.makeRequest)
        self.server = ChatServer(self, self.model)
        self.server_thread = None
        self.running = False
        self.start_server()
        self.textToTalk = ""
        self.patch_to_sound_file = ""
        self.ConnectedToGame = False
        self.root = tk.Tk()
        self.root.title("Чат с MitaAI")

        self.last_price = ""

        self.delete_all_wav_files()
        self.setup_ui()

        # Событие для синхронизации потоков
        self.loop_ready_event = threading.Event()

        self.loop = None  # Переменная для хранения ссылки на цикл событий
        self.asyncio_thread = threading.Thread(target=self.start_asyncio_loop, daemon=True)
        self.asyncio_thread.start()

        self.start_silero_async()

        # Запуск проверки переменной textToTalk через after
        self.root.after(100, self.check_text_to_talk)

    def delete_all_wav_files(self):
        # Получаем список всех .wav файлов в корневой директории
        wav_files = glob.glob("*.wav")

        # Проходим по каждому файлу и удаляем его
        for wav_file in wav_files:
            try:
                os.remove(wav_file)
                print(f"Удален файл: {wav_file}")
            except Exception as e:
                print(f"Ошибка при удалении файла {wav_file}: {e}")

    def start_asyncio_loop(self):
        """Запускает цикл событий asyncio в отдельном потоке."""
        try:
            self.loop = asyncio.new_event_loop()
            asyncio.set_event_loop(self.loop)
            print("Цикл событий asyncio успешно запущен.")
            self.loop_ready_event.set()  # Сигнализируем, что цикл событий готов
            self.loop.run_forever()
        except Exception as e:
            print(f"Ошибка при запуске цикла событий asyncio: {e}")

    def start_silero_async(self):
        """Отправляет задачу для запуска Silero в цикл событий."""
        print("Ожидание готовности цикла событий...")
        self.loop_ready_event.wait()  # Ждем, пока цикл событий будет готов
        if self.loop and self.loop.is_running():
            print("Запускаем Silero через цикл событий.")
            asyncio.run_coroutine_threadsafe(self.startSilero(), self.loop)
        else:
            print("Ошибка: Цикл событий asyncio не запущен.")

    async def startSilero(self):
        """Асинхронный запуск обработчика Telegram Bot."""
        print("Telegram Bot запускается!")
        try:
            self.bot_handler = TelegramBotHandler(self, self.api_id, self.api_hash, self.phone)
            await self.bot_handler.start()
            self.bot_handler_ready = True
            print("Telegram Bot запущен!")
        except Exception as e:
            print(f"Ошибка при запуске Telegram Bot: {e}")
            self.gui.silero_connected.set(False)

    def run_in_thread(self, response):
        """Запуск асинхронной задачи в отдельном потоке."""
        # Убедимся, что цикл событий готов и запускаем задачу в том же цикле
        self.loop_ready_event.wait()  # Ждем, пока цикл событий будет готов
        if self.loop and self.loop.is_running():
            print("Запускаем асинхронную задачу в цикле событий...")
            # Здесь мы вызываем асинхронную задачу через главный цикл
            self.loop.create_task(self.run_send_and_receive(self.textToTalk))
        else:
            print("Ошибка: Цикл событий asyncio не готов.")

    async def run_send_and_receive(self, response):
        """Асинхронный метод для вызова send_and_receive."""
        print("Попытка получить фразу")
        await self.bot_handler.send_and_receive(response)
        print("Завершение получения фразы")

    def check_text_to_talk(self):
        """Периодическая проверка переменной self.textToTalk."""

        if self.textToTalk != "":  #and not self.ConnectedToGame:
            print(f"Есть текст для отправки: {self.textToTalk}")
            # Вызываем метод для отправки текста, если переменная не пуста
            if self.loop and self.loop.is_running():
                print("Цикл событий готов. Отправка текста.")
                asyncio.run_coroutine_threadsafe(self.run_send_and_receive(self.textToTalk), self.loop)
                self.textToTalk = ""  # Очищаем текст после отправки
                print("Выполнено")
            else:
                print("Ошибка: Цикл событий не готов.")
        #if self.patch_to_sound_file !="":

        # Перезапуск проверки через 100 миллисекунд
        self.root.after(100, self.check_text_to_talk)  # Это обеспечит постоянную проверку

    def start_server(self):
        """Запускает сервер в отдельном потоке."""
        if not self.running:
            self.running = True
            self.server.start()  # Инициализация сокета
            self.server_thread = threading.Thread(target=self.run_server_loop, daemon=True)
            self.server_thread.start()
            print("Сервер запущен.")

    def stop_server(self):
        """Останавливает сервер."""
        if self.running:
            self.running = False
            self.server.stop()
            print("Сервер остановлен.")

    def run_server_loop(self):
        """Цикл обработки подключений сервера."""
        while self.running:
            needUpdate = self.server.handle_connection()
            if needUpdate:
                self.load_chat_history()

    def setup_ui(self):
        self.root.config(bg="#2c2c2c")  # Установите темный цвет фона для всего окна

        main_frame = tk.Frame(self.root, bg="#2c2c2c")
        main_frame.pack(fill=tk.BOTH, expand=True, padx=10, pady=10)

        # Первый столбец
        left_frame = tk.Frame(main_frame, bg="#2c2c2c")
        left_frame.pack(side=tk.LEFT, fill=tk.BOTH, expand=True, padx=5, pady=5)

        self.chat_window = tk.Text(
            left_frame, height=30, width=80, state=tk.NORMAL,
            bg="#1e1e1e", fg="#ffffff", insertbackground="white", wrap=tk.WORD,
            font=("Arial", 12)
        )

        self.chat_window.pack(fill=tk.BOTH, expand=True, padx=10, pady=10)
        input_frame = tk.Frame(left_frame, bg="#2c2c2c")
        input_frame.pack(fill=tk.X, padx=10, pady=10)

        self.user_entry = tk.Text(input_frame, height=3, width=50, bg="#1e1e1e", fg="#ffffff", insertbackground="white",
                                  font=("Arial", 12))
        self.user_entry.pack(side=tk.LEFT, fill=tk.X, expand=True, padx=5)
        self.user_entry.bind("<KeyRelease>", self.update_token_count)

        self.send_button = tk.Button(
            input_frame, text="Отправить", command=self.send_message,
            bg="#9370db", fg="#ffffff", font=("Arial", 12)
        )
        self.send_button.pack(side=tk.RIGHT, padx=5)

        self.token_count_label = tk.Label(
            left_frame, text=f"Последнее сообщение: {self.last_price}",
            bg="#2c2c2c", fg="#ffffff", font=("Arial", 12)
        )
        self.token_count_label.pack(fill=tk.X, pady=5)

        self.root.protocol("WM_DELETE_WINDOW", self.on_closing)

        # Второй столбец
        right_frame = tk.Frame(main_frame, bg="#2c2c2c")
        right_frame.pack(side=tk.RIGHT, fill=tk.BOTH, expand=True, padx=4, pady=4)

        # Передаем right_frame как родителя
        self.setup_status_indicators(right_frame)

        # Настройка элементов управления
        # Создаем контейнер для всех элементов управления
        self.controls_frame = tk.Frame(right_frame, bg="#2c2c2c")
        self.controls_frame.pack(fill=tk.X, pady=3)

        # Настройка элементов управления
        self.setup_control("Настроение", "attitude", self.model.attitude)
        self.setup_control("Скука", "boredom", self.model.boredom)
        self.setup_control("Стресс", "stress", self.model.stress)
        self.setup_secret_control()

        self.setup_history_controls(right_frame)
        self.setup_debug_controls(right_frame)
        self.setup_api_controls(right_frame)

        self.load_chat_history()

    def insert_message(self, role, content):
        if role == "user":
            # Вставляем имя пользователя с зеленым цветом, а текст — обычным
            self.chat_window.insert(tk.END, "Вы: ", "user_name")
            self.chat_window.insert(tk.END, f"{content}\n")
        elif role == "assistant":
            # Вставляем имя Миты с синим цветом, а текст — обычным
            self.chat_window.insert(tk.END, "Мита: ", "gpt_name")
            self.chat_window.insert(tk.END, f"{content}\n\n")

    def setup_status_indicators(self, parent):
        # Создаем фрейм для индикаторов
        status_frame = tk.Frame(parent, bg="#2c2c2c")
        status_frame.pack(fill=tk.X, pady=3)

        # Переменные статуса
        self.game_connected = tk.BooleanVar(value=False)  # Статус подключения к игре
        self.silero_connected = tk.BooleanVar(value=False)  # Статус подключения к Silero

        # Галки для подключения
        self.game_status_checkbox = tk.Checkbutton(
            status_frame,
            text="Подключение к игре",
            variable=self.game_connected,
            state="disabled",
            bg="#2c2c2c",
            fg="#ffffff",
            selectcolor="#2c2c2c"
        )
        self.game_status_checkbox.pack(side=tk.LEFT, padx=5, pady=4)

        self.silero_status_checkbox = tk.Checkbutton(
            status_frame,
            text="Подключение к Silero",
            variable=self.silero_connected,
            state="disabled",
            bg="#2c2c2c",
            fg="#ffffff",
            selectcolor="#2c2c2c"
        )
        self.silero_status_checkbox.pack(side=tk.LEFT, padx=5, pady=4)

    def updateAll(self):
        self.update_status_colors()
        self.update_debug_info()

    def update_status_colors(self):
        self.game_connected.set(self.ConnectedToGame)  # Статус подключения к игре
        # Обновление цвета для подключения к игре
        game_color = "#00ff00" if self.game_connected.get() else "#ffffff"
        self.game_status_checkbox.config(fg=game_color)

        # Обновление цвета для подключения к Silero
        silero_color = "#00ff00" if self.silero_connected.get() else "#ffffff"
        self.silero_status_checkbox.config(fg=silero_color)

    def setup_control(self, label_text, attribute_name, initial_value):
        """
        Создает элементы управления для настроения, скуки и стресса.
        :param label_text: Текст метки (например, "Настроение").
        :param attribute_name: Имя атрибута модели (например, "attitude").
        :param initial_value: Начальное значение атрибута.
        """
        frame = tk.Frame(self.controls_frame, bg="#2c2c2c")
        frame.pack(fill=tk.X, pady=5)

        # Метка для отображения текущего значения
        label = tk.Label(frame, text=f"{label_text}: {initial_value}", bg="#2c2c2c", fg="#ffffff")
        label.pack(side=tk.LEFT, padx=5)

        # Кнопки для увеличения и уменьшения значения
        up_button = tk.Button(
            frame, text="+", command=lambda: self.adjust_value(attribute_name, 15, label),
            bg="#8a2be2", fg="#ffffff"
        )
        up_button.pack(side=tk.RIGHT, padx=5)

        down_button = tk.Button(
            frame, text="-", command=lambda: self.adjust_value(attribute_name, -15, label),
            bg="#8a2be2", fg="#ffffff"
        )
        down_button.pack(side=tk.RIGHT, padx=5)

        # Сохраняем ссылку на метку для обновления
        setattr(self, f"{attribute_name}_label", label)

    def setup_secret_control(self):
        """
        Создает чекбокс для управления состоянием "Секрет раскрыт".
        """
        frame = tk.Frame(self.controls_frame, bg="#2c2c2c")
        frame.pack(fill=tk.X, pady=5)

        self.secret_var = tk.BooleanVar(value=self.model.secretExposed)

        secret_checkbox = tk.Checkbutton(
            frame, text="Секрет раскрыт", variable=self.secret_var,
            bg="#2c2c2c", fg="#ffffff", command=self.adjust_secret
        )
        secret_checkbox.pack(side=tk.LEFT, padx=5)

    def adjust_value(self, attribute_name, delta, label):
        """
        Обновляет значение атрибута модели и метки.
        :param attribute_name: Имя атрибута модели (например, "attitude").
        :param delta: Изменение значения (например, +15 или -15).
        :param label: Метка, которую нужно обновить.
        """
        current_value = getattr(self.model, attribute_name)
        new_value = current_value + delta
        setattr(self.model, attribute_name, new_value)

        # Обновляем текст метки
        label.config(text=f"{label.cget('text').split(':')[0]}: {new_value}")

    def adjust_secret(self):
        """
        Обновляет состояние "Секрет раскрыт" в модели.
        """
        self.model.secretExposed = self.secret_var.get()

    def setup_history_controls(self, parent):
        history_frame = tk.Frame(parent, bg="#2c2c2c")
        history_frame.pack(fill=tk.X, pady=4)

        clear_button = tk.Button(
            history_frame, text="Очистить историю", command=self.clear_history,
            bg="#8a2be2", fg="#ffffff"
        )
        clear_button.pack(side=tk.LEFT, padx=5)

        save_history_button = tk.Button(
            history_frame, text="Сохранить историю", command=self.model.save_chat_history,
            bg="#8a2be2", fg="#ffffff"
        )
        save_history_button.pack(side=tk.LEFT, padx=10)

        reload_prompts_button = tk.Button(
            history_frame, text="Перезагрузить промпты", command=self.model.reload_promts,
            bg="#8a2be2", fg="#ffffff"
        )
        reload_prompts_button.pack(side=tk.LEFT, padx=15)

    def load_chat_history(self):
        self.model.load_history()
        """Загрузить историю из модели и отобразить в интерфейсе."""
        for entry in self.model.chat_history:
            role = entry["role"]
            content = entry["content"]
            self.insert_message(role, content)
        self.update_debug_info()
        self.token_count_label.text = self.last_price

    def setup_debug_controls(self, parent):
        debug_frame = tk.Frame(parent, bg="#2c2c2c")
        debug_frame.pack(fill=tk.BOTH, expand=True, padx=10, pady=10)

        self.debug_window = tk.Text(
            debug_frame, height=5, width=50, bg="#1e1e1e", fg="#ffffff",
            state=tk.NORMAL, wrap=tk.WORD, insertbackground="white"
        )
        self.debug_window.pack(fill=tk.BOTH, expand=True)

        self.update_debug_info()  # Отобразить изначальное состояние переменных

    def setup_api_controls(self, parent):
        api_frame = tk.Frame(parent, bg="#2c2c2c")
        api_frame.pack(fill=tk.X, pady=3)

        self.show_api_var = tk.BooleanVar(value=False)

        api_toggle = tk.Checkbutton(
            api_frame, text="Показать настройки API", variable=self.show_api_var,
            command=self.toggle_api_settings, bg="#2c2c2c", fg="#ffffff"
        )
        api_toggle.pack(side=tk.LEFT, padx=4)

        self.api_settings_frame = tk.Frame(parent, bg="#2c2c2c")

        # Элементы в одном столбце
        tk.Label(
            self.api_settings_frame, text="API-ключ:", bg="#2c2c2c", fg="#ffffff"
        ).grid(row=0, column=0, padx=4, pady=4, sticky=tk.W)

        self.api_key_entry = tk.Entry(self.api_settings_frame, width=50, bg="#1e1e1e", fg="#ffffff",
                                      insertbackground="white")
        self.api_key_entry.grid(row=0, column=1, padx=4, pady=4, sticky=tk.W)

        tk.Label(
            self.api_settings_frame, text="Ссылка:", bg="#2c2c2c", fg="#ffffff"
        ).grid(row=1, column=0, padx=5, pady=5, sticky=tk.W)

        self.api_url_entry = tk.Entry(self.api_settings_frame, width=50, bg="#1e1e1e", fg="#ffffff",
                                      insertbackground="white")
        self.api_url_entry.grid(row=1, column=1, padx=4, pady=5, sticky=tk.W)

        tk.Label(
            self.api_settings_frame, text="Модель:", bg="#2c2c2c", fg="#ffffff"
        ).grid(row=2, column=0, padx=5, pady=5, sticky=tk.W)

        self.api_model_entry = tk.Entry(self.api_settings_frame, width=50, bg="#1e1e1e", fg="#ffffff",
                                        insertbackground="white")
        self.api_model_entry.grid(row=2, column=1, padx=4, pady=4, sticky=tk.W)

        tk.Label(
            self.api_settings_frame, text="Telegram API ID:", bg="#2c2c2c", fg="#ffffff"
        ).grid(row=3, column=0, padx=5, pady=5, sticky=tk.W)

        self.api_id_entry = tk.Entry(self.api_settings_frame, width=50, bg="#1e1e1e", fg="#ffffff",
                                     insertbackground="white")
        self.api_id_entry.grid(row=3, column=1, padx=4, pady=4, sticky=tk.W)

        tk.Label(
            self.api_settings_frame, text="Telegram API Hash:", bg="#2c2c2c", fg="#ffffff"
        ).grid(row=4, column=0, padx=5, pady=5, sticky=tk.W)

        self.api_hash_entry = tk.Entry(self.api_settings_frame, width=50, bg="#1e1e1e", fg="#ffffff",
                                       insertbackground="white")
        self.api_hash_entry.grid(row=4, column=1, padx=4, pady=4, sticky=tk.W)

        tk.Label(
            self.api_settings_frame, text="Telegram Phone:", bg="#2c2c2c", fg="#ffffff"
        ).grid(row=5, column=0, padx=5, pady=5, sticky=tk.W)

        self.phone_entry = tk.Entry(self.api_settings_frame, width=50, bg="#1e1e1e", fg="#ffffff",
                                    insertbackground="white")
        self.phone_entry.grid(row=5, column=1, padx=4, pady=4, sticky=tk.W)

        # Переменная для тумблера
        self.makeRequest_entry = tk.Checkbutton(
            self.api_settings_frame, text="openapi or req", variable=self.show_api_var,
            command=self.toggle_makeRequest, bg="#2c2c2c", fg="#ffffff"
        )
        self.makeRequest_entry.grid(row=6, column=0, padx=15, pady=0, sticky=tk.W)
        self.toggle_makeRequest(False)

        save_button = tk.Button(
            self.api_settings_frame, text="Сохранить", command=self.save_api_settings,
            bg="#8a2be2", fg="#ffffff"
        )
        save_button.grid(row=6, column=1, padx=5, sticky=tk.E)

    def toggle_makeRequest(self, change_to_opposite=True):
        if change_to_opposite:
            self.makeRequest = not self.makeRequest

        if self.makeRequest:
            self.makeRequest_entry.config(text="Сейчас Стр.Гемини рек.")
        else:
            self.makeRequest_entry.config(text="Сейчас Стр. OpenAi.")

    def save_api_settings(self):
        """Собирает данные из полей ввода и сохраняет только непустые значения"""
        settings = {}

        # Для строковых полей сохраняем только если не пустые
        if api_key := self.api_key_entry.get().strip():
            settings["NM_API_KEY"] = api_key
        if api_url := self.api_url_entry.get().strip():
            settings["NM_API_URL"] = api_url
        if api_model := self.api_model_entry.get().strip():
            settings["NM_API_MODEL"] = api_model
        if api_id := self.api_id_entry.get().strip():
            settings["NM_TELEGRAM_API_ID"] = api_id
        if api_hash := self.api_hash_entry.get().strip():
            settings["NM_TELEGRAM_API_HASH"] = api_hash
        if phone := self.phone_entry.get().strip():
            settings["NM_TELEGRAM_PHONE"] = phone

        # Булево значение сохраняем всегда
        settings["NM_API_REQ"] = self.makeRequest

        # Удаляем пустые значения
        settings = {k: v for k, v in settings.items() if v}

        # Сериализация и кодирование
        try:
            encoded = base64.b64encode(pickle.dumps(settings))
            with open(self.config_path, "wb") as f:
                f.write(encoded)
            print("Настройки успешно сохранены в файл")
        except Exception as e:
            print(f"Ошибка сохранения: {e}")

        # Сразу же их загружаем
        self.load_api_settings(update_model=True)


        if not self.silero_connected:
            print("попытка запустить силеро заново")
            self.start_silero_async()

    def load_api_settings(self,update_model):
        """Загружает настройки из файла"""
        if not self.config_path.exists():
            return

        try:
            with open(self.config_path, "rb") as f:
                encoded = f.read()
            decoded = base64.b64decode(encoded)
            settings = pickle.loads(decoded)

            # Устанавливаем значения
            self.api_key = settings.get("NM_API_KEY", "")
            self.api_url = settings.get("NM_API_URL", "")
            self.api_model = settings.get("NM_API_MODEL", "")
            self.makeRequest = settings.get("NM_API_REQ", False)
            self.api_id = settings.get("NM_TELEGRAM_API_ID", "")
            self.api_hash = settings.get("NM_TELEGRAM_API_HASH", "")
            self.phone = settings.get("NM_TELEGRAM_PHONE", "")

            if update_model:
                self.model.api_key = self.api_key
                self.model.api_url = self.api_url
                self.model.api_model = self.api_model
                self.model.makeRequest = self.makeRequest
                self.model.update_openai_client()

            # Обновляем поля ввода (если нужно)
            #self.api_key_entry.insert(0, self.api_key)
            #self.api_url_entry.insert(0, self.api_url)
            # ... аналогично для остальных полей

            print("Настройки загружены из файла")
        except Exception as e:
            print(f"Ошибка загрузки: {e}")

    def paste_from_clipboard(self, event=None):
        try:
            clipboard_content = self.root.clipboard_get()
            self.user_entry.insert(tk.INSERT, clipboard_content)
        except tk.TclError:
            pass  # Если буфер обмена пуст, ничего не делаем

    def copy_to_clipboard(self, event=None):
        try:
            # Получение выделенного текста из поля ввода
            selected_text = self.user_entry.selection_get()
            # Копирование текста в буфер обмена
            self.root.clipboard_clear()
            self.root.clipboard_append(selected_text)
            self.root.update()  # Обновление буфера обмена
        except tk.TclError:
            # Если текст не выделен, ничего не делать
            pass

    def toggle_api_settings(self):
        if self.show_api_var.get():
            self.api_settings_frame.pack(fill=tk.X, padx=10, pady=10)
        else:
            self.api_settings_frame.pack_forget()

    def update_controls(self):
        """
        Обновляет значения всех элементов управления на основе текущих данных в self.model.
        """
        # Обновляем метки для настроения, скуки и стресса
        self.attitude_label.config(text=f"Настроение: {self.model.attitude}")
        self.boredom_label.config(text=f"Скука: {self.model.boredom}")
        self.stress_label.config(text=f"Стресс: {self.model.stress}")

        # Обновляем чекбокс "Секрет раскрыт"
        self.secret_var.set(self.model.secretExposed)

    def update_debug_info(self):
        """Обновить окно отладки с отображением актуальных данных."""
        self.debug_window.delete(1.0, tk.END)  # Очистить старые данные
        debug_info = (
            f"Отношение к игроку: {self.model.attitude}\n"
            f"Скука: {self.model.boredom}\n"
            f"Стресс: {self.model.stress}\n"
            f"Секрет: {self.model.secretExposed}\n"
        )
        self.debug_window.insert(tk.END, debug_info)
        self.update_controls()

    def update_token_count(self, event=None):
        if False and self.model.hasTokenizer:
            user_input = self.user_entry.get("1.0", "end-1c")
            token_count, cost = self.model.calculate_cost(user_input)
            self.token_count_label.config(
                text=f"ВЫКЛЮЧЕНО!! Токенов: {token_count}/{self.model.max_input_tokens} | Ориент. стоимость: {cost:.4f} ₽"
            )
            self.update_debug_info()

    def insertDialog(self, input_text="", response=""):
        if input_text != "":
            self.chat_window.insert(tk.END, "Вы: ", "user_name")
            self.chat_window.insert(tk.END, f"{input_text}\n")
        if response != "":
            self.chat_window.insert(tk.END, "Мита: ", "gpt_name")
            self.chat_window.insert(tk.END, f"{response}\n\n")

    def send_message(self, system_input=""):
        user_input = self.user_entry.get("1.0", "end-1c")
        if not user_input.strip() and system_input == "":
            return

        if user_input != "":
            self.insert_message("user", user_input)
            self.user_entry.delete("1.0", "end")

        response = self.model.generate_response(user_input, system_input)
        counter = 0
        #while self.model.repeatResponse and counter<3:
        #   response += self.model.generate_response("", "")
        #  counter+=1
        self.insert_message("assistant", response)
        self.user_entry.delete("1.0", "end")
        self.updateAll()
        # Отправка сообщения на сервер
        if self.server:
            try:
                # Отправляем сообщение клиенту через сервер
                if self.server.client_socket:
                    self.server.send_message_to_server(response)
                    print("Сообщение отправлено на сервер.")
                else:
                    print("Нет активного подключения к клиенту.")
            except Exception as e:
                print(f"Ошибка при отправке сообщения на сервер: {e}")

    def clear_history(self):
        self.model.clear_history()
        self.chat_window.delete(1.0, tk.END)
        self.update_debug_info()

    def run(self):
        self.root.mainloop()

    def on_closing(self):
        self.stop_server()
        #self.send_message("Игрок покинул игру")
        print("Закрываемся")
        self.root.destroy()
        #1

    def close_app(self):
        """Закрытие приложения корректным образом."""
        print("Завершение программы...")
        self.root.destroy()  # Закрывает GUI


import winreg


def set_system_variable(name, value):
    try:
        reg_key = winreg.OpenKey(
            winreg.HKEY_CURRENT_USER,
            "Environment",
            0,
            winreg.KEY_SET_VALUE
        )
        winreg.SetValueEx(reg_key, name, 0, winreg.REG_SZ, value)
        winreg.CloseKey(reg_key)
        print(f"Переменная {name} успешно установлена в системных настройках.")
    except Exception as e:
        print(f"Ошибка при установке переменной {name}: {e}")
