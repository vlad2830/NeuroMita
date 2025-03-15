import base64
import json
import os

import tkinter as tk
from tkinter import ttk

class SettingsManager:
    def __init__(self, config_path):
        self.config_path = config_path
        self.settings = {}
        self.load_settings()

    def load_settings(self):
        try:
            if os.path.exists(self.config_path):
                with open(self.config_path, "rb") as f:
                    encoded = f.read()
                decoded = base64.b64decode(encoded)
                self.settings = json.loads(decoded.decode("utf-8"))
        except Exception as e:
            print(f"Error loading settings: {e}")
            self.settings = {}

    def save_settings(self):
        try:
            json_data = json.dumps(self.settings, ensure_ascii=False)
            encoded = base64.b64encode(json_data.encode("utf-8"))
            with open(self.config_path, "wb") as f:
                f.write(encoded)
        except Exception as e:
            print(f"Error saving settings: {e}")

    def get(self, key, default=None):
        return self.settings.get(key, default)

    def set(self, key, value):
        self.settings[key] = value


class CollapsibleSection(ttk.LabelFrame):
    def __init__(self, parent, title, *args, **kwargs):
        super().__init__(parent, *args, **kwargs)
        self.parent = parent
        self.title = title
        self.is_collapsed = False
        self.content_frame = None
        self.init_ui()

    def init_ui(self):
        # Настройка стилей для ttk виджетов
        style = ttk.Style()
        style.configure("Black.TFrame", background="#2c2c2c")
        style.configure("Black.TLabel", background="#2c2c2c", foreground="#ffffff")
        style.configure("Black.TLabelframe", background="#2c2c2c", foreground="#ffffff")

        # Header with arrow indicator
        self.header = ttk.Frame(self, style="Black.TFrame")
        self.header.pack(fill=tk.X, pady=2)

        # Стрелка для индикации состояния
        self.arrow_label = ttk.Label(
            self.header,
            text="▼",
            font=("Arial", 10),
            style="Black.TLabel"
        )
        self.arrow_label.pack(side=tk.LEFT, padx=5)

        # Заголовок секции
        self.title_label = ttk.Label(
            self.header,
            text=self.title,
            font=("Arial", 10, "bold"),
            style="Black.TLabel"
        )
        self.title_label.pack(side=tk.LEFT)

        # Контентная область
        self.content_frame = ttk.Frame(self, style="Black.TFrame")
        self.content_frame.pack(fill=tk.X, padx=5, pady=5)

        # Привязка событий клика
        self.header.bind("<Button-1>", self.toggle)
        self.arrow_label.bind("<Button-1>", self.toggle)
        self.title_label.bind("<Button-1>", self.toggle)

        # Устанавливаем черный фон для самого LabelFrame
        self.configure(style="Black.TLabelframe")
    def toggle(self, event=None):
        self.is_collapsed = not self.is_collapsed
        if self.is_collapsed:
            self.collapse()
        else:
            self.expand()

    def collapse(self):
        self.arrow_label.config(text="▶")
        self.content_frame.pack_forget()

    def expand(self):
        self.arrow_label.config(text="▼")
        self.content_frame.pack(fill=tk.X, padx=5, pady=5)

    def add_widget(self, widget):
        widget.pack(in_=self.content_frame, fill=tk.X, pady=2)